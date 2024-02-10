using System;
using System.Diagnostics.CodeAnalysis;
using Drizzle.Lingo.Runtime;

namespace Drizzle.Logic;

public struct Vector2i : IEquatable<Vector2i>
{
    public int X;
    public int Y;

    public Vector2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2i(int val)
    {
        X = val;
        Y = val;
    }

    public static Vector2i operator +(Vector2i a, Vector2i b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2i operator -(Vector2i a, Vector2i b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2i operator *(Vector2i a, Vector2i b) => new(a.X * b.X, a.Y * b.Y);
    public static Vector2i operator /(Vector2i a, Vector2i b) => new(a.X / b.X, a.Y / b.Y);
    public static implicit operator Vector2i((int x, int y) tuple) => new(tuple.x, tuple.y);
    public static implicit operator LingoPoint(Vector2i v) => new(v.X, v.Y);
    public static explicit operator Vector2i(LingoPoint p) => new((int) p.loch, (int) p.locv);

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    public bool Equals(Vector2i other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2i other && Equals(other);
    }

    // ReSharper in charge of understanding basic concepts of the language with their analysis.
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Vector2i left, Vector2i right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2i left, Vector2i right)
    {
        return !left.Equals(right);
    }
}