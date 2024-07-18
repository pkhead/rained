using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Bgfx_cs;
using Silk.NET.Windowing;
namespace Glib;

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

/// <summary>
/// Triangles with this ordering are culled when rendering.
/// </summary>
public enum CullMode {
    None,
    Clockwise,
    Counterclockwise,
}

[Flags]
public enum RenderFlags : int
{
    None = 0,
    Blend = 1,
    DepthTest = 2,
    WireframeRendering = 4
}

public enum DebugSeverity
{
    Notification,
    Low,
    Medium,
    High
};

public sealed class RenderContext : IDisposable
{
    internal static RenderContext? Instance { get; private set; } = null;

    private bool _disposed = false;

    public int ScreenWidth { get; private set; }
    public int ScreenHeight { get; private set; }

    /// <summary>
    /// A 1x1 texture consisting of a single white pixel.
    /// Useful for placeholder texture slots.
    /// </summary>
    public Texture WhiteTexture { get; private set; }

    private readonly Shader defaultShader;

    private readonly Stack<Matrix4x4> transformStack = [];
    private readonly Stack<Framebuffer> framebufferStack = [];
    private Framebuffer? curFramebuffer = null;

    private class ViewGraphNode(Framebuffer? fb)
    {
        public Framebuffer? framebuffer = fb; // a Framebuffer value of null represents the window framebuffer
        public List<ViewGraphNode> children = [];
    }

    private ushort curViewId = 0;
    private List<ViewGraphNode> _viewData = [];
    private ViewGraphNode _curView = null!;
    private List<ushort> _viewOrder = [];

    public Matrix4x4 TransformMatrix { get => _drawBatch.TransformMatrix; set => _drawBatch.TransformMatrix = value; }
    public Color BackgroundColor = Color.Black;
    public ref Color DrawColor => ref _drawBatch.DrawColor;
    public Shader? Shader { get => _drawBatch.Shader; set => _drawBatch.Shader = value; }

    /// <summary>
    /// If Glib should use GL_LINES primitives for drawing lines.
    /// When enabled, the LineWidth field will not be respected, and all
    /// lines will be drawn with a width of 1
    /// </summary>
    public bool UseGlLines = true;
    public float LineWidth = 1f;
    public RenderFlags Flags = RenderFlags.Blend;
    public CullMode CullMode = CullMode.None;

    private int _scissorX, _scissorY, _scissorW, _scissorH;
    private bool _scissorEnabled = false;

    private readonly DrawBatch _drawBatch;

    public readonly string GpuVendor;
    public readonly string GpuRenderer;

    private CallbackInterface _cbInterface;

    internal unsafe RenderContext(IWindow window)
    {
        if (Instance is not null)
        {
            throw new NotImplementedException("Multi-window not implemented yet");
        }

        Instance = this;

        _cbInterface = new CallbackInterface()
        {
            Log = (string msg) => Console.WriteLine(msg),
            Fatal = (string filePath, int line, Bgfx.Fatal code, string msg) => Console.WriteLine(msg)
        };

        ScreenWidth = window.FramebufferSize.X;
        ScreenHeight = window.FramebufferSize.Y;

        var init = new Bgfx.Init();
        Bgfx.init_ctor(&init);

        init.type = Bgfx.RendererType.OpenGL;
        init.callback = _cbInterface.Pointer;

        var nativeHandles = window.Native!.Win32!;
        init.platformData.nwh = (void*) nativeHandles.Value.Hwnd;
        init.platformData.type = Bgfx.NativeWindowHandleType.Default;
        init.resolution.width = (uint) window.FramebufferSize.X;
        init.resolution.height = (uint) window.FramebufferSize.Y;
        init.resolution.reset = (uint) Bgfx.ResetFlags.Vsync;
        if (!Bgfx.init(&init))
        {
            throw new Exception("Could not initialize bgfx");
        }

        {
            var rendererId = Bgfx.get_renderer_type();
            var rendererName = Marshal.PtrToStringAnsi(Bgfx.get_renderer_name(rendererId))!;
            GpuRenderer = rendererName;

            var caps = Bgfx.get_caps();
            GpuVendor = ((Bgfx.PciIdFlags)caps->vendorId).ToString();

            var swapChainSupported = (caps->supported & (ulong)Bgfx.CapsFlags.SwapChain) != 0;
            Console.WriteLine("renderer: " + GpuRenderer);
            Console.WriteLine("swap chain supported: " + swapChainSupported);
        }

        defaultShader = new Shader();
         _drawBatch = new DrawBatch(BatchDrawCallback);
        WhiteTexture = new Texture(Image.FromColor(1, 1, Color.White));
        TransformMatrix = Matrix4x4.Identity;
    }

