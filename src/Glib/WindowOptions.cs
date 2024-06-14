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
    /// True if the window should set up an ImGui controller
    /// on load. This is false by default.
    /// </summary>
    public bool SetupImGui = false;

    public WindowOptions() {}

    internal readonly IWindow CreateSilkWindow()
    {
        var opts = Silk.NET.Windowing.WindowOptions.Default;
        var posX = X ?? 50;
        var posY = Y ?? 50;
        
        // for some reason, Silk.NET does not allow the user to
        // use the platform backend default position
        // even though the comments for the Position property say
        // that setting it to (-1, -1) will do that (it doesn't).
        // so, i make it so that if X or Y is null, then it will
        // force-hide the window initially, center the window once it
        // is created, and then show the window.
        bool centerOnCreation = X is null || Y is null;

        opts.UpdatesPerSecond = RefreshRate;
        opts.FramesPerSecond =  RefreshRate;
        opts.Position = new(posX, posY);
        opts.Size = new(Width, Height);
        opts.VSync = VSync;
        opts.Title = Title;
        opts.IsVisible = Visible && !centerOnCreation;
        opts.IsEventDriven = IsEventDriven;
        opts.WindowBorder = Border switch
        {
            WindowBorder.Resizable => Silk.NET.Windowing.WindowBorder.Resizable,
            WindowBorder.Fixed => Silk.NET.Windowing.WindowBorder.Fixed,
            WindowBorder.Hidden => Silk.NET.Windowing.WindowBorder.Hidden,
            _ => throw new Exception("Invalid WindowBorder enum value")
        };

        var win = Silk.NET.Windowing.Window.Create(opts);

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