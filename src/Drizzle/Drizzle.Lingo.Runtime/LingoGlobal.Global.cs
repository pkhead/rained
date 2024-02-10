using System;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public Global _global { get; private set; } = default!;

    public sealed class Global
    {
        private readonly LingoGlobal _global;

        public Global(LingoGlobal global)
        {
            _global = global;
        }

        public void clearglobals()
        {
            Log.Debug("Clearing globals");
            var movieScript = _global.MovieScriptInstance;
            foreach (var field in movieScript.GetType().GetFields())
            {
                if (Attribute.IsDefined(field, typeof(LingoGlobalAttribute)))
                    field.SetValue(movieScript, null);
            }
        }
    }
}