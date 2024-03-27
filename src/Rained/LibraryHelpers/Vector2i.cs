namespace System.Numerics;

struct Vector2i
{
    public int X = 0;
    public int Y = 0;

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
}