using Drizzle.Lingo.Runtime.Scripting;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public LingoRuntime LingoRuntime { get; }

    public LingoGlobal(LingoRuntime lingoRuntime)
    {
        LingoRuntime = lingoRuntime;
    }

    public void Init()
    {
        _system = new System(this);
        _key = new Key();
        _mouse = new Mouse();
        _movie = new Movie(this);
        _global = new Global(this);
        _player = new Player();
        ScriptRuntime = new LingoScriptRuntime(this);
    }
}
