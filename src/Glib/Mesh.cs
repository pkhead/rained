using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

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

public enum AttributeName : uint
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

[System.Serializable]
internal class InsufficientBufferSpaceException : System.Exception
{
    public InsufficientBufferSpaceException() { }
    public InsufficientBufferSpaceException(string message) : base(message) { }
    public InsufficientBufferSpaceException(string message, System.Exception inner) : base(message, inner) { }
}

public record struct MeshVertexAttribute(uint Index, DataType Type, uint Count, bool Normalized = false)
{
    public MeshVertexAttribute(AttributeName attribName, DataType type, uint count, bool normalized = false)
    : this((uint)attribName, type, count, normalized)
    {}
}

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

    public MeshBufferConfiguration LayoutAdd(uint index, DataType type, uint count, bool normalized = false)
    {
        VertexAttributes.Add(new MeshVertexAttribute(index, type, count, normalized));
        return this;
    }

    public MeshBufferConfiguration LayoutAdd(AttributeName attribName, DataType type, uint count, bool normalized = false)
        => LayoutAdd((uint)attribName, type, count, normalized);
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

    public MeshConfiguration AddBuffer(uint index, DataType dataType, uint count, MeshBufferUsage usage = MeshBufferUsage.Static)
    {
        Buffers.Add(
            new MeshBufferConfiguration()
                .LayoutAdd(index, dataType, count)
                .SetUsage(usage)
        );

        return this;
    }

    public MeshConfiguration AddBuffer(AttributeName attribName, DataType dataType, uint count, MeshBufferUsage usage = MeshBufferUsage.Static)
        => AddBuffer((uint)attribName, dataType, count, usage);

    public Mesh Create(int vtxCount) => Mesh.Create(this, vtxCount);
    public Mesh Create(int vtxCount, int idxCount) => new(this, vtxCount, idxCount);
    public Mesh CreateIndexed(Span<ushort> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
    public Mesh CreateIndexed32(Span<uint> indices, int vtxCount) => Mesh.Create(this, indices, vtxCount);
}

public class Mesh : Resource
{   
    private readonly byte[][] bufferData;
    private ushort[]? indexBufferData16 = null;
    private uint[]? indexBufferData32 = null;

    private readonly MeshConfiguration _config;
    private uint[] _elemCounts;
    private (uint baseVertex, uint count) _vertexSlice;
    private uint _baseIndex = 0;
    private bool _reset = true;
    private readonly uint[] _attrStrides;
    private readonly bool[] _uploadedLayout;

    /// <summary>
    /// true if the transient buffer at this index needs to be resized
    /// </summary>
    private readonly bool[] _needResize;

    /// <summary>
    /// true if the transient index buffer needs to be resized 
    /// </summary>
    private bool _indexNeedResize;

    private readonly uint[] buffers = [];
    private uint indexBuffer;
    private uint vertexArray;

    private static bool? _baseVertexSupported = null;

    /// <summary>
    /// Returns true if the rendering backend supports 32-bit indices. False if not.
    /// </summary>
    public static unsafe bool Are32BitIndicesSupported
    {
        get => true;
    }

    /// <summary>
    /// Returns true if the rendering backend supports being able to change the base vertex<br />
    /// when setting the buffer range.
    /// </summary>
    public static bool IsBaseVertexSupported
    {
        get => _baseVertexSupported ??= RenderContext.Gl.IsExtensionPresent("GL_EXT_draw_elements_base_vertex");
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
        var gl = RenderContext.Gl;

        _config = config.Clone();

        if (_config.Use32BitIndices && !Are32BitIndicesSupported)
        {
            throw new UnsupportedOperationException("32-bit indices are not supported");
        }

        bufferData = new byte[_config.Buffers.Count][];
        _elemCounts = new uint[_config.Buffers.Count];
        _attrStrides = new uint[_config.Buffers.Count];
        buffers = new uint[_config.Buffers.Count];
        _uploadedLayout = new bool[_config.Buffers.Count];
        _needResize = new bool[_config.Buffers.Count];

        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            uint stride = 0;

            // calculate stride of vertex attributes
            // pointers will be set on Upload
            foreach (var attr in bufConfig.VertexAttributes)
            {
                stride += attr.Type switch
                {
                    DataType.Byte => attr.Count,
                    DataType.Float => attr.Count * 4,
                    DataType.Short => attr.Count * 2,
                    _ => throw new Exception("Unknown attribute type"),
                };
            }

            _attrStrides[i] = stride;
            _elemCounts[i] = (uint)vertexCount;
            _uploadedLayout[i] = false;
            _needResize[i] = false;
            bufferData[i] = new byte[vertexCount * stride];
            buffers[i] = 0;
        }

