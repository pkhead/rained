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

public enum CullMode {
    Front,
    Back,
    FrontAndBack
}

public enum Feature
{
    Blend,
    ScissorTest,
    DepthTest,
    CullFace,
    WireframeRendering
}

public enum DebugSeverity
{
    Notification,
    Low,
    Medium,
    High
};

public class RenderContext : IDisposable
{
    private bool _disposed = false;

    public int ScreenWidth { get; private set; }
    public int ScreenHeight { get; private set; }

    /// <summary>
    /// A 1x1 texture consisting of a single white pixel.
    /// Useful for placeholder texture slots.
    /// </summary>
    public Texture WhiteTexture { get; private set; }

    private readonly Shader defaultShader;
    private Shader? shaderValue;

    private readonly Stack<Matrix4x4> transformStack = [];
    //private readonly Stack<Framebuffer> framebufferStack = [];
    //private Framebuffer? curFramebuffer = null;
    private ushort curViewId = 0;

    internal Matrix4x4 BaseTransform = Matrix4x4.Identity;
    public Matrix4x4 TransformMatrix;
    public Color BackgroundColor = Color.Black;
    public Color DrawColor = Color.White;
    public float LineWidth = 1.0f;

    public TextureFilterMode DefaultTextureMagFilter = TextureFilterMode.Linear;
    public TextureFilterMode DefaultTextureMinFilter = TextureFilterMode.Linear;

    private DrawBatch _drawBatch;

    public Shader? Shader {
        get => shaderValue;
        set
        {
            if (shaderValue == value) return;
            //_drawBatch.Draw();
            shaderValue = value;
        }
    }

    public readonly string GpuVendor;
    public readonly string GpuRenderer;

    private CallbackInterface _cbInterface;

    internal unsafe RenderContext(IWindow window)
    {
        _cbInterface = new CallbackInterface();
        _cbInterface.Log = (string msg) => Console.WriteLine(msg);
        _cbInterface.Fatal = (string filePath, int line, Bgfx.Fatal code, string msg) => Console.WriteLine(msg);

        ScreenWidth = window.FramebufferSize.X;
        ScreenHeight = window.FramebufferSize.Y;

        var init = new Bgfx.Init();
        Bgfx.init_ctor(&init);

        init.type = Bgfx.RendererType.Count;
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
        WhiteTexture = new Texture(Image.FromColor(1, 1, Color.White));
        TransformMatrix = Matrix4x4.Identity;
        _drawBatch = new DrawBatch();
    }

    public void SetViewport(int width, int height)
    {
        Bgfx.set_view_rect(curViewId, 0, 0, (ushort)width, (ushort)height);
        BaseTransform =
            Matrix4x4.CreateScale(new Vector3(1f / width * 2f, -1f / height * 2f, 1f)) *
            Matrix4x4.CreateTranslation(new Vector3(-1f, 1f, 0f));
    }

    public void Begin(int width, int height)
    {
        curViewId = 0;

        uint bgCol;
        {
            var r = (uint)(Math.Clamp(BackgroundColor.R, 0f, 1f) * 255f);
            var g = (uint)(Math.Clamp(BackgroundColor.G, 0f, 1f) * 255f);
            var b = (uint)(Math.Clamp(BackgroundColor.B, 0f, 1f) * 255f);
            var a = (uint)(Math.Clamp(BackgroundColor.A, 0f, 1f) * 255f);
            bgCol = a | (b << 8) | (g << 16) | (r << 24);
        }

        ScreenWidth = width;
        ScreenHeight = height;

        SetViewport(width, height);
        Bgfx.set_view_clear(curViewId, (ushort)(Bgfx.ClearFlags.Color | Bgfx.ClearFlags.Depth), bgCol, 1f, 0);
        Bgfx.touch(curViewId); // ensure this view will be cleared even if no draw calls are submitted to it

        shaderValue = null;
        defaultShader.SetUniform(Shader.TextureUniform, WhiteTexture);
        defaultShader.SetUniform(Shader.ColorUniform, Color.White);
        _drawBatch.NewFrame(WhiteTexture);
        //curTexture = whiteTexture;
        //lastTexture = whiteTexture;
    }

    public void End()
    {
        Bgfx.frame(false);
        //_drawBatch.Draw();
        //InternalSetTexture(null);
    }

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

    public void Dispose()
    {
        Bgfx.shutdown();
        _cbInterface.Dispose();
        GC.SuppressFinalize(this);
    }
}