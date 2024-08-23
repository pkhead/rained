//using Bgfx_cs;
using System.Numerics;
using System.Runtime.InteropServices;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace Glib;

/// <summary>
/// Class used to handle shape drawing
/// </summary>
internal class DrawBatch
{
    private const uint VertexDataSize = 9;
    private const uint MaxVertices = 4096;
    private const uint MaxIndices = 4096;
    
    private readonly float[] batchData;
    private readonly uint[] batchIndices;
    private int vertexCount;
    private int indexCount;

    private uint _vertexArray;
    private uint _vtxBuffer;
    private uint _idxBuffer;

    private MeshPrimitiveType _drawMode;
    public Color DrawColor = Color.White;
    public Vector2 UV = Vector2.Zero;
    public Matrix4x4 TransformMatrix;

    public int CurrentIndex = 0;
    
    private Texture? _texture;
    public Texture? Texture
    {
        get => _texture;
        set
        {
            if (_texture == value) return;
            Draw();
            _texture = value;
        }
    }

    private Shader? _shader;
    public Shader? Shader
    {
        get => _shader;
        set
        {
            if (_shader == value) return;
            Draw();
            _shader = value;
        }
    }

    public Action DrawCallback { get; set; }

    public unsafe DrawBatch(Action drawCallback)
    {
        var gl = RenderContext.Gl;

        batchData = new float[MaxVertices * VertexDataSize];
        batchIndices = new uint[MaxIndices];
        vertexCount = 0;
        indexCount = 0;
        CurrentIndex = 0;
        _texture = null!;
        DrawCallback = drawCallback;

        _vertexArray = gl.GenVertexArray();
        gl.BindVertexArray(_vertexArray);

        _vtxBuffer = gl.GenBuffer();
        gl.BindBuffer(GLEnum.ArrayBuffer, _vtxBuffer);
        gl.BufferData(GLEnum.ArrayBuffer, (nuint)batchData.Length * sizeof(float), null, GLEnum.StreamDraw);
        GlUtil.CheckError(gl, "Could not create DrawBatch");

        _idxBuffer = gl.GenBuffer();
        gl.BindBuffer(GLEnum.ElementArrayBuffer, _idxBuffer);
        gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)batchIndices.Length * sizeof(uint), null, GLEnum.StreamDraw);
        GlUtil.CheckError(gl, "Could not create DrawBatch");

        var byteStride = VertexDataSize * sizeof(float);
        
        gl.VertexAttribPointer((uint)AttributeName.Position, 3, GLEnum.Float, false, byteStride, 0);
        gl.EnableVertexAttribArray((uint)AttributeName.Position);

        gl.VertexAttribPointer((uint)AttributeName.TexCoord0, 2, GLEnum.Float, false, byteStride, 3*sizeof(float));
        gl.EnableVertexAttribArray((uint)AttributeName.TexCoord0);

        gl.VertexAttribPointer((uint)AttributeName.Color0, 4, GLEnum.Float, false, byteStride, 5*sizeof(float));
        gl.EnableVertexAttribArray((uint)AttributeName.Color0);

        GlUtil.CheckError(gl, "Could not create DrawBatch");
    }

    public void NewFrame(Texture initialTex)
    {
        vertexCount = 0;
        _texture = initialTex;
        _shader = null;

        DrawColor = Color.White;
        UV = Vector2.Zero;
    }

    public unsafe void Draw()
    {
        if (vertexCount == 0) return;

        var gl = RenderContext.Gl;

        // setup additional state
        DrawCallback();

        gl.BindVertexArray(_vertexArray);

        // update vertex buffer
        gl.BindBuffer(GLEnum.ArrayBuffer, _vtxBuffer);
        fixed (float* data = batchData)
        {
            gl.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)vertexCount * VertexDataSize * sizeof(float), data);
        }

        // update index buffer
        gl.BindBuffer(GLEnum.ElementArrayBuffer, _idxBuffer);
        fixed (uint* data = batchIndices)
        {
            gl.BufferSubData(GLEnum.ElementArrayBuffer, 0, (nuint)indexCount * sizeof(uint), data);
        }

        // draw buffers
        var mode = _drawMode switch
        {
            MeshPrimitiveType.Lines => GLEnum.Lines,
            MeshPrimitiveType.LineStrip => GLEnum.LineStrip,
            MeshPrimitiveType.Points => GLEnum.Points,
            MeshPrimitiveType.Triangles => GLEnum.Triangles,
            MeshPrimitiveType.TriangleStrip => GLEnum.TriangleStrip,
            _ => throw new Exception("Invalid MeshPrimitiveType")
        };

        gl.DrawElements(mode, (uint)indexCount, DrawElementsType.UnsignedInt, (void*)0);
        vertexCount = 0;
        indexCount = 0;
        CurrentIndex = 0;
    }

    private void CheckCapacity(uint newVertices, uint numIndices)
    {
        if (vertexCount + newVertices >= MaxVertices || indexCount + numIndices >= MaxIndices)
        {
            Draw();
        }
    }

    internal void BeginDraw(uint requiredCapacity, uint numIndices, MeshPrimitiveType newDrawMode = MeshPrimitiveType.Triangles)
    {
        CheckCapacity(requiredCapacity, numIndices);

        // flush batch on texture/draw mode change
        if (_drawMode != newDrawMode)
        {
            Draw();
            _drawMode = newDrawMode;
        }
    }

    internal void PushVertex(float x, float y)
    {
        var vec = Vector4.Transform(new Vector4(x, y, 0f, 1f), TransformMatrix);

        uint i = (uint)vertexCount * VertexDataSize;
        batchData[i++] = vec.X / vec.W;
        batchData[i++] = vec.Y / vec.W;
        batchData[i++] = vec.Z / vec.W;
        batchData[i++] = UV.X;
        batchData[i++] = UV.Y;
        batchData[i++] = DrawColor.R;
        batchData[i++] = DrawColor.G;
        batchData[i++] = DrawColor.B;
        batchData[i++] = DrawColor.A;

        vertexCount++;
    }

    internal void PushIndex(int idx)
    {
        batchIndices[indexCount++] = (uint)idx;
    }

    public BatchDrawHandle BeginBatchDraw(BatchDrawMode mode, Texture? tex = null)
    {
        Texture = tex;
        return new BatchDrawHandle(mode, this);
    }
}

