using System.Text;
using NLua;
using NLua.Exceptions;
using Rained.EditorGui;
using Rained.LuaScripting.Modules;
namespace Rained.LuaScripting;

static class LuaInterface
{
    public const int VersionMajor = 3;
    public const int VersionMinor = 0;
    public const int VersionRevision = 0;

    static private Lua luaState = null!;
    public static Lua NLuaState { get => luaState; }
    public static KeraLua.Lua LuaState => luaState.State;

    delegate void LuaPrintDelegate(params string[] args);

    private static LuaHelpers.LuaFunction loaderDelegate = new(RainedLoader);
    private static string scriptsPath = null!;

    public static IAPIHost Host { get; private set; } = null!;

    public static void Initialize(IAPIHost host, bool runAutoloads)
    {
        Host = host;

        scriptsPath = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(Boot.AppDataPath, "scripts"));

        luaState = new Lua()
        {
            UseTraceback = true
        };
        LuaHelpers.Init(luaState.State);

        luaState["print"] = new LuaPrintDelegate(LuaPrint);
        luaState["warn"] = new LuaPrintDelegate(LuaWarning);

        // configure package.path
        var package = (LuaTable)luaState["package"];
        package["path"] = Path.Combine(scriptsPath, "?.lua") + ";" + Path.Combine(scriptsPath, "?", "init.lua");

        // global functions
        LuaHelpers.PushCsFunction(luaState.State, new Action<string, bool?>(AutoRequire));
        luaState.State.SetGlobal("autorequire");

        // add modules to require preloader
        {
            luaState.State.GetSubTable((int)KeraLua.LuaRegistry.Index, "_PRELOAD");

            LuaHelpers.PushLuaFunction(luaState.State, loaderDelegate);
            luaState.State.SetField(-2, "rained");

            LuaHelpers.PushLuaFunction(luaState.State, PathModuleLoader);
            luaState.State.SetField(-2, "path");

            luaState.State.Pop(1); // pop preload table

            // extend os library
            luaState.State.GetGlobal("os");
            FileSystemFunctions(luaState.State);
            luaState.State.Pop(1);
        }

        // initialize global variables
        string globalsInit = """
        rained = require("rained")

        GEO_TYPE = {
            AIR = 0,
            SOLID = 1,
            SLOPE_RIGHT_UP = 2,
            SLOPE_LEFT_UP = 3,
            SLOPE_RIGHT_DOWN = 4,
            SLOPE_LEFT_DOWN = 5,
            FLOOR = 6,
            SHORTCUT_ENTRANCE = 7,
            GLASS = 9
        }

        OBJECT_TYPE = {
            NONE = 0,
            HORIZONTAL_BEAM = 1,
            VERTICAL_BEAM = 2,
            HIVE = 3,
            SHORTCUT = 5,
            ENTRANCE = 6,
            CREATURE_DEN = 7,
            ROCK = 9,
            SPEAR = 10,
            CRACK = 11,
            FORBID_FLY_CHAIN = 12,
            GARBAGE_WORM = 13,
            WATERFALL = 18,
            WHACK_A_MOLE_HOLE = 19,
            WORM_GRASS = 20,
            SCAVENGER_HOLE = 21
        }
        """;
        luaState.DoString(globalsInit);

        // disable NLua import function
        // damn... stupid nlua library
        // i can't disable the debug library even though
        // 1. there is a function called luaL_traceback
        // 2. they could have just saved the function from the debug library into the registry
        // ...
        luaState.DoString("import = nil");
        
