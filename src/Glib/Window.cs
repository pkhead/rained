﻿namespace Glib;

using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}

public enum WindowTheme
{
    Default,
    Light,
    Dark
}

public class Window : IDisposable
{
    private readonly IWindow window;
    private IInputContext inputContext = null!;

    public IWindow SilkWindow => window;
    public IInputContext SilkInputContext => inputContext;

    private double deltaTime = 0.0;
    public double DeltaTime => deltaTime;

    public int Width { get => window.Size.X; }
    public int Height { get => window.Size.Y; }
    public int PixelWidth { get => window.FramebufferSize.X; }
    public int PixelHeight { get => window.FramebufferSize.Y; }
    public double Time { get => window.Time; }
    public bool Visible { get => window.IsVisible; set => window.IsVisible = value; }
    public bool IsClosing { get => window.IsClosing; set => window.IsClosing = value; }
    public string Title { get => window.Title; set => window.Title = value; }
    public WindowState WindowState { get => window.WindowState; set => window.WindowState = value; }

    /// <summary>
    /// The content scale of the window. Only valid after Initialize has been called.
    /// <br /><br />
    /// Supported only for the GLFW backend. On other backends, this always reports Vector2.One
    /// </summary>
    public Vector2 ContentScale
    {
        get
        {
            unsafe
            {
                var glfwWindow = Silk.NET.Windowing.Glfw.GlfwWindowing.GetHandle(window);
                if (glfwWindow is null)
                {
                    return Vector2.One;
                }
                
                try
                {
                    MoreGlfw.GlfwGetWindowContentScale(glfwWindow, out float xScale, out float yScale);
                    return new Vector2(xScale, yScale);
                }
                catch (DllNotFoundException)
                {
                    return Vector2.One;
                }
            }
        }
    }

    /// <summary>
    /// The theme of the window's titlebar. It is always
    /// Default before Initialize is called.
    /// </summary>
    public WindowTheme Theme { get; private set; } = WindowTheme.Default;

    public event Action? Load;
    public event Action? ImGuiConfigure;
    public event Action<float>? Update;
    public event Action<float, RenderContext>? Draw;
    public event Action<int, int>? Resize;
    public event Action? Closing;
    public event Action<Vector2>? ContentScaleChanged;

    public event Action<Key, int>? KeyDown;
    public event Action<Key, int>? KeyUp;
    public event Action<char>? KeyChar;

    public event Action<float, float>? MouseMove;
    public event Action<MouseButton>? MouseDown;
    public event Action<MouseButton>? MouseUp;

    private Vector2 _mousePos = Vector2.Zero;
    private readonly List<Key> keyList = []; // The list of currently pressed keys
    private readonly List<Key> pressList = []; // The list of keys that was pressed on this frame
    private readonly List<Key> releaseList = []; // The list of keys that was released on this frame

    public float MouseX { get => _mousePos.X; }
    public float MouseY { get => _mousePos.Y; }
    public Vector2 MouseWheel {
        get
        {
            var wheel = inputContext.Mice[0].ScrollWheels[0];
            return new Vector2(wheel.X, wheel.Y);
        }
    }

    private bool setupGlErrorCallback = false;

    public Vector2 MousePosition {
        get => _mousePos;
        set
        {
            _mousePos = value;
            inputContext.Mice[0].Position = value;
        }    
    }

    private RenderContext? _renderContext = null;
    public RenderContext? RenderContext { get => _renderContext; }

    //private ImGuiController? imGuiController = null;
    private bool setupImGui;
    //public ImGuiController? ImGuiController => imGuiController;

    private double lastTime = 0.0;

    public Window(WindowOptions options)
    {
        window = options.CreateSilkWindow();
        setupImGui = options.SetupImGui;
        setupGlErrorCallback = options.SetupGlErrorCallback;

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
        unsafe
        {
            var glfwWindow = Silk.NET.Windowing.Glfw.GlfwWindowing.GetHandle(window);

            if (glfwWindow is not null)
            {
                MoreGlfw.InitializeDllImportResolver();

                _glfwContentScaleChangedCallback = UnsafeOnContentScaleChanged;
                var ptr = Marshal.GetFunctionPointerForDelegate(_glfwContentScaleChangedCallback);

                try
                {
                    MoreGlfw.GlfwSetWindowContentScaleCallback(glfwWindow, ptr);
                }
                catch (DllNotFoundException)
                {}
            }
        }
        
        PlatformSpecific.TryWindowTheme(window, out WindowTheme theme);
        Theme = theme;

        IInputContext input = window.CreateInput();
        inputContext = input;

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

        if (setupImGui)
        {
            /*imGuiController = new ImGuiController(
                gl: _renderContext.gl,
                view: window,
                input: input,
                onConfigureIO: OnConfigureImGuiIO
            );*/
        }

        Load?.Invoke();

        lastTime = window.Time;
    }

