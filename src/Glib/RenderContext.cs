namespace Glib;

using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Numerics;

[Flags]
public enum ClearFlags
{
    Color, Depth, Stencil
}

public enum BlendMode
{
    Normal,
    Add
}

public enum CullMode {
    Front,
    Back,
    FrontAndBack
}

public enum Feature
{
    Blend,
    ScissorTest,
    DepthTest,
    CullFace
}

public class RenderContext : IDisposable
{
    internal readonly GL gl;
    private bool _disposed = false;

    // batch variables
    private const uint VertexDataSize = 9;
    private const uint MaxVertices = 1024;

    private readonly float[] batchData;
    private uint numVertices = 0;
    private readonly uint batchBuffer;
    private readonly uint batchVertexArray;

    private int ScreenWidth = 0;
    private int ScreenHeight = 0;

    private readonly Texture whiteTexture;

    private Texture curTexture;
    private Texture lastTexture;

    private readonly Shader defaultShader;
    private Shader? shaderValue;

    private readonly Stack<Matrix4x4> transformStack = [];
    private readonly Stack<Framebuffer> framebufferStack = [];
    private Framebuffer? curFramebuffer = null;

    internal Matrix4x4 BaseTransform = Matrix4x4.Identity;
    public Matrix4x4 TransformMatrix;
    public Color BackgroundColor = Color.Black;
    public Color DrawColor = Color.White;
    public float LineWidth = 1.0f;
    private Vector2 UV = Vector2.Zero;
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
        //gl.Enable(EnableCap.CullFace);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // create default shader
        defaultShader = new Shader(gl);

        // create default texture
        var img = new Image([255, 255, 255, 255], 1, 1, PixelFormat.RGBA);
        whiteTexture = CreateTexture(img);
        defaultShader.SetUniform("uTexture", whiteTexture);
        curTexture = whiteTexture;
        lastTexture = whiteTexture;

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

    public void Clear(Color color)
    {
        gl.ClearColor(color.R, color.G, color.B, color.A);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    public void Clear() => Clear(BackgroundColor);

    public void Clear(ClearFlags flags)
        => Clear(BackgroundColor, flags);

    public void Clear(Color bgColor, ClearFlags flags)
    {
        gl.ClearColor(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
        ClearBufferMask glFlags = 0;
        if (flags.HasFlag(ClearFlags.Color)) glFlags |= ClearBufferMask.ColorBufferBit;
        if (flags.HasFlag(ClearFlags.Depth)) glFlags |= ClearBufferMask.DepthBufferBit;
        if (flags.HasFlag(ClearFlags.Stencil)) glFlags |= ClearBufferMask.StencilBufferBit;

        if (glFlags != 0)
            gl.Clear(glFlags);
    }

    internal void Begin(int width, int height)
    {
        ScreenWidth = width;
        ScreenHeight = height;
        SetViewport();
        
        Clear();
        ClearTransformationStack();
        ResetTransform();
        ResetScissorBounds();

        shaderValue = null;
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        framebufferStack.Clear();
        curFramebuffer = null;

        defaultShader.SetUniform("uTexture", whiteTexture);
        defaultShader.SetUniform("uColor", Color.White);
        curTexture = whiteTexture;
        lastTexture = whiteTexture;
    }

    internal void End()
    {
        DrawBatch();
        SetTexture(null);
    }

    public void SetScissorBounds(int x, int y, int w, int h)
    {
        gl.Scissor(x, y, (uint)w, (uint)h);
    }

    /// <summary>
    /// Resets the scissor bounds to the window size
    /// </summary>
    public void ResetScissorBounds()
    {
        gl.Scissor(0, 0, (uint)ScreenWidth, (uint)ScreenHeight);
    }

    /// <summary>
    /// Enable/disable a certain feature
    /// </summary>
    /// <param name="feature"></param>
    public void SetEnabled(Feature feature, bool enabled)
    {
        switch (feature)
        {
            case Feature.Blend:
                if (enabled)
                    gl.Enable(EnableCap.Blend);
                else
                    gl.Disable(EnableCap.Blend);
                break;

            case Feature.ScissorTest:
                if (enabled)
                    gl.Enable(EnableCap.ScissorTest);
                else
                    gl.Disable(EnableCap.ScissorTest);
                break;

            case Feature.DepthTest:
                if (enabled)
                    gl.Enable(EnableCap.DepthTest);
                else
                    gl.Disable(EnableCap.DepthTest);
                break;

            case Feature.CullFace:
                if (enabled)
                    gl.Enable(EnableCap.CullFace);
                else
                    gl.Disable(EnableCap.CullFace);
                break;
        }
    }

    public void SetCullMode(CullMode mode)
    {
        gl.CullFace(mode switch
        {
            CullMode.Front => GLEnum.Front,
            CullMode.Back => GLEnum.Back,
            CullMode.FrontAndBack => GLEnum.FrontAndBack,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)) 
        });
    }