    public unsafe bool CanUseMultipleWindows()
    {
        var caps = Bgfx.get_caps();
        return (caps->supported & (ulong)Bgfx.CapsFlags.SwapChain) != 0;
    }

    public unsafe bool CanReadBackFramebufferAttachments()
    {
        var caps = Bgfx.get_caps();
        return (caps->supported & (ulong)(Bgfx.CapsFlags.TextureReadBack | Bgfx.CapsFlags.TextureBlit)) != 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        BgfxResource.DisposeRemaining();
        Bgfx.shutdown();
        _cbInterface.Dispose();
        GC.SuppressFinalize(this);
    }

    internal unsafe void SetViewport(int width, int height)
    {
        Bgfx.set_view_rect(curViewId, 0, 0, (ushort)width, (ushort)height);
        var viewMat =
            Matrix4x4.CreateScale(new Vector3(1f / width * 2f, -1f / height * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, 1f, 0f));

        Matrix4x4 projMatrix = Matrix4x4.Identity;
        Bgfx.set_view_transform(curViewId, &viewMat, &projMatrix);
    }

    private uint ColorToUint(Color color)
    {
        var r = (uint)(Math.Clamp(color.R, 0f, 1f) * 255f);
        var g = (uint)(Math.Clamp(color.G, 0f, 1f) * 255f);
        var b = (uint)(Math.Clamp(color.B, 0f, 1f) * 255f);
        var a = (uint)(Math.Clamp(color.A, 0f, 1f) * 255f);
        return a | (b << 8) | (g << 16) | (r << 24);
    }

    internal void Begin(int width, int height)
    {
        curViewId = 0;

        uint bgCol = ColorToUint(BackgroundColor);

        if (width != ScreenWidth || height != ScreenHeight)
        {
            Bgfx.reset((uint)width, (uint)height, (uint)Bgfx.ResetFlags.Vsync, Bgfx.TextureFormat.Count);
            ScreenWidth = width;
            ScreenHeight = height;
        }

        _viewData.Clear();
        SetViewport(ScreenWidth, ScreenHeight);
        Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);
        Bgfx.set_view_clear(curViewId, (ushort)(Bgfx.ClearFlags.Color | Bgfx.ClearFlags.Depth), bgCol, 1f, 0);
        Bgfx.touch(curViewId); // ensure this view will be cleared even if no draw calls are submitted to it

        _curView = new ViewGraphNode(null);
        _viewData.Add(_curView);

        transformStack.Clear();
        TransformMatrix = Matrix4x4.Identity;

