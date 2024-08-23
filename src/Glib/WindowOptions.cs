using Silk.NET.Windowing;

namespace Glib;

public enum WindowBorder
{
    Resizable,
    Fixed,
    Hidden
}

public struct WindowOptions
{
    /// <summary>
    /// The X position of the window. Set to null for the system default.
    /// </summary>
    public int? X = null;

    /// <summary>
    /// The Y position of the window. Set to null for the system default.
    /// </summary>
    public int? Y = null;

    /// <summary>
    /// The width of the window.
    /// </summary>
    public int Width = 1200;

    /// <summary>
    /// The height of the window.
    /// </summary>
    public int Height = 800;

    /// <summary>
    /// The title of the window.
    /// </summary>
    public string Title = "Window";

    /// <summary>
    /// The window's border mode.
    /// </summary>
    public WindowBorder Border = WindowBorder.Resizable;

    /// <summary>
    /// True if the window will have VSync enabled.
    /// </summary>
    public bool VSync = true;

    /// <summary>
    /// True if the window is visible.
    /// </summary>
    public bool Visible = true;

    /// <summary>
    /// If true, the Update and Draw events will be called when there are
    /// events to be processed in the queue. Otherwise, they will be
    /// called at a fixed interval.
    /// </summary>
    public bool IsEventDriven = false;

    /// <summary>
    /// The amount of times per second that Update and Render
    /// will be called, if IsEventDriven is set to false. If
    /// set to 0, the window will instead run as fast as
    /// the backend can support it.
    /// </summary>
    public int RefreshRate = 0;

    /// <summary>
    /// True if the window should set up the a default OpenGL debug error callback
    /// as soon as possible. If set to true, any OpenGL debug messages will be printed
    /// to Console.Out.
    /// </summary>
    public bool GlDebugContext = false;

    /// <summary>
    /// Reference to the window that the new window should share its GL context with.
    /// </summary>
    public Window? GlSharedContext = null;

    public WindowOptions() {}

    internal readonly IWindow CreateSilkWindow()
    {
        var opts = Silk.NET.Windowing.WindowOptions.Default;
        var posX = X ?? 50;
        var posY = Y ?? 50;
        bool centerOnCreation = X is null || Y is null;

        #if GLES
        opts.API = new GraphicsAPI(
            ContextAPI.OpenGLES,
            ContextProfile.Core,
            GlDebugContext ? ContextFlags.Debug : ContextFlags.Default,
            new APIVersion(3, 0)
        );
        #else
        opts.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            GlDebugContext ? ContextFlags.Debug : ContextFlags.Default,
            new APIVersion(3, 3)
        );
        #endif

        opts.IsContextControlDisabled = true;
        opts.ShouldSwapAutomatically = false;
        opts.UpdatesPerSecond = RefreshRate;
        opts.FramesPerSecond =  RefreshRate;
        opts.Position = new(posX, posY);
        opts.Size = new(Width, Height);
        opts.VSync = VSync;
        opts.Title = Title;
        opts.SharedContext = GlSharedContext?.SilkWindow.GLContext!;
        opts.IsVisible = Visible && !centerOnCreation;
        opts.IsEventDriven = IsEventDriven;
        opts.WindowBorder = Border switch
        {
            WindowBorder.Resizable => Silk.NET.Windowing.WindowBorder.Resizable,
            WindowBorder.Fixed => Silk.NET.Windowing.WindowBorder.Fixed,
            WindowBorder.Hidden => Silk.NET.Windowing.WindowBorder.Hidden,
            _ => throw new Exception("Invalid WindowBorder enum value")
        };

        IWindow win;
        if (GlSharedContext is not null)
            win = GlSharedContext.SilkWindow.CreateWindow(opts);
        else
            win = Silk.NET.Windowing.Window.Create(opts);
        
        if (centerOnCreation)
        {
            bool vis = Visible;
            win.Load += () =>
            {
                win.Center();
                if (vis) win.IsVisible = true;
            };
        }

        return win;
    }
}