    public void SetBlendMode(BlendMode mode)
    {
        switch (mode)
        {
            case BlendMode.Normal:
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;

            case BlendMode.Add:
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
        }
    }

    /*public void SetBlendFactorsSeparate()
    {
        gl.BlendEquationSeparate()    
    }*/

    /// <summary>
    /// Translate the transformation matrix
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void Translate(float x, float y, float z)
    {
        TransformMatrix = Matrix4x4.CreateTranslation(x, y, z) * TransformMatrix;
    }

    /// <summary>
    /// Translate the transformation matrix
    /// </summary>
    /// <param name="vec"></param>
    public void Translate(Vector3 vec)
    {
        TransformMatrix = Matrix4x4.CreateTranslation(vec) * TransformMatrix;
    }

    /// <summary>
    /// Rotate the transformation matrix
    /// </summary>
    /// <param name="angle"></param>
    public void RotateX(float angle)
        => TransformMatrix = Matrix4x4.CreateRotationX(angle) * TransformMatrix;

    /// <summary>
    /// Rotate the transformation matrix
    /// </summary>
    /// <param name="angle"></param>
    public void RotateY(float angle)
        => TransformMatrix = Matrix4x4.CreateRotationY(angle) * TransformMatrix;

    /// <summary>
    /// Rotate the transformation matrix
    /// </summary>
    /// <param name="angle"></param>
    public void RotateZ(float angle)
        => TransformMatrix = Matrix4x4.CreateRotationZ(angle) * TransformMatrix;

    /// <summary>
    /// Rotate the transformation matrix in 2D
    /// </summary>
    /// <param name="angle"></param>
    public void Rotate(float angle)
        => TransformMatrix = Matrix4x4.CreateRotationZ(angle) * TransformMatrix;

    /// <summary>
    /// Scale the transformation matrix
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void Scale(float x, float y, float z)
        => TransformMatrix = Matrix4x4.CreateScale(x, y, z) * TransformMatrix;

    /// <summary>
    /// Scale the transformation matrix
    /// </summary>
    /// <param name="scale"></param>
    public void Scale(Vector3 scale)
        => TransformMatrix = Matrix4x4.CreateScale(scale) * TransformMatrix;
    
    public void ResetTransform()
        => TransformMatrix = Matrix4x4.Identity;

    public void ClearTransformationStack()
        => transformStack.Clear();

    /// <summary>
    /// Push the current transformation matrix to the
    /// transform stack.
    /// </summary>
    public void PushTransform()
    {
        transformStack.Push(TransformMatrix);
    }

    /// <summary>
    /// Pop a matrix from the transformation stack, and set it to
    /// the current transformation matrix.
    /// </summary>
    public void PopTransform()
    {
        TransformMatrix = transformStack.Pop();
    }

    /// <summary>
    /// Create a shader.
    /// </summary>
    /// <param name="vsSource">The vertex shader source. If null, will use the default vertex shader.</param>
    /// <param name="fsSource">The fragment shader source. If null, will use the default fragment shader.</param>
    /// <returns></returns>
    public Shader CreateShader(string? vsSource = null, string? fsSource = null)
        => new(gl, vsSource, fsSource);
    
    /// <summary>
    /// Create a mesh with a custom buffer setup.
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public Mesh CreateMesh(MeshConfiguration config)
        => new(gl, config);
    
    /// <summary>
    /// Create a mesh with a standard buffer setup.
    /// </summary>
    /// <param name="indexed"></param>
    /// <returns></returns>
    public StandardMesh CreateMesh(bool indexed = false)
        => new(gl, indexed);
    
    /// <summary>
    /// Create a texture from an Image.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="mipmaps"></param>
    /// <returns></returns>
    public Texture CreateTexture(Image image, bool mipmaps = false)
        => new(gl, image, mipmaps);
    
    public Framebuffer CreateFramebuffer(FramebufferConfiguration config)
    {
        var buffer = new Framebuffer(gl, config);

        // reset framebuffer to the framebuffer it was in before
        // as creating a new framebuffer requires binding it
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, curFramebuffer?.Handle ?? 0);

