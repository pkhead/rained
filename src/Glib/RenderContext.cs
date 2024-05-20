namespace Glib;

using Silk.NET.Windowing;
using Silk.NET.OpenGL;

using System.Drawing;
using System.Numerics;

public class RenderContext : IDisposable
{
    private readonly GL gl;
    private bool _disposed = false;

    // batch variables
    private const uint VertexDataSize = 9;
    private const uint MaxVertices = 1024;

    private readonly float[] batchData;
    private uint numVertices = 0;
    private readonly uint batchBuffer;
    private readonly uint batchVertexArray;

    private readonly Shader defaultShader;
    private Shader? shaderValue;

    public Matrix4x4 TransformMatrix;
    public Color BackgroundColor = Color.Black;
    public Color DrawColor = Color.White;
    public Vector2 UV = Vector2.Zero;
    public Shader? Shader {
        get => shaderValue;
        set
        {
            if (shaderValue == value) return;
            DrawBatch();
            shaderValue = value;
        }
    }

    internal unsafe RenderContext(IWindow window)
    {
        gl = GL.GetApi(window);
        gl.Disable(EnableCap.CullFace);

        // create default shader
        defaultShader = new Shader(gl);

        batchData = new float[MaxVertices * VertexDataSize];

        // create batch buffer
        batchVertexArray = gl.CreateVertexArray();
        batchBuffer = gl.CreateBuffer();
        gl.BindVertexArray(batchVertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, batchBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, MaxVertices * VertexDataSize*sizeof(float), null, BufferUsageARB.StreamDraw);
        
        gl.VertexAttribPointer(0, 3, GLEnum.Float, false, VertexDataSize*sizeof(float), 0); // vertices
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, GLEnum.Float, false, VertexDataSize*sizeof(float), 3*sizeof(float)); // uvs
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(2, 4, GLEnum.Float, false, VertexDataSize*sizeof(float), 5*sizeof(float)); // colors
        gl.EnableVertexAttribArray(2);

        TransformMatrix = Matrix4x4.Identity;
    }

    /*~Graphics()
    {
        Dispose(false);
    }*/

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                defaultShader.Dispose();
            }

            gl.DeleteBuffer(batchBuffer);
            gl.DeleteVertexArray(batchVertexArray);

            _disposed = true;
        }
    }

    internal void Resize(uint w, uint h)
    {
        gl.Viewport(0, 0, w, h);
    }

    public void ClearBackground(Color color)
    {
        gl.ClearColor(color);
        gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void ClearBackground()
    {
        gl.ClearColor(BackgroundColor);
        gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public Shader CreateShader(string? vsSource = null, string? fsSource = null)
    {
        return new Shader(gl, vsSource, fsSource);
    }

    public unsafe void DrawBatch()
    {
        if (numVertices == 0) return;

        gl.BindVertexArray(batchVertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, batchBuffer);

        fixed (float* data = batchData)
        {
            gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, numVertices * VertexDataSize*sizeof(float), data);
        }

        var shader = shaderValue ?? defaultShader;
        shader.Use(gl);
        shader.SetUniform("uTransformMatrix", TransformMatrix);
        gl.DrawArrays(GLEnum.Triangles, 0, numVertices);
        numVertices = 0;
    }

    private void CheckCapacity(uint newVertices)
    {
        if (numVertices + newVertices >= MaxVertices)
        {
            Console.WriteLine("Batch was full - flush batch");
            DrawBatch();
        }
    }

    private void PushVertex(float x, float y)
    {
        CheckCapacity(1);

        uint i = numVertices * VertexDataSize;
        batchData[i++] = x;
        batchData[i++] = y;
        batchData[i++] = 0f;
        batchData[i++] = UV.X;
        batchData[i++] = UV.Y;
        batchData[i++] = DrawColor.R / 255f;
        batchData[i++] = DrawColor.G / 255f;
        batchData[i++] = DrawColor.B / 255f;
        batchData[i++] = DrawColor.A / 255f;

        numVertices++;
    }

    public void FillRect(float x, float y, float w, float h)
    {
        CheckCapacity(6);
        PushVertex(x, y);
        PushVertex(x, y+h);
        PushVertex(x+w, y);
        
        PushVertex(x+w, y);
        PushVertex(x, y+h);
        PushVertex(x+w, y+h);
    }
}