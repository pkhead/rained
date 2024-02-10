using System;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public System _system { get; private set; } = default!;

    public sealed class System
    {
        private readonly LingoGlobal _global;

        public System(LingoGlobal global)
        {
            _global = global;
        }

        public LingoNumber milliseconds => (int)_global.LingoRuntime.Stopwatch.ElapsedMilliseconds;

        public LingoPoint desktoprectlist => new LingoPoint(1024f, 768f);
    }
}
