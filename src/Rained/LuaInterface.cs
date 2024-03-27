using System.Numerics;
using System.Text;
using ImGuiNET;
using NLua;
using NLua.Exceptions;
namespace RainEd;

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

    [LuaMember(Name = "requiredTiles")]
    public LuaTable? LuaRequiredTiles = null;

    [LuaHide]
    public bool IsValid = false;

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

class LuaInterface
{
    private Lua luaState;
    public readonly List<Autotile> Autotiles = [];

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

    private List<LogEntry> Log = [];
    
    delegate void LuaPrintDelegate(params string[] args);
    
    private readonly KeraLua.LuaFunction loaderDelegate;
    private readonly string scriptsPath = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(Boot.AppDataPath, "scripts"));

    public LuaInterface()
    {
        luaState = new Lua()
        {
            UseTraceback = true
        };

        luaState["print"] = new LuaPrintDelegate(LuaPrint);
        luaState["warn"] = new LuaPrintDelegate(LuaWarning); 

        // configure package.path
        var package = (LuaTable) luaState["package"];
        package["path"] = Path.Combine(scriptsPath, "?.lua") + ";" + Path.Combine(scriptsPath, "?", "init.lua");

        // add rained module to require preloader
        // (this is just so that my stupid/smart lua linter doesn't give a bunch of warnings about an undefined global)
        loaderDelegate = new KeraLua.LuaFunction(RainedLoader);
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
    }

    private int RainedLoader(nint luaStatePtr)
    {
        luaState.State.NewTable();

        luaState.Push(new Func<Autotile>(CreateAutotile));
        luaState.State.SetField(-2, "createAutotile");

        luaState.Push(new Func<string>(GetVersion));
        luaState.State.SetField(-2, "getVersion");

        luaState.Push(new Action<string>(ShowNotification));
        luaState.State.SetField(-2, "alert");

        luaState.Push(new PlaceTileDelegate(PlaceTile));
        luaState.State.SetField(-2, "placeTile");

        return 1;
    }

    public void Initialize()
    {
        luaState.DoFile(Path.Combine(scriptsPath, "init.lua"));
    }
    
    private Autotile CreateAutotile()
    {
        var autotile = new Autotile();
        Autotiles.Add(autotile);

        return autotile;
    }

    private void ShowNotification(object? msg)
    {
        if (msg is null) return;
        RainEd.Instance.ShowNotification(msg.ToString()!);
    }

    delegate (bool, string) PlaceTileDelegate(string tileName, int layer, int x, int y, string? modifier);
    public (bool, string) PlaceTile(string tileName, int layer, int x, int y, string? modifier)
    {
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
            return (false, "out of bounds");
        }
        
        if (validationStatus == TilePlacementStatus.Overlap)
        {
            return (false, "overlap");
        }
        
        if (validationStatus == TilePlacementStatus.Geometry)
        {
            return (false, "geometry");
        }

        level.PlaceTile(
            tile,
            layer, x, y,
            modifier == "geometry"
        );

        return (true, null!);
    }

    private string GetVersion()
    {
        return RainEd.Version;
    }

    public void LogInfo(string msg)
    {
        RainEd.Logger.Information("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Info, msg));
    }

    public void LogWarning(string msg)
    {
        RainEd.Logger.Warning("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Warning, msg));
    }

    public void LogError(string msg)
    {
        RainEd.Logger.Error("[LUA] " + msg);
        Log.Add(new LogEntry(LogLevel.Error, msg));
    }

    private void LuaPrint(params object[] args)
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

    private void LuaWarning(params object[] args)
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

    public bool IsLogWindowOpen = false;
    public void ShowLogs()
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

    public void CheckAutotileRequirements()
    {
        foreach (var autotile in Autotiles)
        {
            var table = autotile.LuaRequiredTiles;

            if (table is null)
            {
                autotile.IsValid = true;
                continue;
            }

            List<string> missingTiles = [];

            for (int i = 1; table[i] is not null; i++)
            {
                if (table[i] is string tileName)
                {
                    if (!RainEd.Instance.TileDatabase.HasTile(tileName))
                    {
                        autotile.IsValid = false;
                        missingTiles.Add(tileName);
                    }
                }
                else
                {
                    luaState.Push(table[i]);
                    LogError($"invalid requiredTiles table for autotile '{autotile.Name}': expected string for item {i}, got {luaState.State.TypeName(-1)}");
                    autotile.IsValid = false;
                    RainEd.Instance.ShowNotification($"Error loading autotile {autotile.Name}");
                    break;
                }
            }

            if (missingTiles.Count > 0)
            {
                LogWarning($"missing required tiles for autotile '{autotile.Name}': {string.Join(", ", missingTiles)}");
            }
        }
    }

    struct SegmentStruct(int x, int y)
    {
        public bool Left = false;
        public bool Right = false;
        public bool Up = false;
        public bool Down = false;
        public int X = x;
        public int Y = y;
    }
    public void RunAutotile(Autotile autotile, int layer, List<Vector2i> pathPositions, bool forcePlace, bool forceGeometry)
    {
        void CreateSegment(SegmentStruct seg)
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

        luaState.State.NewTable();
        LuaTable segmentTable = (LuaTable) luaState.Pop();

        if (pathPositions.Count == 1)
        {
            var pos = pathPositions[0];
            
            SegmentStruct segment = new(pos.X, pos.Y);
            CreateSegment(segment);
            segmentTable[1] = luaState.Pop();
        }
        else if (pathPositions.Count > 1)
        {
            for (int i = 0; i < pathPositions.Count; i++)
            {
                SegmentStruct segment = new(pathPositions[i].X, pathPositions[i].Y);

                var lastSeg = pathPositions[^1]; // wraps around
                var curSeg = pathPositions[i];
                var nextSeg = pathPositions[0]; // wraps around

                if (i > 0)
                    lastSeg = pathPositions[i-1];

                if (i < pathPositions.Count - 1)
                    nextSeg = pathPositions[i+1];
                
                segment.Left =  (curSeg.Y == lastSeg.Y && curSeg.X - 1 == lastSeg.X) || (curSeg.Y == nextSeg.Y && curSeg.X - 1 == nextSeg.X);
                segment.Right = (curSeg.Y == lastSeg.Y && curSeg.X + 1 == lastSeg.X) || (curSeg.Y == nextSeg.Y && curSeg.X + 1 == nextSeg.X);
                segment.Up =    (curSeg.X == lastSeg.X && curSeg.Y - 1 == lastSeg.Y) || (curSeg.X == nextSeg.X && curSeg.Y - 1 == nextSeg.Y);
                segment.Down =  (curSeg.X == lastSeg.X && curSeg.Y + 1 == lastSeg.Y) || (curSeg.X == nextSeg.X && curSeg.Y + 1 == nextSeg.Y);

                CreateSegment(segment);
                segmentTable[i+1] = luaState.Pop();
            }
        }

        try
        {
            autotile.LuaFillPathProcedure?.Call(autotile, layer + 1, segmentTable, modifierStr);
        }
        catch (LuaScriptException e)
        {
            RainEd.Instance.ShowNotification("Error!");

            Exception actualException = e.IsNetException ? e.InnerException! : e;
            string? stackTrace = actualException.Data["Traceback"] as string;
            LogError(stackTrace is not null ? actualException.Message + '\n' + stackTrace : actualException.Message);
        }
    }
}