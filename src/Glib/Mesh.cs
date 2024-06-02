using System.Numerics;
using Silk.NET.OpenGL;

namespace Glib;

public enum MeshBufferType
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
    Static, Dynamic, Stream
}

public struct MeshConfiguration
{
    private readonly List<MeshBufferType?> types;
    public bool Indexed = false;
    public MeshBufferUsage BufferUsage = MeshBufferUsage.Dynamic;
    public MeshPrimitiveType PrimitiveType = MeshPrimitiveType.Triangles;

    public readonly List<MeshBufferType?> Types => types;

    public MeshConfiguration()
    {
        types = [];
    }

    public MeshConfiguration(ReadOnlySpan<MeshBufferType> types, bool indexed)
    {
        this.types = [..types];
        Indexed = indexed;
    }

    public readonly void SetupBuffer(int index, MeshBufferType type)
    {
        while (types.Count < index)
            types.Add(null);
        types.Add(type);
    }
}

public class Mesh : GLResource
{   
    private readonly int[][] intBuffers;
    private readonly float[][] floatBuffers;
    private readonly int[] bufferIndices;

    private int[]? indexBuffer = null;

    private readonly MeshBufferType?[] types;
    private readonly bool indexed;
    private uint elemCount = 0;
    private readonly BufferUsageARB usage;
    private readonly PrimitiveType primitiveType;
    private bool ready = false;

    public bool IsReady => ready;

    private readonly GL gl;
    private readonly uint vao = 0;
    private readonly uint[] vbo = null!;
    private readonly uint ebo = 0;

    internal Mesh(GL gl, MeshConfiguration config)
    {
        this.gl = gl;

        // copy config options
        types = [..config.Types];
        indexed = config.Indexed;
        usage = config.BufferUsage switch
        {
            MeshBufferUsage.Static => BufferUsageARB.StaticDraw,
            MeshBufferUsage.Dynamic => BufferUsageARB.DynamicDraw,
            MeshBufferUsage.Stream => BufferUsageARB.StreamDraw,
            _ => throw new ArgumentException("Invalid MeshBufferUsage enum", nameof(config))
        };
        primitiveType = config.PrimitiveType switch
        {
            MeshPrimitiveType.Points => PrimitiveType.Points,
            MeshPrimitiveType.LineStrip => PrimitiveType.LineStrip,
            MeshPrimitiveType.Lines => PrimitiveType.Lines,
            MeshPrimitiveType.TriangleStrip => PrimitiveType.TriangleStrip,
            MeshPrimitiveType.TriangleFan => PrimitiveType.TriangleFan,
            MeshPrimitiveType.Triangles => PrimitiveType.Triangles,
            _ => throw new ArgumentException("Invalid MeshPrimitiveType enum value", nameof(config))
        };

        int intBufCount = 0;
        int floatBufCount = 0;
        bufferIndices = new int[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] is null)
            {
                bufferIndices[i] = -1;
                continue;
            }

