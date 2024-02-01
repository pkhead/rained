namespace Lingo;

internal class TokenParser
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
        throw new ParseException($"{savedLine}:{savedCharOffset}: {msg}");
    }

    public void BeginToken()
    {
        if (tokenBegan) throw new ParseException("BeginToken already called");
        tokenBegan = true;

        savedCharOffset = charOffset;
        savedLine = line;
    }

    public void EndToken(TokenType type, object? value = null)
    {
        if (!tokenBegan) throw new ParseException("BeginToken not called");
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
    private object ReadNumber(bool negative, out bool isFloat)
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
            return float.Parse(string.Join("", strBuffer)) * (negative ? -1f : 1f);
        else
            return int.Parse(string.Join("", strBuffer)) * (negative ? -1 : 1);
    }

    private void ParseNumber(bool negative = false)
    {
        var num = ReadNumber(negative, out bool isFloat);

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
                    ParseNumber(true);
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