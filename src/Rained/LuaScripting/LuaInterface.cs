using System.Text;
using NLua;
using NLua.Exceptions;
using Rained.EditorGui;
using Rained.LevelData;
namespace Rained.LuaScripting;

static class LuaInterface
{
    public const int VersionMajor = 3;
    public const int VersionMinor = 0;
    public const int VersionRevision = 0;
    
    static private Lua luaState = null!;
    public static Lua NLuaState { get => luaState; }
    
    delegate void LuaPrintDelegate(params string[] args);

    private static LuaHelpers.LuaFunction loaderDelegate = new(RainedLoader);
    private static string scriptsPath = null!;

    public static void Initialize()
    {
        scriptsPath = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(Boot.AppDataPath, "scripts"));

        luaState = new Lua()
        {
            UseTraceback = true
        };
        LuaHelpers.Init(luaState.State);

        luaState["print"] = new LuaPrintDelegate(LuaPrint);
        luaState["warn"] = new LuaPrintDelegate(LuaWarning); 

        // configure package.path
        var package = (LuaTable) luaState["package"];
        package["path"] = Path.Combine(scriptsPath, "?.lua") + ";" + Path.Combine(scriptsPath, "?", "init.lua");

        // global functions
        LuaHelpers.PushCsFunction(luaState.State, new Action<string, bool?>(AutoRequire));
        luaState.State.SetGlobal("autorequire");

        // add rained module to require preloader
        // (this is just so that my stupid/smart lua linter doesn't give a bunch of warnings about an undefined global)
        luaState.State.GetSubTable((int)KeraLua.LuaRegistry.Index, "_PRELOAD");
        LuaHelpers.PushLuaFunction(luaState.State, loaderDelegate);
        luaState.State.SetField(-2, "rained");
        luaState.State.Pop(1); // pop preload table

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

        luaState.DoFile(Path.Combine(scriptsPath, "init.lua"));
        if (Directory.Exists(Path.Combine(scriptsPath, "autoload")))
        {
            luaState.DoString("autorequire('autoload', true)");
        }
    }

    public static void HandleException(LuaScriptException e)
    {
        EditorWindow.ShowNotification("Error!");

        Exception actualException = e.IsNetException ? e.InnerException! : e;
        string? stackTrace = actualException.Data["Traceback"] as string;
        LogError(stackTrace is not null ? actualException.Message + '\n' + stackTrace : actualException.Message);
    }

    private static int RainedLoader(KeraLua.Lua lua)
    {
        lua.NewTable();
        RainedModule.Init(lua, luaState);
        CellsModule.Init(lua, luaState);
        TilesModule.Init(lua, luaState);

        return 1;
    }

    private static void AutoRequire(string path, bool? recurse)
    {
        List<string> combineParams = [Boot.AppDataPath, "scripts"];
        combineParams.AddRange(path.Split('.'));
        var filePath = Path.Combine([..combineParams]);

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

    public static void LogInfo(string msg)
    {
        Log.UserLogger.Information("[lua] " + msg);
    }

    public static void LogWarning(string msg)
    {
        Log.UserLogger.Warning("[lua] " + msg);
    }

    public static void LogError(string msg)
    {
        Log.UserLogger.Error("[lua] " + msg);
    }

    private static void LuaPrint(params object[] args)
    {
        StringBuilder stringBuilder = new();

        foreach (var v in args)
        {
            var str = v is null ? "nil" : v.ToString()!;
            stringBuilder.Append(str);
            stringBuilder.Append(' ', 8 - str.Length % 8);
        }

        LogInfo(stringBuilder.ToString());
    }

    private static void LuaWarning(params object[] args)
    {
        StringBuilder stringBuilder = new();

        foreach (var v in args)
        {
            var str = v is null ? "nil" : v.ToString()!;
            stringBuilder.Append(str);
            stringBuilder.Append(' ', 8 - str.Length % 8);
        }

        LogWarning(stringBuilder.ToString());        
    }
}