using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Bgfx_cs;

namespace Glib;

public enum DataType
{
    Int,
    Float,
    Vector2,
    Vector3,
    Vector4,
    Color
}

public enum MeshPrimitiveType
{
    Points,
    LineStrip,
    Lines,
    TriangleStrip,
    TriangleFan,
    Triangles
}

public enum MeshBufferUsage
{
    /// <summary>
    /// Buffer will not be updated after initialization.
    /// </summary>
    Static,

    /// <summary>
    /// Buffer may be updated after initialization. 
    /// </summary>
    Dynamic,

    /// <summary>
    /// Buffer will be updated on every frame.
    /// </summary>
    Transient
}

public enum MeshBufferTarget
{
    Position = 0,
    Normal = 1,
    Tangent = 2,
    Bitangent = 3,
    Color0 = 4,
    Color1 = 5,
    Color2 = 6,
    Color3 = 7,
    Indices = 8,
    Weight = 9,
    TexCoord0 = 10,
    TexCoord1 = 11,
    TexCoord2 = 12,
    TexCoord3 = 13,
    TexCoord4 = 14,
    TexCoord5 = 15,
    TexCoord6 = 16,
    TexCoord7 = 17,
}

public record struct MeshBufferConfiguration(MeshBufferTarget Target, DataType Type = DataType.Float, MeshBufferUsage Usage = MeshBufferUsage.Static);

public struct MeshConfiguration
{
    public MeshPrimitiveType PrimitiveType = MeshPrimitiveType.Triangles;
    public readonly List<MeshBufferConfiguration> Buffers;

    public bool Indexed = false;
    public bool Use32BitIndices = false;
    public MeshBufferUsage IndexBufferUsage = MeshBufferUsage.Static;

    public MeshConfiguration()
    {
        Buffers = [];
    }

    public readonly MeshConfiguration Clone()
    {
        return new MeshConfiguration(Buffers, Indexed)
            .SetIndexed(Indexed, Use32BitIndices)
            .SetPrimitiveType(PrimitiveType);
    }

    public MeshConfiguration(ReadOnlySpan<MeshBufferConfiguration> buffers, bool indexed)
    {
        Buffers = [..buffers];
        Indexed = indexed;
    }

    public MeshConfiguration(List<MeshBufferConfiguration> buffers, bool indexed)
    {
        Buffers = [..buffers];
        Indexed = indexed;
    }

    public MeshConfiguration SetIndexed(bool indexed, bool int32)
    {
        Indexed = indexed;
        Use32BitIndices = int32;
        return this;
    }

    public MeshConfiguration SetPrimitiveType(MeshPrimitiveType type)
    {
        PrimitiveType = type;
        return this;
    }

    public readonly MeshConfiguration AddBuffer(MeshBufferTarget target, DataType type, MeshBufferUsage usage = MeshBufferUsage.Static)
    {
        Buffers.Add(new MeshBufferConfiguration(target, type, usage));
        return this;
    }

