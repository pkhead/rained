namespace RainEd;

using System.Numerics;
using System.Text;
using ImGuiNET;
using NLua;
using NLua.Exceptions;
using Autotiles;

using LuaNativeFunction = KeraLua.LuaFunction;

static class LuaInterface
{
    // this is the C# side for autotiles programmed in Lua
    class LuaAutotile : Autotile
    {
        public LuaAutotile(LuaAutotileInterface wrapper) : base() {
            LuaWrapper = wrapper;
        }

        public LuaAutotile(LuaAutotileInterface wrapper, string name) : base(name) {
            LuaWrapper = wrapper;
        }

        public LuaFunction? LuaFillPathProcedure = null;
        public LuaFunction? LuaFillRectProcedure = null;
        public LuaAutotileInterface LuaWrapper;

        public record class ConfigOption
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

        public Dictionary<string, ConfigOption> Options = [];

        public void AddOption(string id, string name, bool defaultValue)
        {
            Options.Add(id, new ConfigOption(id, name, defaultValue));
        }

        public bool TryGetOption(string id, out ConfigOption? data)
        {
            return Options.TryGetValue(id, out data);
        }

        public override void TileRect(int layer, Vector2i rectMin, Vector2i rectMax, bool force, bool geometry)
        {
            string? modifierStr = null;
            if (geometry)
                modifierStr = "geometry";
            else if (force)
                modifierStr = "force";

            try
            {
                LuaFillRectProcedure?.Call(
                    LuaWrapper, layer + 1,
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

        public override void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry)
        {
            var luaState = LuaInterface.NLuaState;

            // push a new table on the stack with data
            // initialized to the given path segment
            static void CreateSegment(PathSegment seg)
            {
                var state = LuaInterface.NLuaState.State;
                state.NewTable();
                state.PushBoolean(seg.Left);
                state.SetField(-2, "left");
                state.PushBoolean(seg.Right);
                state.SetField(-2, "right");
                state.PushBoolean(seg.Up);
                state.SetField(-2, "up");
                state.PushBoolean(seg.Down);
                state.SetField(-2, "down");

                state.PushInteger(seg.X);
                state.SetField(-2, "x");
                state.PushInteger(seg.Y);
                state.SetField(-2, "y");
            }

            string? modifierStr = null;
            if (geometry)
                modifierStr = "geometry";
            else if (force)
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
                LuaFillPathProcedure?.Call(LuaWrapper, layer + 1, segmentTable, modifierStr);
            }
            catch (LuaScriptException e)
            {
                HandleException(e);
            }
        }

        public override void ConfigGui()
        {
            foreach (var opt in Options.Values)
            {
                ImGui.Checkbox(opt.Name, ref opt.Value);
            }
        }

        private string[]? missingCache = null;

        public override string[] MissingTiles {
            get {
                if (missingCache is not null) return missingCache;
                var luaState = LuaInterface.NLuaState;

                var table = LuaWrapper.RequiredTiles;
                if (table is null)
                {
                    missingCache = [];
                    return missingCache;
                }

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
                        LuaInterface.LogError($"invalid requiredTiles table for autotile '{Name}': expected string for item {i}, got {luaState.State.TypeName(-1)}");
                        IsReady = false;
                        break;
                    }
                }

                if (missingTiles.Count > 0)
                {
                    missingCache = missingTiles.ToArray();
                }
                else
                {
                    missingCache = [];
                }

                return missingCache;
            }
        }
    }

    // this is what Lua interacts with when creating autotiles
    class LuaAutotileInterface
    {
        [LuaHide]
        public readonly LuaAutotile autotile;

        [LuaMember(Name = "name")]
        public string Name { get => autotile.Name; set => autotile.Name = value; }

        [LuaMember(Name = "type")]
        public string Type
        {
            get => autotile.Type switch
            {
                AutotileType.Path => "path",
                AutotileType.Rect => "rect",
                _ => throw new Exception()
            };

            set => autotile.Type = value switch
            {
                "path" => AutotileType.Path,
                "rect" => AutotileType.Rect,
                _ => throw new Exception($"invalid autotile type '{value}'"),
            };
        }

        [LuaMember(Name = "tilePath")]
        public LuaFunction? TilePath { get => autotile.LuaFillPathProcedure; set => autotile.LuaFillPathProcedure = value; }
        
        [LuaMember(Name = "tileRect")]
        public LuaFunction? TileRect { get => autotile.LuaFillRectProcedure; set => autotile.LuaFillRectProcedure = value; }
        
        [LuaMember(Name = "requiredTiles")]
        public LuaTable? RequiredTiles = null;

        public LuaAutotileInterface()
        {
            autotile = new LuaAutotile(this);
        }

        [LuaMember(Name = "addOption")]
        public void AddOption(string id, string name, bool defaultValue) => autotile.AddOption(id, name, defaultValue);

        [LuaMember(Name = "getOption")]
        public bool GetOption(string id)
        {
            if (autotile.TryGetOption(id, out LuaAutotile.ConfigOption? data))
            {
                return data!.Value;
            }

            throw new LuaException($"option '{id}' does not exist");
        }
    }

    static private Lua luaState = null!;
    public static Lua NLuaState { get => luaState; }

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

    private static Dictionary<int, int> registeredCmds = [];
    private const string CommandID = "RainedCommandID";

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

        luaState.State.NewMetaTable(CommandID);
        luaState.State.PushCFunction(static (nint luaPtr) =>
        {
            luaState.State.PushString("The metatable is locked!");
            return 1;
        });
        luaState.State.SetField(-2, "__metatable");

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

    private static void HandleException(LuaScriptException e)
    {
        RainEd.Instance.ShowNotification("Error!");

        Exception actualException = e.IsNetException ? e.InnerException! : e;
        string? stackTrace = actualException.Data["Traceback"] as string;
        LuaInterface.LogError(stackTrace is not null ? actualException.Message + '\n' + stackTrace : actualException.Message);
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

        // function getTileAt
        luaState.Push(static (int x, int y, int layer) => {
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) return null;
            if (!level.IsInBounds(x, y)) return null;
            var tile = RainEd.Instance.Level.GetTile(level.Layers[layer-1, x, y]);
            return tile?.Name;
        });
        luaState.State.SetField(-2, "getTileAt");

        // function hasTileHead
        luaState.Push(static (int x, int y, int layer) => {
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) return false;
            if (!level.IsInBounds(x, y)) return false;
            return level.Layers[layer-1, x, y].TileHead is not null;
        });
        luaState.State.SetField(-2, "hasTileHead");

        // function deleteTile
        luaState.State.PushCFunction(static (nint luaStatePtr) => {
            int x = (int) luaState.State.CheckInteger(1);
            int y = (int) luaState.State.CheckInteger(2);
            int layer = (int) luaState.State.CheckInteger(3);
            bool removeGeo = false;

            if (!luaState.State.IsNoneOrNil(4))
                removeGeo = luaState.State.ToBoolean(4);
            
            var level = RainEd.Instance.Level;
            if (layer < 1 || layer > 3) return 0;
            if (!level.IsInBounds(x, y)) return 0;
            level.RemoveTileCell(layer, x, y, removeGeo);
            return 0;
        });
        luaState.State.SetField(-2, "deleteTile");

        // function registerCommand
        luaState.State.PushCFunction(static (nint luaStatePtr) => {
            string name = luaState.State.CheckString(1);
            luaState.State.CheckType(2, KeraLua.LuaType.Function);

            luaState.State.PushCopy(2);
            int funcRef = luaState.State.Ref(KeraLua.LuaRegistry.Index);
            int cmdId = RainEd.Instance.RegisterCommand(name, RunCommand);
            registeredCmds[cmdId] = funcRef;

            unsafe
            {
                var ud = (int*) luaState.State.NewUserData(sizeof(int));
                luaState.State.SetMetaTable(CommandID);
                *ud = cmdId;
            }

            return 1;
        });
        luaState.State.SetField(-2, "registerCommand");

        return 1;
    }

    private static void RunCommand(int id)
    {
        luaState.State.RawGetInteger(KeraLua.LuaRegistry.Index, registeredCmds[id]);
        
        try
        {
            var func = (LuaFunction) luaState.Pop();
            func.Call();
        }
        catch (LuaScriptException e)
        {
            HandleException(e);
        }
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

        var autotile = new LuaAutotileInterface()
        {
            Name = name
        };

        RainEd.Instance.Autotiles.AddAutotile(autotile.autotile, category);
        luaState.Push(autotile);
        return 1;
    }

    private static void ShowNotification(object? msg)
    {
        if (msg is null) return;
        RainEd.Instance.ShowNotification(msg.ToString()!);
    }

    delegate bool PlaceTileDelegate(out string? result, string tileName, int x, int y, int layer, string? modifier);
    public static bool PlaceTile(out string? result, string tileName, int x, int y, int layer, string? modifier)
    {
        result = null;

        var level = RainEd.Instance.Level;
        var placeMode = TilePlacementMode.Normal;
        
        // validate arguments
        if (modifier is not null)
        {
            if (modifier != "geometry" && modifier != "force")
            {
                throw new Exception($"expected 'geometry' or 'force' for argument 5, got '{modifier}'");
            }

            if (modifier == "geometry")   placeMode = TilePlacementMode.Geometry;
            else if (modifier == "force") placeMode = TilePlacementMode.Force;
        }
        if (layer < 1 || layer > 3)
            throw new Exception($"invalid layer {layer}");
        if (!RainEd.Instance.TileDatabase.HasTile(tileName))
            throw new Exception($"tile '{tileName}' is not recognized");
        layer--; // layer is 1-based in the lua code
        
        // begin placement
        var tile = RainEd.Instance.TileDatabase.GetTileFromName(tileName);

        var validationStatus = level.SafePlaceTile(tile, layer, x, y, placeMode);
        switch (validationStatus)
        {
            case TilePlacementStatus.OutOfBounds:
                result = "out of bounds";
                return false;
            case TilePlacementStatus.Overlap:
                result = "overlap";
                return false;
            case TilePlacementStatus.Geometry:
                result = "geometry";
                return false;
            case TilePlacementStatus.Success:
                return true;
        }

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
                AutoRequire(path + "." + Path.GetFileName(dirName), true);
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

    /*public static void CheckAutotileRequirements()
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
    }*/
}