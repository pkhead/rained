namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public Player _player { get; private set; } = default!;

    public sealed class Player
    {
        public void appminimize() { }
        public void quit() { }
    }
}
