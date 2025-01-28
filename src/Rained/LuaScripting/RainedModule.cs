namespace Rained.LuaScripting;
using KeraLua;
using Rained.EditorGui;

static class RainedModule
{
    private static readonly Dictionary<int, int> registeredCmds = [];
    private const string CommandID = "RainedCommandID";

    public static void Init(Lua lua, NLua.Lua nLua)
    {
        // function rained.getVersion
        lua.ModuleFunction("getVersion", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushString(RainEd.Version);
            return 1;
        });

        // function rained.getApiVersion
        lua.ModuleFunction("getApiVersion", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(LuaInterface.VersionMajor);
            lua.PushInteger(LuaInterface.VersionMinor);
            lua.PushInteger(LuaInterface.VersionRevision);
            return 3;
        });

        lua.ModuleFunction("alert", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);

            lua.PushCopy(1);
            EditorWindow.ShowNotification(lua.ToString(-1));
            lua.Pop(1);
            return 0;
        });

        lua.ModuleFunction("getLevelWidth", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(RainEd.Instance.Level.Width);
            return 1;
        });

        lua.ModuleFunction("getLevelHeight", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(RainEd.Instance.Level.Height);
            return 1;
        });

        lua.ModuleFunction("isInBounds", static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);
            var x = (int) lua.CheckNumber(1);
            var y = (int) lua.CheckNumber(2);
            lua.PushBoolean(RainEd.Instance.Level.IsInBounds(x, y));
            return 1;
        });

        lua.NewMetaTable(CommandID);
        LuaHelpers.PushLuaFunction(lua, static (Lua lua) =>
        {
            lua.PushString("The metatable is locked!");
            return 1;
        });
        lua.SetField(-2, "__metatable");
        lua.Pop(1);

        lua.ModuleFunction("registerCommand", static (KeraLua.Lua lua) => {
            string name = lua.CheckString(1);
            lua.CheckType(2, KeraLua.LuaType.Function);

            lua.PushCopy(2);
            int funcRef = lua.Ref(KeraLua.LuaRegistry.Index);
            int cmdId = RainEd.Instance.RegisterCommand(name, (id) => RunCommand(lua, id));
            registeredCmds[cmdId] = funcRef;

            unsafe
            {
                var ud = (int*) lua.NewUserData(sizeof(int));
                lua.SetMetaTable(CommandID);
                *ud = cmdId;
            }

            return 1;
        });
    }

    private static void RunCommand(Lua lua, int id)
    {
        lua.RawGetInteger(LuaRegistry.Index, registeredCmds[id]);

        RainEd.Instance.LevelView.CellChangeRecorder.BeginChange();

        lua.PushCFunction(static (nint luaPtr) => {
            var lua = Lua.FromIntPtr(luaPtr);

            // create exception from first argument (error object)
            var exception = new NLua.Exceptions.LuaScriptException(lua.ToString(1), "unknown");
            lua.Traceback(lua);
            exception.Data["Traceback"] = lua.ToString(-1);
            LuaInterface.HandleException(exception);
            lua.Pop(1); // remove traceback

            // return original error object?
            lua.PushCopy(1);
            return 1;
        });
        lua.PCall(0, 0, -1);

        RainEd.Instance.LevelView.CellChangeRecorder.PushChange();
    }
}