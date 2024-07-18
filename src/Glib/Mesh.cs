using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Bgfx_cs;

namespace Glib;

public enum DataType
{
    /// <summary>
    /// An unsigned 8-bit integer
    /// </summary>
    Byte,

    /// <summary>
    /// A 16-bit integer.
    /// </summary>
    Short,

    /// <summary>
    /// A 32-bit float.
    /// </summary>
    Float
}

public enum MeshPrimitiveType
{
    Points,
    LineStrip,
    Lines,
    TriangleStrip,
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

public record struct MeshVertexAttribute(MeshBufferTarget Target, DataType Type, uint Count, bool Normalized = false);

public class MeshBufferConfiguration()
{
    public List<MeshVertexAttribute> VertexAttributes = [];
    public MeshBufferUsage Usage = MeshBufferUsage.Static;

    public MeshBufferConfiguration Clone()
    {
        return new MeshBufferConfiguration()
        {
            VertexAttributes = new List<MeshVertexAttribute>(VertexAttributes),
            Usage = Usage
        };
    }

    public MeshBufferConfiguration SetUsage(MeshBufferUsage usage)
    {
        Usage = usage;
        return this;
    }

    public MeshBufferConfiguration LayoutAdd(MeshBufferTarget target, DataType type, uint count, bool normalized = false)
    {
        VertexAttributes.Add(new MeshVertexAttribute(target, type, count, normalized));
        return this;
    }
}

public class MeshConfiguration
{
    public MeshPrimitiveType PrimitiveType = MeshPrimitiveType.Triangles;
    public readonly List<MeshBufferConfiguration> Buffers;

    public bool Indexed = false;

    /// <summary>
    /// If the index buffer uses 32-bit indices instead of 16-bit indices
    /// </summary>
    public bool Use32BitIndices = false;

    public MeshBufferUsage IndexBufferUsage = MeshBufferUsage.Static;

    public MeshConfiguration()
    {
        Buffers = [];
    }

    public MeshConfiguration Clone()
    {
        return new MeshConfiguration(Buffers.Select(x => x.Clone()), Indexed)
        {
            PrimitiveType = PrimitiveType,
            Use32BitIndices = Use32BitIndices,
            IndexBufferUsage = IndexBufferUsage
        };
    }

    private MeshConfiguration(IEnumerable<MeshBufferConfiguration> buffers, bool indexed)
    {
        Buffers = [..buffers];
        Indexed = indexed;
    }

    public MeshConfiguration SetIndexed(bool int32, MeshBufferUsage usage = MeshBufferUsage.Static)
    {
        Indexed = true;
        Use32BitIndices = int32;
        IndexBufferUsage = usage;
        return this;
    }

    public MeshConfiguration SetPrimitiveType(MeshPrimitiveType type)
    {
        PrimitiveType = type;
        return this;
    }

    public MeshConfiguration AddBuffer(MeshBufferConfiguration config)
    {
        Buffers.Add(config);
        return this;
    }

    public MeshConfiguration AddBuffer(MeshBufferTarget target, DataType dataType, uint count, MeshBufferUsage usage = MeshBufferUsage.Static)
    {
        Buffers.Add(
            new MeshBufferConfiguration()
                .LayoutAdd(target, dataType, count)
                .SetUsage(usage)
        );

        return this;
    }

