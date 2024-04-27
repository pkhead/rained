using System.Numerics;
using System.Text;
using ImGuiNET;
using NLua;
using NLua.Exceptions;
namespace RainEd;

using LuaNativeFunction = KeraLua.LuaFunction;

class Autotile
{
    [LuaMember(Name = "name")]
    public string Name;

    [LuaMember(Name = "pathThickness")]
    public int PathThickness;

    [LuaMember(Name = "segmentLength")]
    public int SegmentLength;

    public enum AutoType
    {
        Path, Rect
    }

    [LuaHide]
    public AutoType Type; 

    [LuaMember(Name = "type")]
    public string LuaType
    {
        get => Type switch
        {
            AutoType.Path => "path",
            AutoType.Rect => "rect",
            _ => throw new Exception()
        };

        set => Type = value switch
        {
            "path" => AutoType.Path,
            "rect" => AutoType.Rect,
            _ => throw new Exception($"invalid autotile type '{value}'"),
        };
    }

    public record ConfigOption
    {
        public readonly string ID;
        public readonly string Name;
        public bool Value;

        public ConfigOption(string id, string name, bool defaultValue)
        {
            ID = id;
            Name = name;
            Value = defaultValue;
        }
    }

    [LuaHide]
    public Dictionary<string, ConfigOption> Options = []; 

    [LuaMember(Name = "tilePath")]
    public LuaFunction? LuaFillPathProcedure = null;

    [LuaMember(Name = "tileRect")]
    public LuaFunction? LuaFillRectProcedure = null;

    [LuaMember(Name = "requiredTiles")]
    public LuaTable? LuaRequiredTiles = null;

    [LuaHide]
    public string[] MissingTiles = [];

    public Autotile()
    {
        Name = "(unnamed)";
        Type = AutoType.Rect;
        PathThickness = 1;
        SegmentLength = 1;
    }

    [LuaMember(Name = "addOption")]
    public void AddOption(string id, string name, bool defaultValue)
    {
        Options.Add(id, new ConfigOption(id, name, defaultValue));
    }

    [LuaMember(Name = "getOption")]
    public bool GetOption(string id)
    {
        if (Options.TryGetValue(id, out ConfigOption? data))
        {
            return data.Value;
        }
        else
        {
            throw new LuaException($"option '{id}' does not exist");
        }
    }
}

static class LuaInterface
{
    static private Lua luaState = null!;
    public static readonly List<string> AutotileCategories = ["Misc"];
    private static readonly List<List<Autotile>> Autotiles = [[]];

    enum LogLevel : byte
    {
        Warning,
        Error,
        Info
    };

    readonly struct LogEntry(LogLevel level, string msg)
    {
        public readonly LogLevel Level = level;
        public readonly string Message = msg;
    }

    private static readonly List<LogEntry> Log = [];
    
    delegate void LuaPrintDelegate(params string[] args);

    private static LuaNativeFunction loaderDelegate = new(RainedLoader);
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
        LuaHelpers.PushDelegate(luaState.State, new Action<string, bool?>(AutoRequire));
        luaState.State.SetGlobal("autorequire");

        // add rained module to require preloader
        // (this is just so that my stupid/smart lua linter doesn't give a bunch of warnings about an undefined global)
        luaState.State.GetSubTable((int)KeraLua.LuaRegistry.Index, "_PRELOAD");
        luaState.State.PushCFunction(loaderDelegate);
        luaState.State.SetField(-2, "rained");
        luaState.State.Pop(1); // pop preload table

        // assign module to global variable
        luaState.DoString("rained = require(\"rained\")");

        // disable NLua import function
        // damn... stupid nlua library
        // i can't disable the debug library even though
        // 1. there is a function called luaL_traceback
        // 2. they could have just saved the function from the debug library into the registry
        // ...
        luaState.DoString("import = nil");

