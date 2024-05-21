namespace Glib;

using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public class Window : IDisposable
{
    private readonly IWindow window;

    public int Width { get => window.Size.X; }
    public int Height { get => window.Size.Y; }
    public double Time { get => window.Time; }

    public event Action? Load;
    public event Action<float>? Update;
    public event Action<float, RenderContext>? Draw;
    public event Action<int, int>? Resize;
    public event Action? Closing;

    public event Action<Key, int>? KeyDown;
    public event Action<Key, int>? KeyUp;
    public event Action<char>? KeyChar;

    public event Action<float, float>? MouseMove;
    public event Action<int>? MouseDown;
    public event Action<int>? MouseUp;

    private Vector2 _mousePos = Vector2.Zero;
    private readonly List<Key> keyList = []; // The list of currently pressed keys
    private readonly List<Key> pressList = []; // The list of keys that was pressed on this frame

    public float MouseX { get => _mousePos.X; }
    public float MouseY { get => _mousePos.Y; }

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

    public void Close()
    {
        window.Close();
    }

    private void OnLoad()
    {
        IInputContext input = window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += OnKeyDown;
            input.Keyboards[i].KeyUp += OnKeyUp;
            input.Keyboards[i].KeyChar += OnKeyChar;
        }

        if (input.Mice.Count > 0)
        {
            var mouse = input.Mice[0];
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
        }

        _renderContext = new RenderContext(window);
        Load?.Invoke();
    }

    private void OnKeyChar(IKeyboard keyboard, char @char)
    {
        KeyChar?.Invoke(@char);
    }

    private void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int keyCode)
    {
        var k = (Key)(int)key;

        if (!keyList.Contains(k))
            keyList.Add(k);
        pressList.Add(k);
        
        KeyDown?.Invoke(k, keyCode);
    }

    private void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int keyCode)
    {
        var k = (Key)(int)key;
        keyList.Remove(k);

        KeyUp?.Invoke((Key)(int)key, keyCode);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _mousePos = position;
        MouseMove?.Invoke(position.X, position.Y);
    }

    /// <summary>
    /// Check if a given key is held down.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key is down, false if not.</returns>
    public bool IsKeyDown(Key key) => keyList.Contains(key);

    /// <summary>
    /// Check if a given key is not held down.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key is up, false if not.</returns>
    public bool IsKeyUp(Key key) => !keyList.Contains(key);

    /// <summary>
    /// Check if a given key was pressed on this frame.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key was pressed on this frame.</returns>
    public bool IsKeyPressed(Key key) => pressList.Contains(key);

    private static int GetMouseButtonIndex(MouseButton button){
        return button switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => -1
        };
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        int intBtn = GetMouseButtonIndex(button);
        if (intBtn == -1) return;
        MouseDown?.Invoke(intBtn);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        int intBtn = GetMouseButtonIndex(button);
        if (intBtn == -1) return;
        MouseUp?.Invoke(intBtn);
    }

    private void OnUpdate(double dt)
    {
        GLResource.UnloadGCQueue();
        Update?.Invoke((float)dt);
    }

    private void OnRender(double dt)
    {
        // set transform matrix to have coordinates drawn in
        // pixel space
        var winSize = window.Size;
        _renderContext!.BaseTransform =
            Matrix4x4.CreateScale(new Vector3(1f / winSize.X * 2f, -1f / winSize.Y * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, 1f, 0f));

        _renderContext!.Clear();
        _renderContext!.ClearTransformationStack();
        _renderContext!.ResetTransform();
        
        Draw?.Invoke((float)dt, _renderContext!);
        _renderContext!.DrawBatch();

        pressList.Clear();
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        _renderContext!.Resize((uint)newSize.X, (uint)newSize.Y);
        Resize?.Invoke(newSize.X, newSize.Y);
    }

    private void OnClose()
    {
        Closing?.Invoke();
        GLResource.UnloadGCQueue();
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
        GC.SuppressFinalize(this);
    }
}