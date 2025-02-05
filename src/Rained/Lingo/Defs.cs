using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Rained.Lingo;

using LinearList = List<object>;

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

public class PropertyList : IDictionary<string, object>
{
    [Obsolete("fields field is deprecated, as PropertyList itself now can be used like a normal dictionary")]
    public ref readonly Dictionary<string, object> fields => ref _fields;

    private readonly Dictionary<string, object> _fields = new(StringComparer.InvariantCultureIgnoreCase);

    public object this[string key]
    {
        get => _fields[key];
        set => _fields[key] = value;
    }

    public int Count => _fields.Count;
    public bool IsReadOnly => false;
    public void Clear() => _fields.Clear();
    public void Add(string key, object value) => _fields.Add(key, value);
    public void Add(KeyValuePair<string, object> pair) => _fields.Add(pair.Key, pair.Value);
    public bool ContainsKey(string key) => _fields.ContainsKey(key);
    public bool Contains(KeyValuePair<string, object> pair) => _fields.Contains(pair);
    public bool Remove(string key) => _fields.Remove(key);
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? v) => _fields.TryGetValue(key, out v);
    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => (_fields as IDictionary<string, object>).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, object> pair) => _fields.Remove(pair.Key);
    
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _fields.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ICollection<string> Keys => _fields.Keys;
    public ICollection<object> Values => _fields.Values;
}

public class LinearList : IList<object>
{
    [Obsolete("values field is deprecated, as LinearList itself now can be used like a normal dictionary")]
    public ref readonly List<object> values => ref _values;

    public readonly List<object> _values = [];
    
    public object this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    public int Count => _values.Count;
    public bool IsReadOnly => false;
    public void Add(object v) => _values.Add(v);
    public int IndexOf(object v) => _values.IndexOf(v);
    public void Insert(int index, object v) => _values.Insert(index, v);
    public void Clear() => _values.Clear();
    public void RemoveAt(int index) => _values.RemoveAt(index);
    public bool Remove(object v) => _values.Remove(v);
    public bool Contains(object v) => _values.Contains(v);
    public void CopyTo(object[] values, int index) => values.CopyTo(values, index);
    public IEnumerator<object> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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