    private void OnConfigureImGuiIO()
    {
        ImGuiConfigure?.Invoke();
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
        releaseList.Add(k);

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
    /// Check if a given key was released on this frame
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key was released, false if not.</returns>
    public bool IsKeyReleased(Key key) => !releaseList.Contains(key);

    /// <summary>
    /// Check if a given key was pressed on this frame.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>True if the key was pressed on this frame.</returns>
    public bool IsKeyPressed(Key key) => pressList.Contains(key);

    private static MouseButton? GetMouseButtonIndex(Silk.NET.Input.MouseButton button){
        return button switch
        {
            Silk.NET.Input.MouseButton.Left => MouseButton.Left,
            Silk.NET.Input.MouseButton.Right => MouseButton.Right,
            Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
            _ => null
        };
    }

    private void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        MouseButton? intBtn = GetMouseButtonIndex(button);
        if (intBtn is null) return;
        MouseDown?.Invoke(intBtn.Value);
    }

    private void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        MouseButton? intBtn = GetMouseButtonIndex(button);
        if (intBtn is null) return;
        MouseUp?.Invoke(intBtn.Value);
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
        
        _renderContext!.Begin(window.FramebufferSize.X, window.FramebufferSize.Y);
        Draw?.Invoke((float)dt, _renderContext!);
        _renderContext!.End();
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Resize?.Invoke(newSize.X, newSize.Y);
    }

    private void OnClose()
    {
        Closing?.Invoke();
    }

    private unsafe void UnsafeOnContentScaleChanged(nint _, float xscale, float yscale)
    {
        ContentScaleChanged?.Invoke(new Vector2(xscale, yscale));
    }

    public void SetSize(int width, int height)
    {
        window.Size = new Vector2D<int>(width, height);
    }

    private delegate void GlfwContentScaleChanged(nint window, float xscale, float yscale);
    private GlfwContentScaleChanged? _glfwContentScaleChangedCallback = null;

    public void Initialize()
    {
        window.Initialize();
    }

    public void MakeCurrent()
    {
        window.MakeCurrent();
    }

    public void PollEvents()
    {
        pressList.Clear();
        releaseList.Clear();

        deltaTime = window.Time - lastTime;
        lastTime = window.Time;
        window.DoEvents();
    }

    public void SwapBuffers()
    {
        if (window.GLContext is not null)
        {
            window.GLContext.SwapInterval(window.VSync?1:0);
            window.GLContext.SwapBuffers();
        }
    }

    public void DoUpdate()
    {
        window.DoUpdate();
    }

    public void DoRender()
    {
        _renderContext!.Begin(Width, Height);
        window.DoRender();
        _renderContext!.End();
    }

    public void BeginRender()
    {
        _renderContext!.Begin(Width, Height);
    }

    public void EndRender()
    {
        _renderContext!.End();
    }

    public void SetIcon(ReadOnlySpan<Image> icons)
    {
        // convert Glib.Images into rawImages
        // (ImageSharp does not store image memory in a contiguous region)
        Silk.NET.Core.RawImage[] rawImages = new Silk.NET.Core.RawImage[icons.Length];

        for (int i = 0; i < rawImages.Length; i++)
        {
            var srcImage = icons[i];
            byte[] pixels = new byte[srcImage.Width * srcImage.Height * srcImage.BytesPerPixel];
            
            if (srcImage.PixelFormat != PixelFormat.RGBA)
            {
                using var converted = srcImage.ConvertToFormat(PixelFormat.RGBA);
                converted.CopyPixelDataTo(pixels);
            }
            else
            {
                srcImage.CopyPixelDataTo(pixels);
            }
            
            rawImages[i] = new Silk.NET.Core.RawImage(srcImage.Width, srcImage.Height, pixels);
        }

        window.SetWindowIcon(rawImages);
    }

    public void Dispose()
    {
        Closing?.Invoke();
        //imGuiController?.Dispose();
        _renderContext!.Dispose();
        window.Dispose();
        GC.SuppressFinalize(this);
    }
}