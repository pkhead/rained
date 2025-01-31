namespace Rained.LuaScripting.Modules;
using KeraLua;
using Rained.EditorGui;

static class RainedModule
{
    private static readonly Dictionary<int, int> registeredCmds = [];
    private const string CommandID = "RainedCommandID";
    private static readonly LuaFunction _errHandler = new(LuaHelpers.ErrorHandler);

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

        lua.ModuleFunction("getDocumentCount", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushInteger(RainEd.Instance.Tabs.Count);
            return 1;
        });

        lua.ModuleFunction("getDocumentName", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx >+ RainEd.Instance.Tabs.Count)
            {
                lua.PushNil(); return 1;
            }

            lua.PushString(RainEd.Instance.Tabs[idx].Name);
            return 1;
        });

        lua.ModuleFunction("getDocumentInfo", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx >+ RainEd.Instance.Tabs.Count)
            {
                lua.PushNil(); return 1;
            }

            var name = RainEd.Instance.Tabs[idx].Name;
            var filePath = RainEd.Instance.Tabs[idx].FilePath;

            lua.NewTable();
            lua.PushString(name);
            lua.SetField(-2, "name");
            lua.PushString(filePath);
            lua.SetField(-2, "filePath");

            return 1;
        });

        lua.ModuleFunction("getActiveDocument", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            if (RainEd.Instance.CurrentTab is not null)
            {
                lua.PushInteger(RainEd.Instance.Tabs.IndexOf(RainEd.Instance.CurrentTab) + 1);
            } else
            {
                lua.PushNil();
            }

            return 1;
        });

        lua.ModuleFunction("setActiveDocument", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);

            var idx = (int)lua.CheckInteger(1) - 1;
            if (idx < 0 || idx >+ RainEd.Instance.Tabs.Count)
            {
                lua.PushBoolean(false);
                return 1;
            }
            
            RainEd.Instance.CurrentTab = RainEd.Instance.Tabs[idx];
            lua.PushBoolean(true);
            return 1;
        });

        lua.ModuleFunction("isDocumentOpen", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            lua.PushBoolean(RainEd.Instance.CurrentTab is not null);
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
        Lua coro = lua.NewThread();
        coro.RawGetInteger(LuaRegistry.Index, registeredCmds[id]);
        var status = coro.Resume(null, 0, out _);
        if (status is not LuaStatus.OK or LuaStatus.Yield)
        {
            LuaHelpers.ErrorHandler(coro.Handle);
        }
        
        //lua.PushCFunction(_errHandler);
        //lua.PCall(0, 0, -2);
    }
}