        luaState.DoFile(Path.Combine(scriptsPath, "init.lua"));
    }

    private static int RainedLoader(nint luaStatePtr)
    {
        luaState.State.NewTable();

        //luaState.Push(new Func<string, object?, Autotile>(CreateAutotile));
        luaState.State.PushCFunction(LuaCreateAutotile);
        luaState.State.SetField(-2, "createAutotile");

        luaState.Push(new Func<string>(GetVersion));
        luaState.State.SetField(-2, "getVersion");

        luaState.Push(new Action<string>(ShowNotification));
        luaState.State.SetField(-2, "alert");

        LuaHelpers.PushDelegate(luaState.State, new PlaceTileDelegate(PlaceTile));
        luaState.State.SetField(-2, "placeTile");

        return 1;
    }

    private static int LuaCreateAutotile(nint luaStatePtr)
    {
        var name = luaState.State.CheckString(1);
        var category = "Misc";

        // the optional second argument is the category name
        if (!luaState.State.IsNoneOrNil(2))
        {
            category = luaState.State.CheckString(2);
        }

        var autotile = new Autotile()
        {
            Name = name
        };
        
        var catIndex = AutotileCategories.IndexOf(category);
        if (catIndex == -1)
        {
            catIndex = AutotileCategories.Count;
            AutotileCategories.Add(category);
            Autotiles.Add([]);
        }

        Autotiles[catIndex].Add(autotile);

        luaState.Push(autotile);
        return 1;
    }

    public static List<Autotile> GetAutotilesInCategory(string category)
        => Autotiles[AutotileCategories.IndexOf(category)];

    public static List<Autotile> GetAutotilesInCategory(int index)
        => Autotiles[index];

    private static void ShowNotification(object? msg)
    {
        if (msg is null) return;
        RainEd.Instance.ShowNotification(msg.ToString()!);
    }

    delegate bool PlaceTileDelegate(out string? result, string tileName, int layer, int x, int y, string? modifier);
    public static bool PlaceTile(out string? result, string tileName, int layer, int x, int y, string? modifier)
    {
        result = null;

        var level = RainEd.Instance.Level;
        
        // validate arguments
        if (modifier is not null)
        {
            if (modifier != "geometry" && modifier != "force")
            {
                throw new Exception($"expected 'geometry' or 'force' for argument 5, got '{modifier}'");
            }
        }
        if (layer < 1 || layer > 3)
            throw new Exception($"invalid layer {layer}");
        if (!RainEd.Instance.TileDatabase.HasTile(tileName))
            throw new Exception($"tile '{tileName}' is not recognized");
        layer--; // layer is 1-based in the lua code
        
        // begin placement
        var tile = RainEd.Instance.TileDatabase.GetTileFromName(tileName);

        // check if requirements are satisfied
        TilePlacementStatus validationStatus;

        if (level.IsInBounds(x, y))
            validationStatus = level.ValidateTilePlacement(
                tile,
                x, y, layer,
                modifier is not null
            );
        else
        {
            result = "out of bounds";
            return false;
        }
        
        if (validationStatus == TilePlacementStatus.Overlap)
        {
            result = "overlap";
            return false;
        }
        
        if (validationStatus == TilePlacementStatus.Geometry)
        {
            result = "geometry";
            return false;
        }

        level.PlaceTile(
            tile,
            layer, x, y,
            modifier == "geometry"
        );

        return true;
    }

    private static string GetVersion()
    {
        return RainEd.Version;
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
                AutoRequire(path + "." + dirName, true);
            }
        }
    }

    public static void LogInfo(string msg)
    {
        RainEd.Logger.Information("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Info, msg));
    }

    public static void LogWarning(string msg)
    {
        RainEd.Logger.Warning("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Warning, msg));
    }

    public static void LogError(string msg)
    {
        RainEd.Logger.Error("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Error, msg));
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

    public static bool IsLogWindowOpen = false;
    public static void ShowLogs()
    {
        if (!IsLogWindowOpen) return;

        if (ImGui.Begin("Logs", ref IsLogWindowOpen))
        {
            if (ImGui.Button("Clear"))
                Log.Clear();

            if (ImGui.BeginChild("scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 5f));
                
                foreach (var msg in Log)
                {
                    switch (msg.Level)
                    {
                        case LogLevel.Error:
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 48f/255f, 48/255f, 1f));
                            break;

                        case LogLevel.Warning:
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 165f/255f, 48f/255f, 1f));
                            break;

                        case LogLevel.Info:
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                            break;
                    }
                    
                    ImGui.TextUnformatted(msg.Message);
                    ImGui.PopStyleColor();
                }
                ImGui.PopStyleVar();

                // auto-scroll
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    ImGui.SetScrollHereY(1f);
            }
        } ImGui.End();
    }

    public static void CheckAutotileRequirements()
    {
        foreach (var list in Autotiles)
        {
            foreach (var autotile in list)
            {
                var table = autotile.LuaRequiredTiles;

                if (table is null)
                    continue;

                List<string> missingTiles = [];

                for (int i = 1; table[i] is not null; i++)
                {
                    if (table[i] is string tileName)
                    {
                        if (!RainEd.Instance.TileDatabase.HasTile(tileName))
                        {
                            missingTiles.Add(tileName);
                        }
                    }
                    else
                    {
                        luaState.Push(table[i]);
                        LogError($"invalid requiredTiles table for autotile '{autotile.Name}': expected string for item {i}, got {luaState.State.TypeName(-1)}");
                        RainEd.Instance.ShowNotification($"Error loading autotile {autotile.Name}");
                        break;
                    }
                }

                if (missingTiles.Count > 0)
                {
                    LogWarning($"missing required tiles for autotile '{autotile.Name}': {string.Join(", ", missingTiles)}");
                    autotile.MissingTiles = missingTiles.ToArray();
                }
            }
        }
    }

    public struct PathSegment(int x, int y)
    {
        public bool Left = false;
        public bool Right = false;
        public bool Up = false;
        public bool Down = false;
        public int X = x;
        public int Y = y;
    }

    private static void HandleException(LuaScriptException e)
    {
        RainEd.Instance.ShowNotification("Error!");

        Exception actualException = e.IsNetException ? e.InnerException! : e;
        string? stackTrace = actualException.Data["Traceback"] as string;
        LogError(stackTrace is not null ? actualException.Message + '\n' + stackTrace : actualException.Message);
    }

    public static void RunPathAutotile(Autotile autotile, int layer, PathSegment[] pathSegments, bool forcePlace, bool forceGeometry)
    {
        // push a new table on the stack with data
        // initialized to the given path segment
        static void CreateSegment(PathSegment seg)
        {
            luaState.State.NewTable();
            luaState.State.PushBoolean(seg.Left);
            luaState.State.SetField(-2, "left");
            luaState.State.PushBoolean(seg.Right);
            luaState.State.SetField(-2, "right");
            luaState.State.PushBoolean(seg.Up);
            luaState.State.SetField(-2, "up");
            luaState.State.PushBoolean(seg.Down);
            luaState.State.SetField(-2, "down");

            luaState.State.PushInteger(seg.X);
            luaState.State.SetField(-2, "x");
            luaState.State.PushInteger(seg.Y);
            luaState.State.SetField(-2, "y");
        }

        string? modifierStr = null;
        if (forceGeometry)
            modifierStr = "geometry";
        else if (forcePlace)
            modifierStr = "force";

        // create segment table
        luaState.State.CreateTable(pathSegments.Length, 0);
        for (int i = 0; i < pathSegments.Length; i++)
        {
            CreateSegment(pathSegments[i]);

            // segmentTable[i+1] = newSegment
            // segmentTable is at stack index -2
            // newSegment is at the top of the stack
            luaState.State.RawSetInteger(-2, i + 1);
        }

        try
        {
            LuaTable segmentTable = (LuaTable) luaState.Pop();
            autotile.LuaFillPathProcedure?.Call(autotile, layer + 1, segmentTable, modifierStr);
        }
        catch (LuaScriptException e)
        {
            HandleException(e);
        }
    }

    /// <summary>
    /// Run the given rect autotiler.
    /// </summary>
    /// <param name="autotile">The autotiler to invoke.</param>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="rectPos">The position of the input rectangle.</param>
    /// <param name="rectSize">The size of the input rectangle.</param>
    /// <param name="force">If the autotiler should force-place</param>
    /// <param name="geometry">If the autotiler should force geometry.</param>
    public static void RunRectAutotile(
        Autotile autotile,
        int layer,
        Vector2i rectMin, Vector2i rectMax,
        bool force, bool geometry
    )
    {
        string? modifierStr = null;
        if (geometry)
            modifierStr = "geometry";
        else if (force)
            modifierStr = "force";

        try
        {
            autotile.LuaFillRectProcedure?.Call(
                autotile, layer + 1,
                rectMin.X, rectMin.Y,
                rectMax.X, rectMax.Y,
                modifierStr
            );
        }
        catch (LuaScriptException e)
        {
            HandleException(e);
        }
    }
}