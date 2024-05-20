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

    public WindowOptions() {}

    internal readonly Silk.NET.Windowing.WindowOptions ToSilk()
    {
        var opts = Silk.NET.Windowing.WindowOptions.Default;
        var posX = X ?? 50;
        var posY = Y ?? 50;

        opts.Position = new(posX, posY);
        opts.Size = new(Width, Height);
        opts.VSync = VSync;
        opts.Title = Title;
        opts.WindowBorder = Border switch
        {
            WindowBorder.Resizable => Silk.NET.Windowing.WindowBorder.Resizable,
            WindowBorder.Fixed => Silk.NET.Windowing.WindowBorder.Fixed,
            WindowBorder.Hidden => Silk.NET.Windowing.WindowBorder.Hidden,
            _ => throw new Exception("Invalid WindowBorder enum value")
        };

        return opts;
    }
}