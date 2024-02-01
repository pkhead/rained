using System.Numerics;
using System.Text;
namespace Lingo;

public class LingoParser
{
    private readonly TokenParser tokenParser;
    private readonly Queue<Token> tokens = new();

    public LingoParser(StreamReader stream)
    {
        tokenParser = new TokenParser(stream);
    }

    public LingoParser(string str)
    {
        var stream = new MemoryStream(Encoding.ASCII.GetBytes(str));
        var reader = new StreamReader(stream);
        tokenParser = new TokenParser(reader);
    }

    // this reads in the tile init format
    // TODO: include this code in TileDatabase instead of here 
    public List<List<object>> ReadTileInitFormat()
    {
        tokens.Clear();
        foreach (Token tok in tokenParser.Read())
        {
            tokens.Enqueue(tok);
        }

        var tables = new List<List<object>>();
        
        if (PeekToken().Type == TokenType.Hyphen)
            PopToken();
        
        while (tokens.Count > 0)
        {
            tables.Add(ReadTable());

            if (tokens.Count > 0 && PeekToken().Type == TokenType.Hyphen)
                PopToken();
        }

        return tables;
    }

    private object? Read()
    {
        try
        {
            tokens.Clear();
            foreach (Token tok in tokenParser.Read())
            {
                tokens.Enqueue(tok);
            }
            
            return ReadValue();
        }
        
        // do this because even level file reading is janky
        catch (ParseException)
        {
            return null;
        }
    }

    public static object? Read(string str) => new LingoParser(str).Read();

    private Token PeekToken() => tokens.Peek();
    private Token PopToken() => tokens.Dequeue();

    /*private void Error(string msg)
    {
        var tok = lastProcessedToken;
        throw new ParseException($"{tok.Line}:{tok.CharOffset}: {msg});
    }*/

    private Token Expect(TokenType type)
    {
        var tok = PopToken();
        if (tok.Type != type)
        {
            throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected {type}, got {tok.Type}");
        }
        return tok;
    }

    private float ExpectNumber()
    {
        var tok = PopToken();
        if (tok.Type != TokenType.Integer && tok.Type != TokenType.Float)
        {
            throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected float or integer, got {tok.Type}");
        }

        if (tok.Value is null) throw new NullReferenceException();
        if (tok.Type == TokenType.Integer)
            return (float) (int) tok.Value;
        else
            return (float) tok.Value;
    }

    // this assumes the open bracket was already popped off
    private List ReadList()
    {
        List list = new();
        if (PeekToken().Type == TokenType.CloseBracket){
            PopToken();
            return list;
        }

        while (true)
        {
            var initTok = PeekToken();

            if (initTok.Type == TokenType.Symbol)
            {
                PopToken();
                if (initTok.Value is null) throw new NullReferenceException();
                Expect(TokenType.Colon);
                object? value = ReadValue();
                if (value is not null)
                    list.fields[(string) initTok.Value] = value;
            }
            else if (initTok.Type != TokenType.Void)
            {
                var value = ReadValue() ?? throw new NullReferenceException();
                list.values.Add(value);
            }
            else
            {
                PopToken();
            }

            if (PeekToken().Type != TokenType.Comma) break;
            PopToken();
        }

        Expect(TokenType.CloseBracket);
        return list;
    }

    private object? ReadValue()
    {
        var tok = PopToken();
        switch (tok.Type)
        {
            case TokenType.Void:
                return null;

            case TokenType.String:
            case TokenType.Float:
            case TokenType.Integer:
                return tok.Value;

            case TokenType.OpenBracket:
                return ReadList();
            
            case TokenType.KeywordColor:
            {
                Expect(TokenType.OpenParen);
                int[] components = new int[3];

                for (int i = 0; i < 3; i++)
                {
                    var num = Expect(TokenType.Integer);
                    if (num.Value is null) throw new NullReferenceException();
                    components[i] = (int) num.Value;
                    if (i < 2) Expect(TokenType.Comma);
                }

                Expect(TokenType.CloseParen);
                return new Color(components[0], components[1], components[2]);
            }

            case TokenType.KeywordPoint:
            {
                Expect(TokenType.OpenParen);
                float[] components = new float[2];

                for (int i = 0; i < 2; i++)
                {
                    components[i] = ExpectNumber();
                    if (i < 1) Expect(TokenType.Comma);
                }

                Expect(TokenType.CloseParen);
                return new Vector2(components[0], components[1]);
            }

            case TokenType.KeywordRect:
            {
                Expect(TokenType.OpenParen);
                float[] components = new float[4];

                for (int i = 0; i < 4; i++)
                {
                    components[i] = ExpectNumber();
                    if (i < 3) Expect(TokenType.Comma);
                }

                Expect(TokenType.CloseParen);
                return new Rectangle(components[0], components[1], components[2], components[3]);
            }
        }

        
        throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected value, got {tok.Type}");
    }

    private List<object> ReadTable()
    {
        List<object> items = new();

        while (tokens.Count > 0 && PeekToken().Type != TokenType.Hyphen)
        {
            var val = ReadValue();
            if (val is not null)
                items.Add(val);
            
            // for some reason in the Custom/DSMachines section,
            // there are unmatched closing brackets. but editors still parse it correctly?
            // im not sure what that's supposed to mean, so i just ignore dangling closing brackets
            if (tokens.Count > 0 && PeekToken().Type == TokenType.CloseBracket)
                PopToken();
        }

        return items;
    }
}