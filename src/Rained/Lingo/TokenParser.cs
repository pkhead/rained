using System.Globalization;

namespace Rained.Lingo;

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
        bool isNewline;
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
            return float.Parse(string.Join("", strBuffer), CultureInfo.InvariantCulture) * (negative ? -1f : 1f);
        else
            return int.Parse(string.Join("", strBuffer), CultureInfo.InvariantCulture) * (negative ? -1 : 1);
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
            
            case '&':
            {
                BeginToken();
                ReadChar();
                EndToken(TokenType.Ampersand);
                break;
            }

            case '-':
            {
                BeginToken();
                ReadChar();
                EndToken(TokenType.Hyphen);
                break;
            }

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
            if (char.IsDigit(PeekChar()))
            {
                BeginToken();
                ParseNumber();
            }

            // parse keywords and string constants
            else
            {
                BeginToken();
                var kw = ReadWord();

                switch (kw.ToLowerInvariant())
                {
                    case "point":
                        EndToken(TokenType.KeywordPoint);
                        break;
                    
                    case "color":
                        EndToken(TokenType.KeywordColor);
                        break;
                    
                    case "rect":
                        EndToken(TokenType.KeywordRect);
                        break;
                    
                    case "void":
                        EndToken(TokenType.Void);
                        break;

                    // parse string constants
                    case "backspace":
                        EndToken(TokenType.StringConstant, "\b");
                        break;
                    
                    case "empty":
                        EndToken(TokenType.StringConstant, string.Empty);
                        break;
                    
                    case "enter":
                        EndToken(TokenType.StringConstant, Environment.NewLine);
                        break;
                    
                    case "false":
                        EndToken(TokenType.IntConstant, 0);
                        break;
                    
                    case "pi":
                        EndToken(TokenType.FloatConstant, MathF.PI);
                        break;
                    
                    case "quote":
                        EndToken(TokenType.StringConstant, "\"");
                        break;
                    
                    case "return":
                        EndToken(TokenType.StringConstant, "\r");
                        break;
                    
                    case "space":
                        EndToken(TokenType.StringConstant, " ");
                        break;
                    
                    case "tab":
                        EndToken(TokenType.StringConstant, "\t");
                        break;
                    
                    case "true":
                        EndToken(TokenType.IntConstant, 1);
                        break;
                    
                    default:
                        Error($"Invalid keyword {kw}");
                        break;
                }
            }

            break;
        }
    } 
}