        if (runAutoloads)
        {
            luaState.DoFile(Path.Combine(scriptsPath, "init.lua"));
            if (Directory.Exists(Path.Combine(scriptsPath, "autoload")))
            {
                luaState.DoString("autorequire('autoload', true)");
            }
        }
    }

    public static void Unload()
    {
        RainedModule.RemoveAllCommands(luaState.State);
        LuaCallback.RemoveAllCallbacks();
        TilesModule.RemoveAllAutotiles();
        luaState.Dispose();
        luaState = null!;
    }

    public static void HandleException(LuaScriptException e)
    {
        if (Host.IsGui)
            EditorWindow.ShowNotification("Error!");

        Exception actualException = e.IsNetException ? e.InnerException! : e;
        string? stackTrace = actualException.Data["Traceback"] as string;
        LogError(stackTrace is not null ? actualException.Message + '\n' + stackTrace : actualException.Message);
    }

    private static int RainedLoader(KeraLua.Lua lua)
    {
        LuaCallback.InitType(lua);

        lua.NewTable();
        RainedModule.Init(lua, luaState);
        LevelModule.Init(lua, luaState);
        CellsModule.Init(lua, luaState);
        TilesModule.Init(lua, luaState);
        MaterialsModule.Init(lua, luaState);
        EffectsModule.Init(lua, luaState);
        CameraModule.Init(lua, luaState);
        PropModule.Init(lua, luaState);
        HistoryModule.Init(lua, luaState);

        return 1;
    }

    private static void FileSystemFunctions(KeraLua.Lua lua)
    {
        lua.ModuleFunction("getcwd", static (KeraLua.Lua lua) =>
        {
            lua.PushString(Directory.GetCurrentDirectory());
            return 1;
        });

        lua.ModuleFunction("mkdir", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Log.Error("os.mkdir: " + e.Message);
                return lua.ErrorWhere("could not create directory: " + e.Message);
            }

            return 1;
        });

        lua.ModuleFunction("rmdir", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            var recursive = false;
            if (!lua.IsNoneOrNil(2))
            {
                recursive = lua.ToBoolean(2);
            }

            try
            {
                Directory.Delete(path, recursive);
            }
            catch (Exception e)
            {
                Log.Error("os.mkdir: " + e.Message);
                return lua.ErrorWhere("could not remove directory: " + e.Message);
            }

            return 1;
        });

        lua.ModuleFunction("list", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);

            string filter;
            if (lua.IsNoneOrNil(2)) filter = "*";
            else                    filter = lua.ToString(2);

            IEnumerator<string> enumerator;
            try
            {
                enumerator = Directory.EnumerateFileSystemEntries(path, filter).GetEnumerator() ?? throw new NullReferenceException();
            }
            catch (Exception e)
            {
                Log.Information("path.list error: " + e);
                return lua.ErrorWhere("could not list entries of " + path);
            }

            LuaHelpers.PushClosureWithUserdata(lua, enumerator, static (nint luaPtr) =>
            {
                var lua = KeraLua.Lua.FromIntPtr(luaPtr);
                var enumr = (IEnumerator<string>) LuaHelpers.GetUserData(lua);
                if (enumr.MoveNext())
                {
                    lua.PushString(enumr.Current);
                    return 1;
                }
                else
                {
                    return 0;
                }
            });
            return 1;
        });

        lua.ModuleFunction("listfiles", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);

            string filter;
            if (lua.IsNoneOrNil(2)) filter = "*";
            else                    filter = lua.ToString(2);

            IEnumerator<string> enumerator;
            try
            {
                enumerator = Directory.EnumerateFiles(path, filter).GetEnumerator() ?? throw new NullReferenceException();
            }
            catch (Exception e)
            {
                Log.Information("path.list error: " + e);
                return lua.ErrorWhere("could not list files of " + path);
            }

            LuaHelpers.PushClosureWithUserdata(lua, enumerator, static (nint luaPtr) =>
            {
                var lua = KeraLua.Lua.FromIntPtr(luaPtr);
                var enumr = (IEnumerator<string>) LuaHelpers.GetUserData(lua);
                if (enumr.MoveNext())
                {
                    lua.PushString(enumr.Current);
                    return 1;
                }
                else
                {
                    return 0;
                }
            });
            return 1;
        });

        lua.ModuleFunction("listdirs", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);

            string filter;
            if (lua.IsNoneOrNil(2)) filter = "*";
            else                    filter = lua.ToString(2);

            IEnumerator<string> enumerator;
            try
            {
                enumerator = Directory.EnumerateDirectories(path, filter).GetEnumerator() ?? throw new NullReferenceException();
            }
            catch (Exception e)
            {
                Log.Information("path.list error: " + e);
                return lua.ErrorWhere("could not list subdirectories of " + path);
            }

            LuaHelpers.PushClosureWithUserdata(lua, enumerator, static (nint luaPtr) =>
            {
                var lua = KeraLua.Lua.FromIntPtr(luaPtr);
                var enumr = (IEnumerator<string>) LuaHelpers.GetUserData(lua);
                if (enumr.MoveNext())
                {
                    lua.PushString(enumr.Current);
                    return 1;
                }
                else
                {
                    return 0;
                }
            });
            return 1;
        });
    }

    // this is based off the python os.path module
    private static int PathModuleLoader(KeraLua.Lua lua)
    {
        lua.NewTable();

        lua.PushString(Path.DirectorySeparatorChar.ToString());
        lua.SetField(-2, "sep");

        // FileSystemFunctions(lua);

        lua.ModuleFunction("abspath", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.GetFullPath(path));
            return 1;
        });

        lua.ModuleFunction("basename", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.GetFileName(path));
            return 1;
        });

        lua.ModuleFunction("dirname", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            var res = Path.GetDirectoryName(path);

            if (res is not null)
                lua.PushString(res);
            else
                lua.PushNil();

            return 1;
        });

        lua.ModuleFunction("exists", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushBoolean(Path.Exists(path));
            return 1;
        });

        lua.ModuleFunction("isfile", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushBoolean(File.Exists(path));
            return 1;
        });

        lua.ModuleFunction("isdir", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushBoolean(Directory.Exists(path));
            return 1;
        });
        
        lua.ModuleFunction("isabs", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushBoolean(Path.IsPathFullyQualified(path));
            return 1;
        });

        lua.ModuleFunction("join", static (KeraLua.Lua lua) =>
        {
            int nargs = lua.GetTop();
            if (nargs == 0)
            {
                lua.PushString("");
                return 1;
            }

            string[] args = new string[nargs];
            for (int i = 1; i <= nargs; i++)
            {
                args[i - 1] = lua.CheckString(i);
            }

            try
            {
                lua.PushString(Path.Combine(args));
                return 1;
            }
            catch (Exception e)
            {
                Log.Error("path.join error: " + e.Message);
                return lua.ErrorWhere("could not combine paths");
            }
        });

        lua.ModuleFunction("normcase", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            if (OperatingSystem.IsWindows())
                lua.PushString(path.Replace('/', '\\').ToLowerInvariant());
            else
                lua.PushString(path);
            return 1;
        });

        lua.ModuleFunction("normpath", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(path)));
            return 1;
        });

        lua.ModuleFunction("relpath", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            var start = lua.CheckString(2);
            lua.PushString(Path.GetRelativePath(start, path));
            return 1;
        });

        lua.ModuleFunction("split", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.GetDirectoryName(path) ?? "");
            lua.PushString(Path.GetFileName(path));
            return 2;
        });

        lua.ModuleFunction("splitext", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.ChangeExtension(path, null));
            lua.PushString(Path.GetExtension(path));
            return 2;
        });

        lua.ModuleFunction("getext", static (KeraLua.Lua lua) =>
        {
            var path = lua.CheckString(1);
            lua.PushString(Path.GetExtension(path));
            return 1;
        });

        return 1;
    }

    private static void AutoRequire(string path, bool? recurse)
    {
        List<string> combineParams = [Boot.AppDataPath, "scripts"];
        combineParams.AddRange(path.Split('.'));
        var filePath = Path.Combine([.. combineParams]);

        if (!Directory.Exists(filePath))
            throw new Exception($"path '{path}' does not exist");

        var files = Directory.GetFiles(filePath).ToList();
        files.Sort();
        foreach (var fileName in files)
        {
            if (Path.GetExtension(fileName) != ".lua") continue;

            luaState.State.GetGlobal("require");
            luaState.State.PushString(path + "." + Path.GetFileNameWithoutExtension(fileName));
            luaState.State.Call(1, 0);
        }

        if (recurse.GetValueOrDefault())
        {
            var subDirs = Directory.GetDirectories(filePath).ToList();
            subDirs.Sort();
            foreach (var dirName in subDirs)
            {
                AutoRequire(path + "." + Path.GetFileName(dirName), true);
            }
        }
    }

    public static void LogError(string msg)
    {
        Host.Error(msg);
    }

    private static void LuaPrint(params object[] args)
    {
        StringBuilder stringBuilder = new();

        for (int i = 0; i < args.Length; i++)
        {
            var v = args[i];
            var str = v is null ? "nil" : v.ToString()!;
            stringBuilder.Append(str);
            if (i < args.Length - 1) stringBuilder.Append(' ', 8 - str.Length % 8);
        }

        Host.Print(stringBuilder.ToString());
    }

    private static void LuaWarning(params object[] args)
    {
        StringBuilder stringBuilder = new();

        for (int i = 0; i < args.Length; i++)
        {
            var v = args[i];
            var str = v is null ? "nil" : v.ToString()!;
            stringBuilder.Append(str);
            if (i < args.Length - 1) stringBuilder.Append(' ', 8 - str.Length % 8);
        }

        Host.Warn(stringBuilder.ToString());
    }

    public static void UIUpdate()
    {
        RainedModule.UIUpdate();
        PropModule.UpdateSettingsSnapshot();
    }

    public static void Update(float dt)
    {
        RainedModule.UpdateCallback(dt);
    }
}