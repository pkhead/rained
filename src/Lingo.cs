using System.Collections.Generic;

namespace Lingo
{
    public struct Color
    {
        public int R, G, B;
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

    class TokenParser
    {
        public enum TokenType
        {
            LeftBracket,
            RightBracket,
            LeftParen,
            RightParen,
            Comma,
            Hyphen,
            Colon,
            
            Void,
            String,
            Float,
            Symbol,
            KeywordColor,
            KeywordPoint,
        }

        public struct Token
        {
            public TokenType Type;
            public object? Value;

            public int CharOffset;
            public int Line;
        }

        private readonly StreamReader stream;
        private readonly List<char> strBuffer = new();
        private List<Token> tokens = new();

        private int charOffset = 1;
        private int line = 1;

        private int savedCharOffset, savedLine;

        public List<Token> Tokens { get => tokens; }

        public TokenParser(StreamReader stream)
        {
            this.stream = stream;

            // read
            while (!stream.EndOfStream)
            {
                ReadToken();
            }
        }

        private void Error(string msg)
        {
            throw new Exception($"{savedLine}:{savedCharOffset}: {msg}");
        }

        public void BeginToken()
        {
            savedCharOffset = charOffset;
            savedLine = line;
        }

        public void EndToken(TokenType type, object? value = null)
        {
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
                stream.Read();
        }

        private char ReadChar()
        {
            var ch = (char) stream.Read();
            if (ch == '\n')
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

        private float ReadFloat()
        {
            strBuffer.Clear();
            while (char.IsDigit(PeekChar()) || PeekChar() == '.' || PeekChar() == '-')
            {
                strBuffer.Add(ReadChar());
            }

            return float.Parse(string.Join("", strBuffer));
        }

        private string ReadWord()
        {
            strBuffer.Clear();
            while (char.IsLetter(PeekChar()) || PeekChar() == '_')
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
                    EndToken(TokenType.LeftBracket);
                    break;

                case ']':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.RightBracket);
                    break;
                
                case '(':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.LeftParen);
                    break;

            	case ')':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.RightParen);
                    break;

            	case ',':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.Comma);
                    break;

            	case '-':
                    BeginToken();
                    ReadChar();
                    EndToken(TokenType.Hyphen);
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
                    stream.Read(); // pop off quotation mark

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
                // number
                if (char.IsDigit(PeekChar()))
                {
                    BeginToken();
                    EndToken(TokenType.Float, ReadFloat());
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
}