    public readonly Mesh Create(RenderContext rctx, int vtxCount) => Mesh.Create(this, vtxCount);
    public readonly Mesh Create(RenderContext rctx, Span<short> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
    public readonly Mesh Create(RenderContext rctx, Span<int> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
}

public class Mesh : BgfxResource
{   
    private readonly short[][] intBufferData;
    private readonly float[][] floatBufferData;
    private short[]? indexBufferData16 = null;
    private int[]? indexBufferData32 = null;
    private readonly int[] bufferDataIndices;

    private readonly MeshConfiguration _config;
    private uint elemCount = 0;
    private readonly MeshPrimitiveType primitiveType;
    private bool ready = false;
    private uint _elemCount = uint.MaxValue;

    public bool IsReady => ready;

    private readonly List<Bgfx.VertexBufferHandle> staticBuffers = [];
    private readonly List<Bgfx.DynamicVertexBufferHandle> dynamicBuffers = [];

     // given an index to a buffer, stores the index of the buffer in its respective list
     // transient buffer handles don't need to be retained, so there is no list for them.
    private readonly int[] bufferIndices;
    private readonly Bgfx.VertexLayout[] vertexLayouts = []; // given an index to a buffer, stores its vertex layout

    private Bgfx.IndexBufferHandle? staticIndexBuffer;
    private Bgfx.DynamicIndexBufferHandle? dynamicIndexBuffer;

    private List<int> _dirtyBuffers = [];
    private static bool? _index32Supported = null;

    /// <summary>
    /// Returns true if the rendering backend supports 32-bit indices. False if not.
    /// </summary>
    public static unsafe bool Are32BitIndicesSupported
    {
        get
        {
            if (_index32Supported is null)
            {
                var caps = Bgfx.get_caps();
                _index32Supported = (caps->supported & (ulong)Bgfx.CapsFlags.Index32) != 0;
            }

            return _index32Supported.Value;
        }
    }

    /// <summary>
    /// Creates a mesh given a configuration as well as the number of
    /// vertices and indices to allocate.<br/><br/>
    /// The static method Mesh.Create is an alternative constructor.
    /// </summary>
    /// <param name="config">The MeshConfiguration</param>
    /// <param name="vertexCount">The number of vertices to allocate</param>
    /// <param name="indexCount">The number of indices to allocate. Unused if config.Indexed is false</param>
    /// <exception cref="UnsupportedOperationException">Thrown if MeshConfiguration requires 32-bit indices, but the renderer does not support it.</exception>
    public unsafe Mesh(MeshConfiguration config, int vertexCount, int indexCount = 0)
    {
        _config = config.Clone();

        if (_config.Use32BitIndices && !Are32BitIndicesSupported)
        {
            throw new UnsupportedOperationException("32-bit indices are not supported");
        }
        
        int intBufCount = 0;
        int floatBufCount = 0;
        bufferIndices = new int[_config.Buffers.Count];
        bufferDataIndices = new int[_config.Buffers.Count];

        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            if (_config.Buffers[i].Type == DataType.Int)
            {
                bufferDataIndices[i] = intBufCount++;
            }
            else
            {
                bufferDataIndices[i] = floatBufCount++;
            }

            bufferIndices[i] = -1;
        }

        intBufferData = new short[intBufCount][];
        floatBufferData = new float[floatBufCount][];

        // allocate buffer data and setup vertex layout
        vertexLayouts = new Bgfx.VertexLayout[_config.Buffers.Count];
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            byte numElements = (byte) GetElementCount(bufConfig.Type);

            if (bufConfig.Type == DataType.Int)
            {
                intBufferData[bufferDataIndices[i]] = new short[vertexCount * numElements];
            }
            else
            {
                floatBufferData[bufferDataIndices[i]] = new float[vertexCount * numElements];
            }

            Bgfx.AttribType attribType = bufConfig.Type == DataType.Int ? Bgfx.AttribType.Int16 : Bgfx.AttribType.Float;
            
            var layout = new Bgfx.VertexLayout();
            Bgfx.vertex_layout_begin(&layout, Bgfx.RendererType.Noop);
            Bgfx.vertex_layout_add(&layout, (Bgfx.Attrib)bufConfig.Target, numElements, attribType, false, false);
            Bgfx.vertex_layout_end(&layout);
            vertexLayouts[i] = layout;

            _dirtyBuffers.Add(i);
        }

        if (_config.Indexed)
        {
            _dirtyBuffers.Add(-1);

            if (_config.Use32BitIndices)
            {
                indexBufferData32 = new int[indexCount];
            }
            else
            {
                indexBufferData16 = new short[indexCount];
            }
        }
    }

    public static Mesh Create(MeshConfiguration config, int vertexCount)
    {
        if (config.Indexed) throw new ArgumentException("MeshConfiguration specifies an indexed mesh, but indices array was not given.");
        return new(config, vertexCount);
    }