        indexBuffer = 0;
        vertexArray = 0;
        _indexNeedResize = false;
        _vertexSlice = (0, (uint)vertexCount);
        _baseIndex = 0;

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

        if (_config.IndexBufferUsage == MeshBufferUsage.Static && indexBuffer != 0)
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

        if (_config.IndexBufferUsage == MeshBufferUsage.Static && indexBuffer != 0)
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
        if (!_config.Indexed) throw new InvalidOperationException("The mesh is not indexed");

        if (_config.IndexBufferUsage == MeshBufferUsage.Static && indexBuffer != 0)
            throw new InvalidOperationException("Cannot set data for a static buffer after it has been uploaded.");

        if (_config.IndexBufferUsage != MeshBufferUsage.Transient || input.Length == indexBufferData16!.Length)
        {
            GetIndexBufferSpan(out Span<ushort> span);
            if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic && indexBuffer != 0 && input.Length != indexBufferData16!.Length)
                throw new ArgumentException("Span size must match that of the underlying dynamic buffer");
            
            input.CopyTo(span);
        }
        else // resize, if transient buffer
        {
            _indexNeedResize = true;
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
        if (!_config.Indexed) throw new InvalidOperationException("The mesh is not indexed");

        if (_config.IndexBufferUsage == MeshBufferUsage.Static && indexBuffer != 0)
            throw new InvalidOperationException("Cannot set data for a static buffer after it has been uploaded.");

        if (_config.IndexBufferUsage != MeshBufferUsage.Transient || input.Length == indexBufferData32!.Length)
        {
            GetIndexBufferSpan(out Span<uint> span);
            if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic && indexBuffer != 0 && input.Length != indexBufferData32!.Length)
                throw new ArgumentException("Span size must match that of the underlying dynamic buffer");
            
            input.CopyTo(span);
        }
        else // resize, if transient buffer
        {
            _indexNeedResize = true;
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
        
        if (buffers[bufferIndex] != 0 && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
            throw new InvalidOperationException("Cannot retrieve data for a static buffer after it has been uploaded");

        var itemSize = Marshal.SizeOf<T>();
        if (itemSize > _attrStrides[bufferIndex])
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
        
        if (buffers[bufferIndex] != 0 && _config.Buffers[bufferIndex].Usage == MeshBufferUsage.Static)
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
            if (input.Length * itemSize % _attrStrides[bufferIndex] != 0)
                throw new ArgumentException("Incomplete vertex input data");
            
            _needResize[bufferIndex] = true;
            bufferData[bufferIndex] = new byte[input.Length * itemSize];
            MemoryMarshal.Cast<T, byte>(input).CopyTo(bufferData[bufferIndex]);
            _elemCounts[bufferIndex] = (uint)(input.Length * itemSize / _attrStrides[bufferIndex]);
        }
    }

    /// <summary>
    /// Returns the number of vertices that are in a buffer.
    /// </summary>
    /// <param name="bufferIndex">The index of the buffer</param>
    /// <returns>The number of vertices in a buffer.</returns>
    /// <exception cref="ArgumentException">Thrown if the buffer at the given index does not exist.</exception>
    public uint GetBufferVertexCount(int bufferIndex)
    {
        if (bufferIndex < 0 || bufferIndex >= _elemCounts.Length)
            throw new ArgumentException("The buffer at the given index does not exist", nameof(bufferIndex));
        
        return _elemCounts[bufferIndex];
    }

    /// <summary>
    /// Returns the length of the index buffer
    /// </summary>
    /// <returns>The length of the index buffer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the mesh is not indexed.</exception>   
    public uint GetIndexVertexCount()
    {
        if (!_config.Indexed) throw new InvalidOperationException("The mesh is not indexed");
        return (uint)(indexBufferData16?.Length ?? indexBufferData32!.Length);
    }

    /// <summary>
    /// Upload buffer data to the GPU. Can be called again for the same buffer if it is a dynamic buffer.
    /// Calling it on transient buffers has no effect, as they are updated on each draw call.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the buffer data could not be uploaded.</exception>
    public unsafe void Upload(int bufferIndex)
    {
        var gl = RenderContext.Gl;

        var bufConfig = _config.Buffers[bufferIndex];

        // transient buffer data will be created in the draw function
        if (bufConfig.Usage == MeshBufferUsage.Transient) return;

        uint vertexCount = _elemCounts[bufferIndex];
        if (vertexCount == 0) throw new InvalidOperationException("Cannot upload empty buffer");

        var buf = bufferData[bufferIndex] ?? throw new InvalidOperationException("Could not access buffer data for " + bufferIndex);
        var stride = _attrStrides[bufferIndex];

        Debug.Assert(buf.Length % stride == 0);
        Debug.Assert(_elemCounts[bufferIndex] == (buf.Length / stride));

        if (!_uploadedLayout[bufferIndex])
        {
            if (vertexArray == 0)
                vertexArray = gl.GenVertexArray();
            
            gl.BindVertexArray(vertexArray);
        }
        
        if (bufConfig.Usage == MeshBufferUsage.Static)
        {
            Debug.Assert(buffers[bufferIndex] == 0);

            var bufId = gl.GenBuffer();
            buffers[bufferIndex] = bufId;

            gl.BindBuffer(GLEnum.ArrayBuffer, bufId);
            gl.BufferData<byte>(GLEnum.ArrayBuffer, bufferData[bufferIndex], GLEnum.StaticDraw);

            // it is impossible to update a static buffer after creation,
            // so there is no need to retain the data
            bufferData[bufferIndex] = null!;
        }
        else if (bufConfig.Usage == MeshBufferUsage.Dynamic)
        {
            uint buffer;

            if (buffers[bufferIndex] == 0)
            {
                buffer = gl.GenBuffer();
                buffers[bufferIndex] = buffer;

                gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)bufferData[bufferIndex].Length, null, GLEnum.DynamicDraw);
                if (gl.GetError() == GLEnum.OutOfMemory)
                    throw new InsufficientBufferSpaceException();
            }
            else
            {
                buffer = buffers[bufferIndex];
                gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
            }

            gl.BufferSubData<byte>(GLEnum.ArrayBuffer, 0, bufferData[bufferIndex]);
        }
        else if (bufConfig.Usage == MeshBufferUsage.Transient)
        {
            uint buffer;

            if (buffers[bufferIndex] == 0)
            {
                buffer = gl.GenBuffer();
                buffers[bufferIndex] = buffer;

                gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
                gl.BufferData(GLEnum.ArrayBuffer, (nuint)bufferData[bufferIndex].Length, null, GLEnum.StreamDraw);
                if (gl.GetError() == GLEnum.OutOfMemory)
                    throw new InsufficientBufferSpaceException();
                _needResize[bufferIndex] = false;
            }
            else
            {
                buffer = buffers[bufferIndex];
                gl.BindBuffer(GLEnum.ArrayBuffer, buffer);
            }

            //gl.BufferSubData<byte>(GLEnum.ArrayBuffer, 0, bufferData[bufferIndex]);
        }
        else throw new UnreachableException();

        // set vertex attribute pointers
        if (!_uploadedLayout[bufferIndex])
        {
            _uploadedLayout[bufferIndex] = true;

            uint offset = 0;
            foreach (var attr in bufConfig.VertexAttributes)
            {
                GLEnum attribType;
                uint attrSize = 0;

                switch (attr.Type)
                {
                    case DataType.Byte:
                        attribType = GLEnum.UnsignedByte;
                        attrSize = attr.Count;
                        break;

                    case DataType.Short:
                        attribType = GLEnum.Short;
                        attrSize = attr.Count * 2;
                        break;

                    case DataType.Float:
                        attribType = GLEnum.Float;
                        attrSize = attr.Count * 4;
                        break;
                    
                    default:
                        throw new Exception("Invalid DataType enum");
                }

                gl.VertexAttribPointer(attr.Index, (int)attr.Count, attribType, attr.Normalized, stride, (nint)offset);
                gl.EnableVertexAttribArray(attr.Index);
                offset += attrSize;
            }
        }
    }

    /// <summary>
    /// Upload index buffer data to the GPU. Can be called again for the same buffer if it is dynamic.
    /// Calling it on a transient buffer has no effect, as it is updated on each draw call.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the buffer data could not be reuploaded.</exception>
    public unsafe void UploadIndices()
    {
        var gl = RenderContext.Gl;

        if (_config.Indexed)
        {
            if ((_config.Use32BitIndices && indexBufferData32 == null) || (!_config.Use32BitIndices && indexBufferData16 == null))
                throw new NullReferenceException("Index data was not set");
            
            var count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            if (count == 0) throw new InvalidOperationException("Cannot upload empty buffer");

            if (vertexArray == 0) vertexArray = gl.GenVertexArray();
            gl.BindVertexArray(vertexArray);

            ReadOnlySpan<byte> indexBufferData;
            if (_config.Use32BitIndices)
                indexBufferData = MemoryMarshal.Cast<uint, byte>(indexBufferData32);
            else
                indexBufferData = MemoryMarshal.Cast<ushort, byte>(indexBufferData16);
            
            if (_config.IndexBufferUsage == MeshBufferUsage.Static)
            {
                Debug.Assert(indexBuffer == 0);
                indexBuffer = gl.GenBuffer();
                gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
                gl.BufferData(GLEnum.ElementArrayBuffer, indexBufferData, GLEnum.StaticDraw);
                if (gl.GetError() == GLEnum.OutOfMemory)
                    throw new InsufficientBufferSpaceException();
            }
            else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
            {
                if (indexBuffer == 0)
                {
                    indexBuffer = gl.GenBuffer();
                    gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
                    gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)indexBufferData.Length, null, GLEnum.DynamicDraw);
                    if (gl.GetError() == GLEnum.OutOfMemory)
                        throw new InsufficientBufferSpaceException();
                }

                gl.BufferSubData(GLEnum.ElementArrayBuffer, 0, indexBufferData);
            }
            else if (_config.IndexBufferUsage == MeshBufferUsage.Transient)
            {
                if (indexBuffer == 0)
                {
                    indexBuffer = gl.GenBuffer();
                    gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
                    gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)indexBufferData.Length, null, GLEnum.StreamDraw);
                    if (gl.GetError() == GLEnum.OutOfMemory)
                        throw new InsufficientBufferSpaceException();
                }
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
            if (usage == MeshBufferUsage.Dynamic || buffers[i] == 0)
                Upload(i);
        }

        if (_config.Indexed)
        {
            var usage = _config.IndexBufferUsage;
            if (usage == MeshBufferUsage.Dynamic || (indexBuffer == 0))
                UploadIndices();
        }
    }

    internal unsafe void Draw()
    {
        // check if all buffers have been uploaded
        // and that they have the same element count
        uint vertexCount = uint.MaxValue;

        for (int i = 0; i < buffers.Length; i++)
        {
            if (_config.Buffers[i].Usage != MeshBufferUsage.Transient && buffers[i] == 0)
                throw new InvalidOperationException("Attempt to draw a Mesh that has not been fully uploaded.");
            
            if (vertexCount == uint.MaxValue)
            {
                vertexCount = _elemCounts[i];
            }
            else if (_elemCounts[i] != vertexCount)
            {
                throw new InvalidOperationException("One or more buffers have a different element count.");
            }
        }

        var drawMode = _config.PrimitiveType switch
        {
            MeshPrimitiveType.Points => GLEnum.Points,
            MeshPrimitiveType.LineStrip => GLEnum.LineStrip,
            MeshPrimitiveType.Lines => GLEnum.Lines,
            MeshPrimitiveType.TriangleStrip => GLEnum.TriangleStrip,
            MeshPrimitiveType.Triangles => GLEnum.Triangles,
            _ => throw new Exception("Invalid MeshPrimitiveType")
        };

        // activate vertex buffers
        var gl = RenderContext.Gl;

        Debug.Assert(vertexArray != 0);
        gl.BindVertexArray(vertexArray);

        // update transient buffers
        for (int i = 0; i < _config.Buffers.Count; i++)
        {
            var bufConfig = _config.Buffers[i];
            
            if (bufConfig.Usage == MeshBufferUsage.Transient)
            {
                var buf = buffers[i];

                if (!_reset)
                {
                    //Bgfx.set_transient_vertex_buffer((byte)i, &tvb, start, length);
                }
                else
                {
                    gl.BindBuffer(GLEnum.ArrayBuffer, buf);

                    if (_needResize[i])
                    {
                        gl.BufferData<byte>(GLEnum.ArrayBuffer, bufferData[i], GLEnum.StreamDraw);
                        _needResize[i] = false;
                    }
                    else
                    {
                        gl.BufferSubData<byte>(GLEnum.ArrayBuffer, 0, bufferData[i]);
                    }
                }
            }
            //else throw new Exception("Unreachable code");
        }

        // activate index buffer
        // and then do indexed draw
        var (baseVertex, elemCount) = _vertexSlice;
        if (_config.Indexed)
        {
            int count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            //gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
            // vao is bound, so element array buffer should be too

            //var (start, length) = _indexIndexSettings;
            //Debug.Assert(start >= 0 && start < count);
            //Debug.Assert(length >= 0 && (start + length) <= count);
            
            //if (_config.IndexBufferUsage == MeshBufferUsage.Static)
            //    Bgfx.set_index_buffer(staticIndexBuffer!.Value, 0, (uint)count);
            //else if (_config.IndexBufferUsage == MeshBufferUsage.Dynamic)
            //    Bgfx.set_dynamic_index_buffer(dynamicIndexBuffer!.Value, 0, (uint)count);
            if (_config.IndexBufferUsage == MeshBufferUsage.Transient)
            {
                if (!_reset)
                {
                    //tib = transientIndexBuffer ?? throw new Exception("TransientIndexBuffer is null, but reset == false");
                    //Bgfx.set_transient_index_buffer(&tib, start, length);
                }
                else
                {
                    ReadOnlySpan<byte> bufData;
                    if (_config.Use32BitIndices)
                        bufData = MemoryMarshal.Cast<uint, byte>(indexBufferData32!);
                    else
                        bufData = MemoryMarshal.Cast<ushort, byte>(indexBufferData16!);

                    if (_indexNeedResize)
                    {
                        gl.BufferData(GLEnum.ElementArrayBuffer, bufData, GLEnum.StreamDraw);
                        if (gl.GetError() == GLEnum.OutOfMemory)
                            throw new InsufficientBufferSpaceException();
                        _indexNeedResize = false;
                    }
                    else
                    {
                        gl.BufferSubData(GLEnum.ArrayBuffer, 0, bufData);
                    }
                }
            }
            //else throw new Exception("Unreachable code");
            
            if (IsBaseVertexSupported)
            {
                gl.DrawElementsBaseVertex(
                    drawMode,
                    elemCount,
                    _config.Use32BitIndices ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort,
                    (void*)_baseIndex,
                    (int)baseVertex
                );
            }
            else if (baseVertex == 0)
            {
                gl.DrawElements(
                    drawMode,
                    elemCount,
                    _config.Use32BitIndices ? DrawElementsType.UnsignedInt : DrawElementsType.UnsignedShort,
                    (void*)_baseIndex
                );
            }
            else throw new UnsupportedOperationException("Rendering backend does not support base vertex offset.");

            //Debug.Assert(start >= 0 && start < _elemCounts[i]);
            //Debug.Assert(length >= 0 && (start + length) <= _elemCounts[i]);
        }
        else
        {
            gl.DrawArrays(drawMode, (int)baseVertex, elemCount);
        }

        _reset = false;
    }

    internal void ResetSlice()
    {
        if (_config.Indexed)
        {
            var count = _config.Use32BitIndices ? indexBufferData32!.Length : indexBufferData16!.Length;
            _vertexSlice = (0, (uint)count);
        }
        else
        {
            // assume all buffers have the same element count...
            _vertexSlice = (0, _elemCounts[0]);
        }

        _baseIndex = 0;
        _reset = true;
    }

    internal void SetIndexedSlice(uint startIndex, uint startVertex, uint elemCount)
    {
        if (!_config.Indexed) throw new InvalidOperationException("Called SetIndexedSlice on a non-indexed mesh.");
        _vertexSlice = (startVertex, elemCount);
        _baseIndex = startIndex;
    }

    internal void SetSlice(uint start, uint elemCount)
    {
        if (_config.Indexed) throw new InvalidOperationException("Called SetSlice on an indexed mesh.");
        _vertexSlice = (start, elemCount);
    }

    protected override void FreeResources(bool disposing)
    {
        var gl = RenderContext.Gl;

        foreach (var buffer in buffers)
        {
            if (buffer != 0)
                gl.DeleteBuffer(buffer);
        }

        if (indexBuffer != 0)
            gl.DeleteBuffer(indexBuffer);
        
        if (vertexArray != 0)
            gl.DeleteVertexArray(vertexArray);
    }
}

public class StandardMesh : Mesh
{
    private static readonly MeshConfiguration Config = new MeshConfiguration()
        .AddBuffer(AttributeName.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.Color0, DataType.Float, 4, MeshBufferUsage.Static);

    private static readonly MeshConfiguration ConfigIndexed16 = new MeshConfiguration()
        .SetIndexed(false, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.Color0, DataType.Float, 4, MeshBufferUsage.Static);
    
    private static readonly MeshConfiguration ConfigIndexed32 = new MeshConfiguration()
        .SetIndexed(true, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.Position, DataType.Float, 3, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.TexCoord0, DataType.Float, 2, MeshBufferUsage.Static)
        .AddBuffer(AttributeName.Color0, DataType.Float, 4, MeshBufferUsage.Static);

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