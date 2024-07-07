namespace Glib;

using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

public enum BatchDrawMode
{
    Triangles, Quads, Lines
}

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
    CullFace,
    WireframeRendering
}

public enum DebugSeverity
{
    Notification,
    Low,
    Medium,
    High
};

public class RenderContext : IDisposable
{
    internal readonly GL gl;
    private bool _disposed = false;

    // batch variables
    private const uint VertexDataSize = 9;
    private const uint MaxVertices = 4096;

    private readonly float[] batchData;
    private uint numVertices = 0;
    private readonly uint batchBuffer;
    private readonly uint batchVertexArray;

    private int screenWidth = 0;
    private int screenHeight = 0;

    public int ScreenWidth => screenWidth;
    public int ScreenHeight => screenHeight;

    private readonly Texture whiteTexture;

    /// <summary>
    /// A 1x1 texture consisting of a single white pixel.
    /// Useful for placeholder texture slots.
    /// </summary>
    public Texture WhiteTexture => whiteTexture;

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

    /// <summary>
    /// If Glib should use GL_LINES primitives for drawing lines.
    /// When enabled, the LineWidth field will not be respected, and all
    /// lines will be drawn with a width of 1
    /// </summary>
    public bool UseGlLines = false;

    private Vector2 UV = Vector2.Zero;
    public TextureFilterMode DefaultTextureMagFilter = TextureFilterMode.Linear;
    public TextureFilterMode DefaultTextureMinFilter = TextureFilterMode.Linear;
    private PrimitiveType drawMode;
    public Shader? Shader {
        get => shaderValue;
        set
        {
            if (shaderValue == value) return;
            DrawBatch();
            shaderValue = value;
            value?.Use(gl);
        }
    }

    public readonly string GpuVendor;
    public readonly string GpuRenderer;
    private static bool debugOutputEnabled = false;

    private static void DefaultErrorCallback(string msg, DebugSeverity severity)
    {
        Console.WriteLine($"GL message (Severity: {severity}): {msg}");
    }

    internal unsafe RenderContext(IWindow window, bool glDebug)
    {
        gl = GL.GetApi(window);

        unsafe
        {
            byte* vendorStr = gl.GetString(GLEnum.Vendor);
            byte* rendererStr = gl.GetString(GLEnum.Renderer);
            GpuVendor = Marshal.PtrToStringAnsi((nint) vendorStr) ?? "[unknown]";
            GpuRenderer = Marshal.PtrToStringAnsi((nint) rendererStr) ?? "[unknown]";
        }

        Console.WriteLine("GL_VENDOR: " + GpuVendor);
        Console.WriteLine("GL_RENDERER: " + GpuRenderer);

        if (glDebug)
        {
            Shader._debug = true;
            glErrorCallback = DefaultErrorCallback;
            debugOutputEnabled = true;
            gl.Enable(EnableCap.DebugOutput);
            gl.DebugMessageCallback(ErrorCallbackHandler, null);
        }

        //gl.Enable(EnableCap.CullFace);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        drawMode = PrimitiveType.Triangles;

        // create default shader
        defaultShader = new Shader(gl);
        defaultShader.Use(gl);

        // create default texture
        var img = new Image([255, 255, 255, 255], 1, 1, PixelFormat.RGBA);
        whiteTexture = CreateTexture(img);
        defaultShader.SetUniform(Shader.TextureUniform, whiteTexture);
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
        screenWidth = width;
        screenHeight = height;
        SetViewport(width, height);
        
        Clear();
        ClearTransformationStack();
        ResetTransform();
        ResetScissorBounds();

        shaderValue = null;
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        framebufferStack.Clear();
        curFramebuffer = null;

        defaultShader.Use(gl);
        defaultShader.SetUniform(Shader.TextureUniform, whiteTexture);
        defaultShader.SetUniform(Shader.ColorUniform, Color.White);
        curTexture = whiteTexture;
        lastTexture = whiteTexture;
    }

    internal void End()
    {
        DrawBatch();
        InternalSetTexture(null);
    }

    public void SetScissorBounds(int x, int y, int w, int h)
    {
        DrawBatch();

        if (curFramebuffer is not null)
        {
            gl.Scissor(x, curFramebuffer.Height - (y + h), (uint)w, (uint)h);
        }
        else
        {
            gl.Scissor(x, y, (uint)w, (uint)h);
        }
        // TODO: what if high-dpi on framebuffer?
    }

