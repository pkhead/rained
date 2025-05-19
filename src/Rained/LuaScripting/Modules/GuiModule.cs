using ImGuiNET;
using KeraLua;
namespace Rained.LuaScripting.Modules;

static class GuiModule
{
    private static readonly List<LuaCallback> menuCallbacks = [];
    private static readonly List<LuaCallback> prefsCallbacks = [];

    public static bool HasPreferencesCallbacks => prefsCallbacks.Count > 0;

    public static void MenuHook(string menuName)
    {
        foreach (var cb in menuCallbacks)
        {
            ImGui.Separator();
            cb.LuaState.PushString(menuName);
            cb.Invoke(1);
        }
    }

    public static void PrefsHook()
    {
        foreach (var cb in prefsCallbacks)
        {
            cb.Invoke(0);
        }
    }
    
    public static void Init(Lua lua)
    {
        if (!LuaInterface.Host.IsGui) return;

        lua.NewTable();

        lua.ModuleFunction("menuHook", static (Lua lua) =>
        {
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => menuCallbacks.Remove(cb)
            };
            menuCallbacks.Add(cb);
            
            return 1;
        });

        lua.ModuleFunction("prefsHook", static (Lua lua) =>
        {
            lua.CheckType(1, LuaType.Function);
            lua.PushCopy(1);
            var cb = new LuaCallback(lua)
            {
                OnDisconnect = static (Lua lua, LuaCallback cb) => prefsCallbacks.Remove(cb)
            };
            prefsCallbacks.Add(cb);
            
            return 1;
        });

        lua.SetField(-2, "gui");
    }
}