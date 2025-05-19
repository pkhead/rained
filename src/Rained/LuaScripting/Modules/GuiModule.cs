using ImGuiNET;
using KeraLua;
using Rained.EditorGui;
namespace Rained.LuaScripting.Modules;

static class GuiModule
{
    private static readonly List<LuaCallback> menuCallbacks = [];
    private static readonly List<LuaCallback> prefsCallbacks = [];

    public static bool HasPreferencesCallbacks => prefsCallbacks.Count > 0;

    public static void MenuHook(string menuName)
    {
        var i = 0;
        foreach (var cb in menuCallbacks)
        {
            ImGui.PushID(i++);
            ImGui.Separator();
            cb.LuaState.PushString(menuName);
            cb.Invoke(1);
            ImGui.PopID();
        }
    }

    public static void PrefsHook()
    {
        var i = 0;
        foreach (var cb in prefsCallbacks)
        {
            ImGui.PushID(i++);
            cb.Invoke(0);
            ImGui.PopID();
        }
    }
    
    public static void Init(Lua lua)
    {
        if (!LuaInterface.Host.IsGui) return;

        lua.NewTable();

        lua.ModuleFunction("fileBrowserWidget", static (Lua lua) =>
        {
            var id = lua.CheckString(1);
            var openMode = (FileBrowser.OpenMode) lua.CheckOption(2, null, ["write", "read", "multiRead", "directory", "multiDirectory"]);

            if (openMode is FileBrowser.OpenMode.MultiDirectory or FileBrowser.OpenMode.MultiRead)
                return lua.ArgumentError(2, "cannot use a multi-select open mode");
            var path = lua.IsNoneOrNil(3) ? null : lua.CheckString(3);

            lua.PushBoolean(FileBrowser.Button(id, openMode, ref path));

            if (path is null)
                lua.PushNil();
            else
                lua.PushString(path);
            
            return 2;
        });

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