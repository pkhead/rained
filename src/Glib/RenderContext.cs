using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using System.Numerics;
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

public enum LogLevel
{
    Debug,
    Information,
    Error
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
}

public sealed class RenderContext : IDisposable
{
    public static RenderContext? Instance { get; private set; } = null;
    private bool _disposed = false;
    private readonly GL gl;

    /*public int ScreenWidth { get; private set; }
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

    public static Action<LogLevel, string>? Log;

    internal static void LogInfo(string msg) => Log?.Invoke(LogLevel.Information, msg);
    internal static void LogError(string msg) => Log?.Invoke(LogLevel.Error, msg);

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
    public readonly RendererType GpuRendererType;

    private CallbackInterface _cbInterface;
    private Window _mainWindow;
    private List<(Window window, Framebuffer framebuffer)> _windows = [];

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

    public bool VSync { get; set; } = true;
    private bool _vsync;
    private List<(uint frameEnd, TaskCompletionSource tcs)> _waitingRequests = [];*/

    private RenderContext(Window mainWindow)
    {
        if (Instance is not null)
            throw new NotImplementedException("No more than one RenderContext allowed");
        Instance = this;
        
        gl = mainWindow.SilkWindow.CreateOpenGLES();
        var vendor = gl.GetStringS(StringName.Vendor);
        var renderer = gl.GetStringS(StringName.Renderer);

        Console.WriteLine(vendor);
        Console.WriteLine(renderer);
    }

    public static RenderContext Init(Window mainWindow)
    {
        return new RenderContext(mainWindow);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        gl.Dispose();
        GC.SuppressFinalize(this);
        Instance = null;
    }
}