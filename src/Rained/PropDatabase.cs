using RainEd.Tiles;
using Raylib_cs;
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
    CanColorTube = 64
}

class PropInit
{
    public readonly string Name;
    public readonly PropCategory Category;
    private readonly PropType Type;
    public readonly RlManaged.Texture2D Texture;
    public readonly PropFlags PropFlags;
    public readonly string[] Notes;

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

        PropFlags = 0;
        Notes = Array.Empty<string>();
    }

    public PropInit(PropCategory category, Tile srcTile)
    {
        if (!srcTile.CanBeProp)
        {
            throw new Exception("Attempt to create a prop from a tile marked notProp");
        }

        Category = category;
        Name = srcTile.Name;
        Type = PropType.Standard; // this doesn't actually matter
        Texture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "Data", "Graphics", Name + ".png"));
        PropFlags = 0;
        Notes = Array.Empty<string>();
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