namespace Lingo;

public class ParseException : Exception
{
    public ParseException() {}
    public ParseException(string message) : base(message) {}
    public ParseException(string message, Exception inner) : base(message, inner) {}
}

public struct Color
{
    public int R, G, B;
    public Color(int r, int g, int b)
    {
        R = r;
        G = g;
        B = b;
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

public class List
{
    public List<object> values = new();
    public Dictionary<string, object> fields = new(); 
}

public class Table
{
    public object Header;
    public List<Lingo.List> Items;

    public Table(object header)
    {
        Header = header;
        Items = new();
    }
}

public enum TokenType
{
    OpenBracket,
    CloseBracket,
    CloseParen,
    OpenParen,
    Comma,
    Colon,
    
    Void,
    String,
    Float,
    Integer,
    Symbol,
    KeywordColor,
    KeywordPoint,
    KeywordRect
}

public struct Token
{
    public TokenType Type;
    public object? Value;

    public int CharOffset;
    public int Line;
}