    public static Mesh Create(MeshConfiguration config, ReadOnlySpan<short> indices, int vertexCount)
    {
        if (!config.Indexed || config.Use32BitIndices) throw new ArgumentException("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<short> data);
        indices.CopyTo(data);
        return mesh;
    }

    public static Mesh Create(MeshConfiguration config, ReadOnlySpan<int> indices, int vertexCount)
    {
        if (!config.Indexed || !config.Use32BitIndices) throw new ArgumentException("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<int> data);
        indices.CopyTo(data);
        return mesh;
    }

    /// <summary>
    /// Get the index buffer span.<br/><br/>
    /// Valid only if the mesh is indexed, uses 16-bit indices, and buffer usage isn't static and Upload hasn't been called yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <returns>True if successful, false if not.</returns>
    public bool GetIndexBufferSpan(out Span<short> output)
    {
        output = null;

        if (!_config.Indexed || _config.Use32BitIndices)
            return false;

        if (ready && _config.IndexBufferUsage == MeshBufferUsage.Static)
            return false;
        
        output = indexBufferData16!;
        _dirtyBuffers.Add(-1);
        return true;
    }

    /// <summary>
    /// Get the index buffer span.<br/><br/>
    /// Valid only if the mesh is indexed, uses 32-bit indices, and buffer usage isn't static and Upload hasn't been called yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <returns>True if successful, false if not.</returns>
    public bool GetIndexBufferSpan(out Span<int> output)
    {
        output = null;

        if (!_config.Indexed || !_config.Use32BitIndices)
            return false;

        if (ready && _config.IndexBufferUsage == MeshBufferUsage.Static)
            return false;
        
        output = indexBufferData32!;
        _dirtyBuffers.Add(-1);
        return true;
    }

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a short span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public void GetBufferData(int bufferIndex, out Span<short> span)
    {
        if (bufferIndex < 0 || bufferIndex >= bufferDataIndices.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferIndex));

        var type = _config.Buffers[bufferIndex].Type;
        if (type != DataType.Int)
            throw new InvalidOperationException($"Attempt to get a Span<short> for buffer data, but the buffer type is {type}");
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after Mesh.Upload()");
        
        span = intBufferData[bufferDataIndices[bufferIndex]];
        _dirtyBuffers.Add(bufferIndex);
    }

    private bool GetFloatSpan<T>(int bufferIndex, int elemCount, out Span<T> output) where T : unmanaged
    {
        if (bufferIndex < 0 || bufferIndex >= bufferDataIndices.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferIndex));

        var type = _config.Buffers[bufferIndex].Type;
        if (type == DataType.Int)
            throw new InvalidOperationException($"Attempt to get a Span<{typeof(T).Name}> for buffer data, but the buffer type is Int");
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after Mesh.Upload()");
        
        var buf = floatBufferData[bufferDataIndices[bufferIndex]];
        if (buf.Length % elemCount != 0)
        {
            output = null;
            return false;
        }
        
        output = MemoryMarshal.Cast<float, T>(buf);
        _dirtyBuffers.Add(bufferIndex);
        return true;
    }

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a float span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public bool GetBufferData(int bufferIndex, out Span<float> span) => GetFloatSpan(bufferIndex, 1, out span);

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a Vector2 span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public bool GetBufferData(int bufferIndex, out Span<Vector2> span) => GetFloatSpan(bufferIndex, 1, out span);

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a Vector3 span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public bool GetBufferData(int bufferIndex, out Span<Vector3> span) => GetFloatSpan(bufferIndex, 1, out span);

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a Vector4 span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public bool GetBufferData(int bufferIndex, out Span<Vector4> span) => GetFloatSpan(bufferIndex, 1, out span);

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a Color span.<br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <returns>False if the buffer could not be interpreted as the span type. True otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer type is incompatible, or if the buffer cannot be accessed.</exception>
    public bool GetBufferData(int bufferIndex, out Span<Color> span) => GetFloatSpan(bufferIndex, 1, out span);

