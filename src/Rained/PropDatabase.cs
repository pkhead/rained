using Drizzle.Lingo.Runtime;
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
    Antimatter
};

// Used to generate note synopses
[Flags]
enum PropFlags
{
    ProcedurallyShaded = 1,
    RandomVariations = 2,
    HasVariations = 4,
    PostEffectsWhenColorized = 8,
    CustomColorAvailable = 16,
    CanSetThickness = 32,
    CanColorTube = 64,
    Tile = 128,
}

class PropInit
{
    public readonly string Name;
    public readonly PropCategory Category;
    private readonly PropType Type;
    public readonly RlManaged.Texture2D Texture;
    public readonly PropFlags PropFlags;
    public readonly int Depth;
    public readonly int VariationCount;
    public readonly bool HasRandomVariation;
    public readonly string[] Notes;

    // used for obtaining preview image
    private readonly int pixelWidth;
    private readonly int pixelHeight;
    private readonly int layerCount;

    public float Width { get => pixelWidth / 20f; }
    public float Height { get => pixelHeight / 20f; }
    public int LayerCount { get => layerCount; }

    public PropInit(PropCategory category, Lingo.List init)
    {
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
            _ => throw new Exception("Invalid prop init file")
        };
        Texture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "Data", "Props", Name + ".png"));

        // obtain size of image cel
        if (init.fields.TryGetValue("pxlSize", out object? tempObject))
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
        else
        {
            pixelWidth = Texture.Width;
            pixelHeight = Texture.Height;
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
            Depth = (int)tempObject;
        }

        // variation count
        VariationCount = 1;
        HasRandomVariation = false;

        if (init.fields.TryGetValue("vars", out tempObject))
        {
            VariationCount = (int)tempObject;
        }

        if (init.fields.TryGetValue("random", out tempObject))
        {
            HasRandomVariation = (int)tempObject != 0;   
        }

        // read notes and flags
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

        if (init.fields.TryGetValue("colorTreatment", out tempObject) && (string)tempObject == "bevel")
        {
            PropFlags |= PropFlags.ProcedurallyShaded;
        }

        if (Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.fields.TryGetValue("colorize", out tempObject) && (int)tempObject != 0)
            {
                PropFlags |= PropFlags.PostEffectsWhenColorized;
            }
        }

        if (Type == PropType.Soft || Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.fields.TryGetValue("selfShade", out tempObject) && (int)tempObject != 0)
            {
                PropFlags |= PropFlags.ProcedurallyShaded;
            }
        }

        if (tags.Contains("customColor") || tags.Contains("customColorRainBow"))
        {
            PropFlags |= PropFlags.CustomColorAvailable;
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
        Texture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "Data", "Graphics", Name + ".png"));
        PropFlags = PropFlags.Tile;
        Notes = Array.Empty<string>();

        layerCount = srcTile.LayerCount;
        Depth = srcTile.LayerDepth;
        pixelWidth = (srcTile.Width + srcTile.BfTiles * 2) * 20;
        pixelHeight = (srcTile.Height + srcTile.BfTiles * 2) * 20;
        VariationCount = srcTile.VariationCount;

        if (VariationCount > 1)
            HasRandomVariation = true;
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

class PropCategory
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

class PropTileCategory
{
    public string Name;
    public List<PropInit> Props = new();

    public PropTileCategory(string name)
    {
        Name = name;
    }
}

class PropDatabase
{
    public readonly List<PropCategory> Categories;
    public readonly List<PropTileCategory> TileCategories;

    public PropDatabase(TileDatabase tileDatabase)
    {
        Categories = new List<PropCategory>();
        TileCategories = new List<PropTileCategory>();

        // read prop init file
        var initFilePath = Path.Combine(Boot.AppDataPath, "Data", "Props", "Init.txt");
        var lingoParser = new Lingo.LingoParser();

        PropCategory? currentCategory = null;
        int catIndex = 0;
        foreach (var line in File.ReadLines(initFilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // read header
            if (line[0] == '-')
            {
                var header = (Lingo.List) (lingoParser.Read(line[1..]) ?? throw new Exception("Invalid header"));
                currentCategory = new PropCategory(catIndex++, (string) header.values[0], (Lingo.Color) header.values[1]);
                Categories.Add(currentCategory);
                RainEd.Logger.Information("Register prop category {PropCategory}", currentCategory.Name);
            }

            // read prop
            else
            {
                if (currentCategory is null) throw new Exception("Invalid prop init file");
                
                Lingo.List? propData = null;
                try // curse you Wryak
                {
                    propData = (Lingo.List) (lingoParser.Read(line) ?? throw new Exception("Malformed tile init"));
                    var propInit = new PropInit(currentCategory, propData);
                    currentCategory.Props.Add(propInit);
                }
                catch (Exception e)
                {
                    var name = propData is null ? "Unknown Prop" : (string) propData.fields["nm"];
                    RainEd.Logger.Warning("Could not add prop '{PropName}': {ErrorMessage}", name, e.Message);
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
            var tilePropCategory = new PropTileCategory(category.Name);

            foreach (var tile in category.Tiles)
            {
                if (!tile.CanBeProp) continue;

                var propInit = new PropInit(currentCategory, tile);
                currentCategory.Props.Add(propInit);
                tilePropCategory.Props.Add(propInit);
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
}