            if (config.Types[i] == MeshBufferType.Int)
            {
                bufferIndices[i] = intBufCount++;
            }
            else
            {
                bufferIndices[i] = floatBufCount++;
            }
        }

        intBuffers = new int[intBufCount][];
        floatBuffers = new float[floatBufCount][];

        // create gl resources
        vao = gl.GenVertexArray();
        vbo = new uint[intBufCount + floatBufCount];
        if (indexed) ebo = gl.GenBuffer();

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] is null) continue;
            vbo[i] = gl.GenBuffer();
        }
    }

    public void SetIndexBufferData(ReadOnlySpan<int> data)
    {
        if (!indexed)
            throw new Exception("Attempt to set index data for a non-indexed Mesh");
        
        indexBuffer = new int[data.Length];
        data.CopyTo(indexBuffer);
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<int> data)
    {
        if (types[bufferIndex] != MeshBufferType.Int)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var intArr = new int[data.Length];
        data.CopyTo(intArr);
        intBuffers[bufferIndices[bufferIndex]] = intArr;
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<float> data)
    {
        if (types[bufferIndex] != MeshBufferType.Float)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var floatArr = new float[data.Length];
        data.CopyTo(floatArr);
        floatBuffers[bufferIndices[bufferIndex]] = floatArr;
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector2> data)
    {
        if (types[bufferIndex] != MeshBufferType.Vector2)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var floatArr = new float[data.Length * 2];
        floatBuffers[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
        }
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector3> data)
    {
        if (types[bufferIndex] != MeshBufferType.Vector3)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var floatArr = new float[data.Length * 3];
        floatBuffers[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
            floatArr[j++] = data[i].Z;
        }
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Vector4> data)
    {
        if (types[bufferIndex] != MeshBufferType.Vector4)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var floatArr = new float[data.Length * 4];
        floatBuffers[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].X;
            floatArr[j++] = data[i].Y;
            floatArr[j++] = data[i].Z;
            floatArr[j++] = data[i].W;
        }
    }

    public void SetBufferData(int bufferIndex, ReadOnlySpan<Color> data)
    {
        if (types[bufferIndex] != MeshBufferType.Vector4 && types[bufferIndex] != MeshBufferType.Color)
            throw new ArgumentException("The given data is not compatible with the buffer type", nameof(data));
        
        var floatArr = new float[data.Length * 4];
        floatBuffers[bufferIndices[bufferIndex]] = floatArr;

        int j = 0;
        for (int i = 0; i < data.Length; i++)
        {
            floatArr[j++] = data[i].R;
            floatArr[j++] = data[i].G;
            floatArr[j++] = data[i].B;
            floatArr[j++] = data[i].A;
        }
    }

    /// <summary>
    /// Upload mesh data to the GPU so that it can be drawn.
    /// </summary>
    /// <exception cref="Exception">Thrown if the buffer element counts are not the same.</exception>
    public unsafe void Upload()
    {
        // check that each buffer has the same amount of elements
        elemCount = uint.MaxValue;
        for (int i = 0; i < types.Length; i++)
        {
            // if this buffer is null, don't process it
            if (types[i] == MeshBufferType.Int && intBuffers[bufferIndices[i]] is null || floatBuffers[bufferIndices[i]] is null)
                continue;
            
            uint count = types[i] switch
            {
                MeshBufferType.Int => (uint)intBuffers[bufferIndices[i]].Length,
                MeshBufferType.Float => (uint)floatBuffers[bufferIndices[i]].Length,
                MeshBufferType.Vector2 => (uint)floatBuffers[bufferIndices[i]].Length / 2,
                MeshBufferType.Vector3 => (uint)floatBuffers[bufferIndices[i]].Length / 3,
                MeshBufferType.Vector4 or MeshBufferType.Color => (uint)floatBuffers[bufferIndices[i]].Length / 4,
                _ => throw new Exception("Invalid MeshBufferType enum value")
            };

            if (count != elemCount && elemCount != uint.MaxValue)
                throw new Exception("Mismatched mesh buffer sizes");
            elemCount = count;
        }

        gl.BindVertexArray(vao);

        for (uint i = 0; i < types.Length; i++)
        {
            var type = types[i];
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo[i]);

            if (type == MeshBufferType.Int)
            {
                var buf = intBuffers[i];
                if (buf is null) continue;

                fixed (int* data = buf)
                {
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(buf.Length * sizeof(int)), data, usage);
                }
            }
            else
            {
                var buf = floatBuffers[i];
                if (buf is null) continue;
                
                fixed (float* data = buf)
                {
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(buf.Length * sizeof(float)), data, usage);
                }
            }

            int size = 1;
            GLEnum glType;

            switch (type)
            {
                case MeshBufferType.Int:
                    size = 1;
                    glType = GLEnum.Int;
                    break;
                
                case MeshBufferType.Float:
                    size = 1;
                    glType = GLEnum.Float;
                    break;
                
                case MeshBufferType.Vector2:
                    size = 2;
                    glType = GLEnum.Float;
                    break;
                
                case MeshBufferType.Vector3:
                    size = 3;
                    glType = GLEnum.Float;
                    break;
                
                case MeshBufferType.Vector4:
                case MeshBufferType.Color:
                    size = 4;
                    glType = GLEnum.Float;
                    break;

                default:
                    throw new Exception("Invalid MeshBufferType enum");
            }

            gl.VertexAttribPointer(i, size, glType, false, 0, 0);
            gl.EnableVertexAttribArray(i);
        }

        if (indexed)
        {
            if (indexBuffer == null)
                throw new NullReferenceException("Index data was not set");
            
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (int* data = indexBuffer)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexBuffer.Length * sizeof(int)), data, usage);
            }
        }

        ready = true;
    }

    internal unsafe void Draw()
    {
        if (!ready)
            throw new Exception("Attempt to draw a Mesh that has not been uploaded.");
        
        gl.BindVertexArray(vao);

        if (indexed)
        {
            gl.DrawElements(primitiveType, (uint)indexBuffer!.Length, DrawElementsType.UnsignedInt, null);            
        }
        else
        {
            gl.DrawArrays(primitiveType, 0, elemCount);
        }
    }

    protected override void FreeResources(bool disposing)
    {
        Console.WriteLine("Free mesh handles");

        QueueFreeHandle(gl.DeleteVertexArray, vao);

        foreach (var handle in vbo)
        {
            QueueFreeHandle(gl.DeleteBuffer, handle);
        }

        if (indexed)
            QueueFreeHandle(gl.DeleteBuffer, ebo);
    }
}

public class StandardMesh : Mesh
{
    private static readonly MeshConfiguration Config = new([
        MeshBufferType.Vector3, // vertices
        MeshBufferType.Vector2, // uvs
        MeshBufferType.Color,   // colors
    ], false);

    private static readonly MeshConfiguration ConfigIndexed = new([
        MeshBufferType.Vector3, // vertices
        MeshBufferType.Vector2, // uvs
        MeshBufferType.Color,   // colors
    ], true);

    internal StandardMesh(GL gl, bool indexed) : base(gl, indexed ? ConfigIndexed : Config)
    {}

    public void SetVertices(ReadOnlySpan<Vector3> vertices)
        => SetBufferData(0, vertices);

    public void SetTexCoords(ReadOnlySpan<Vector2> uvs)
        => SetBufferData(1, uvs);
    
    public void SetColors(ReadOnlySpan<Color> colors)
        => SetBufferData(2, colors);
}