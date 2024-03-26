using System.Numerics;
using System.Text;
using ImGuiNET;
using NLua;
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

    [LuaMember(Name = "fillPath")]
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
            throw new Exception($"option '{id}' does not exist");
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

    public LuaInterface()
    {
        luaState = new Lua();
    
        // disable NLua import function and debug library
        luaState.DoString("import = nil debug = nil");

        luaState["print"] = new LuaPrintDelegate(LuaPrint);
        luaState["warn"] = new LuaPrintDelegate(LuaWarning); 

        // configure package.path
        var package = (LuaTable) luaState["package"];
        package["path"] = Path.Combine(Boot.AppDataPath, "scripts", "?.lua") + ";" + Path.Combine(Boot.AppDataPath, "scripts", "?", "init.lua");

        luaState.DoString("Rained = {}");
        var luaRained = (LuaTable) luaState["Rained"];
        luaRained["createAutotile"] = new Func<Autotile>(CreateAutotile);
    }

    public void Initialize()
    {
        luaState.DoFile(Path.Combine(Boot.AppDataPath, "scripts", "init.lua"));
    }
    
    private Autotile CreateAutotile()
    {
        var autotile = new Autotile();
        Autotiles.Add(autotile);

        return autotile;
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

    private void LuaPrint(params string[] args)
    {
        StringBuilder stringBuilder = new();

        foreach (var str in args)
        {
            stringBuilder.Append(str);
            stringBuilder.Append(' ', 8 - str.Length % 8);
        }

        LogInfo(stringBuilder.ToString());
    }

    private void LuaWarning(params string[] args)
    {
        StringBuilder stringBuilder = new();

        foreach (var str in args)
        {
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
            if (ImGui.IsKeyPressed(ImGuiKey.T))
            {
                LogError("Test " + Log.Count);
            }

            if (ImGui.Button("Clear"))
                Log.Clear();

            if (ImGui.BeginChild("scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                unsafe
                {
                    ImGuiListClipper clipperStruct = new();
                    ImGuiListClipperPtr clipper = new(&clipperStruct);

                    clipper.Begin(Log.Count, ImGui.GetTextLineHeight());
                    while (clipper.Step())
                    {
                        for (int lineNo = clipper.DisplayStart; lineNo < clipper.DisplayEnd; lineNo++)
                        {
                            switch (Log[lineNo].Level)
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
                            
                            ImGui.TextUnformatted(Log[lineNo].Message);
                            ImGui.PopStyleColor();
                        }
                    }
                    clipper.End();
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
}