    /*private void SetFloatSpan<T>(int bufferIndex, int elemCount, Span<T> input) where T : unmanaged
    {
        if (bufferIndex < 0 || bufferIndex >= bufferDataIndices.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferIndex));

        var type = _config.Buffers[bufferIndex].Type;
        if (type == DataType.Int)
            throw new InvalidOperationException($"Attempt to get a Span<{typeof(T).Name}> for buffer data, but the buffer type is Int");
        
        if (_config.Buffers[bufferIndex].Usage != MeshBufferUsage.Transient)
            throw new InvalidOperationException("Cannot use SetBufferData on a non-transient buffer");

        var span = MemoryMarshal.Cast<T, float>(input);
    }*/

    /*public void SetBufferData(int bufferIndex, ReadOnlySpan<short> data)
    {
        if (_config.Buffers[bufferIndex].Type != DataType.Int)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var intArr = new short[data.Length];
        data.CopyTo(intArr);
        intBufferData[bufferIndices[bufferIndex]] = intArr;
        _updatedBuffers.Add(bufferIndex);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<float> data)
    {
        if (_config.Buffers[bufferIndex].Type != DataType.Float)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var floatArr = new float[data.Length];
        data.CopyTo(floatArr);
        floatBufferData[bufferIndices[bufferIndex]] = floatArr;
        _updatedBuffers.Add(bufferIndex);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector2> data)
    {
        if (_config.Buffers[bufferIndex].Type != DataType.Vector2)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var floatArr = new float[data.Length * 2];
        floatBufferData[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
        }

        _updatedBuffers.Add(bufferIndex);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector3> data)
    {
        if (_config.Buffers[bufferIndex].Type != DataType.Vector3)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var floatArr = new float[data.Length * 3];
        floatBufferData[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
            floatArr[j++] = data[i].Z;
        }

        _updatedBuffers.Add(bufferIndex);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector4> data)
    {
        if (_config.Buffers[bufferIndex].Type is not DataType.Vector4 or DataType.Color)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var floatArr = new float[data.Length * 4];
        floatBufferData[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
            floatArr[j++] = data[i].Z;
            floatArr[j++] = data[i].W;
        }

        _updatedBuffers.Add(bufferIndex);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Color> data)
    {
        if (_config.Buffers[bufferIndex].Type != DataType.Vector4 && _config.Buffers[bufferIndex].Type != DataType.Color)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        if (ready && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Attempt to change data of a static buffer");
        
        var floatArr = new float[data.Length * 4];
        floatBufferData[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].R;
            floatArr[j++] = data[i].G;
            floatArr[j++] = data[i].B;
            floatArr[j++] = data[i].A;
        }

        _updatedBuffers.Add(bufferIndex);
    }*/

