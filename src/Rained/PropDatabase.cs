using System.ComponentModel;
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
    public readonly PropType Type;
    public readonly RlManaged.Texture2D Texture;
    public readonly PropFlags PropFlags;
    public readonly string[] Notes;

    public PropInit(Lingo.List init)
    {
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
}

class PropCategory
{
    public string Name;
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

class PropDatabase
{
    public readonly List<PropCategory> Categories;

    public PropDatabase()
    {
        Categories = new List<PropCategory>();

        // read init file
        var initFilePath = Path.Combine(Boot.AppDataPath, "Data", "Props", "Init.txt");
        var lingoParser = new Lingo.LingoParser();

        PropCategory? currentCategory = null;
        int index = 0;
        foreach (var line in File.ReadLines(initFilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // read header
            if (line[0] == '-')
            {
                var header = (Lingo.List) (lingoParser.Read(line[1..]) ?? throw new Exception("Invalid header"));
                currentCategory = new PropCategory(++index, (string) header.values[0], (Lingo.Color) header.values[1]);
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
                    var propInit = new PropInit(propData);
                    currentCategory.Props.Add(propInit);
                }
                catch (Exception e)
                {
                    var name = propData is null ? "Unknown Prop" : (string) propData.fields["nm"];
                    RainEd.Logger.Warning("Could not add prop '{PropName}': {ErrorMessage}", name, e.Message);
                }
            }
        }
    }
}