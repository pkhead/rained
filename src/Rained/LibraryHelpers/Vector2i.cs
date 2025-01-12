namespace System.Numerics;

struct Vector2i
{
    public int X = 0;
    public int Y = 0;

    public static Vector2i UnitX { get => new(1, 0); } 
    public static Vector2i UnitY { get => new(0, 1); }
    public static Vector2i One { get => new(1, 1); }
    public static Vector2i Zero { get => new(0, 0); }
    
    public Vector2i()
    {}

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    // override object.Equals
    public readonly override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        Vector2i other = (Vector2i) obj;
        return X == other.X && Y == other.Y;
    }
    
    // override object.GetHashCode
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode());
    }

    public static bool operator==(Vector2i a, Vector2i b)
    {
        return a.X == b.X && a.Y == b.Y;
    }

    public static bool operator!=(Vector2i a, Vector2i b)
    {
        return !(a == b);
    }

    public static Vector2i operator+(Vector2i a, Vector2i b)
        => new(a.X + b.X, a.Y + b.Y);

    public static Vector2 operator+(Vector2i a, Vector2 b)
        => new(a.X + b.X, a.Y + b.Y);

    public static Vector2i operator-(Vector2i a, Vector2i b)
        => new(a.X - b.X, a.Y - b.Y);

    public static Vector2i operator-(Vector2i v)
        => new(-v.X, -v.Y);

    public static Vector2i operator*(Vector2i v, int s)
        => new(v.X * s, v.Y * s);

    public static Vector2 operator*(Vector2i v, float s)
        => new(v.X * s, v.Y * s);

    public static Vector2i operator/(Vector2i v, int s)
        => new(v.X / s, v.Y / s);

    public static Vector2 operator/(Vector2i v, float s)
        => new(v.X / s, v.Y / s);
    
    public static explicit operator Vector2(Vector2i v)
        => new(v.X, v.Y);
}