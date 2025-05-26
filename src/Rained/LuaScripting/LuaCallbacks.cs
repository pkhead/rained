namespace Rained.LuaScripting.Modules;
using KeraLua;

class LuaCallback : IDisposable
{
    public Action<Lua, LuaCallback>? OnDisconnect;
    private readonly int @ref;
    public readonly Lua LuaState;

    /// <summary>
    /// Create a LuaCallback object. Pops a function from the stack,
    /// and pushes the Lua wrapper of itself.
    /// </summary>
    public LuaCallback(Lua lua)
    {
        LuaState = lua;
        while (LuaState.MainThread != LuaState)
        {
            LuaState = LuaState.MainThread;
        }

        @ref = lua.Ref(LuaRegistry.Index);
        allCallbacks.AddLast(this);

        wrap.PushWrapper(lua, this);
    }

    public void Invoke(int nargs)
    {
        Lua coro = LuaState.NewThread();
        LuaState.Pop(1);
        
        coro.RawGetInteger(LuaRegistry.Index, @ref);
        for (int i = -nargs; i <= -1; i++)
        {
            LuaState.PushCopy(i);
            var tmpRef = LuaState.Ref(LuaRegistry.Index);
            coro.RawGetInteger(LuaRegistry.Index, tmpRef);
            LuaState.Unref(LuaRegistry.Index, tmpRef);
        }
        LuaState.Pop(nargs);
        LuaHelpers.ResumeCoroutine(coro, null, nargs, out int results);
        coro.Pop(results);
    }

    public void Dispose()
    {
        OnDisconnect?.Invoke(LuaState, this);
        LuaState.Unref(LuaRegistry.Index, @ref);
    }

    private static readonly ObjectWrap<LuaCallback> wrap = new("CallbackHandle", "CBH_REGISTRY");
    private static readonly LinkedList<LuaCallback> allCallbacks = [];
    public static void InitType(Lua lua)
    {
        wrap.InitMetatable(lua);

        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "disconnect":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var cb = wrap.GetRef(lua, 1);
                        cb.Dispose();
                        return 0;
                    });
                    break;

                default:
                    lua.PushNil();
                    break;
            }

            return 1;
        });

        lua.Pop(1);
    }

    public static void RemoveAllCallbacks()
    {
        foreach (var cb in allCallbacks)
        {
            cb.Dispose();
        }
        allCallbacks.Clear();
    }
}