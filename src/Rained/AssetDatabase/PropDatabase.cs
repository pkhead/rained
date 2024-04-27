using RainEd.Tiles;
using Raylib_cs;
using System.Numerics;
namespace RainEd.Props;

enum PropType
{
    Standard,
    VariedStandard,
    Soft,
    ColoredSoft,
    VariedSoft,
    SimpleDecal,
    VariedDecal,
    Antimatter,
    Rope,
    Long
};

// Used to generate note synopses
[Flags]
enum PropFlags
{
    ProcedurallyShaded = 1,
    RandomVariation = 2,
    CustomDepthAvailable = 4,
    CustomColorAvailable = 8,
    Colorize = 16,
    CanSetThickness = 32,
    Tile = 64,
}

// data in propColors.txt, used for custom colors
struct PropColor
{
    public string Name;
    public Color Color;
}

record PropInit
{
    public readonly string Name;
    public readonly PropCategory Category;
    public readonly PropType Type;
    public readonly RlManaged.Texture2D? Texture;
    public readonly PropFlags PropFlags;
    public readonly int Depth;
    public readonly int VariationCount;
    public readonly string[] Notes;
    public readonly RopeInit? Rope;

    // used for obtaining preview image
    private readonly int pixelWidth;
    private readonly int pixelHeight;
    private readonly int layerCount;

    public float Width { get => pixelWidth / 20f; }
    public float Height { get => pixelHeight / 20f; }
    public int LayerCount { get => layerCount; }

    public PropInit(PropCategory category, Lingo.List init)
    {
        object? tempObject; // used with TryGetValue on init list

        Category = category;
        Name = (string) init.fields["nm"];
        Type = (string) init.fields["tp"] switch
        {
            "standard" => PropType.Standard,
            "variedStandard" => PropType.VariedStandard,
            "soft" => PropType.Soft,
            "coloredSoft" => PropType.ColoredSoft,
            "variedSoft" => PropType.VariedSoft,
            "simpleDecal" => PropType.SimpleDecal,
            "variedDecal" => PropType.VariedDecal,
            "antimatter" => PropType.Antimatter,
            "rope" => PropType.Rope,
            "long" => PropType.Long,
            _ => throw new Exception("Invalid prop init")
        };

        // find prop path
        // for some reason, previews for drought props are in cast data instead of in the Props folder
        // kind of annoying. so i just put those images in assets/internal
        string texturePath = Path.Combine(RainEd.Instance.AssetDataPath, "Props", Name + ".png");
        if (!File.Exists(texturePath) && DrizzleCastMap.TryGetValue(Name, out string? castPath))
        {
            texturePath = Path.Combine(Boot.AppDataPath, "assets", "internal", castPath!);
        }
        Texture = RlManaged.Texture2D.Load(texturePath);

        var randVar = false;

        if (!Raylib.IsTextureReady(Texture))
        {
            RainEd.Logger.Warning($"Image {texturePath} is invalid or missing!");
            Texture.Dispose();
            Texture = null;
        }

        if (Texture is not null)
        {
            pixelWidth = Texture.Width;
            pixelHeight = Texture.Height;
        }
        else
        {
            pixelWidth = 20;
            pixelHeight = 20;
        }

        // initialize rope-type prop
        if (Type == PropType.Rope)
        {
            Rope = new RopeInit(init);
            Depth = Lingo.LingoNumber.AsInt(init.fields["depth"]);
            VariationCount = 1;
            layerCount = 1;
        }

        // initialize long-type prop
        else if (Type == PropType.Long)
        {
            Depth = Lingo.LingoNumber.AsInt(init.fields["depth"]);
            VariationCount = 1;
            layerCount = 1;
        }

        // initialize other types of props
        else
        {
            // obtain size of image cel
            if (init.fields.TryGetValue("pxlSize", out tempObject))
            {
                var pxlSize = (Vector2) tempObject;
                pixelWidth = (int) pxlSize.X;
                pixelHeight = (int) pxlSize.Y;
            }
            else if (init.fields.TryGetValue("sz", out tempObject))
            {
                var sz = (Vector2) tempObject;
                pixelWidth = (int)sz.X * 20;
                pixelHeight = (int)sz.Y * 20;
            }

            // get image layer count and depth
            Depth = 0;
            layerCount = 1;
            
            if (init.fields.TryGetValue("repeatL", out tempObject))
            {
                var list = ((Lingo.List)tempObject).values;
                layerCount = list.Count;
                foreach (int n in list.Cast<int>())
                {
                    Depth += n;
                }
            }
            else if (init.fields.TryGetValue("depth", out tempObject))
            {
                Depth = Lingo.LingoNumber.AsInt(tempObject);
            }

            // variation count
            VariationCount = 1;

            if (init.fields.TryGetValue("vars", out tempObject))
            {
                VariationCount = Lingo.LingoNumber.AsInt(tempObject);
            }

            if (init.fields.TryGetValue("random", out tempObject))
            {
                randVar = Lingo.LingoNumber.AsInt(tempObject) != 0;   
            }
        }

        // read notes
        var tags = ((Lingo.List)init.fields["tags"]).values.Cast<string>();

        PropFlags = 0;
        if (init.fields.TryGetValue("notes", out tempObject))
        {
            var notes = ((Lingo.List)tempObject).values;
            Notes = notes.Cast<string>().ToArray();
        }
        else
        {
            Notes = Array.Empty<string>();
        }

        // post effects recommended when colorized note
        if (Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.fields.TryGetValue("colorize", out tempObject) && Lingo.LingoNumber.AsInt(tempObject) != 0)
            {
                PropFlags |= PropFlags.Colorize;
            }
        }
        // set flags
        if (init.fields.TryGetValue("colorTreatment", out tempObject) && (string)tempObject == "bevel")
        {
            PropFlags |= PropFlags.ProcedurallyShaded;
        }

