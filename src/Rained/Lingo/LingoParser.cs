using System.Diagnostics;
using System.Numerics;
using System.Text;
namespace Rained.Lingo;

public class LingoParser
{
    private readonly Queue<Token> tokens = new();

    public object? Read(string str, out ParseException? exception)
    {
        exception = null;

        var stream = new MemoryStream(Encoding.ASCII.GetBytes(str));
        var reader = new StreamReader(stream);
        var tokenParser = new TokenParser(reader);
        
        try
        {
            tokens.Clear();
            foreach (Token tok in tokenParser.Read())
            {
                tokens.Enqueue(tok);
            }
            
            return ParseExpression();
        }

        // do this because even level file reading is janky
        catch (ParseException e)
        {
            exception = e;
            return null;
        }
    }

    public object? Read(string str)
        => Read(str, out _);

    private Token PeekToken() => tokens.Count == 0 ? throw new ParseException("Unexpected EOF") : tokens.Peek();
    private Token PopToken() => tokens.Count == 0 ? throw new ParseException("Unexpected EOF") : tokens.Dequeue();

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
        int sign = 1;

        // check if there is a hyphen before the number
        // meaning that the number will be negative
        var signTok = PeekToken();
        if (signTok.Type == TokenType.Hyphen)
        {
            PopToken();
            sign = -1;
        }

        var tok = PopToken();
        if (tok.Type != TokenType.Integer && tok.Type != TokenType.Float)
        {
            throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected float or integer, got {tok.Type}");
        }

        var v = tok.Value!;
        if (tok.Type == TokenType.Integer)
            return (float)(int)v * sign;
        else
            return (float)v * sign;
    }

    enum ListType { Linear, Property }

    private ListType CheckListType()
    {
        // [] = empty linear list
        // [: = (expect) empty property list
        // [# ... = (expect) property list
        // else = (expect) linear list
        
        var tok = PeekToken();
        if (tok.Type is TokenType.Colon or TokenType.Symbol)
            return ListType.Property;

        return ListType.Linear;
    }

    // this assumes the open bracket was already popped off
    private LinearList ParseLinearList()
    {
        LinearList list = new();
        if (PeekToken().Type == TokenType.CloseBracket) {
            PopToken();
            return list;
        }

        while (true)
        {
            var initTok = PeekToken();
            
            if (initTok.Type != TokenType.Void)
            {
                var value = ParseExpression()!;
                list.Add(value);
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

    private PropertyList ParsePropertyList()
    {
        PropertyList list = new();
        if (PeekToken().Type == TokenType.Colon) {
            PopToken();
            Expect(TokenType.CloseBracket);
            return list;
        }

        while (true)
        {
            var initTok = PeekToken();
            if (initTok.Type is not TokenType.Symbol)
                throw new ParseException($"{initTok.Line}:{initTok.CharOffset}: Expected pound-symbol, got {initTok.Type}");

            PopToken();
            Expect(TokenType.Colon);

            object? value = ParseExpression();
            if (value is not null)
                list[(string) initTok.Value!] = value;

            if (PeekToken().Type != TokenType.Comma) break;
            PopToken();
        }

        Expect(TokenType.CloseBracket);
        return list;
    }

    private object? ParseValue()
    {
        var tok = PopToken();
        switch (tok.Type)
        {
            case TokenType.Void:
                return null;

            case TokenType.String: case TokenType.StringConstant:
            case TokenType.Float: case TokenType.FloatConstant:
            case TokenType.Integer: case TokenType.IntConstant:
                return tok.Value;
            
            case TokenType.Hyphen:
            {
                var number = PopToken();
                if (number.Type == TokenType.Float || number.Type == TokenType.FloatConstant)
                {
                    return -(float)number.Value!;
                }
                else if (number.Type == TokenType.Integer || number.Type == TokenType.IntConstant)
                {
                    return -(int)number.Value!;
                }
                else
                {
                    throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected float or integer, got {tok.Type}");
                }
            }
            
            case TokenType.OpenBracket:
            {
                return CheckListType() switch
                {
                    ListType.Linear => ParseLinearList(),
                    ListType.Property => ParsePropertyList(),
                    _ => throw new UnreachableException("CheckListType returned invalid value?")
                };
            }
            
            case TokenType.KeywordColor:
            {
                Expect(TokenType.OpenParen);
                int[] components = new int[3];

                for (int i = 0; i < 3; i++)
                {
                    var num = Expect(TokenType.Integer);
                    components[i] = (int) num.Value!;
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

    private object? ParseExpression()
    {
        var accumValue = ParseValue();

        while (tokens.Count > 0)
        {
            var op = PeekToken();

            switch (op.Type)
            {
                // ampersand: string concatenation operator
                case TokenType.Ampersand:
                {
                    PopToken();
                    
                    var nextValue = ParseValue();

                    // check that both arguments are a string
                    if (accumValue is not string strLeft || nextValue is not string strRight)
                    {
                        var typeA = accumValue?.GetType().Name ?? "null";
                        var typeB = nextValue?.GetType().Name ?? "null";
                        throw new ParseException($"{op.Line}:{op.CharOffset}: Attempt to concatenate {typeA} with {typeB}");
                    }

                    accumValue = strLeft + strRight;
                    break;
                }
                
                default:
                    goto endLoop;
            }
        }
        endLoop:;

        return accumValue;
    }
}