    public static int GetElementCount(DataType type)
    {
        return type switch
        {
            DataType.Int or DataType.Float => 1,
            DataType.Vector2 => 2,
            DataType.Vector3 => 3,
            DataType.Vector4 or DataType.Color => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>
    /// Initialize mesh data to the GPU.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the mesh was already uploaded or buffer element counts are not the same.</exception>
    public unsafe void Upload()
    {
        // check that each buffer has the same amount of elements
        elemCount = uint.MaxValue;
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            
            uint count = bufConfig.Type switch
            {
                DataType.Int => (uint)intBufferData[bufferIndices[i]].Length,
                DataType.Float => (uint)floatBufferData[bufferIndices[i]].Length,
                DataType.Vector2 => (uint)floatBufferData[bufferIndices[i]].Length / 2,
                DataType.Vector3 => (uint)floatBufferData[bufferIndices[i]].Length / 3,
                DataType.Vector4 or DataType.Color => (uint)floatBufferData[bufferIndices[i]].Length / 4,
                _ => throw new Exception("Invalid MeshBufferType enum value")
            };

            if (count != elemCount && elemCount != uint.MaxValue)
                throw new InvalidOperationException("Mismatched mesh buffer sizes");
            elemCount = count;
        }

        if (ready && elemCount != _elemCount)
            throw new InvalidOperationException("Buffer size is not allowed to change");
        
        _elemCount = elemCount;

        // set buffer data
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];

            // transient buffer data will be created in the draw function
            if (bufConfig.Usage == MeshBufferUsage.Transient) continue;

            // only update if dirty
            if (!_dirtyBuffers.Contains(i)) continue;

            Bgfx.Memory* alloc;
            int vertexCount;

            if (bufConfig.Type == DataType.Int)
            {
                var buf = intBufferData[bufferDataIndices[i]] ?? throw new NullReferenceException($"Data for buffer {i} was not created!");
                alloc = BgfxUtil.Load<short>(buf);
                vertexCount = buf.Length;
            }
            else
            {
                var buf = floatBufferData[bufferDataIndices[i]]  ?? throw new NullReferenceException($"Data for buffer {i} was not created!");
                alloc = BgfxUtil.Load<float>(buf);

                Debug.Assert(buf.Length % GetElementCount(bufConfig.Type) == 0);
                vertexCount = buf.Length / GetElementCount(bufConfig.Type);
            }

            fixed (Bgfx.VertexLayout* layout = &vertexLayouts[i])
            {
                if (bufConfig.Usage == MeshBufferUsage.Static)
                {
                    Debug.Assert(!ready);
                    bufferIndices[i] = staticBuffers.Count;
                    staticBuffers.Add(Bgfx.create_vertex_buffer(alloc, layout, (ushort) Bgfx.BufferFlags.None));

                    // it is impossible to update a static buffer after creation,
                    // so there is no need to retain the data
                    intBufferData[bufferDataIndices[i]] = null!;
                }
                else if (bufConfig.Usage == MeshBufferUsage.Dynamic)
                {
                    Bgfx.DynamicVertexBufferHandle buffer;
                    
                    if (bufferIndices[i] == -1)
                    {
                        bufferIndices[i] = dynamicBuffers.Count;
                        buffer = Bgfx.create_dynamic_vertex_buffer((uint)vertexCount, layout, (ushort) Bgfx.BufferFlags.None);
                        dynamicBuffers.Add(buffer);
                    }
                    else
                    {
                        buffer = dynamicBuffers[bufferIndices[i]];
                    }

                    Bgfx.update_dynamic_vertex_buffer(buffer, 0, alloc);
                }
                else throw new Exception("Unreachable code");
            }
        }

        if (_config.Indexed && _dirtyBuffers.Contains(-1) && _config.IndexBufferUsage != MeshBufferUsage.Transient)
        {
            if ((_config.Use32BitIndices && indexBufferData32 == null) || (!_config.Use32BitIndices && indexBufferData16 == null))
                throw new NullReferenceException("Index data was not set");
            
            var alloc = _config.Use32BitIndices ? BgfxUtil.Load<int>(indexBufferData32) : BgfxUtil.Load<short>(indexBufferData16);
            var count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            var flags = Bgfx.BufferFlags.None;
            if (_config.Use32BitIndices) flags |= Bgfx.BufferFlags.Index32;

            if (_config.IndexBufferUsage == MeshBufferUsage.Static)
            {
                Debug.Assert(!ready);
                staticIndexBuffer = Bgfx.create_index_buffer(alloc, (ushort) flags);
            }
            else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
            {
                dynamicIndexBuffer ??= Bgfx.create_dynamic_index_buffer((uint)count, (ushort)flags);
                Bgfx.update_dynamic_index_buffer(dynamicIndexBuffer.Value, 0, alloc);
            }
            else throw new Exception("Unreachable code");
        }

