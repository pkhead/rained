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
    /// set to 0, the window will instead use a default value.
    /// </summary>
    public int RefreshRate = 0;

    public WindowOptions() {}

    internal readonly Silk.NET.Windowing.IWindow CreateSilkWindow()
    {
        var opts = Silk.NET.Windowing.WindowOptions.Default;
        var posX = X ?? 50;
        var posY = Y ?? 50;

        opts.UpdatesPerSecond = RefreshRate;
        opts.FramesPerSecond =  RefreshRate;
        opts.Position = new(posX, posY);
        opts.Size = new(Width, Height);
        opts.VSync = VSync;
        opts.Title = Title;
        opts.IsVisible = Visible;
        opts.IsEventDriven = IsEventDriven;
        opts.IsVisible = Visible;
        opts.WindowBorder = Border switch
        {
            WindowBorder.Resizable => Silk.NET.Windowing.WindowBorder.Resizable,
            WindowBorder.Fixed => Silk.NET.Windowing.WindowBorder.Fixed,
            WindowBorder.Hidden => Silk.NET.Windowing.WindowBorder.Hidden,
            _ => throw new Exception("Invalid WindowBorder enum value")
        };

        var win = Silk.NET.Windowing.Window.Create(opts);
        //if (X is null || Y is null)
        //{
        //    Silk.NET.Windowing.WindowExtensions.Center(win);
        //}

        return win;
    }
}