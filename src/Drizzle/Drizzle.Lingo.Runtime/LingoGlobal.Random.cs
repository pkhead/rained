namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public LingoNumber the_randomSeed
    {
        get => (int) LingoRuntime.RngSeed;
        set => LingoRuntime.RngSeed = (uint) value.IntValue;
    }

    public LingoNumber random(LingoNumber max)
    {
        return LingoRuntime.Random(max.IntValue);
    }
}
