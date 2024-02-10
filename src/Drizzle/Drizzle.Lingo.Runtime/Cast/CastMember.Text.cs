using System.IO;
using System.Text;

namespace Drizzle.Lingo.Runtime.Cast;

public sealed partial class CastMember
{
    private string _text = "";

    public string text
    {
        get
        {
            AssertType(CastMemberType.Text);
            return _text;
        }
        set
        {
            AssertType(CastMemberType.Text);
            _text = value;
        }
    }

    public LingoSymbol alignment { get; set; }

    private void ImportFileImplText(string path)
    {
        try
        {
            using var sr = new StreamReader(path);
            var sb = new StringBuilder();

            while (true)
            {
                var line = sr.ReadLine();
                if (line == null)
                    break;

                sb.Append(line);
                sb.Append('\r');
            }

            text = sb.ToString();
        }
        catch(FileNotFoundException)
        {
            text = "";
        }
        catch(DirectoryNotFoundException)
        {
            text = "";
        }
    }
}
