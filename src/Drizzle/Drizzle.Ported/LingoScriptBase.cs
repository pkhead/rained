using Drizzle.Lingo.Runtime;

namespace Drizzle.Ported;

public abstract class LingoScriptBase : LingoScriptRuntimeBase
{
    protected MovieScript _movieScript;
    protected LingoGlobal _global;

    public void Init(MovieScript movieScript, LingoGlobal global)
    {
        _movieScript = movieScript;
        _global = global;
    }

    public sealed override void Init(object movieScript, LingoGlobal global)
    {
        Init((MovieScript) movieScript, global);
    }
}