        // is procedurally shaded?
        if (Type == PropType.Soft || Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.fields.TryGetValue("selfShade", out tempObject) && Lingo.LingoNumber.AsInt(tempObject) != 0)
            {
                PropFlags |= PropFlags.ProcedurallyShaded;
            }
        }
        else if (
            Type == PropType.Rope || Type == PropType.Long ||
            Type == PropType.SimpleDecal || Type == PropType.VariedDecal
            || Type == PropType.Antimatter
        )
        {
            PropFlags |= PropFlags.ProcedurallyShaded;
        }

        // is custom color available?
        if (tags.Contains("customColor") || tags.Contains("customColorRainBow"))
        {
            PropFlags |= PropFlags.CustomColorAvailable;
        }

        // random variation
        if (randVar)
        {
            PropFlags |= PropFlags.RandomVariation;
        }

        // the following two are tags defined by me,
        // written in the rope-type prop init data (written by me)
        if (tags.Contains("wire"))
        {
            PropFlags |= PropFlags.CanSetThickness;
        }

        if (tags.Contains("colorize"))
        {
            PropFlags |= PropFlags.Colorize;
        }

        // is custom depth available?
        switch (Type)
        {
            case PropType.VariedDecal:
            case PropType.VariedSoft:
            case PropType.SimpleDecal:
            case PropType.Soft:
            // case PropType.SoftEffect: -- unused prop type?
            case PropType.Antimatter:
            case PropType.ColoredSoft:
                PropFlags |= PropFlags.CustomDepthAvailable;
                break;
        }
    }

    public PropInit(PropCategory category, Tile srcTile)
    {
        if (!srcTile.CanBeProp)
        {
            throw new Exception("Attempt to create a prop from a tile marked notProp");
        }

        Category = category;
        Name = srcTile.Name;
        Type = srcTile.VariationCount > 1 ? PropType.VariedStandard : PropType.Standard;
        Texture = RlManaged.Texture2D.Load(srcTile.GraphicsPath);
        PropFlags = PropFlags.Tile;
        Notes = [];

        if (!Raylib.IsTextureReady(Texture))
        {
            RainEd.Logger.Warning($"Image {srcTile.GraphicsPath} is invalid or missing!");
            Texture.Dispose();
            Texture = null;
        }

        layerCount = srcTile.LayerCount;
        Depth = srcTile.LayerDepth;
        pixelWidth = (srcTile.Width + srcTile.BfTiles * 2) * 20;
        pixelHeight = (srcTile.Height + srcTile.BfTiles * 2) * 20;
        VariationCount = srcTile.VariationCount;

        if (VariationCount > 1)
            PropFlags |= PropFlags.RandomVariation;
    }

    public Rectangle GetPreviewRectangle(int variation, int layer)
    {
        // standard types, like tiles, have an entire row of pixels at the top dedicated to
        // a single black pixel.
        int oy = 0;

        if (Type == PropType.Standard || Type == PropType.VariedStandard)
        {
            oy = 1;
        }
        
        return new Rectangle(
            pixelWidth * variation,
            pixelHeight * layer + oy,
            pixelWidth, pixelHeight
        );
    }
}

