namespace Glib;

using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public class Window : IDisposable
{
    private readonly IWindow window;

    public event Action? Load;
    public event Action<float>? Update;
    public event Action<float, RenderContext>? Draw;
    public event Action<int, int>? Resize;
    public event Action? Closing;

    private RenderContext? _renderContext = null;
    public RenderContext? RenderContext { get => _renderContext; }

    public Window(WindowOptions options)
    {
        window = Silk.NET.Windowing.Window.Create(options.ToSilk());

        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
        window.Closing += OnClose;
    }

    private void OnLoad()
    {
        IInputContext input = window.CreateInput();
        /*for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += KeyDown;
        }*/

        _renderContext = new RenderContext(window);
        Load?.Invoke();
    }

    private void OnUpdate(double dt)
    {
        Update?.Invoke((float)dt);
    }

    private void OnRender(double dt)
    {
        // set transform matrix to have coordinates drawn in
        // pixel space
        var winSize = window.Size;
        _renderContext!.TransformMatrix =
            Matrix4x4.CreateScale(new Vector3(1f / winSize.X * 2f, -1f / winSize.Y * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, 1f, 0f));

        _renderContext!.ClearBackground();
        
        Draw?.Invoke((float)dt, _renderContext!);
        _renderContext!.DrawBatch();
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        _renderContext!.Resize((uint)newSize.X, (uint)newSize.Y);
        Resize?.Invoke(newSize.X, newSize.Y);
    }

    private void OnClose()
    {
        Closing?.Invoke();
        _renderContext!.Dispose();
    }

    public void Run()
    {
        window.Run();
    }

    public void Dispose()
    {
        _renderContext?.Dispose();
        window.Dispose();
    }
}