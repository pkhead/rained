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
        
    public LuaInterface()
    {
        luaState = new Lua();
    
        // disable NLua import function and debug library
        luaState.DoString("import = nil debug = nil");

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
}