        defaultShader.SetUniform(Shader.TextureUniform, WhiteTexture);
        defaultShader.SetUniform(Shader.ColorUniform, Color.White);
        _drawBatch.NewFrame(WhiteTexture);
        //curTexture = whiteTexture;
        //lastTexture = whiteTexture;
    }

    private static Bgfx.StateFlags BgfxStateBlendFuncSeparate(Bgfx.StateFlags srcRgb, Bgfx.StateFlags dstRgb, Bgfx.StateFlags srcA, Bgfx.StateFlags dstA)
    {
        return (Bgfx.StateFlags)(0
            | ( ( (ulong)(srcRgb)|( (ulong)(dstRgb)<<4) )   )
            | ( ( (ulong)(srcA  )|( (ulong)(dstA  )<<4) )<<8)
        );
    }

    private static Bgfx.StateFlags BgfxStateBlendFunc(Bgfx.StateFlags src, Bgfx.StateFlags dst)
    {
        return BgfxStateBlendFuncSeparate(src, dst, src, dst);
    }

    public void DrawBatch() => _drawBatch.Draw();

    private Bgfx.StateFlags SetupState()
    {
        if (_scissorEnabled)
        {
            Bgfx.set_scissor((ushort)_scissorX, (ushort)_scissorY, (ushort)_scissorW, (ushort)_scissorH);
        }

        var state = Bgfx.StateFlags.None;
        state |= Bgfx.StateFlags.WriteRgb | Bgfx.StateFlags.WriteA | Bgfx.StateFlags.Msaa;
        if (Flags.HasFlag(RenderFlags.Blend)) state |= BgfxStateBlendFunc(Bgfx.StateFlags.BlendSrcAlpha, Bgfx.StateFlags.BlendInvSrcAlpha);
        if (Flags.HasFlag(RenderFlags.DepthTest)) state |= Bgfx.StateFlags.DepthTestLess;
        if (CullMode == CullMode.Clockwise) state |= Bgfx.StateFlags.CullCw;
        else if (CullMode == CullMode.Counterclockwise) state |= Bgfx.StateFlags.CullCcw;
        return state;
    }

    public void SetScissorBox(int x, int y, int w, int h)
    {
        _scissorEnabled = true;
        _scissorX = x;
        _scissorY = y;
        _scissorW = w;
        _scissorH = h;
    }

    public bool GetScissor(out int x, out int y, out int w, out int h)
    {
        if (_scissorEnabled)
        {
            x = 0;
            y = 0;
            w = 0;
            h = 0;
        }
        else
        {
            x = _scissorX;
            y = _scissorY;
            w = _scissorW;
            h = _scissorH;
        }

        return _scissorEnabled;
    }

    public void ClearScissorBox()
    {
        _scissorEnabled = false;
    }

    /// <summary>
    /// Draw a mesh.
    /// <br/><br/>
    /// Uses the full contents of each buffer.
    /// </summary>
    /// <param name="mesh">The mesh to draw</param>
    /// <param name="texture">The texture to bind to glib_texture in the active shader when drawing.</param>
    public unsafe void Draw(Mesh mesh, Texture texture)
    {
        _drawBatch.Draw();

        var shader = Shader ?? defaultShader;

        if (shader.HasUniform(Shader.TextureUniform))
            shader.SetUniform(Shader.TextureUniform, texture);
        
        if (shader.HasUniform(Shader.ColorUniform))
            shader.SetUniform(Shader.ColorUniform, DrawColor);
                
        var programHandle = shader.Activate(WhiteTexture);
        
        mesh.ResetSliceSettings();
        var state = SetupState() | mesh.Activate();
        Bgfx.set_state((ulong)state, 0);

        Matrix4x4 transformMat = TransformMatrix;
        Bgfx.set_transform(&transformMat, 1);

        Bgfx.submit(curViewId, programHandle, 0, (byte)Bgfx.DiscardFlags.All);
    }

    /// <summary>
    /// Draw a mesh.
    /// <br/><br/>
    /// Uses the full contents of each buffer.
    /// </summary>
    /// <param name="mesh">The mesh to draw</param>
    public void Draw(Mesh mesh) => Draw(mesh, WhiteTexture);

    public record ActiveMeshHandle : IDisposable
    {
        public Mesh Mesh;
        public RenderContext RenderContext;

        public ActiveMeshHandle(Mesh mesh, RenderContext rctx)
        {
            Mesh = mesh;
            RenderContext = rctx;
            Mesh.ResetSliceSettings();
        }

        /// <summary>
        /// Set the slice of the buffer drawn.
        /// </summary>
        /// <param name="bufferIndex">The index of the buffer to configure.</param>
        /// <param name="startVertex">The index of the starting vertex.</param>
        /// <param name="vertexCount">The number of vertices to draw.</param>
        /// <exception cref="ArgumentException">Thrown when the buffer at the given index does not exist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the startVertex and vertexCount parameters are out of bounds.</exception>
        public void SetBufferDrawSlice(int bufferIndex, uint startVertex, uint vertexCount)
        {
            Mesh.SetBufferDrawSlice(bufferIndex, startVertex, vertexCount);
        }

        /// <summary>
        /// Set the slice of the index buffer that is drawn.
        /// </summary>
        /// <param name="startVertex">The index of the starting vertex.</param>
        /// <param name="vertexCount">The number of vertices to draw.</param>
        /// <exception cref="ArgumentException">Thrown when the buffer at the given index does not exist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the startVertex and vertexCount parameters are out of bounds.</exception>
        public void SetIndexBufferDrawSlice(uint startVertex, uint vertexCount)
        {
            Mesh.SetIndexBufferDrawSlice(startVertex, vertexCount);
        }

        /// <summary>
        /// Set the slice of the index buffer that is drawn.
        /// </summary>
        /// <param name="startVertex">The index of the starting vertex.</param>
        /// <exception cref="ArgumentException">Thrown when the buffer at the given index does not exist.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the startVertex.</exception>
        public void SetIndexBufferDrawSlice(uint startVertex)
        {
            Mesh.SetIndexBufferDrawSlice(startVertex);
        }

        /// <summary>
        /// Draw a mesh.
        /// <br/><br/>
        /// Uses a specified portion of each buffer.
        /// </summary>
        /// <param name="mesh">The mesh to draw</param>
        /// <param name="texture">The texture to bind to glib_texture in the active shader when drawing.</param>
        public unsafe void Draw(Texture texture)
        {
            var rctx = this.RenderContext;
            rctx._drawBatch.Draw();

            var shader = rctx.Shader ?? rctx.defaultShader;

            if (shader.HasUniform(Shader.TextureUniform))
                shader.SetUniform(Shader.TextureUniform, texture);
            
            if (shader.HasUniform(Shader.ColorUniform))
                shader.SetUniform(Shader.ColorUniform, rctx.DrawColor);
                    
            var programHandle = shader.Activate(rctx.WhiteTexture);
            
            var state = rctx.SetupState() | Mesh.Activate();
            Bgfx.set_state((ulong)state, 0);

            Matrix4x4 transformMat = rctx.TransformMatrix;
            Bgfx.set_transform(&transformMat, 1);

            Bgfx.submit(rctx.curViewId, programHandle, 0, (byte)Bgfx.DiscardFlags.All);
        }

        /// <summary>
        /// Draw a mesh.
        /// <br/><br/>
        /// Uses a specified portion of each buffer.
        /// </summary>
        /// <param name="mesh">The mesh to draw</param>
        public void Draw() => Draw(RenderContext.WhiteTexture);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Draw a mesh, retaining its buffer state. Used to draw a mesh
    /// using a portion of its buffers one or more times.
    /// </summary>
    /// <param name="mesh">The mesh to draw</param>
    public ActiveMeshHandle UseMesh(Mesh mesh)
        => new(mesh, this);

    public void DrawTexture(Texture texture, Rectangle srcRect, Rectangle dstRect)
    {
        using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Quads, texture);
        var texW = texture.Width;
        var texH = texture.Height;

        draw.TexCoord(srcRect.Left / texW, srcRect.Top / texH);
        draw.Vertex(dstRect.Left, dstRect.Top);

        draw.TexCoord(srcRect.Left / texW, srcRect.Bottom / texH);
        draw.Vertex(dstRect.Left, dstRect.Bottom);

        draw.TexCoord(srcRect.Right / texW, srcRect.Bottom / texH);
        draw.Vertex(dstRect.Right, dstRect.Bottom);

        draw.TexCoord(srcRect.Right / texW, srcRect.Top / texH);
        draw.Vertex(dstRect.Right, dstRect.Top);
    }

    public void DrawTexture(Texture texture, Rectangle rect)
        => DrawTexture(texture, new Glib.Rectangle(0f, 0f, texture.Width, texture.Height), rect);

    public void DrawTexture(Texture texture, Vector2 pos, Vector2 size)
        => DrawTexture(texture, new Rectangle(0f, 0f, texture.Width, texture.Height), new Rectangle(pos, size));
    
    public void DrawTexture(Texture texture, Vector2 pos)
        => DrawTexture(texture, new Rectangle(0f, 0f, texture.Width, texture.Height), new Rectangle(pos.X, pos.Y, texture.Width, texture.Height));
    
    public void DrawTexture(Texture texture, float x, float y)
        => DrawTexture(texture, new Vector2(x, y));
    
    public void DrawTexture(Texture texture)
        => DrawTexture(texture, new Vector2(0f, 0f));

    internal void AddViewDependency(Framebuffer framebuffer)
    {
        var childView = _viewData.Find(x => x.framebuffer == framebuffer)
            ?? throw new Exception("Cannot use framebuffer before rendering to it");
        
        if (!_curView.children.Contains(childView))
        {
            _curView.children.Add(childView);
        }
    }

    public void PushFramebuffer(Framebuffer framebuffer)
    {
        if (curFramebuffer is not null)
            framebufferStack.Push(curFramebuffer);

        _drawBatch.Draw();
        curFramebuffer = framebuffer;

        var viewId = _viewData.FindIndex(x => x.framebuffer == framebuffer);
        if (viewId == -1)
        {
            viewId = _viewData.Count;
            _viewData.Add(new ViewGraphNode(framebuffer));
            
            curViewId = (ushort)viewId;
            Bgfx.set_view_frame_buffer(curViewId, framebuffer.Handle);
            Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);

            var clearFlags = Bgfx.ClearFlags.None;
            if (framebuffer.clearFlags.HasFlag(ClearFlags.Color)) clearFlags |= Bgfx.ClearFlags.Color;
            if (framebuffer.clearFlags.HasFlag(ClearFlags.Depth)) clearFlags |= Bgfx.ClearFlags.Depth;
            if (framebuffer.clearFlags.HasFlag(ClearFlags.Stencil)) clearFlags |= Bgfx.ClearFlags.Stencil;

            var colorUint = ColorToUint(framebuffer.clearColor);
            Bgfx.set_view_clear(curViewId, (ushort)clearFlags, colorUint, 1f, 0);
            SetViewport(framebuffer.Width, framebuffer.Height);
            Bgfx.touch(curViewId);
        }

        curViewId = (ushort)viewId;
    }

    public Framebuffer? PopFramebuffer()
    {
        _drawBatch.Draw();
        if (framebufferStack.TryPop(out Framebuffer? framebuffer))
        {
            var viewId = _viewData.FindIndex(x => x.framebuffer == framebuffer);
            Debug.Assert(viewId >= 0);
            curViewId = (ushort)viewId;
            //SetViewport(framebuffer.Width, framebuffer.Height);
        }
        else
        {
            curViewId = 0;
            //SetViewport(ScreenWidth, ScreenHeight);
        }
        
        curFramebuffer = framebuffer;
        return curFramebuffer;
    }

    internal void End()
    {
        _drawBatch.Draw();
        
        // create view order from framebuffer graph
        _viewOrder.Clear();

        void RegisterNode(ViewGraphNode node)
        {
            var viewId = _viewData.IndexOf(node);
            if (viewId < 0) throw new Exception("Could not find view id of node");

            foreach (var child in node.children)
                RegisterNode(child);
            _viewOrder.Add((ushort) viewId);
        }

        RegisterNode(_viewData[0]);

        unsafe
        {
            ushort* viewOrder = stackalloc ushort[_viewOrder.Count];
            for (int i = 0; i < _viewOrder.Count; i++)
                viewOrder[i] = _viewOrder[i];
        
            Bgfx.set_view_order(0, (ushort)_viewOrder.Count, viewOrder);
        }

        Bgfx.frame(false);
        BgfxResource.Housekeeping();
    }

    #region Transform
    public void PushTransform()
    {
        transformStack.Push(TransformMatrix);
    }

    public void PopTransform()
    {
        if (transformStack.Count == 0) return;
        TransformMatrix = transformStack.Pop();
    }

    public void Translate(float x, float y, float z = 0f)
    {
        TransformMatrix = Matrix4x4.CreateTranslation(x, y, z) * TransformMatrix;
    }

    public void Translate(Vector3 translation)
    {
        TransformMatrix = Matrix4x4.CreateTranslation(translation) * TransformMatrix;
    }

    public void Translate(Vector2 translation)
    {
        TransformMatrix = Matrix4x4.CreateTranslation(translation.X, translation.Y, 0f) * TransformMatrix;
    }

    public void Scale(float x, float y, float z = 1f)
    {
        TransformMatrix = Matrix4x4.CreateScale(x, y, z) * TransformMatrix;
    }

    public void Scale(Vector3 scale)
    {
        TransformMatrix = Matrix4x4.CreateScale(scale) * TransformMatrix;
    }

    public void Scale(Vector2 scale)
    {
        TransformMatrix = Matrix4x4.CreateScale(scale.X, scale.Y, 1f) * TransformMatrix;
    }

    public void RotateX(float rad)
    {
        TransformMatrix = Matrix4x4.CreateRotationX(rad) * TransformMatrix;
    }

    public void RotateY(float rad)
    {
        TransformMatrix = Matrix4x4.CreateRotationY(rad) * TransformMatrix;
    }

    public void RotateZ(float rad)
    {
        TransformMatrix = Matrix4x4.CreateRotationZ(rad) * TransformMatrix;
    }

    public void Rotate(float rad) => RotateZ(rad);
    #endregion

    #region Shapes

    private unsafe void BatchDrawCallback(Bgfx.StateFlags flags)
    {
        var shader = Shader ?? defaultShader;
        flags |= SetupState();
        Bgfx.set_state((ulong)flags, 0);

        if (shader.HasUniform(Shader.TextureUniform))
            shader.SetUniform(Shader.TextureUniform, _drawBatch.Texture ?? WhiteTexture);

        if (shader.HasUniform(Shader.ColorUniform))
            shader.SetUniform(Shader.ColorUniform, Color.White);
        
        Matrix4x4 mat = Matrix4x4.Identity;
        Bgfx.set_transform(&mat, 1);

        Bgfx.submit(curViewId, shader.Activate(WhiteTexture), 0, (byte)Bgfx.DiscardFlags.All);
    }

    public void DrawTriangle(float x0, float y0, float x1, float y1, float x2, float y2)
    {
        using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Triangles);
        draw.Vertex(x0, y0);
        draw.Vertex(x1, y1);
        draw.Vertex(x2, y2);
    }

    public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Triangles);
        draw.Vertex(a);
        draw.Vertex(b);
        draw.Vertex(c);
    }

    public void DrawRectangle(float x, float y, float w, float h)
    {
        using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Quads);
        draw.Vertex(x, y);
        draw.Vertex(x, y+h);
        draw.Vertex(x+w, y+h);
        draw.Vertex(x+w, y);
    }

    public void DrawRectangle(Vector2 origin, Vector2 size) => DrawRectangle(origin.X, origin.Y, size.X, size.Y);
    public void DrawRectangle(Rectangle rectangle) => DrawRectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);

    public void DrawLine(float x0, float y0, float x1, float y1)
    {
        if (UseGlLines)
        {
            using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Lines);
            draw.Vertex(x0, y0);
            draw.Vertex(x1, y1);
        }
        else
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            if (dx == 0f && dy == 0f) return;

            var dist = MathF.Sqrt(dx*dx + dy*dy);

            var perpX = dy / dist * LineWidth / 2f;
            var perpY = -dx / dist * LineWidth / 2f;

            using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Triangles);
            draw.Vertex(x0 + perpX, y0 + perpY);
            draw.Vertex(x0 - perpX, y0 - perpY);
            draw.Vertex(x1 - perpX, y1 - perpY);
            draw.Vertex(x1 + perpX, y1 + perpY);
            draw.Vertex(x0 + perpX, y0 + perpY);
            draw.Vertex(x1 - perpX, y1 - perpY);
        }
    }

    public void DrawLine(Vector2 a, Vector2 b) => DrawLine(a.X, a.Y, b.X, b.Y);

    public void DrawRectangleLines(float x, float y, float w, float h)
    {
        if (UseGlLines)
        {
            using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Lines);

            draw.Vertex(x, y);
            draw.Vertex(x, y + h);

            draw.Vertex(x, y + h);
            draw.Vertex(x + w, y + h);

            draw.Vertex(x + w, y + h);
            draw.Vertex(x + w, y);

            draw.Vertex(x + w, y);
            draw.Vertex(x, y);
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

        using var draw = _drawBatch.BeginBatchDraw(BatchDrawMode.Quads);

        // NOTE: Every QUAD actually represents two segments
        for (int i = 0; i < segments / 2; i++)
        {
            draw.Vertex(x0, y0);
            draw.Vertex(x0 + MathF.Cos(angle + stepLength * 2f) * radius, y0 + MathF.Sin(angle + stepLength * 2f) * radius);
            draw.Vertex(x0 + MathF.Cos(angle + stepLength) * radius, y0 + MathF.Sin(angle + stepLength) * radius);
            draw.Vertex(x0 + MathF.Cos(angle) * radius, y0 + MathF.Sin(angle) * radius);
            angle += 2f * stepLength;
        }

        // NOTE: In case number of segments is odd, we add one last piece to the cake
        if ((((uint)segments)%2) == 1)
        {
            draw.Vertex(x0, y0);
            draw.Vertex(x0 + MathF.Cos(angle + stepLength) * radius, y0 + MathF.Sin(angle + stepLength) * radius);
            draw.Vertex(x0 + MathF.Cos(angle) * radius, y0 + MathF.Sin(angle) * radius);
            draw.Vertex(x0, y0);
        }

        /*for (int i = 0; i < segments; i++)
        {
            BeginBatchDraw(3);
            PushVertex(x0, y0);
            PushVertex(x0 + MathF.Cos(angle + stepLength) * radius, y0 + MathF.Sin(angle + stepLength) * radius);
            PushVertex(x0 + MathF.Cos(angle) * radius, y0  + MathF.Sin(angle) * radius);
            angle += stepLength;
        }*/
    }

    public void DrawRingSector(float x0, float y0, float radius, float startAngle, float endAngle, int segments)
    {
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

    #endregion

    /*public Mesh CreateMesh(MeshConfiguration config, int vertexCount)
    {
        if (config.Indexed) throw new Exception("Missing index count");
        return new(config, vertexCount);
    }
    
    public Mesh CreateMesh(MeshConfiguration config, ReadOnlySpan<short> indices, int vertexCount)
    {
        if (!config.Indexed || config.Use32BitIndices) throw new Exception("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<short> data);
        indices.CopyTo(data);
        return mesh;
    }

    public Mesh CreateMesh(MeshConfiguration config, ReadOnlySpan<int> indices, int vertexCount)
    {
        if (!config.Indexed || !config.Use32BitIndices) throw new Exception("Incompatible index options for MeshConfiguration");
        var mesh = new Mesh(config, vertexCount, indices.Length);
        mesh.GetIndexBufferSpan(out Span<int> data);
        indices.CopyTo(data);
        return mesh;
    }

    public Texture CreateTexture(Image image)
        => new(image);
    
    public Texture CreateTexture(int width, int height, PixelFormat format)
        => new(width, height, format)*/

    /*public unsafe void Draw(Mesh mesh)
    {
        _drawBatch.Draw();

        fixed (Matrix4x4* mat = &TransformMatrix)
        {
            Bgfx.set_transform(mat, 1);
        }

        mesh.Activate();
        Bgfx.set_state((ulong)Bgfx.StateFlags.Default, 0);
        Bgfx.submit(curViewId, (shaderValue ?? defaultShader).Activate(WhiteTexture), 0, (byte)Bgfx.DiscardFlags.All);
    }*/
}