using System.Numerics;
using System.Text;
namespace RainEd.Lingo;

public class LingoParser
{
    private readonly Queue<Token> tokens = new();

    public object? Read(string str)
    {
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
            
            return ReadValue();
        }

        // do this because even level file reading is janky
        catch (ParseException)
        {
            return null;
        }
    }

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
                Expect(TokenType.Colon);
                object? value = ReadValue();
                if (value is not null)
                    list.fields[(string) initTok.Value!] = value;
            }
            else if (initTok.Type != TokenType.Void)
            {
                var value = ReadValue()!;
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
                return ReadList();
            
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
}