    public Mesh Create(int vtxCount) => Mesh.Create(this, vtxCount);
    public Mesh Create(int vtxCount, int idxCount) => new Mesh(this, vtxCount, idxCount);
    public Mesh CreateIndexed(Span<ushort> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
    public Mesh CreateIndexed32(Span<uint> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
}

public class Mesh : BgfxResource
{   
    private readonly byte[][] bufferData;
    private ushort[]? indexBufferData16 = null;
    private uint[]? indexBufferData32 = null;

    private readonly MeshConfiguration _config;
    private uint[] _elemCounts;
    private (uint start, uint length)[] _bufferIndexSettings;
    private (uint start, uint length) _indexIndexSettings; // lol
    private bool _reset = true;
    private uint[] _attrSizes;

    private readonly List<Bgfx.VertexBufferHandle> staticBuffers = [];
    private readonly List<Bgfx.DynamicVertexBufferHandle> dynamicBuffers = [];
    private readonly List<Bgfx.TransientVertexBuffer> transientBuffers = [];    

    // given an index to a buffer, stores the index of the buffer in its respective list
    // transient buffer handles don't need to be retained, so there is no list for them.
    private readonly int[] bufferIndices;
    private readonly Bgfx.VertexLayout[] vertexLayouts = []; // given an index to a buffer, stores its vertex layout

    private Bgfx.IndexBufferHandle? staticIndexBuffer;
    private Bgfx.DynamicIndexBufferHandle? dynamicIndexBuffer;
    private Bgfx.TransientIndexBuffer? transientIndexBuffer;

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
    /// <exception cref="UnsupportedRendererOperationException">Thrown if MeshConfiguration requires 32-bit indices, but the renderer does not support it.</exception>
    public unsafe Mesh(MeshConfiguration config, int vertexCount, int indexCount = 0)
    {
        _config = config.Clone();

        if (_config.Use32BitIndices && !Are32BitIndicesSupported)
        {
            throw new UnsupportedRendererOperationException("32-bit indices are not supported");
        }

        bufferData = new byte[_config.Buffers.Count][];
        _elemCounts = new uint[_config.Buffers.Count];
        _bufferIndexSettings = new (uint, uint)[_config.Buffers.Count];
        _attrSizes = new uint[_config.Buffers.Count];
        bufferIndices = new int[_config.Buffers.Count];
        vertexLayouts = new Bgfx.VertexLayout[_config.Buffers.Count];

        int transientBufIndex = 0;
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            uint attrSize = 0;

            var layout = new Bgfx.VertexLayout();
            Bgfx.vertex_layout_begin(&layout, Bgfx.RendererType.Count);
            foreach (var attr in bufConfig.VertexAttributes)
            {
                Bgfx.AttribType attribType;

                switch (attr.Type)
                {
                    case DataType.Byte:
                        attribType = Bgfx.AttribType.Uint8;
                        attrSize += attr.Count;
                        break;

                    case DataType.Short:
                        attribType = Bgfx.AttribType.Int16;
                        attrSize += attr.Count * 2;
                        break;

                    case DataType.Float:
                        attribType = Bgfx.AttribType.Float;
                        attrSize += attr.Count * 4;
                        break;
                    
                    default:
                        throw new Exception("Invalid DataType enum");
                }

                Bgfx.vertex_layout_add(&layout, (Bgfx.Attrib)attr.Target, (byte)attr.Count, attribType, attr.Normalized, false);
            }
            Bgfx.vertex_layout_end(&layout);

            vertexLayouts[i] = layout;

            _elemCounts[i] = (uint)vertexCount;
            _bufferIndexSettings[i] = (0, (uint)vertexCount);
            _attrSizes[i] = attrSize;
            bufferData[i] = new byte[vertexCount * attrSize];
            bufferIndices[i] = -1;

            if (bufConfig.Usage == MeshBufferUsage.Transient)
                bufferIndices[i] = transientBufIndex++;
        }

        if (_config.Indexed)
        {
            if (_config.Use32BitIndices)
            {
                indexBufferData32 = new uint[indexCount];
            }
            else
            {
                indexBufferData16 = new ushort[indexCount];
            }
        }

        _indexIndexSettings = (0, (uint)indexCount);
    }

    public static Mesh Create(MeshConfiguration config, int vertexCount)
    {
        if (config.Indexed) throw new ArgumentException("MeshConfiguration specifies an indexed mesh, but indices array was not given.");
        return new(config, vertexCount);
    }

    public static Mesh Create(MeshConfiguration config, ReadOnlySpan<ushort> indices, int vertexCount)
    {
        if (!config.Indexed || config.Use32BitIndices) throw new ArgumentException("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<ushort> data);
        indices.CopyTo(data);
        mesh.UploadIndices();
        return mesh;
    }

    public static Mesh Create(MeshConfiguration config, ReadOnlySpan<uint> indices, int vertexCount)
    {
        if (!config.Indexed || !config.Use32BitIndices) throw new ArgumentException("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<uint> data);
        indices.CopyTo(data);
        mesh.UploadIndices();
        return mesh;
    }

    /// <summary>
    /// Get the index buffer span as 16-bit integers.<br/><br/>
    /// Valid only if the mesh is indexed, uses 16-bit indices, and buffer usage isn't static and the buffer hasn't been uploaded yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <exception cref="InvalidOperationException">The buffer is inaccessible or it uses an incompatible element type.</exception>
    public void GetIndexBufferSpan(out Span<ushort> output)
    {
        if (!_config.Indexed || _config.Use32BitIndices)
            throw new InvalidOperationException("The mesh does not use 16-bit indices");

        if (staticIndexBuffer is not null)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after it has been uploaded.");
        
        output = indexBufferData16!;
    }
    
    /// <summary>
    /// Get the index buffer span as 32-bit integers.<br/><br/>
    /// Valid only if the mesh is indexed, uses 32-bit indices, and buffer usage isn't static and the buffer hasn't been uploaded yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <exception cref="InvalidOperationException">The buffer is inaccessible or it uses an incompatible element type.</exception>
    public void GetIndexBufferSpan(out Span<uint> output)
    {
        if (!_config.Indexed || !_config.Use32BitIndices)
            throw new InvalidOperationException("The mesh does not use 32-bit indices");

        if (staticIndexBuffer is not null)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after it has been uploaded.");
        
        output = indexBufferData32!;
    }

    /// <summary>
    /// Set the 16-bit index buffer.<br/><br/>
    /// Valid only if the mesh is indexed, uses 16-bit indices, and buffer usage isn't static and the buffer hasn't been uploaded yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <exception cref="InvalidOperationException">The buffer is inaccessible or it uses an incompatible element type.</exception>
    /// <exception cref="ArgumentException">The input data size does not match the size of the underlying already-uploaded dynamic buffer.</exception>
    public void SetIndexBuffer(ReadOnlySpan<ushort> input)
    {
        if (_config.IndexBufferUsage != MeshBufferUsage.Transient || input.Length == indexBufferData16!.Length)
        {
            GetIndexBufferSpan(out Span<ushort> span);
            if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic && dynamicIndexBuffer is not null && input.Length != indexBufferData16!.Length)
                throw new ArgumentException("Span size must match that of the underlying dynamic buffer");
            
            input.CopyTo(span);
        }
        else // resize, if transient buffer
        {
            indexBufferData16 = new ushort[input.Length];
            input.CopyTo(indexBufferData16);
        }
    }

    /// <summary>
    /// Set the 32-bit index buffer.<br/><br/>
    /// Valid only if the mesh is indexed, uses 32-bit indices, and buffer usage isn't static and the buffer hasn't been uploaded yet.
    /// </summary>
    /// <param name="data">The buffer span.</param>
    /// <exception cref="InvalidOperationException">The buffer is inaccessible or it uses an incompatible element type.</exception>
    /// <exception cref="ArgumentException">The input data size does not match the size of the underlying already-uploaded dynamic buffer.</exception>
    public void SetIndexBuffer(ReadOnlySpan<uint> input)
    {
        if (_config.IndexBufferUsage != MeshBufferUsage.Transient || input.Length == indexBufferData32!.Length)
        {
            GetIndexBufferSpan(out Span<uint> span);
            if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic && dynamicIndexBuffer is not null && input.Length != indexBufferData32!.Length)
                throw new ArgumentException("Span size must match that of the underlying dynamic buffer");
            
            input.CopyTo(span);
        }
        else // resize, if transient buffer
        {
            indexBufferData32 = new uint[input.Length];
            input.CopyTo(indexBufferData32);
        }
    }

    /// <summary>
    /// Get the buffer of a dynamic or transient buffer as a span of a compatible type/struct.
    /// The size of the generic type must match the size of a vertex attribute for the buffer as specified in its configuration.
    /// <br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. Doing so will throw an exception.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the given bufferIndex does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer cannot be accessed.</exception>
    /// <exception cref="InvalidCastException">Thrown if the size of a vertex attribute for the buffer is greater than size of the given generic type.</exception>
    public Span<T> GetBufferData<T>(int bufferIndex) where T : unmanaged
    {
        if (bufferIndex < 0 || bufferIndex >= bufferData.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferIndex));
        
        if (bufferIndices[bufferIndex] != -1 && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after it has been uploaded");

        var itemSize = Marshal.SizeOf<T>();
        if (itemSize > _attrSizes[bufferIndex])
            throw new InvalidCastException("The vertex attribute for a buffer could not be represented with the given type.");
        
        var data = new Span<byte>(bufferData[bufferIndex]);
        return MemoryMarshal.Cast<byte, T>(data);
    }

    /// <summary>
    /// Set the data for a given buffer.
    /// <br/><br/>
    /// This cannot be called if the given buffer has static usage and Upload() had already been called,
    /// since the buffer will then be inaccessible. When called on a dynamic buffer, the input size must
    /// match the size of the underlying buffer. Transient buffers have no restrictions.
    /// </summary>
    /// <param name="bufferIndex">The buffer index</param>
    /// <param name="span">The data span</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the buffer at given bufferIndex does not exist.</exception> 
    /// <exception cref="ArgumentException">Thrown if the vertex count of the input span does not match that of the underlying dynamic buffer.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the buffer is static and has already been uploaded.</exception>
    public void SetBufferData<T>(int bufferIndex, ReadOnlySpan<T> input) where T : unmanaged
    {
        if (bufferIndex < 0 || bufferIndex >= bufferData.Length)
            throw new ArgumentOutOfRangeException(nameof(bufferIndex));
        
        if (bufferIndices[bufferIndex] != -1 && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Cannot use SetBufferData on a static buffer after the mesh has been uploaded");
        
        var data = bufferData[bufferIndex];
        var itemSize = Marshal.SizeOf<T>();
        if (_config.Buffers[bufferIndex].Usage == MeshBufferUsage.Dynamic && input.Length * itemSize != data.Length)
            throw new ArgumentException("Span size must match that of the underlying dynamic buffer");

        if (input.Length * itemSize == data.Length)
        {
            MemoryMarshal.Cast<T, byte>(input).CopyTo(data);
        }
        else
        {
            if (input.Length * itemSize % _attrSizes[bufferIndex] != 0)
                throw new ArgumentException("Incomplete vertex input data");
            
            bufferData[bufferIndex] = new byte[input.Length * itemSize];
            MemoryMarshal.Cast<T, byte>(input).CopyTo(bufferData[bufferIndex]);
            _elemCounts[bufferIndex] = (uint)(input.Length * itemSize / _attrSizes[bufferIndex]);
        }
    }

    /// <summary>
    /// Upload buffer data to the GPU. Can be called again for the same buffer if it is a dynamic buffer.
    /// Calling it on transient buffers has no effect, as they are updated on each draw call.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the buffer data could not be reuploaded.</exception>
    public unsafe void Upload(int bufferIndex)
    {
        // set buffer data
        var bufConfig = _config.Buffers[bufferIndex];

        // transient buffer data will be created in the draw function
        if (bufConfig.Usage == MeshBufferUsage.Transient) return;

        Bgfx.Memory* alloc;
        uint vertexCount;

        var buf = bufferData[bufferIndex] ?? throw new InvalidOperationException("Could not access buffer data for " + bufferIndex);
        alloc = BgfxUtil.Load<byte>(buf);

        Debug.Assert(buf.Length % _attrSizes[bufferIndex] == 0);
        Debug.Assert(_elemCounts[bufferIndex] == (buf.Length / _attrSizes[bufferIndex]));
        vertexCount = _elemCounts[bufferIndex];

        fixed (Bgfx.VertexLayout* layout = &vertexLayouts[bufferIndex])
        {
            if (bufConfig.Usage == MeshBufferUsage.Static)
            {
                Debug.Assert(bufferIndices[bufferIndex] == -1);
                bufferIndices[bufferIndex] = staticBuffers.Count;
                staticBuffers.Add(Bgfx.create_vertex_buffer(alloc, layout, (ushort) Bgfx.BufferFlags.None));

                // it is impossible to update a static buffer after creation,
                // so there is no need to retain the data
                bufferData[bufferIndex] = null!;
            }
            else if (bufConfig.Usage == MeshBufferUsage.Dynamic)
            {
                Bgfx.DynamicVertexBufferHandle buffer;
                
                if (bufferIndices[bufferIndex] == -1)
                {
                    bufferIndices[bufferIndex] = dynamicBuffers.Count;
                    buffer = Bgfx.create_dynamic_vertex_buffer(vertexCount, layout, (ushort) Bgfx.BufferFlags.None);
                    dynamicBuffers.Add(buffer);
                }
                else
                {
                    buffer = dynamicBuffers[bufferIndices[bufferIndex]];
                }

                Bgfx.update_dynamic_vertex_buffer(buffer, 0, alloc);
            }
            else throw new Exception("Unreachable code");
        }
    }

    /// <summary>
    /// Upload index buffer data to the GPU. Can be called again for the same buffer if it is dynamic.
    /// Calling it on a transient buffer has no effect, as it is updated on each draw call.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the buffer data could not be reuploaded.</exception>
    public unsafe void UploadIndices()
    {
        if (_config.Indexed && _config.IndexBufferUsage != MeshBufferUsage.Transient)
        {
            if ((_config.Use32BitIndices && indexBufferData32 == null) || (!_config.Use32BitIndices && indexBufferData16 == null))
                throw new NullReferenceException("Index data was not set");
            
            var alloc = _config.Use32BitIndices ? BgfxUtil.Load<uint>(indexBufferData32) : BgfxUtil.Load<ushort>(indexBufferData16);
            var count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            var flags = Bgfx.BufferFlags.None;
            if (_config.Use32BitIndices) flags |= Bgfx.BufferFlags.Index32;

            if (_config.IndexBufferUsage == MeshBufferUsage.Static)
            {
                Debug.Assert(staticIndexBuffer == null);
                staticIndexBuffer = Bgfx.create_index_buffer(alloc, (ushort) flags);
            }
            else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
            {
                dynamicIndexBuffer ??= Bgfx.create_dynamic_index_buffer((uint)count, (ushort)flags);
                Bgfx.update_dynamic_index_buffer(dynamicIndexBuffer.Value, 0, alloc);
            }
            else throw new Exception("Unreachable code");
        }
    }

    /// <summary>
    /// Upload all possible buffers to the GPU.
    /// </summary>
    public void Upload()
    {
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var usage = _config.Buffers[i].Usage;
            if (usage != MeshBufferUsage.Transient && (usage == MeshBufferUsage.Dynamic || bufferIndices[i] == -1))
                Upload(i);
        }

        if (_config.Indexed)
        {
            var usage = _config.IndexBufferUsage;
            if (usage != MeshBufferUsage.Transient && (usage == MeshBufferUsage.Dynamic || (dynamicIndexBuffer == null && staticIndexBuffer == null)))
                UploadIndices();
        }
    }

    internal unsafe Bgfx.StateFlags Activate()
    {
        // check if all buffers has been uploaded
        for (int i = 0; i < bufferIndices.Length; i++)
        {
            if (_config.Buffers[i].Usage != MeshBufferUsage.Transient && bufferIndices[i] == -1)
                throw new InvalidOperationException("Attempt to draw a Mesh that has not been fully uploaded.");
        }

        // activate vertex buffers
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            var (start, length) = _bufferIndexSettings[i];
            Debug.Assert(start >= 0 && start < _elemCounts[i]);
            Debug.Assert(length >= 0 && (start + length) <= _elemCounts[i]);
            
            if (bufConfig.Usage == MeshBufferUsage.Static)
                Bgfx.set_vertex_buffer((byte)i, staticBuffers[bufferIndices[i]], start, length);
            else if (bufConfig.Usage == MeshBufferUsage.Dynamic)
                Bgfx.set_dynamic_vertex_buffer((byte)i, dynamicBuffers[bufferIndices[i]], start, length);
            else if (bufConfig.Usage == MeshBufferUsage.Transient)
            {
                Bgfx.TransientVertexBuffer tvb;

                if (!_reset)
                {
                    tvb = transientBuffers[bufferIndices[i]];
                    Bgfx.set_transient_vertex_buffer((byte)i, &tvb, start, length);
                }
                else
                {
                    tvb = new Bgfx.TransientVertexBuffer();
                    fixed (Bgfx.VertexLayout* layout = &vertexLayouts[i])
                    {
                        Bgfx.alloc_transient_vertex_buffer(&tvb, _elemCounts[i], layout);
                    }

                    Debug.Assert(bufferData[i].Length == tvb.size);
                    var tvbDataSpan = new Span<byte>(tvb.data, (int)tvb.size);
                    bufferData[i].CopyTo(tvbDataSpan);

                    Bgfx.set_transient_vertex_buffer((byte)i, &tvb, start, length);
                }

                transientBuffers.Add(tvb);
            }
            else throw new Exception("Unreachable code");
        }

        // activate index buffer
        if (_config.Indexed)
        {
            int count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;

            var (start, length) = _indexIndexSettings;
            Debug.Assert(start >= 0 && start < count);
            Debug.Assert(length >= 0 && (start + length) <= count);
            
            if (_config.IndexBufferUsage == MeshBufferUsage.Static)
                Bgfx.set_index_buffer(staticIndexBuffer!.Value, 0, (uint)count);
            else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
                Bgfx.set_dynamic_index_buffer(dynamicIndexBuffer!.Value, 0, (uint)count);
            else if (_config.IndexBufferUsage == MeshBufferUsage.Transient)
            {
                Bgfx.TransientIndexBuffer tib;

                if (!_reset)
                {
                    tib = transientIndexBuffer ?? throw new Exception("TransientIndexBuffer is null, but reset == false");
                    Bgfx.set_transient_index_buffer(&tib, start, length);
                }
                else
                {
                    tib = new Bgfx.TransientIndexBuffer();
                    Bgfx.alloc_transient_index_buffer(&tib, (uint)count, _config.Use32BitIndices);

                    var tibDataSpan = new Span<byte>(tib.data, (int)tib.size);
                    if (_config.Use32BitIndices)
                    {
                        Debug.Assert(indexBufferData32!.Length * 4 == tib.size);
                        MemoryMarshal.Cast<uint, byte>(indexBufferData32).CopyTo(tibDataSpan);
                    }
                    else
                    {
                        Debug.Assert(indexBufferData16!.Length * 2 == tib.size);
                        MemoryMarshal.Cast<ushort, byte>(indexBufferData16).CopyTo(tibDataSpan);
                    }

                    Bgfx.set_transient_index_buffer(&tib, start, length);
                }

                transientIndexBuffer = tib;
            }
            else throw new Exception("Unreachable code");
        }

        _reset = false;
        
        return _config.PrimitiveType switch
        {
            MeshPrimitiveType.Points => Bgfx.StateFlags.PtPoints,
            MeshPrimitiveType.LineStrip => Bgfx.StateFlags.PtLinestrip,
            MeshPrimitiveType.Lines => Bgfx.StateFlags.PtLines,
            MeshPrimitiveType.TriangleStrip => Bgfx.StateFlags.PtTristrip,
            MeshPrimitiveType.Triangles => Bgfx.StateFlags.None,
            _ => throw new Exception("Invalid MeshPrimitiveType")
        };
    }

