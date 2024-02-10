using Drizzle.Lingo.Runtime.Cast;

namespace Drizzle.Lingo.Runtime;

public sealed class LingoSprite
{
    public LingoRect rect { get; set; }

    public LingoNumber locv
    {
        get => loc.locv;
        set => loc = new LingoPoint(loch, value);
    }

    public LingoNumber loch
    {
        get => loc.loch;
        set => loc = new LingoPoint(value, loch);
    }

    public dynamic? visibility { get; set; } // Fairly certain this is invalid.
    public LingoNumber visible { get; set; }

    public CastMember? member { get; set; }

    public LingoNumber blend { get; set; } = 100;

    public LingoColor color { get; set; }
    public LingoColor bgcolor { get; set; }

    public LingoColor forecolor
    {
        get => color;
        set => color = value;
    }
    public LingoColor backcolor
    {
        get => bgcolor;
        set => bgcolor = value;
    }

    // This is the REGISTRATION POINT
    public LingoPoint loc { get; set; }

    public LingoList quad { get; set; } = new();

    public LingoNumber linesize { get; set; }

    public string text { get; set; } = "";

    public LingoSprite()
    {

    }
}