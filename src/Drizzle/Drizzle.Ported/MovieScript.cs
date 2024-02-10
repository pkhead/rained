using System;
using Drizzle.Lingo.Runtime;

namespace Drizzle.Ported;

[MovieScript]
public sealed partial class MovieScript : LingoScriptBase
{
    public void Init(LingoGlobal global)
    {
        Init(this, global);
    }
}

public static class MovieScriptExt
{
    public static MovieScript MovieScript(this LingoRuntime runtime) => (MovieScript)runtime.MovieScriptInstance;
}