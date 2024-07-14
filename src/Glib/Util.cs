using Bgfx_cs;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Glib;

[Serializable]
public class UnsupportedOperationException : Exception
{
    public UnsupportedOperationException() { }
    public UnsupportedOperationException(string message) : base(message) { }
    public UnsupportedOperationException(string message, Exception inner) : base(message, inner) { }
}

public struct Color
{
    public float R;
    public float G;
    public float B;
    public float A;

    public Color(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

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

    public static bool operator ==(Color a, Color b)
    {
        return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
    }

    public static bool operator !=(Color a, Color b)
    {
        return !(a == b);
    }

    public static explicit operator Color(System.Drawing.Color color) =>
        FromRGBA(color.R, color.G, color.B, color.A);
    
    public static explicit operator System.Drawing.Color(Color color) =>
        System.Drawing.Color.FromArgb(
            (int)(Math.Clamp(color.A, 0f, 1f) * 255),
            (int)(Math.Clamp(color.R, 0f, 1f) * 255),
            (int)(Math.Clamp(color.G, 0f, 1f) * 255),
            (int)(Math.Clamp(color.B, 0f, 1f) * 255)
        );
    
    public static explicit operator Vector4(Color color) =>
        new(color.R, color.G, color.B, color.A);

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return this == (Color)obj;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(R.GetHashCode(), G.GetHashCode(), B.GetHashCode(), A.GetHashCode());
    }
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
    public float Y { readonly get => Position.Y; set => Position.Y = value; }
    public float Width { readonly get => Size.X; set => Size.X = value; }
    public float Height { readonly get => Size.Y; set => Size.Y = value; }
    public readonly float Right => Position.X + Size.X;
    public readonly float Bottom => Position.Y + Size.Y;
}

internal static class BgfxUtil
{
    public static unsafe Bgfx.Memory* Load<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        fixed (T* src = values)
        {
            return Bgfx.copy(src, (uint)(values.Length * sizeof(T)));
        }
    }

    public static unsafe Bgfx.Memory* LoadString(string str)
    {
        var mgBytes = System.Text.Encoding.UTF8.GetBytes(str);
        var alloc = Bgfx.alloc((uint)mgBytes.Length + 1);

        fixed (byte* bytes = mgBytes)
        {
            Buffer.MemoryCopy(bytes, alloc, mgBytes.Length, mgBytes.Length);
            alloc->data[alloc->size - 1] = 0;
        }

        return alloc;
    }
}