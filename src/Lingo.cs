using System.Collections.Generic;
using System.Numerics;

namespace Lingo
{
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
        public Dictionary<string, object> pairs = new(); 
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
        Hyphen,
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

    class TokenParser
    {
        private readonly StreamReader stream;
        private readonly List<char> strBuffer = new();
        private List<Token> tokens = new();

        private int charOffset = 1;
        private int line = 1;

        private int savedCharOffset, savedLine;
        private bool tokenBegan = false;

        public List<Token> Tokens { get => tokens; }

        public TokenParser(StreamReader stream)
        {
            this.stream = stream;
        }

        public List<Token> Read()
        {
            tokens.Clear();
            strBuffer.Clear();

            DiscardWhitespace();
            while (!stream.EndOfStream)
            {
                ReadToken();
                DiscardWhitespace();
            }

            return tokens;
        }

        private void Error(string msg)
        {
            throw new Exception($"{savedLine}:{savedCharOffset}: {msg}");
        }

        public void BeginToken()
        {
            if (tokenBegan) throw new Exception("BeginToken already called");
            tokenBegan = true;

            savedCharOffset = charOffset;
            savedLine = line;
        }

        public void EndToken(TokenType type, object? value = null)
        {
            if (!tokenBegan) throw new Exception("BeginToken not called");
            tokenBegan = false;

            tokens.Add(new Token()
            {
                Type = type,
                Value = value,
                CharOffset = savedCharOffset,
                Line = savedLine
            });
        }

        private void DiscardWhitespace()
        {
            while (char.IsWhiteSpace((char) stream.Peek()))
                ReadChar();
        }

        private char ReadChar()
        {
            var ch = (char) stream.Read();

            // handle the three different line endings:
            // Unix: LF
            // Mac: CR
            // Windows: CR LF
            bool isNewline = false;
            if (ch == '\r')
            {
                if ((char) stream.Peek() == '\n')
                {
                    stream.Read();
                    isNewline = true;
                }
                else
                {
                    isNewline = true;
                }
            }
            else isNewline = ch == '\n';

            if (isNewline)
            {
                line++;
                charOffset = 0;
            }
            charOffset++;

            return ch;
        }

        private char PeekChar()
        {
            return (char) stream.Peek();
        }

        // read either a float or an integer
        private object ReadNumber(out bool isFloat)
        {
            isFloat = false;

            strBuffer.Clear();
            while (char.IsDigit(PeekChar()) || PeekChar() == '.')
            {
                var ch = ReadChar();
                if (ch == '.') isFloat = true;
                strBuffer.Add(ch);
            }

            if (isFloat)
                return float.Parse(string.Join("", strBuffer));
            else
                return int.Parse(string.Join("", strBuffer));
        }

        private void ParseNumber()
        {
            var num = ReadNumber(out bool isFloat);

            if (isFloat)
            {
                EndToken(TokenType.Float, num);
            }
            else
            {
                EndToken(TokenType.Integer, num);
            }
        }

        private string ReadWord()
        {
            strBuffer.Clear();
            while (char.IsLetter(PeekChar()) || char.IsDigit(PeekChar()) || PeekChar() == '_')
            {
                strBuffer.Add(ReadChar());
            }
            return string.Join("", strBuffer);
        }

        private void ReadToken()
        {
            DiscardWhitespace();

            switch (PeekChar())
            {
                case '[':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.OpenBracket);
                    break;

                case ']':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.CloseBracket);
                    break;
                
                case '(':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.OpenParen);
                    break;

            	case ')':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.CloseParen);
                    break;

            	case ',':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.Comma);
                    break;

            	case ':':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.Colon);
                    break;

                // symbol
            	case '#':
                {
                    BeginToken();
                    ReadChar();
                    var id = ReadWord();
                    EndToken(TokenType.Symbol, id);
                    break;
                }

                // string
            	case '\"':
                {
                    BeginToken();
                    ReadChar(); // pop off quotation mark

                    strBuffer.Clear();                
                    while (true)
                    {
                        char ch = ReadChar();
                        if (ch == '"') break;
                        strBuffer.Add(ch);
                    }

                    string str = string.Join("", strBuffer);
                    EndToken(TokenType.String, str);
                    break;
                }

            default:
                // hyphen -- may be a negative number, or simply a hyphen
                if (PeekChar() == '-')
                {
                    BeginToken();
                    ReadChar();

                    if (char.IsDigit(PeekChar()))
                    {
                        ParseNumber();
                    }
                    else
                    {
                        EndToken(TokenType.Hyphen);
                    }
                }

                else if (char.IsDigit(PeekChar()))
                {
                    BeginToken();
                    ParseNumber();
                }

                // keyword
                else
                {
                    BeginToken();
                    var kw = ReadWord();

                    if (kw == "point")
                    {
                        EndToken(TokenType.KeywordPoint);
                    }
                    else if (kw == "color")
                    {
                        EndToken(TokenType.KeywordColor);
                    }
                    else if (kw == "rect")
                    {
                        EndToken(TokenType.KeywordRect);
                    }
                    else if (kw == "void")
                    {
                        EndToken(TokenType.Void);
                    }
                    else
                    {
                        Error($"Invalid keyword {kw}");
                    }
                }

                break;
            }
        } 
    }

    class Parser
    {
        private readonly TokenParser tokenParser;
        private readonly Queue<Token> tokens = new();
        private Token lastProcessedToken;

        public Parser(StreamReader stream)
        {
            tokenParser = new TokenParser(stream);
        }

        public List<List<object>> Read()
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

        private Token PeekToken() => tokens.Peek();
        private Token PopToken() => lastProcessedToken = tokens.Dequeue();

        /*private void Error(string msg)
        {
            var tok = lastProcessedToken;
            throw new Exception($"{tok.Line}:{tok.CharOffset}: {msg});
        }*/

        private Token Expect(TokenType type)
        {
            var tok = PopToken();
            if (tok.Type != type)
            {
                throw new Exception($"{tok.Line}:{tok.CharOffset}: Expected {type}, got {tok.Type}");
            }
            return tok;
        }

        private float ExpectNumber()
        {
            var tok = PopToken();
            if (tok.Type != TokenType.Integer && tok.Type != TokenType.Float)
            {
                throw new Exception($"{tok.Line}:{tok.CharOffset}: Expected float or integer, got {tok.Type}");
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
                        list.pairs[(string) initTok.Value] = value;
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

            
            throw new Exception($"{tok.Line}:{tok.CharOffset}: Expected value, got {tok.Type}");
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
}