namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public Mouse _mouse { get; private set; } = default!;

    public sealed class Mouse
    {
        public LingoPoint mouseloc => default;
        public LingoNumber mousedown => 0;
        public LingoNumber rightmousedown => 0;
    }
}
