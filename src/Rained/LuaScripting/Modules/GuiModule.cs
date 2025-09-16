using ImGuiNET;
using KeraLua;
using Rained.EditorGui;
namespace Rained.LuaScripting.Modules;

static class GuiModule
{
    private static readonly List<LuaCallback> prefsCallbacks = [];
    private static readonly ObjectWrap<FileBrowser> fileBrowserWrap = new("FileBrowser", "FILEBROWSER_REGISTRY");
    private static readonly string[] fileBrowserOpenMode = ["write", "read", "multiRead", "directory", "multiDirectory"];

    private static readonly List<(string menuName, List<LuaCallback> callbacks)> menuCallbacks = [];
    private static readonly List<LuaCallback> globalMenuCallbacks = []; // deprecated...

    private static nint levelFilterUserdata;

    public static bool HasPreferencesCallbacks => prefsCallbacks.Count > 0;

    private static readonly string[] BuiltInMenus = ["File", "Edit", "View", "Help"];
    public static IEnumerable<string> CustomMenus => menuCallbacks.Where(x => Array.IndexOf(BuiltInMenus, x.menuName) == -1).Select(x => x.menuName);

    public static void MenuHook(string menuName, bool separator)
    {
        foreach (var (name, callbacks) in menuCallbacks)
        {
            if (name == menuName)
            {
                var id = 0;
                foreach (var cb in callbacks)
                {
                    ImGui.PushID(id);
                    if (separator || id > 0) ImGui.Separator();
                    cb.Invoke(0);
                    ImGui.PopID();
                    id++;
                }

                break;
            }
        }

        // global menu callbacks
        // Undocumented feature, because this is how it worked in 3.0.0 and then in 4.1.0
        // i was like "wait. this is stupid. unrelated menus have separators that separate
        // nothing. God damnit." and I didn't want to bump the major version because of a
        // breaking change.
        if (Array.IndexOf(BuiltInMenus, menuName) != -1)
        {
            int id = 0;
            foreach (var cb in globalMenuCallbacks)
            {
                ImGui.PushID(id + 1000); // add 1000 to prevent collision with menu-local callbacks
                cb.LuaState.PushString(menuName.ToLowerInvariant());
                if (separator || id > 0) ImGui.Separator();
                cb.Invoke(1);
                ImGui.PopID();
                id++;
            }
        }
    }

    private static void AddMenuCallback(string menuName, LuaCallback callback)
    {
        foreach (var (name, cbs) in menuCallbacks)
        {
            if (menuName == name)
            {
                cbs.Add(callback);
                return;
            }
        }

        // menu of name doesn't already exist, create it
        menuCallbacks.Add((menuName, [callback]));
    }