        ready = true;
        _dirtyBuffers.Clear();
    }

    internal unsafe void Activate()
    {
        if (!ready)
            throw new Exception("Attempt to draw a Mesh that has not been uploaded.");
        
        // activate vertex buffers
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            
            if (bufConfig.Usage == MeshBufferUsage.Static)
                Bgfx.set_vertex_buffer((byte)i, staticBuffers[bufferIndices[i]], 0, _elemCount);
            else if (bufConfig.Usage == MeshBufferUsage.Dynamic)
                Bgfx.set_dynamic_vertex_buffer((byte)i, dynamicBuffers[bufferIndices[i]], 0, _elemCount);
            else if (bufConfig.Usage == MeshBufferUsage.Transient)
            {
                var tvb = new Bgfx.TransientVertexBuffer();
                fixed (Bgfx.VertexLayout* layout = &vertexLayouts[i])
                {
                    Bgfx.alloc_transient_vertex_buffer(&tvb, _elemCount, layout);
                }

                var tvbDataSpan = new Span<byte>(tvb.data, (int)tvb.size);
                if (bufConfig.Type == DataType.Int)
                {
                    var data = intBufferData[bufferDataIndices[i]];
                    MemoryMarshal.Cast<short, byte>(data).CopyTo(tvbDataSpan);
                }
                else
                {
                    var data = floatBufferData[bufferDataIndices[i]];
                    MemoryMarshal.Cast<float, byte>(data).CopyTo(tvbDataSpan);
                }

                Bgfx.set_transient_vertex_buffer((byte)i, &tvb, 0, _elemCount);
            }
            else throw new Exception("Unreachable code");
        }

        // activate index buffer
        if (_config.Indexed)
        {
            int count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            
            if (_config.IndexBufferUsage == MeshBufferUsage.Static)
                Bgfx.set_index_buffer(staticIndexBuffer!.Value, 0, (uint)count);
            else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
                Bgfx.set_dynamic_index_buffer(dynamicIndexBuffer!.Value, 0, (uint)count);
            else if (_config.IndexBufferUsage == MeshBufferUsage.Transient)
            {
                var tib = new Bgfx.TransientIndexBuffer();
                Bgfx.alloc_transient_index_buffer(&tib, (uint)count, _config.Use32BitIndices);

                var tibDataSpan = new Span<byte>(tib.data, (int)tib.size);
                if (_config.Use32BitIndices)
                {
                    MemoryMarshal.Cast<int, byte>(indexBufferData32).CopyTo(tibDataSpan);
                }
                else
                {
                    MemoryMarshal.Cast<short, byte>(indexBufferData16).CopyTo(tibDataSpan);
                }

                Bgfx.set_transient_index_buffer(&tib, 0, (uint)count);
            }
            else throw new Exception("Unreachable code");
        }
    }

    protected override void FreeResources(bool disposing)
    {
        Console.WriteLine("Free mesh handles");

        foreach (var buffer in staticBuffers)
        {
            Bgfx.destroy_vertex_buffer(buffer);
        }

        foreach (var buffer in dynamicBuffers)
        {
            Bgfx.destroy_dynamic_vertex_buffer(buffer);
        }

        if (staticIndexBuffer is not null)
            Bgfx.destroy_index_buffer(staticIndexBuffer.Value);

        if (dynamicIndexBuffer is not null)
            Bgfx.destroy_dynamic_index_buffer(dynamicIndexBuffer.Value);
    }
}

public class StandardMesh : Mesh
{
    /*private static readonly MeshConfiguration Config = new([
        DataType.Vector3, // vertices
        DataType.Vector2, // uvs
        DataType.Color,   // colors
    ], false);*/
    private static readonly MeshConfiguration Config = new MeshConfiguration()
        .AddBuffer(MeshBufferTarget.Position, DataType.Float, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.TexCoord0, DataType.Float, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Color0, DataType.Float, MeshBufferUsage.Static);

    private static readonly MeshConfiguration ConfigIndexed = new MeshConfiguration()
        .SetIndexed(true, false)
        .AddBuffer(MeshBufferTarget.Position, DataType.Float, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.TexCoord0, DataType.Float, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Color0, DataType.Float, MeshBufferUsage.Static);

    internal StandardMesh(int vertexCount) : base(Config, vertexCount)
    {}
    internal StandardMesh(int vertexCount, int indexCount) : base(ConfigIndexed, vertexCount, indexCount)
    {}

    public bool GetVertexData(out Span<Vector3> vertices)
        => GetBufferData(0, out vertices);

    public bool GetTexCoordData(out Span<Vector2> uvs)
        => GetBufferData(1, out uvs);
    
    public bool GetColorData(out Span<Color> colors)
        => GetBufferData(2, out colors);
}