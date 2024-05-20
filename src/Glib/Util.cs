using System.Numerics;

namespace Glib;

public struct Color(float r, float g, float b, float a = 1f)
{
    public float R = r;
    public float G = g;
    public float B = b;
    public float A = a;

    public static Color FromRGBA(int r, int g, int b, int a = 255)
    => new(
        Math.Clamp(r / 255f, 0f, 1f),
        Math.Clamp(g / 255f, 0f, 1f),
        Math.Clamp(b / 255f, 0f, 1f),
        Math.Clamp(a / 255f, 0f, 1f)
    );

    public static Color Transparent => new(0f, 0f, 0f, 0f);
    public static Color White => new(1f, 1f, 1f);
    public static Color Black => new(0f, 0f, 0f);
    public static Color Red => new(1f, 0f, 0f);
    public static Color Green => new(0f, 1f, 0f);
    public static Color Blue => new(0f, 0f, 1f);
    public static Color Yellow => new(1f, 1f, 0f);
    public static Color Cyan => new(1f, 0f, 1f);
    public static Color Magenta => new(0f, 0f, 1f);

    public static explicit operator Color(System.Drawing.Color color) =>
        FromRGBA(color.R, color.G, color.B, color.A);
    
    public static explicit operator System.Drawing.Color(Color color) =>
        System.Drawing.Color.FromArgb(
            (int)(Math.Clamp(color.A, 0f, 1f) * 255),
            (int)(Math.Clamp(color.R, 0f, 1f) * 255),
            (int)(Math.Clamp(color.G, 0f, 1f) * 255),
            (int)(Math.Clamp(color.B, 0f, 1f) * 255)
        );
}

public struct Rectangle
{
    public Vector2 Position;
    public Vector2 Size;

    public Rectangle(float x, float y, float w, float h)
    {
        Position = new Vector2(x, y);
        Size = new Vector2(w, h);
    }

    public Rectangle(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public float Left { readonly get => Position.X; set => Position.X = value; }
    public float X { readonly get => Position.X; set => Position.X = value; }
    public float Top { readonly get => Position.Y; set => Position.Y = value; }
    public float Y { readonly get => Position.X; set => Position.X = value; }
    public float Width { readonly get => Size.X; set => Size.X = value; }
    public float Height { readonly get => Size.Y; set => Size.Y = value; }
    public readonly float Right => Position.X + Size.X;
    public readonly float Bottom => Position.Y + Size.Y;
}