    private static void RemoveMenuCallback(LuaCallback callback)
    {
        for (int i = 0; i < menuCallbacks.Count; i++)
        {
            var (_, cbs) = menuCallbacks[i];
            if (cbs.Remove(callback))
            {
                if (cbs.Count == 0)
                    menuCallbacks.RemoveAt(i);
                
                break;
            }
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

    private static List<(string name, bool isRw, string[] ext)> ParseFileFilters(Lua lua, int tableParameter)
    {
        List<(string name, bool isRw, string[] ext)> filters = [];

        int c = (int)lua.Length(tableParameter);
        for (int i = 1; i <= c; i++)
        {
            lua.GetInteger(tableParameter, i);

            // hardcoded built-in filters
            if (lua.IsUserData(-1) && lua.ToUserData(-1) == levelFilterUserdata)
            {
                filters.Add(("Level file", true, null!));
                lua.Pop(1);
                continue;
            }

            lua.ArgumentCheck(lua.IsTable(-1), tableParameter, "invalid filters table");

            // get filter name
            lua.GetInteger(-1, 1);
            lua.ArgumentCheck(lua.IsString(-1), tableParameter, "invalid filters table");
            var filterName = lua.ToString(-1);
            lua.Pop(1);

            // get filter extensions
            lua.GetInteger(-1, 2);

            if (lua.IsTable(-1))
            {
                int c2 = (int)lua.Length(-1);
                string[] filterExts = new string[c2];
                int j = 0;
                for (int k = 1; k <= c2; k++)
                {
                    lua.GetInteger(-1, k);
                    lua.ArgumentCheck(lua.IsString(-1), tableParameter, "invalid filters table");
                    filterExts[j++] = lua.ToString(-1);
                    lua.Pop(1);
                }

                filters.Add((filterName, false, filterExts));
            }
            else
            {
                lua.ArgumentCheck(lua.IsString(-1), tableParameter, "invalid filters table");
                var filterExt = lua.ToString(-1);
                filters.Add((filterName, false, [filterExt]));
            }

            lua.Pop(1);
        }

        return filters;
    }

    private static void ApplyFilters(FileBrowser fileBrowser, List<(string name, bool isRw, string[] ext)> filters)
    {
        foreach (var (name, isRw, ext) in filters)
        {
            if (isRw)
            {
                LevelData.FileFormats.LevelFileFormats.SetUpFileBrowser(fileBrowser);
            }
            else
            {
                fileBrowser.AddFilter(name, ext);
            }
        }
    }
    
    public static void Init(Lua lua)
    {
        if (!LuaInterface.Host.IsGui) return;

        InitFileBrowserMetatable(lua);

        lua.NewTable();

        {
            lua.NewTable();

            levelFilterUserdata = lua.NewIndexedUserData(1, 1);
            lua.SetField(-2, "level");

            lua.SetField(-2, "fileFilters");
        }

        lua.ModuleFunction("fileBrowserWidget", static (Lua lua) =>
        {
            var id = lua.CheckString(1);
            var openMode = (FileBrowser.OpenMode) lua.CheckOption(2, null, fileBrowserOpenMode);

            if (openMode is FileBrowser.OpenMode.MultiDirectory or FileBrowser.OpenMode.MultiRead)
                return lua.ArgumentError(2, "cannot use a multi-select open mode");
            var path = lua.IsNoneOrNil(3) ? null : lua.CheckString(3);

            List<(string name, bool isRw, string[] exts)>? filters = null;
            if (!lua.IsNoneOrNil(4))
            {
                lua.CheckType(4, LuaType.Table);
                filters = ParseFileFilters(lua, 4);
            }

            string? openDir = null;
            if (!lua.IsNoneOrNil(5))
                openDir = lua.CheckString(5);

            lua.PushBoolean(FileBrowser.Button(id, openMode, ref path, (fileBrowser) =>
            {
                if (filters is not null)
                    ApplyFilters(fileBrowser, filters);
            }, openDir));

            if (path is null)
                lua.PushNil();
            else
                lua.PushString(path);
            
            return 2;
        });

        lua.ModuleFunction("openFileBrowser", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var narg = lua.GetTop();
            var openMode = (FileBrowser.OpenMode)lua.CheckOption(1, null, fileBrowserOpenMode);
            if (!lua.IsNoneOrNil(2)) lua.CheckType(2, LuaType.Table);

            int funcRef;
            string? openDir = null;
            if (narg == 4)
            {
                if (!lua.IsNoneOrNil(3)) openDir = lua.CheckString(3);

                lua.CheckType(4, LuaType.Function);
                lua.PushCopy(4);
                funcRef = lua.Ref(LuaRegistry.Index);
            }
            else
            {
                // assume narg == 3
                lua.CheckType(3, LuaType.Function);
                lua.PushCopy(3);
                funcRef = lua.Ref(LuaRegistry.Index);
            }

            var filters = ParseFileFilters(lua, 2);

            void Callback(string[] paths)
            {
                var lua = LuaInterface.LuaState;

                // push callback function onto stack
                lua.RawGetInteger(LuaRegistry.Index, funcRef);
                lua.Unref(LuaRegistry.Index, funcRef);

                // push Arguments
                lua.NewTable();
                for (int i = 0; i < paths.Length; i++)
                {
                    lua.PushString(paths[i]);
                    lua.RawSetInteger(-2, i+1);
                }

                LuaHelpers.Call(lua, 1, 0);
            }

            var fileBrowser = new FileBrowser(openMode, Callback, openDir);
            ApplyFilters(fileBrowser, filters);

            fileBrowserWrap.PushWrapper(lua, fileBrowser);
            return 1;
        });

        lua.ModuleFunction("menuHook", static (Lua lua) =>
        {
            if (lua.GetTop() == 1 && lua.IsFunction(1)) // don't use this version of the function...
            {
                if (lua.GetGlobal("warn") is LuaType.Function or LuaType.UserData)
                {
                    lua.PushString("global menu hooks are a deprecated feature.");
                    lua.Call(1, 0);
                }

                lua.CheckType(1, LuaType.Function);
                lua.PushCopy(1);
                var cb = new LuaCallback(lua)
                {
                    OnDisconnect = static (Lua lua, LuaCallback cb) => globalMenuCallbacks.Remove(cb)
                };
                globalMenuCallbacks.Add(cb);
            }
            else
            {
                var menuName = lua.CheckString(1);
                lua.CheckType(2, LuaType.Function);
                lua.PushCopy(2);
                var cb = new LuaCallback(lua)
                {
                    OnDisconnect = static (Lua lua, LuaCallback cb) => RemoveMenuCallback(cb)
                };
                AddMenuCallback(menuName, cb);
            }
            
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

    public static void InitFileBrowserMetatable(Lua lua)
    {
        fileBrowserWrap.InitMetatable(lua);
        
        lua.ModuleFunction("__index", static (nint luaPtr) =>
        {
            var lua = Lua.FromIntPtr(luaPtr);
            var k = lua.CheckString(2);

            switch (k)
            {
                case "render":
                    lua.PushCFunction(static (nint luaPtr) =>
                    {
                        var lua = Lua.FromIntPtr(luaPtr);
                        var fileBrowser = fileBrowserWrap.GetRef(lua, 1);
                        lua.PushBoolean(fileBrowser.Render());
                        return 1;
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
}