using System.Numerics;
namespace Raylib_cs;

enum TraceLogLevel
{
    Warning
}

enum ConfigFlags
{
    ResizableWindow,
    HiddenWindow,
    VSyncHint
}

enum KeyboardKey
{
    //
    // Summary:
    //     NULL, used for no key pressed
    Null = 0,
    Apostrophe = 39,
    Comma = 44,
    Minus = 45,
    Period = 46,
    Slash = 47,
    Zero = 48,
    One = 49,
    Two = 50,
    Three = 51,
    Four = 52,
    Five = 53,
    Six = 54,
    Seven = 55,
    Eight = 56,
    Nine = 57,
    Semicolon = 59,
    Equal = 61,
    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,
    Space = 32,
    Escape = 256,
    Enter = 257,
    Tab = 258,
    Backspace = 259,
    Insert = 260,
    Delete = 261,
    Right = 262,
    Left = 263,
    Down = 264,
    Up = 265,
    PageUp = 266,
    PageDown = 267,
    Home = 268,
    End = 269,
    CapsLock = 280,
    ScrollLock = 281,
    NumLock = 282,
    PrintScreen = 283,
    Pause = 284,
    F1 = 290,
    F2 = 291,
    F3 = 292,
    F4 = 293,
    F5 = 294,
    F6 = 295,
    F7 = 296,
    F8 = 297,
    F9 = 298,
    F10 = 299,
    F11 = 300,
    F12 = 301,
    LeftShift = 340,
    LeftControl = 341,
    LeftAlt = 342,
    LeftSuper = 343,
    RightShift = 344,
    RightControl = 345,
    RightAlt = 346,
    RightSuper = 347,
    KeyboardMenu = 348,
    LeftBracket = 91,
    Backslash = 92,
    RightBracket = 93,
    Grave = 96,
    Kp0 = 320,
    Kp1 = 321,
    Kp2 = 322,
    Kp3 = 323,
    Kp4 = 324,
    Kp5 = 325,
    Kp6 = 326,
    Kp7 = 327,
    Kp8 = 328,
    Kp9 = 329,
    KpDecimal = 330,
    KpDivide = 331,
    KpMultiply = 332,
    KpSubtract = 333,
    KpAdd = 334,
    KpEnter = 335,
    KpEqual = 336,
    Back = 4,
    Menu = 82,
    VolumeUp = 24,
    VolumeDown = 25
}

struct Color(byte r, byte g, byte b, byte a)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public Color(int r, int g, int b, int a) : this((byte)r, (byte)g, (byte)b, (byte)a)
    {}

    public static Color White => new(255, 255, 255, 255);
    public static Color Black => new(0, 0, 0, 255);
    public static Color DarkGray => new(80, 80, 80, 255);
    public static Color Red => new(230, 41, 55, 255);
    public static Color Blue => new(0, 121, 241, 255);
    public static Color Blank => new(0, 0, 0, 0);
}

struct RenderTexture2D
{
    public Glib.Framebuffer? ID;

    public Texture2D Texture => new()
    {
        ID = ID!.GetTexture(0)
    };
}

struct Shader
{
    public Glib.Shader? ID;
}

enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}

struct Rectangle(float x, float y, float width, float height)
{
    public float X = x;
    public float Y = y;
    public float Width = width;
    public float Height = height;

    public Rectangle(Vector2 origin, Vector2 size) : this(origin.X, origin.Y, size.X, size.Y)
    {}

    public Rectangle(Vector2 origin, float width, float height) : this(origin.X, origin.Y, width, height)
    {}

    public readonly Vector2 Position => new(X, Y);
    public readonly Vector2 Size => new(Width, Height);
}

struct Image
{
    public Glib.Image? image;

    public readonly int Width => image!.Width;
    public readonly int Height => image!.Height;
    public readonly PixelFormat Format => image!.PixelFormat switch
    {
        Glib.PixelFormat.Grayscale => PixelFormat.UncompressedGrayscale,
        Glib.PixelFormat.RGBA => PixelFormat.UncompressedR8G8B8A8,
        _ => throw new NotImplementedException(image!.PixelFormat.ToString())
    };
    public readonly byte[] Data => image!.Pixels;
}

struct Texture2D
{
    public Glib.Texture? ID;

    public readonly int Width => ID!.Width;
    public readonly int Height => ID!.Height;
}

enum PixelFormat
{
    UncompressedGrayscale,
    UncompressedR8G8B8A8,
}