    internal void ResetSliceSettings()
    {
        transientIndexBuffer = null;
        transientBuffers.Clear();
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            _bufferIndexSettings[i] = (0, _elemCounts[i]);
        }

        _indexIndexSettings = (0, 0);
        if (_config.Indexed)
        {
            if (indexBufferData16 is not null)
                _indexIndexSettings = (0, (uint)indexBufferData16.Length);
            else if (indexBufferData32 is not null)
                _indexIndexSettings = (0, (uint)indexBufferData32.Length);
            else
                throw new Exception("reset: (_config.Indexed == true), yet indexBufferData32 or indexBufferData16 is null");
        }

        _reset = true;
    }

    internal void SetBufferDrawSlice(int bufferIndex, uint startVertex, uint vertexCount)
    {
        if (bufferIndex < 0 || bufferIndex >= _bufferIndexSettings.Length)
            throw new ArgumentException("The buffer at the given index does not exist", nameof(bufferIndex));
        
        // bounds check
        if (!((startVertex >= 0 && startVertex < _elemCounts[bufferIndex]) ||
            (vertexCount >= 0 && (startVertex + vertexCount) <= _elemCounts[bufferIndex]))
        ) throw new ArgumentOutOfRangeException($"Requested slice does not fit in the required range [0, {_elemCounts[bufferIndex]})");

        _bufferIndexSettings[bufferIndex] = (startVertex, vertexCount);
    }