        return buffer;
    }

    public void SetViewport(int width, int height)
    {
        gl.Viewport(0, 0, (uint)width, (uint)height);
        BaseTransform =
            Matrix4x4.CreateScale(new Vector3(1f / width * 2f, 1f / height * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, -1f, 0f));
    }

    public void SetViewport()
    {
        gl.Viewport(0, 0, (uint)ScreenWidth, (uint)ScreenHeight);
        BaseTransform =
            Matrix4x4.CreateScale(new Vector3(1f / ScreenWidth * 2f, -1f / ScreenHeight * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, 1f, 0f));
    }
    
    /// <summary>
    /// Push the framebuffer to the stack and set it
    /// as the current framebuffer.
    /// </summary>
    /// <param name="buffer"></param>
    public void PushFramebuffer(Framebuffer buffer)
    {
        DrawBatch();
        framebufferStack.Push(buffer);
        gl.BindFramebuffer(GLEnum.Framebuffer, buffer.Handle);
        curFramebuffer = buffer;

        gl.Viewport(0, 0, (uint)buffer.Width, (uint)buffer.Height);
        SetViewport(buffer.Width, buffer.Height);
    }

    /// <summary>
    /// Pops a framebuffer from the stack.
    /// <returns>The previously bound framebuffer.</returns>
    /// </summary>
    public Framebuffer? PopFramebuffer()
    {
        DrawBatch();
        if (framebufferStack.Count == 0) return null;
        var ret = framebufferStack.Pop();

        if (framebufferStack.TryPeek(out Framebuffer? newBuffer))
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, newBuffer.Handle);
            SetViewport(newBuffer.Width, newBuffer.Height);
            curFramebuffer = newBuffer;
        }
        else
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            SetViewport();
            curFramebuffer = null;
        }

        return ret;
    }

    /// <summary>
    /// Load a texture from a file path.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="mipmaps"></param>
    /// <returns></returns>
    public Texture LoadTexture(string filePath, bool mipmaps = false)
        => new(gl, Image.FromFile(filePath), mipmaps);
    
    /// <summary>
    /// Draw a mesh.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    public void Draw(Mesh mesh)
    {
        DrawBatch();

        var shader = shaderValue ?? defaultShader;
        shader.Use(gl);

        if (shader.HasUniform("uTransformMatrix"))
            shader.SetUniform("uTransformMatrix", TransformMatrix * BaseTransform);

        if (shader == defaultShader)
        {
            shader.SetUniform("uTexture", whiteTexture);
            shader.SetUniform("uColor", DrawColor);
        }
        
        mesh.Draw();
    }

    /// <summary>
    /// Draw a textured rectangle. Note that for the src coordinates,
    /// (0, 0) is the bottom-left of the texture, while for the dst
    /// coordinates, (0, 0) is the top-left of the render buffer.
    /// </summary>
    /// <param name="tex">The texture to draw</param>
    /// <param name="srcX"></param>
    /// <param name="srcY"></param>
    /// <param name="srcW"></param>
    /// <param name="srcH"></param>
    /// <param name="dstX"></param>
    /// <param name="dstY"></param>
    /// <param name="dstW"></param>
    /// <param name="dstH"></param>
    public void Draw(
        Texture tex,
        float srcX, float srcY, float srcW, float srcH,
        float dstX, float dstY, float dstW, float dstH
    )
    {
        SetTexture(tex);

        var uvLeft = srcX / tex.Width;
        var uvRight = (srcX + srcW) / tex.Width;
        var uvBottom = (srcY + srcH) / tex.Height;
        var uvTop = srcY / tex.Height;
        
        BeginBatchDraw(6);

        // first triangle
        UV.X = uvLeft;
        UV.Y = uvTop;
        PushVertex(dstX, dstY);

        UV.X = uvLeft;
        UV.Y = uvBottom;
        PushVertex(dstX, dstY + dstH);

        UV.X = uvRight;
        UV.Y = uvBottom;
        PushVertex(dstX + dstW, dstH);

        // second triangle
        // uv is the same as the last vertex
        PushVertex(dstX + dstW, dstH);

        UV.X = uvLeft;
        UV.Y = uvTop;
        PushVertex(dstX, dstY);

        UV.X = uvRight;
        UV.Y = uvTop;
        PushVertex(dstX + dstW, dstY);
    }

    public void Draw(Texture tex, Rectangle src, Rectangle dst)
        => Draw(tex, src.X, src.Y, src.Width, src.Height, dst.X, dst.Y, dst.Width, dst.Height);

    public void Draw(Texture tex, float x, float y)
        => Draw(tex, 0, 0, tex.Width, tex.Height, x, y, tex.Width, tex.Height);

    public void Draw(Texture tex, float x, float y, float w, float h)
        => Draw(tex, 0, 0, tex.Width, tex.Height, x, y, w, h);

    public void Draw(Texture tex, Vector2 pos)
        => Draw(tex, pos.X, pos.Y);

    public void Draw(Texture tex, Vector2 pos, Vector2 scale)
        => Draw(tex, 0, 0, tex.Width, tex.Height, pos.X, pos.Y, tex.Width * scale.X, tex.Height * scale.Y);
    
    public void Draw(Texture tex, Rectangle rect)
        => Draw(tex, rect.X, rect.Y, rect.Width, rect.Height);
    
    public void Draw(Texture tex)
        => Draw(tex, 0f, 0f);

    private void SetTexture(Texture? newTex)
    {
        curTexture = newTex ?? whiteTexture;
    }

    private void BeginBatchDraw(uint requiredCapacity)
    {
        CheckCapacity(requiredCapacity);

        // flush batch on texture change
        if (curTexture != lastTexture)
        {
            DrawBatch();
            lastTexture = curTexture;
        }
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
        
        if (shader.HasUniform("uTransformMatrix"))
            shader.SetUniform("uTransformMatrix", BaseTransform); // vertices are already transformed

        if (shader == defaultShader)
        {
            shader.SetUniform("uTexture", lastTexture);
            shader.SetUniform("uColor", Color.White); // color is in mesh data
        }
        
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
        var vec = Vector4.Transform(new Vector4(x, y, 0f, 1f), TransformMatrix);

        uint i = numVertices * VertexDataSize;
        batchData[i++] = vec.X / vec.W;
        batchData[i++] = vec.Y / vec.W;
        batchData[i++] = vec.Z / vec.W;
        batchData[i++] = UV.X;
        batchData[i++] = UV.Y;
        batchData[i++] = DrawColor.R;
        batchData[i++] = DrawColor.G;
        batchData[i++] = DrawColor.B;
        batchData[i++] = DrawColor.A;

        numVertices++;
    }

    public void DrawTriangle(float x0, float y0, float x1, float y1, float x2, float y2)
    {
        SetTexture(null);

        BeginBatchDraw(3);
        PushVertex(x0, y0);
        PushVertex(x1, y1);
        PushVertex(x2, y2);
    }

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c) => DrawTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y);

    public void DrawRectangle(float x, float y, float w, float h)
    {
        SetTexture(null);

        BeginBatchDraw(6);
        PushVertex(x, y);
        PushVertex(x, y+h);
        PushVertex(x+w, y);
        
        PushVertex(x+w, y);
        PushVertex(x, y+h);
        PushVertex(x+w, y+h);
    }

    public void DrawRectangle(Vector2 origin, Vector2 size) => DrawRectangle(origin.X, origin.Y, size.X, size.Y);
    public void DrawRectangle(Rectangle rectangle) => DrawRectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);

    public void DrawLine(float x0, float y0, float x1, float y1)
    {
        SetTexture(null);

        var dx = x1 - x0;
        var dy = y1 - y0;
        if (dx == 0f && dy == 0f) return;

        var dist = MathF.Sqrt(dx*dx + dy*dy);

        var perpX = dy / dist * LineWidth / 2f;
        var perpY = -dx / dist * LineWidth / 2f;

        BeginBatchDraw(6);
        PushVertex(x0 + perpX, y0 + perpY);
        PushVertex(x0 - perpX, y0 - perpY);
        PushVertex(x1 - perpX, y1 - perpY);
        PushVertex(x1 + perpX, y1 + perpY);
        PushVertex(x0 + perpX, y0 + perpY);
        PushVertex(x1 - perpX, y1 - perpY);
    }

    public void DrawLine(Vector2 a, Vector2 b) => DrawLine(a.X, a.Y, b.X, b.Y);

    public void DrawRectangleLines(float x, float y, float w, float h)
    {
        DrawRectangle(x, y, w, LineWidth); // top side
        DrawRectangle(x, y+LineWidth, LineWidth, h-LineWidth); // left side
        DrawRectangle(x, y+h-LineWidth, w-LineWidth, LineWidth); // bottom side
        DrawRectangle(x+w-LineWidth, y+LineWidth, LineWidth, h-LineWidth); // right side
    }

    private const float SmoothCircleErrorRate = 0.5f;

    public void DrawCircleSector(float x0, float y0, float radius, float startAngle, float endAngle, int segments)
    {
        SetTexture(null);

        // copied from raylib code
        if (radius <= 0f) radius = 0.1f; // Avoid div by zero

        // expects (endAngle > startAngle)
        // if not, swap
        if (endAngle < startAngle)
        {
            (endAngle, startAngle) = (startAngle, endAngle);
        }

        int minSegments = (int)MathF.Ceiling((endAngle - startAngle) / (MathF.PI / 2f));
        if (segments < minSegments)
        {
            // calc the max angle between segments based on the error rate (usually 0.5f)
            float th = MathF.Acos(2f * MathF.Pow(1f - SmoothCircleErrorRate/radius, 2f) - 1f);
            segments = (int)((endAngle - startAngle) * MathF.Ceiling(2f * MathF.PI / th) / (2f * MathF.PI));
            if (segments <= 0) segments = minSegments;
        }

        float stepLength = (float)(endAngle - startAngle) / segments;
        float angle = startAngle;

        for (int i = 0; i < segments; i++)
        {
            BeginBatchDraw(3);
            PushVertex(x0, y0);
            PushVertex(x0 + MathF.Cos(angle + stepLength) * radius, y0 + MathF.Sin(angle + stepLength) * radius);
            PushVertex(x0 + MathF.Cos(angle) * radius, y0  + MathF.Sin(angle) * radius);
            angle += stepLength;
        }
    }

    public void DrawRingSector(float x0, float y0, float radius, float startAngle, float endAngle, int segments)
    {
        SetTexture(null);

        // copied from raylib code
        if (radius <= 0f) radius = 0.1f; // Avoid div by zero

        // expects (endAngle > startAngle)
        // if not, swap
        if (endAngle < startAngle)
        {
            (endAngle, startAngle) = (startAngle, endAngle);
        }

        int minSegments = (int)MathF.Ceiling((endAngle - startAngle) / (MathF.PI / 2f));
        if (segments < minSegments)
        {
            // calc the max angle between segments based on the error rate (usually 0.5f)
            float th = MathF.Acos(2f * MathF.Pow(1f - SmoothCircleErrorRate/radius, 2f) - 1f);
            segments = (int)((endAngle - startAngle) * MathF.Ceiling(2f * MathF.PI / th) / (2f * MathF.PI));
            if (segments <= 0) segments = minSegments;
        }

        float stepLength = (float)(endAngle - startAngle) / segments;
        float angle = startAngle;

        // cap line
        /*DrawLine(
            x0, y0,
            x0 + MathF.Cos(angle) * radius, y0 + MathF.Sin(angle) * radius
        );*/

        for (int i = 0; i < segments; i++)
        {
            DrawLine(
                x0 + MathF.Cos(angle) * radius, y0 + MathF.Sin(angle) * radius,
                x0 + MathF.Cos(angle+stepLength) * radius, y0 + MathF.Sin(angle+stepLength) * radius
            );

            angle += stepLength;
        }

        // cap line
        /*DrawLine(
            x0, y0,
            x0 + MathF.Cos(angle) * radius, y0 + MathF.Sin(angle) * radius
        );*/
    }

    public void DrawCircleSector(Vector2 center, float radius, float startAngle, float endAngle, int segments)
        => DrawCircleSector(center.X, center.Y, radius, startAngle, endAngle, segments);
    
    public void DrawCircle(float x, float y, float radius, int segments = 36)
        => DrawCircleSector(x, y, radius, 0f, 2f * MathF.PI, segments);
    
    public void DrawCircle(Vector2 center, float radius, int segments = 36)
        => DrawCircleSector(center.X, center.Y, radius, 0f, 2f * MathF.PI, segments);

    public void DrawRingSector(Vector2 center, float radius, float startAngle, float endAngle, int segments)
        => DrawRingSector(center.X, center.Y, radius, startAngle, endAngle, segments);
    
    public void DrawRing(float x, float y, float radius, int segments = 36)
        => DrawRingSector(x, y, radius, 0f, 2f * MathF.PI, segments);
    
    public void DrawRing(Vector2 center, float radius, int segments = 36)
        => DrawRingSector(center.X, center.Y, radius, 0f, 2f * MathF.PI, segments);
}