record PropCategory
{
    public string Name;
    public bool IsTileCategory = false;
    public int Index;
    public Color Color;
    public List<PropInit> Props = new();

    public PropCategory()
    {
        Name = "";
        Index = -1;
    }

    public PropCategory(int index, string name, Lingo.Color color)
    {
        Name = name;
        Index = index;
        Color = new Color(color.R, color.G, color.B, 255);
    }
}

record PropTileCategory
{
    public readonly string Name;
    public readonly Color Color;
    public readonly List<PropInit> Props = [];

    public PropTileCategory(string name, Color color)
    {
        Name = name;
        Color = color;
    }
}

record RopeInit
{
    public readonly RopePhysicalProperties PhysicalProperties;
    public readonly int CollisionDepth;
    public readonly Color PreviewColor;
    public readonly int PreviewInterval;

    public RopeInit(Lingo.List init)
    {
        var previewColor = (Lingo.Color)init.fields["previewColor"];

        CollisionDepth = Lingo.LingoNumber.AsInt(init.fields["collisionDepth"]);
        PreviewInterval = Lingo.LingoNumber.AsInt(init.fields["previewEvery"]);
        PreviewColor = new Color(previewColor.R, previewColor.G, previewColor.B, 255);
        PhysicalProperties = new RopePhysicalProperties()
        {
            segmentLength = Lingo.LingoNumber.AsFloat(init.fields["segmentLength"]),
            grav = Lingo.LingoNumber.AsFloat(init.fields["grav"]),
            stiff = Lingo.LingoNumber.AsInt(init.fields["stiff"]) == 1,
            friction = Lingo.LingoNumber.AsFloat(init.fields["friction"]),
            airFric = Lingo.LingoNumber.AsFloat(init.fields["airFric"]),
            segRad = Lingo.LingoNumber.AsFloat(init.fields["segRad"]),
            rigid = Lingo.LingoNumber.AsFloat(init.fields["rigid"]),
            edgeDirection = Lingo.LingoNumber.AsFloat(init.fields["edgeDirection"]),
            selfPush = Lingo.LingoNumber.AsFloat(init.fields["selfPush"]),
            sourcePush = Lingo.LingoNumber.AsFloat(init.fields["sourcePush"])
        };
    }
}

