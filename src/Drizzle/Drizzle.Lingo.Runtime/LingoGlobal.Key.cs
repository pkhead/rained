using System.Collections.Generic;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public Key _key { get; private set; } = default!;

    public sealed class Key
    {
        public LingoNumber keypressed(object keyName)
        {
            return 0;
        }

        public LingoNumber keypressed(int keyCode)
        {
            return 0;
        }
    }
}
