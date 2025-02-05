using System.Diagnostics.CodeAnalysis;

namespace Rained.Lingo;

public class ParseException : Exception
{
    public ParseException() {}
    public ParseException(string message) : base(message) {}
    public ParseException(string message, Exception inner) : base(message, inner) {}
}

public static class LingoNumber
{
    public static float AsFloat(object obj)
    {
        if (obj is int vi) return vi;
        if (obj is float vf) return vf;
        throw new InvalidCastException($"Unable to cast object of type '{obj.GetType().FullName}' to 'System.Int32'.");
    }

    public static int AsInt(object obj)
    {
        return (int) obj;
    }
}

public struct Color(int r, int g, int b)
{
    public int R = r, G = g, B = b;

    public readonly override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        return this == (Color) obj;
    }
    
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(R.GetHashCode(), G.GetHashCode(), B.GetHashCode());
    }

    public static bool operator==(Color a, Color b)
    {
        return
            a.R == b.R &&
            a.G == b.G &&
            a.B == b.B;
    }

    public static bool operator!=(Color a, Color b)
    {
        return !(a == b);
    }
}

public struct Rectangle
{
    public float X, Y, Width, Height;

    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

public class PropertyList // : IDictionary<string, object>
{
    public readonly Dictionary<string, object> fields = [];

    public object this[string key]
    {
        get => fields[key];
        set => fields[key.ToLowerInvariant()] = value;
    }

    public void Add(string key, object value) => fields.Add(key.ToLowerInvariant(), value);
    public bool ContainsKey(string key) => fields.ContainsKey(key.ToLowerInvariant());
    public bool Remove(string key) => fields.Remove(key.ToLowerInvariant());
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? v) => fields.TryGetValue(key.ToLowerInvariant(), out v);

    public ICollection<string> Keys => fields.Keys;
    public ICollection<object> Values => fields.Values;
}

public class LinearList// : IList<object>
{
    public readonly List<object> values = [];
    
    public object this[int index]
    {
        get => values[index];
        set => values[index] = value;
    }

    public int Count => values.Count;
    public void Add(object v) => values.Add(v);
    public int IndexOf(object v) => values.IndexOf(v);
    public void Insert(int index, object v) => values.Insert(index, v);
    public void Clear() => values.Clear();
    public void RemoveAt(int index) => values.RemoveAt(index);
    public bool Remove(object v) => values.Remove(v);
    public bool Contains(object v) => values.Contains(v);
    public void CopyTo(object[] values, int index) => values.CopyTo(values, index);
}

enum TokenType
{
    OpenBracket,
    CloseBracket,
    CloseParen,
    OpenParen,
    Comma,
    Colon,
    Hyphen,
    Ampersand,
    
    Void,
    String,
    Float,
    Integer,
    Symbol,
    KeywordColor,
    KeywordPoint,
    KeywordRect,

    StringConstant,
    IntConstant,
    FloatConstant,
}

struct Token
{
    public TokenType Type;
    public object? Value;

    public int CharOffset;
    public int Line;
}