public class BatchDrawHandle : IDisposable
{
    private readonly static Vector2[] verts = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero];
    private readonly static Vector2[] uvs = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero,];
    private readonly static Color[] colors = [Glib.Color.Transparent, Glib.Color.Transparent, Glib.Color.Transparent, Glib.Color.Transparent];

    private DrawBatch _batch;
    private int vertIndex = 0;
    private readonly BatchDrawMode mode;
    private Vector2 uv;
    private Color color;

    internal BatchDrawHandle(BatchDrawMode mode, DrawBatch batch)
    {
        this.mode = mode;
        this._batch = batch;

        uv = Vector2.Zero;
        color = batch.DrawColor;
    }

    private bool IsFull()
    {
        return mode switch
        {
            BatchDrawMode.Lines => vertIndex >= 2,
            BatchDrawMode.Triangles => vertIndex >= 3,
            BatchDrawMode.Quads => vertIndex >= 4,
            _ => false,
        };
    }

    public void Flush()
    {
        switch (mode)
        {
            case BatchDrawMode.Triangles:
            {
                _batch.BeginDraw(3, 3, MeshPrimitiveType.Triangles);
                for (int i = 0; i < 3; i++)
                {
                    _batch.DrawColor = colors[i];
                    _batch.UV = uvs[i];
                    _batch.PushVertex(verts[i].X, verts[i].Y);
                    _batch.PushIndex(_batch.CurrentIndex++);
                }
                break;
            }

            case BatchDrawMode.Quads:
            {
                _batch.BeginDraw(4, 6, MeshPrimitiveType.Triangles);

                _batch.DrawColor = colors[0];
                _batch.UV = uvs[0];
                _batch.PushVertex(verts[0].X, verts[0].Y);

                _batch.DrawColor = colors[1];
                _batch.UV = uvs[1];
                _batch.PushVertex(verts[1].X, verts[1].Y);

                _batch.DrawColor = colors[2];
                _batch.UV = uvs[2];
                _batch.PushVertex(verts[2].X, verts[2].Y);

                _batch.DrawColor = colors[3];
                _batch.UV = uvs[3];
                _batch.PushVertex(verts[3].X, verts[3].Y);

                int idx = _batch.CurrentIndex;
                _batch.PushIndex(idx + 0);
                _batch.PushIndex(idx + 1);
                _batch.PushIndex(idx + 2);
                _batch.PushIndex(idx + 2);
                _batch.PushIndex(idx + 3);
                _batch.PushIndex(idx + 0);
                _batch.CurrentIndex += 4;
                break;
            }

            case BatchDrawMode.Lines:
            {
                _batch.BeginDraw(2, 2, MeshPrimitiveType.Lines);

                _batch.DrawColor = colors[0];
                _batch.UV = uvs[0];
                _batch.PushVertex(verts[0].X, verts[0].Y);

                _batch.DrawColor = colors[1];
                _batch.UV = uvs[1];
                _batch.PushVertex(verts[1].X, verts[1].Y);

                _batch.PushIndex(_batch.CurrentIndex++);
                _batch.PushIndex(_batch.CurrentIndex++);
                break;
            }
        }

        vertIndex = 0;
    }

    public void Vertex(Vector2 v)
        => Vertex(v.X, v.Y);

    public void Vertex(float x, float y)
    {
        if (IsFull()) Flush();
        uvs[vertIndex] = uv;
        colors[vertIndex] = color;
        verts[vertIndex] = new Vector2(x, y);
        vertIndex++;
    }

    public void TexCoord(float u, float v)
    {
        uv = new Vector2(u, v);
    }

    public void TexCoord(Vector2 uv)
    {
        this.uv = uv;
    }

    public void Color(Color color)
    {
        this.color = color;
    }

    public void End()
    {
        if (IsFull()) Flush();
    }

    public void Dispose() => End();
}