class PropDatabase
{
    // taken from startUp.lingo, and re-formatted
    private const string ExtraPropsInit = """
    -["Rope type props", color(0, 255, 0)]
    [#nm:"Wire", #tp:"rope", #depth:0, #tags:["wire"], #notes:[], #segmentLength:3, #collisionDepth:0, #segRad:1, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:0, #previewColor:color(255,0, 0), #previewEvery:4, #edgeDirection:0, #rigid:0, #selfPush:0, #sourcePush:0]
    [#nm:"Tube", #tp:"rope", #depth:4, #tags:[], #notes:[], #segmentLength:10, #collisionDepth:2, #segRad:4.5, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:1, #previewColor:color(0,0, 255), #previewEvery:2, #edgeDirection:5, #rigid:1.6, #selfPush:0, #sourcePush:0]
    [#nm:"ThickWire", #tp:"rope", #depth:3, #tags:[], #notes:[], #segmentLength:4, #collisionDepth:1, #segRad:2, #grav:0.5, #friction:0.8, #airFric:0.9, #stiff:1, #previewColor:color(255,255, 0), #previewEvery:2, #edgeDirection:0, #rigid:0.2, #selfPush:0, #sourcePush:0]
    [#nm:"RidgedTube", #tp:"rope", #depth:4, #tags:[], #notes:[], #segmentLength:5, #collisionDepth:2, #segRad:5, #grav:0.5, #friction:0.3, #airFric:0.7, #stiff:1, #previewColor:color(255,0,255), #previewEvery:2, #edgeDirection:0, #rigid:0.1, #selfPush:0, #sourcePush:0]
    [#nm:"Fuel Hose", #tp:"rope", #depth:5, #tags:[], #notes:[], #segmentLength:16, #collisionDepth:1, #segRad:7, #grav:0.5, #friction:0.8, #airFric:0.9, #stiff:1, #previewColor:color(255,150,0), #previewEvery:1, #edgeDirection:1.4, #rigid:0.2, #selfPush:0, #sourcePush:0]
    [#nm:"Broken Fuel Hose", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:16, #collisionDepth:1, #segRad:7, #grav:0.5, #friction:0.8, #airFric:0.9, #stiff:1, #previewColor:color(255,150,0), #previewEvery:1, #edgeDirection:1.4, #rigid:0.2, #selfPush:0, #sourcePush:0]
    [#nm:"Large Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:28, #collisionDepth:3, #segRad:9.5, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(0,255,0), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    [#nm:"Large Chain 2", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:28, #collisionDepth:3, #segRad:9.5, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(20,205,0), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    [#nm:"Bike Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:38, #collisionDepth:3, #segRad:16.5, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(100,100,100), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:16.5, #sourcePush:0]
    [#nm:"Zero-G Tube", #tp:"rope", #depth:4, #tags:["colorize"], #notes:[], #segmentLength:10, #collisionDepth:2, #segRad:4.5, #grav:0, #friction:0.5, #airFric:0.9, #stiff:1, #previewColor:color(0,255, 0), #previewEvery:2, #edgeDirection:0, #rigid:0.6, #selfPush:2, #sourcePush:0.5]
    [#nm:"Zero-G Wire", #tp:"rope", #depth:0, #tags:["wire"], #notes:[], #segmentLength:8, #collisionDepth:0, #segRad:1, #grav:0, #friction:0.5, #airFric:0.9, #stiff:1, #previewColor:color(255,0, 0), #previewEvery:2, #edgeDirection:0.3, #rigid:0.5, #selfPush:1.2, #sourcePush:0.5]
    [#nm:"Fat Hose", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:40, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(0,100,150), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Wire Bunch", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:50, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(255,100,150), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Wire Bunch 2", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:50, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(255,100,150), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    
    -["Drought Rope Props", color(0, 255, 0)]
    [#nm:"Big Big Pipe", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:40, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(50,150,210), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Ring Chain", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:40, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(100,200,0), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Christmas Wire", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:17, #collisionDepth:0, #segRad:8.5, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:0, #previewColor:color(200,0, 200), #previewEvery:1, #edgeDirection:0, #rigid:0, #selfPush:0, #sourcePush:0]
    [#nm:"Ornate Wire", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:17, #collisionDepth:0, #segRad:8.5, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:0, #previewColor:color(0,200, 200), #previewEvery:1, #edgeDirection:0, #rigid:0, #selfPush:0, #sourcePush:0]
    
    -["Long props", color(0, 255, 0)]
    [#nm:"Cabinet Clamp", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Drill Suspender", #tp:"long", #depth:5, #tags:[], #notes:[]]
    [#nm:"Thick Chain", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Drill", #tp:"long", #depth:10, #tags:[], #notes:[]]
    [#nm:"Piston", #tp:"long", #depth:4, #tags:[], #notes:[]]

    -["Drought Long Props", color(0, 255, 0)]
    [#nm:"Stretched Pipe", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Twisted Thread", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Stretched Wire", #tp:"long", #depth:0, #tags:[], #notes:[]]
    """;

    public readonly List<PropCategory> Categories;
    public readonly List<PropTileCategory> TileCategories;
    public readonly List<PropColor> PropColors; // custom colors
    private readonly Dictionary<string, PropInit> allProps;

    private int catIndex = 0;

    public PropDatabase(TileDatabase tileDatabase)
    {
        Categories = new List<PropCategory>();
        TileCategories = new List<PropTileCategory>();
        PropColors = new List<PropColor>();
        allProps = new Dictionary<string, PropInit>();

        InitProps(tileDatabase);
        InitExtraProps();
        InitCustomColors();
    }

    public bool TryGetPropFromName(string name, out PropInit? value)
    {
        return allProps.TryGetValue(name, out value);
    }
    
    private void AddPropToIndex(PropInit prop)
    {
        if (allProps.ContainsKey(prop.Name))
        {
            RainEd.Logger.Warning("Already added prop {PropName}", prop.Name);
        }

        allProps[prop.Name] = prop;
    }