    internal void SetIndexBufferDrawSlice(uint startVertex, uint vertexCount)
    {
        if (!_config.Indexed) throw new InvalidOperationException("The mesh is not indexed");

        // bounds check
        uint maxSize;
        if (indexBufferData16 is not null)      maxSize = (uint)indexBufferData16.Length;
        else if (indexBufferData32 is not null) maxSize = (uint)indexBufferData32.Length;
        else throw new Exception("This branch is supposed to be unreachable");

        if (!((startVertex >= 0 && startVertex < maxSize) &&
            (vertexCount >= 0 && (startVertex + vertexCount) <= maxSize))
        ) throw new ArgumentOutOfRangeException($"Requested slice does not fit in the required range [0, {maxSize})");

        _indexIndexSettings = (startVertex, vertexCount);
    }

    internal void SetIndexBufferDrawSlice(uint startIndex)
    {
        if (!_config.Indexed) throw new InvalidOperationException("The mesh is not indexed");

        // bounds check
        uint maxSize;
        if (indexBufferData16 is not null)      maxSize = (uint)indexBufferData16.Length;
        else if (indexBufferData32 is not null) maxSize = (uint)indexBufferData32.Length;
        else throw new Exception("This branch is supposed to be unreachable");

        if (startIndex < 0 && startIndex >= maxSize)
            throw new ArgumentOutOfRangeException($"Requested slice does not fit in the required range [0, {maxSize})");

        _indexIndexSettings = (startIndex, maxSize - startIndex);
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
    private static readonly MeshConfiguration Config = new MeshConfiguration()
        .AddBuffer(MeshBufferTarget.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Color0, DataType.Float, 4, MeshBufferUsage.Static);

    private static readonly MeshConfiguration ConfigIndexed16 = new MeshConfiguration()
        .SetIndexed(false, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Color0, DataType.Float, 4, MeshBufferUsage.Static);
    
    private static readonly MeshConfiguration ConfigIndexed32 = new MeshConfiguration()
        .SetIndexed(true, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(MeshBufferTarget.Color0, DataType.Float, 4, MeshBufferUsage.Static);

    internal StandardMesh(int vertexCount) : base(Config, vertexCount)
    {}
    internal StandardMesh(int vertexCount, int indexCount, bool index32) : base(index32 ? ConfigIndexed32 : ConfigIndexed16, vertexCount, indexCount)
    {}

    public Span<Vector3> GetVertexData()
        => GetBufferData<Vector3>(0);

    public Span<Vector2> GetTexCoordData()
        => GetBufferData<Vector2>(1);
    
    public Span<Color> GetColorData()
        => GetBufferData<Color>(2);
    
    public void SetVertexData(ReadOnlySpan<Vector3> vertices)
        => SetBufferData(0, vertices);

    public void SetTexCoordData(ReadOnlySpan<Vector2> uvs)
        => SetBufferData(1, uvs);
    
    public void SetColorData(ReadOnlySpan<Color> colors)
        => SetBufferData(2, colors);
    
    public static StandardMesh Create(int vertexCount) => new(vertexCount);
    public static StandardMesh CreateIndexed(ReadOnlySpan<ushort> indices, int vertexCount)
    {
        var mesh = new StandardMesh(vertexCount, indices.Length, false);
        mesh.GetIndexBufferSpan(out Span<ushort> indexSpan);
        indices.CopyTo(indexSpan);
        mesh.UploadIndices();
        return mesh;
    }
    public static StandardMesh CreateIndexed32(ReadOnlySpan<uint> indices, int vertexCount)
    {
        var mesh = new StandardMesh(vertexCount, indices.Length, true);
        mesh.GetIndexBufferSpan(out Span<uint> indexSpan);
        indices.CopyTo(indexSpan);
        mesh.UploadIndices();
        return mesh;
    }
}