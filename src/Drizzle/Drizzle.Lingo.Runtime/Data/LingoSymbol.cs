using System;

namespace Drizzle.Lingo.Runtime;

public readonly struct LingoSymbol : IEquatable<LingoSymbol>
{
    public string Value { get; }

    public LingoSymbol(string value)
    {
        Value = value;
    }

    public override string ToString() => $"#{Value}";


    public static bool operator ==(LingoSymbol a, LingoSymbol b)
    {
        return a.Value.Equals(b.Value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator !=(LingoSymbol a, LingoSymbol b)
    {
        return !(a == b);
    }

    public bool Equals(LingoSymbol other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is LingoSymbol other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}