    public int GetPropColorIndex(string name)
    {
        for (int i = 0; i < PropColors.Count; i++)
        {
            if (PropColors[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    // helper function to create error message with line inforamtion
    private static string ErrorString(int lineNo, string msg)
            => "Line " + (lineNo == -1 ? "[UNKNOWN]" : lineNo) + ": " + msg; 

    private void InitProps(TileDatabase tileDatabase)
    {
        // read prop init file
        var initFilePath = Path.Combine(RainEd.Instance.AssetDataPath, "Props", "Init.txt");
        var lingoParser = new Lingo.LingoParser();
        int lineNo = 0;

        PropCategory? currentCategory = null;
        foreach (var line in File.ReadLines(initFilePath))
        {
            lineNo++;

            if (string.IsNullOrWhiteSpace(line)) continue;

            // read header
            if (line[0] == '-')
            {
                if (lingoParser.Read(line[1..]) is not Lingo.List header)
                {
                    RainEd.Logger.Warning(ErrorString(lineNo, "Malformed category header, ignoring."));
                    continue;
                }

                currentCategory = new PropCategory(catIndex++, (string) header.values[0], (Lingo.Color) header.values[1]);
                Categories.Add(currentCategory);
                RainEd.Logger.Information("Register prop category {PropCategory}", currentCategory.Name);
            }

            // read prop
            else
            {
                if (currentCategory is null) throw new Exception(ErrorString(lineNo, "The first category header is missing"));
                
                Lingo.List? propData = null;
                try // curse you Wryak
                {
                    propData = (Lingo.List) (lingoParser.Read(line) ?? throw new Exception(ErrorString(lineNo, "Malformed prop init")));
                    var propInit = new PropInit(currentCategory, propData);
                    currentCategory.Props.Add(propInit);
                    AddPropToIndex(propInit);
                }
                catch (Exception e)
                {
                    var name = propData is null ? "Unknown Prop" : (string) propData.fields["nm"];
                    RainEd.Logger.Warning(ErrorString(lineNo, "Could not add prop '{PropName}': {ErrorMessage}"), name, e.Message);
                }
            }
        }

        // read tile database to create Tiles as Prop
        // the "Tiles as props" categories are not touched by the tile editor;
        // it only touches the TileCategories field
        int tilePropCatIndex = 2;

        currentCategory = new PropCategory(catIndex++, "Tiles as props 1", new Lingo.Color(255, 0, 0))
        {
            IsTileCategory = true
        };
        Categories.Add(currentCategory);
        RainEd.Logger.Information("Register prop category Tiles as props 1");

        int tileIndex = 0;
        foreach (var category in tileDatabase.Categories)
        {
            var tilePropCategory = new PropTileCategory(category.Name, category.Color);

            foreach (var tile in category.Tiles)
            {
                if (!tile.CanBeProp) continue;

                var propInit = new PropInit(currentCategory, tile);
                currentCategory.Props.Add(propInit);
                tilePropCategory.Props.Add(propInit);
                AddPropToIndex(propInit);
                tileIndex++;

                // 21 tiles per page
                if (tileIndex >= 21)
                {
                    tileIndex = 0;
                    currentCategory = new PropCategory(catIndex++, "Tiles as props " + tilePropCatIndex++, new Lingo.Color(255, 0, 0))
                    {
                        IsTileCategory = true
                    };
                    Categories.Add(currentCategory);
                    RainEd.Logger.Information("Register prop category {CategoryName}", currentCategory.Name);
                }
            }

            if (tilePropCategory.Props.Count > 0)
            {
                RainEd.Logger.Information("Register tile prop category {CategoryName}", tilePropCategory.Name);
                TileCategories.Add(tilePropCategory);
            }
        }
    }

    private void InitExtraProps()
    {
        RainEd.Logger.Information("Initialize rope-type props...");

        using StringReader reader = new(ExtraPropsInit);
        var lingoParser = new Lingo.LingoParser();

        PropCategory? curGroup = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line[0] == '-')
            {
                var headerData = (Lingo.List)lingoParser.Read(line[1..])!;
                curGroup = new PropCategory(catIndex++, (string)headerData.values[0], (Lingo.Color)headerData.values[1]);
                Categories.Add(curGroup);
            }
            else
            {
                var ropeData = (Lingo.List)lingoParser.Read(line)!;
                var propInit = new PropInit(curGroup!, ropeData);
                curGroup!.Props.Add(propInit);
                AddPropToIndex(propInit);
            }
        }

        RainEd.Logger.Information("Done initializing rope and long props");
    }

    private void InitCustomColors()
    {
        // read propColors.txt
        var initFilePath = Path.Combine(RainEd.Instance.AssetDataPath, "Props", "propColors.txt");
        var lingoParser = new Lingo.LingoParser();

        foreach (var line in File.ReadLines(initFilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var colData = (Lingo.List) (lingoParser.Read(line) ?? throw new Exception("Malformed propColors.txt"));

            var name = (string) colData.values[0];
            var lingoCol = (Lingo.Color) colData.values[1];

            PropColors.Add(new PropColor()
            {
                Name = name,
                Color = new Color(lingoCol.R, lingoCol.G, lingoCol.B, 255)
            });
        }
    }
}