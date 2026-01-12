using System.Diagnostics;
using System.Numerics;
using System.Text;
namespace Rained.Lingo;

public class LingoParser
{
    private List<Token> tokens = null!;
    private int tokenIndex = 0;

    public object? Read(string str, out ParseException? exception)
    {
        exception = null;

        var stream = new MemoryStream(Encoding.ASCII.GetBytes(str));
        var reader = new StreamReader(stream);
        var tokenParser = new TokenParser(reader);
        
        try
        {
            tokens = tokenParser.Read();
            tokenIndex = 0;
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

    private Token PeekToken()
    {
        if (tokenIndex >= tokens.Count)
            throw new ParseException("Unexpected EOF");
        return tokens[tokenIndex];
    }

    private Token PeekToken(int offset)
    {
        if (tokenIndex + offset >= tokens.Count)
            throw new ParseException("Unexpected EOF");
        return tokens[tokenIndex + offset];
    }

    private Token PopToken()
    {
        if (tokenIndex >= tokens.Count)
            throw new ParseException("Unexpected EOF");
        return tokens[tokenIndex++];
    }

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

    // this assumes the open bracket was already popped off
    private LinearList ParseLinearList(TokenType closingSymbol)
    {
        LinearList list = [];
        if (PeekToken().Type == closingSymbol) {
            PopToken();
            return list;
        }

        while (true)
        {
            var value = ParseExpression()!;
            if (value is not null)
                list.Add(value);

            if (PeekToken().Type != TokenType.Comma) break;
            PopToken();
        }

        Expect(closingSymbol);
        return list;
    }

    private PropertyList ParsePropertyList(TokenType closingSymbol)
    {
        PropertyList list = [];
        if (PeekToken().Type == TokenType.Colon) {
            PopToken();
            Expect(closingSymbol);
            return list;
        }

        while (true)
        {
            var initTok = PeekToken();
            if (initTok.Type is not (TokenType.Symbol or TokenType.Word))
                throw new ParseException($"{initTok.Line}:{initTok.CharOffset}: Expected word or symbol-identifier, got {initTok.Type}");

            PopToken();
            Expect(TokenType.Colon);

            object? value = ParseExpression();
            if (value is not null)
                list[(string) initTok.Value!] = value;

            if (PeekToken().Type != TokenType.Comma) break;
            PopToken();
        }

        Expect(closingSymbol);
        return list;
    }

    private object? ParseValue()
    {
        var tok = PopToken();
        switch (tok.Type)
        {
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
            case TokenType.OpenBrace:
            {
                var closingSymbol = tok.Type switch {
                    TokenType.OpenBracket => TokenType.CloseBracket,
                    TokenType.OpenBrace => TokenType.CloseBrace,
                    _ => throw new UnreachableException()
                };

                tok = PeekToken();

                // []: empty linear list
                if (tok.Type == closingSymbol)
                {
                    PopToken();
                    return new LinearList();
                }

                // [:]: empty property list
                if (tok.Type is TokenType.Colon)
                {
                    PopToken();
                    Expect(closingSymbol);
                    return new PropertyList();
                }

                // [(*):(...): property list
                // else, linear list
                if (PeekToken(1).Type is TokenType.Colon)
                    return ParsePropertyList(closingSymbol);
                else
                    return ParseLinearList(closingSymbol);
            }
            
            case TokenType.Word:
            {
                switch (((string)tok.Value!).ToLowerInvariant())
                {
                    case "color":
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

                    case "point":
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

                    case "rect":
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

                    case "void":
                        return null;                        
                    case "backspace":
                        return "\b";
                    case "empty":
                        return "";
                    case "enter":
                        return "\x03"; // wtf is this character??
                    case "false":
                        return 0;
                    case "pi":
                        return MathF.PI;
                    case "quote":
                        return "\"";
                    case "return":
                        return "\n"; // technically shoudl be \r but this is easier
                    case "space":
                        return " ";
                    case "tab":
                        return "\t";
                    case "true":
                        return 1;
                    
                    default:
                        throw new ParseException($"{tok.Line}:{tok.CharOffset}: Use of unknown identifier '{tok.Value}'");
                }
            }
        }
        
        throw new ParseException($"{tok.Line}:{tok.CharOffset}: Expected value, got {tok.Type}");
    }

    private object? ParseExpression()
    {
        var accumValue = ParseValue();

        while (tokenIndex < tokens.Count)
        {
            var op = PeekToken();

            switch (op.Type)
            {
                // ampersand: string concatenation operator
                case TokenType.Ampersand:
                {
                    PopToken();
                    
                    var nextValue = ParseValue();
                    accumValue =
                        (accumValue?.ToString() ?? "") +
                        (nextValue?.ToString() ?? "");
                    
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