    /// <summary>
    /// Resets the scissor bounds to the window size
    /// </summary>
    public void ResetScissorBounds()
    {
        DrawBatch();
        gl.Scissor(0, 0, (uint)screenWidth, (uint)screenHeight);
    }

    /// <summary>
    /// Enable/disable a certain feature
    /// </summary>
    /// <param name="feature"></param>
    public void SetEnabled(Feature feature, bool enabled)
    {
        DrawBatch();

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
            
            case Feature.WireframeRendering:
                if (enabled)
                    gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
                else
                    gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
                break;
        }
    }

    public void SetCullMode(CullMode mode)
    {
        DrawBatch();

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
        DrawBatch();

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

    /// <summary>
    /// I am too lazy to actually add the enum values for this so just
    /// ues the ints or whatever.
    /// </summary>
    public void SetBlendFactorsSeparate(int glSrcRGB, int glDstRGB, int glSrcAlpha, int glDstAlpha, int glEqRGB, int glEqAlpha)
    {
        DrawBatch();
        gl.BlendFuncSeparate((GLEnum)glSrcRGB, (GLEnum)glDstRGB, (GLEnum)glSrcAlpha, (GLEnum)glDstAlpha);
        gl.BlendEquationSeparate((GLEnum)glEqRGB, (GLEnum)glEqAlpha);
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
    
    /// <summary>
    /// Set the transform matrix to the identity matrix.
    /// </summary>
    public void ResetTransform()
        => TransformMatrix = Matrix4x4.Identity;

    /// <summary>
    /// Clear the transformation stack.
    /// <br /><br />See also: PushTransform, PopTransform.
    /// </summary>
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
    /// Create an uninitialized texture.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <param name="pixelFormat">The pixel format to use.</param>
    public Texture CreateTexture(int width, int height, PixelFormat pixelFormat)
    {
        return new Texture(gl, width, height, pixelFormat);
    }
    
    /// <summary>
    /// Create a texture from an Image.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="mipmaps"></param>
    /// <returns></returns>
    public Texture CreateTexture(Image image, bool mipmaps = false)
    {
        var tex = new Texture(gl, image, mipmaps);
        tex.SetFilterMode(DefaultTextureMinFilter, DefaultTextureMagFilter);
        return tex;
    }
    
    /// <summary>
    /// Create a framebuffer from a framebuffer configuration.
    /// <br /><br />
    /// See also: FramebufferConfiguration.Create(RenderContext)
    /// </summary>
    public Framebuffer CreateFramebuffer(FramebufferConfiguration config)
    {
        var buffer = new Framebuffer(gl, config);
        
        // set texture filters to default
        for (int i = 0; i < config.Attachments.Count; i++)
        {
            if (config.Attachments[i].CanRead)
                buffer.GetTexture(i).SetFilterMode(DefaultTextureMinFilter, DefaultTextureMagFilter);
        }

        // reset framebuffer to the framebuffer it was in before
        // as creating a new framebuffer requires binding it
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, curFramebuffer?.Handle ?? 0);

        return buffer;
    }

    public void SetViewport(int width, int height)
    {
        gl.Viewport(0, 0, (uint)width, (uint)height);
        BaseTransform =
            Matrix4x4.CreateScale(new Vector3(1f / width * 2f, -1f / height * 2f, 1f)) *
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
            SetViewport(screenWidth, screenHeight);
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
        => CreateTexture(Image.FromFile(filePath), mipmaps);

    /// <summary>
    /// Draw a mesh with a texture.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    /// <param name="texture">The texture to draw on the mesh.</param>
    public void Draw(Mesh mesh, Texture texture)
    {
        DrawBatch();

        var shader = shaderValue ?? defaultShader;
        shader.Use(gl);

        if (shader.HasUniform(Shader.MatrixUniform))
            shader.SetUniform(Shader.MatrixUniform, TransformMatrix * BaseTransform);

        if (shader.HasUniform(Shader.TextureUniform))
            shader.SetUniform(Shader.TextureUniform, texture);
        
        if (shader.HasUniform(Shader.ColorUniform))
            shader.SetUniform(Shader.ColorUniform, DrawColor);
        
        mesh.Draw();
    }
    
    /// <summary>
    /// Draw a mesh.
    /// </summary>
    /// <param name="mesh">The mesh to draw.</param>
    public void Draw(Mesh mesh)
        => Draw(mesh, whiteTexture);

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
        InternalSetTexture(tex);

        var uvLeft = srcX / tex.Width;
        var uvTop = srcY / tex.Height;
        var uvRight = (srcX + srcW) / tex.Width;
        var uvBottom = (srcY + srcH) / tex.Height;
        
        BeginBatchDraw(6);

        // first triangle
        UV.Y = uvTop;
        UV.X = uvLeft;
        PushVertex(dstX, dstY);

        UV.Y = uvBottom;
        UV.X = uvLeft;
        PushVertex(dstX, dstY + dstH);

        UV.Y = uvBottom;
        UV.X = uvRight;
        PushVertex(dstX + dstW, dstY + dstH);

        // second triangle
        // uv is the same as the last vertex
        PushVertex(dstX + dstW, dstY + dstH);

        UV.Y = uvTop;
        UV.X = uvRight;
        PushVertex(dstX + dstW, dstY);

        UV.Y = uvTop;
        UV.X = uvLeft;
        PushVertex(dstX, dstY);
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

    private void InternalSetTexture(Texture? newTex)
    {
        curTexture = newTex ?? whiteTexture;
    }

    private void BeginBatchDraw(uint requiredCapacity, PrimitiveType newDrawMode = PrimitiveType.Triangles)
    {
        CheckCapacity(requiredCapacity);

        // flush batch on texture/draw mode change
        if (curTexture != lastTexture || drawMode != newDrawMode)
        {
            DrawBatch();
            lastTexture = curTexture;
            drawMode = newDrawMode;
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
        
        if (shader.HasUniform(Shader.MatrixUniform))
            shader.SetUniform(Shader.MatrixUniform, BaseTransform); // vertices are already transformed
        
        if (shader.HasUniform(Shader.TextureUniform))
            shader.SetUniform(Shader.TextureUniform, lastTexture);

        if (shader.HasUniform(Shader.ColorUniform))
            shader.SetUniform(Shader.ColorUniform, Color.White); // color is in mesh data
        
        gl.DrawArrays(drawMode, 0, numVertices);
        numVertices = 0;
    }

    private void CheckCapacity(uint newVertices)
    {
        if (numVertices + newVertices >= MaxVertices)
        {
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

    /// <summary>
    /// Push a vertex to the draw batch with the specified position and texture coordinates.
    /// </summary> 
    public void PushVertex(Vector2 pos, Vector2 uv)
    {
        BeginBatchDraw(1);
        
        UV = uv;
        PushVertex(pos.X, pos.Y);
    }

    public void DrawTriangle(float x0, float y0, float x1, float y1, float x2, float y2)
    {
        InternalSetTexture(null);

        BeginBatchDraw(3);
        PushVertex(x0, y0);
        PushVertex(x1, y1);
        PushVertex(x2, y2);
    }

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c) => DrawTriangle(a.X, a.Y, b.X, b.Y, c.X, c.Y);

    public void DrawRectangle(float x, float y, float w, float h)
    {
        InternalSetTexture(null);

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
        InternalSetTexture(null);

        if (UseGlLines)
        {
            BeginBatchDraw(2, PrimitiveType.Lines);
            PushVertex(x0, y0);
            PushVertex(x1, y1);
        }
        else
        {
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
    }

    public void DrawLine(Vector2 a, Vector2 b) => DrawLine(a.X, a.Y, b.X, b.Y);

    public void DrawRectangleLines(float x, float y, float w, float h)
    {
        if (UseGlLines)
        {
            BeginBatchDraw(8, PrimitiveType.Lines);

            PushVertex(x, y);
            PushVertex(x, y + h);

            PushVertex(x, y + h);
            PushVertex(x + w, y + h);

            PushVertex(x + w, y + h);
            PushVertex(x + w, y);

            PushVertex(x + w, y);
            PushVertex(x, y);
        }
        else
        {
            DrawRectangle(x, y, w, LineWidth); // top side
            DrawRectangle(x, y+LineWidth, LineWidth, h-LineWidth); // left side
            DrawRectangle(x, y+h-LineWidth, w-LineWidth, LineWidth); // bottom side
            DrawRectangle(x+w-LineWidth, y+LineWidth, LineWidth, h-LineWidth); // right side
        }
    }

    private const float SmoothCircleErrorRate = 0.5f;

    public void DrawCircleSector(float x0, float y0, float radius, float startAngle, float endAngle, int segments)
    {
        InternalSetTexture(null);

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
        InternalSetTexture(null);

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
    
    public struct BatchDrawHandle : IDisposable
    {
        private static Vector2[] verts = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero];
        private static Vector2[] uvs = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero,];
        private static Color[] colors = [Glib.Color.Transparent, Glib.Color.Transparent, Glib.Color.Transparent, Glib.Color.Transparent];

        private readonly RenderContext ctx;
        private int vertIndex = 0;
        private BatchDrawMode mode;
        private Vector2 uv;
        private Color color;

        internal BatchDrawHandle(BatchDrawMode mode, RenderContext ctx)
        {
            this.mode = mode;
            this.ctx = ctx;

            uv = Vector2.Zero;
            color = ctx.DrawColor;
        }

        private readonly bool IsFull()
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
                    ctx.BeginBatchDraw(3, PrimitiveType.Triangles);
                    for (int i = 0; i < 3; i++)
                    {
                        ctx.DrawColor = colors[i];
                        ctx.UV = uvs[i];
                        ctx.PushVertex(verts[i].X, verts[i].Y);
                    }
                    break;
                }

                case BatchDrawMode.Quads:
                {
                    ctx.BeginBatchDraw(6, PrimitiveType.Triangles);

                    // first triangle
                    ctx.DrawColor = colors[0];
                    ctx.UV = uvs[0];
                    ctx.PushVertex(verts[0].X, verts[0].Y);

                    ctx.DrawColor = colors[1];
                    ctx.UV = uvs[1];
                    ctx.PushVertex(verts[1].X, verts[1].Y);

                    ctx.DrawColor = colors[2];
                    ctx.UV = uvs[2];
                    ctx.PushVertex(verts[2].X, verts[2].Y);

                    // second triangle
                    ctx.PushVertex(verts[2].X, verts[2].Y);

                    ctx.DrawColor = colors[3];
                    ctx.UV = uvs[3];
                    ctx.PushVertex(verts[3].X, verts[3].Y);

                    ctx.DrawColor = colors[0];
                    ctx.UV = uvs[0];
                    ctx.PushVertex(verts[0].X, verts[0].Y);
                    break;
                }

                case BatchDrawMode.Lines:
                {
                    ctx.BeginBatchDraw(2, PrimitiveType.Lines);

                    ctx.DrawColor = colors[0];
                    ctx.UV = uvs[0];
                    ctx.PushVertex(verts[0].X, verts[0].Y);

                    ctx.DrawColor = colors[1];
                    ctx.UV = uvs[1];
                    ctx.PushVertex(verts[1].X, verts[1].Y);
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

    public BatchDrawHandle BeginBatchDraw(BatchDrawMode mode, Texture? tex = null)
    {
        tex ??= whiteTexture;
        InternalSetTexture(tex);
        return new BatchDrawHandle(mode, this);
    }

    private Action<string, DebugSeverity>? glErrorCallback = null;

    private unsafe void ErrorCallbackHandler(
        GLEnum source,
        GLEnum type,
        int id,
        GLEnum severity,
        int length,
        nint message,
        nint userParam)
    {
        var sev = severity switch
        {
            GLEnum.DebugSeverityNotification => DebugSeverity.Notification,
            GLEnum.DebugSeverityLow => DebugSeverity.Low,
            GLEnum.DebugSeverityMedium => DebugSeverity.Medium,
            GLEnum.DebugSeverityHigh => DebugSeverity.High,
            _ => DebugSeverity.Notification
        };

        var errorStr = System.Text.Encoding.UTF8.GetString((byte*) message, length);
        glErrorCallback!(errorStr, sev);
    }

    public unsafe void SetupErrorCallback(Action<string, DebugSeverity> proc)
    {
        glErrorCallback = proc;

        if (!debugOutputEnabled)
        {
            debugOutputEnabled = true;
            gl.Enable(EnableCap.DebugOutput);
            gl.DebugMessageCallback(ErrorCallbackHandler, null);
        }
    }
}