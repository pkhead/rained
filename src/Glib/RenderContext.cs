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
    None,
    Normal,

    /// <summary>
    /// Not a standard mode, but I don't feel the need to expose
    /// the entirety of the blending configuration only for me to
    /// ultimately use only two of them.
    /// </summary>
        
    CorrectedFramebufferNormal
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
    DepthTest = 1,
    WireframeRendering = 2
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
    public Framebuffer? Framebuffer => curFramebuffer;

    private ushort curViewId = 0;
    internal ushort CurrentBgfxViewId => curViewId;
    private bool _viewHasSubmission = false;

    public Matrix4x4 TransformMatrix { get => _drawBatch.TransformMatrix; set => _drawBatch.TransformMatrix = value; }
    public Color BackgroundColor = Color.Black;
    public ref Color DrawColor => ref _drawBatch.DrawColor;
    public Shader? Shader { get => _drawBatch.Shader; set => _drawBatch.Shader = value; }
    public uint Frame { get; internal set; } = 0;

    /// <summary>
    /// If Glib should use GL_LINES primitives for drawing lines.
    /// When enabled, the LineWidth field will not be respected, and all
    /// lines will be drawn with a width of 1
    /// </summary>
    public bool UseGlLines = true;
    public float LineWidth = 1f;
    private RenderFlags _flags = RenderFlags.None;
    private CullMode _cullMode = CullMode.Clockwise;
    private BlendMode _blendMode = BlendMode.Normal;

    private int _scissorX, _scissorY, _scissorW, _scissorH;
    private bool _scissorEnabled = false;

    private readonly DrawBatch _drawBatch;

    public readonly string GpuVendor;
    public readonly string GpuRenderer;

    private CallbackInterface _cbInterface;

    // Wtf
    public bool OriginBottomLeft
    {
        get
        {
            unsafe
            {
                var caps = Bgfx.get_caps();
                return caps->originBottomLeft != 0;
            }
        }
    }

    public RenderFlags Flags
    {
        get => _flags;
        set
        {
            if (_flags != value) _drawBatch.Draw();
            _flags = value;
        }
    }

    public CullMode CullMode
    {
        get => _cullMode;
        set
        {
            if (_cullMode != value) _drawBatch.Draw();
            _cullMode = value;
        }
    }

    public BlendMode BlendMode
    {
        get => _blendMode;
        set
        {
            if (_blendMode != value) _drawBatch.Draw();
            _blendMode = value;
        }
    }

    public static bool VSync { get; set; } = true;
    private List<(uint frameEnd, TaskCompletionSource tcs)> _waitingRequests = [];

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
        init.resolution.reset = (uint) ((VSync ? Bgfx.ResetFlags.Vsync : Bgfx.ResetFlags.None) | Bgfx.ResetFlags.FlipAfterRender);
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

        for (int i = _waitingRequests.Count - 1; i >= 0; i--)
        {
            if (_waitingRequests[i].frameEnd == Frame)
            {
                _waitingRequests[i].tcs.SetCanceled();
                _waitingRequests.RemoveAt(i);
            }
        }

        Resource.DisposeRemaining();
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
            Bgfx.reset((uint)width, (uint)height, (uint)(VSync ? Bgfx.ResetFlags.Vsync : Bgfx.ResetFlags.None), Bgfx.TextureFormat.Count);
            ScreenWidth = width;
            ScreenHeight = height;
        }

        SetViewport(ScreenWidth, ScreenHeight);
        Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);
        Bgfx.set_view_clear(curViewId, (ushort)(Bgfx.ClearFlags.Color | Bgfx.ClearFlags.Depth), bgCol, 1f, 0);
        Bgfx.touch(curViewId); // ensure this view will be cleared even if no draw calls are submitted to it
        _viewHasSubmission = true;

        transformStack.Clear();
        TransformMatrix = Matrix4x4.Identity;

        defaultShader.SetUniform(Shader.TextureUniform, WhiteTexture);
        defaultShader.SetUniform(Shader.ColorUniform, Color.White);
        _drawBatch.NewFrame(WhiteTexture);
        //curTexture = whiteTexture;
        //lastTexture = whiteTexture;

        for (int i = _waitingRequests.Count - 1; i >= 0; i--)
        {
            if (_waitingRequests[i].frameEnd == Frame)
            {
                _waitingRequests[i].tcs.SetResult();
                _waitingRequests.RemoveAt(i);
            }
        }
    }

    private static Bgfx.StateFlags BgfxStateBlendFuncSeparate(Bgfx.StateFlags srcRgb, Bgfx.StateFlags dstRgb, Bgfx.StateFlags srcA, Bgfx.StateFlags dstA)
    {
        return (Bgfx.StateFlags)(0
            | ( ( (ulong)(srcRgb)|( (ulong)(dstRgb)<<4) )   )
            | ( ( (ulong)(srcA  )|( (ulong)(dstA  )<<4) )<<8)
        );
    }

    private static Bgfx.StateFlags BgfxBlendEquationSeparate(Bgfx.StateFlags equationRGB, Bgfx.StateFlags equationA)
    {
        return (Bgfx.StateFlags)(( (ulong)(equationRGB)|( (ulong)(equationA)<<3) ));
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
            int x = _scissorX;
            int y = _scissorY;
            int w = Framebuffer?.Width ?? ScreenWidth;
            int h = Framebuffer?.Height ?? ScreenHeight;

            int right = _scissorX + _scissorW;
            int bot = _scissorY + _scissorH;
            x = Math.Clamp(x, 0, w);
            y = Math.Clamp(y, 0, h);
            right = Math.Clamp(right, 0, w);
            bot = Math.Clamp(bot, 0, h);

            if (right - x <= 0 || bot - y <= 0)
                Bgfx.set_scissor(0, 0, 0, 0);
            else
                Bgfx.set_scissor((ushort)x, (ushort)y, (ushort)(right - x), (ushort)(bot - y));
        }

        var state = Bgfx.StateFlags.None;
        state |= Bgfx.StateFlags.WriteRgb | Bgfx.StateFlags.WriteA | Bgfx.StateFlags.Msaa;
        switch (BlendMode)
        {
            case BlendMode.Normal:
                state |= BgfxStateBlendFunc(Bgfx.StateFlags.BlendSrcAlpha, Bgfx.StateFlags.BlendInvSrcAlpha);
                break;
            
            case BlendMode.CorrectedFramebufferNormal:
                state |= BgfxStateBlendFuncSeparate(Bgfx.StateFlags.BlendSrcAlpha, Bgfx.StateFlags.BlendInvSrcAlpha, Bgfx.StateFlags.BlendOne, Bgfx.StateFlags.BlendInvSrcAlpha);
                state |= BgfxBlendEquationSeparate(Bgfx.StateFlags.BlendEquationAdd, Bgfx.StateFlags.BlendEquationAdd);
                break;
        }
        if (Flags.HasFlag(RenderFlags.DepthTest)) state |= Bgfx.StateFlags.DepthTestLess;
        if (CullMode == CullMode.Clockwise) state |= Bgfx.StateFlags.CullCw;
        else if (CullMode == CullMode.Counterclockwise) state |= Bgfx.StateFlags.CullCcw;
        return state;
    }

    public void SetScissorBox(int x, int y, int w, int h)
    {
        _drawBatch.Draw();
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
        _drawBatch.Draw();
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

        _viewHasSubmission = true;
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

            rctx._viewHasSubmission = true;
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
    
    public void Clear(ClearFlags clearFlags, Color clearColor)
    {
        var bclearFlags = Bgfx.ClearFlags.None;
        if (clearFlags.HasFlag(ClearFlags.Color)) bclearFlags |= Bgfx.ClearFlags.Color;
        if (clearFlags.HasFlag(ClearFlags.Depth)) bclearFlags |= Bgfx.ClearFlags.Depth;
        if (clearFlags.HasFlag(ClearFlags.Stencil)) bclearFlags |= Bgfx.ClearFlags.Stencil;

        var colorUint = ColorToUint(clearColor);

        // if the view already has submissions, then it is required to enter a new one
        // otherwise, we can stay in the same one
        if (_viewHasSubmission)
        {
            _viewHasSubmission = false;
            curViewId++;

            if (curFramebuffer is not null) Bgfx.set_view_frame_buffer(curViewId, curFramebuffer.Handle);
            Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);

            if (curFramebuffer is not null)
                SetViewport(curFramebuffer.Width, curFramebuffer.Height);
            else
                SetViewport(ScreenWidth, ScreenHeight);
        }

        Bgfx.set_view_clear(curViewId, (ushort)bclearFlags, colorUint, 1f, 0);
    }

    public void Clear() => Clear(ClearFlags.Color | ClearFlags.Depth | ClearFlags.Stencil, BackgroundColor);
    public void Clear(Color clearColor) => Clear(ClearFlags.Color | ClearFlags.Depth | ClearFlags.Stencil, clearColor);

    public void PushFramebuffer(Framebuffer framebuffer)
    {
        var lastFb = curFramebuffer;

        if (curFramebuffer is not null)
            framebufferStack.Push(curFramebuffer);

        _drawBatch.Draw();
        curFramebuffer = framebuffer;

        // change to a new pass if the framebuffer is different
        if (curFramebuffer != lastFb)
        {
            if (!_viewHasSubmission) Bgfx.touch(curViewId);

            curViewId++;
            _viewHasSubmission = false;
            
            Bgfx.set_view_frame_buffer(curViewId, curFramebuffer.Handle);
            Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);
            Bgfx.set_view_clear(curViewId, (ushort)Bgfx.ClearFlags.None, 0, 1f, 0);
            SetViewport(curFramebuffer.Width, curFramebuffer.Height);
        }
    }

    public Framebuffer? PopFramebuffer()
    {
        _drawBatch.Draw();
        if (!_viewHasSubmission) Bgfx.touch(curViewId);

        int newWidth, newHeight;
        var lastFb = curFramebuffer;

        if (framebufferStack.TryPop(out Framebuffer? framebuffer))
        {
            newWidth = framebuffer.Width;
            newHeight = framebuffer.Height;
        }
        else
        {
            newWidth = ScreenWidth;
            newHeight = ScreenHeight;
        }

        // if framebuffer has changed, a new view is required
        if (curFramebuffer != framebuffer)
        {
            curFramebuffer = framebuffer;
            curViewId++;

            if (curFramebuffer is not null) Bgfx.set_view_frame_buffer(curViewId, curFramebuffer.Handle);
            Bgfx.set_view_mode(curViewId, Bgfx.ViewMode.Sequential);
            Bgfx.set_view_clear(curViewId, (ushort)Bgfx.ClearFlags.None, 0, 1f, 0);
            SetViewport(newWidth, newHeight);
        }
        
        return curFramebuffer;
    }

    internal void End()
    {
        _drawBatch.Draw();
        if (!_viewHasSubmission) Bgfx.touch(curViewId);
        Frame = Bgfx.frame(false);
        Resource.Housekeeping();
    }

    internal Task WaitUntilFrame(uint frameNum)
    {
        var tcs = new TaskCompletionSource();
        _waitingRequests.Add((frameNum, tcs));
        return tcs.Task;
    }

    #region Transform

    public void ResetTransform()
    {
        TransformMatrix = Matrix4x4.Identity;
    }
    
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

        _viewHasSubmission = true;
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
    
    public BatchDrawHandle BeginBatchDraw(BatchDrawMode mode, Texture? tex = null) =>
        _drawBatch.BeginBatchDraw(mode, tex);

    #endregion
}