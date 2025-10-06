using Rained.Assets;
using Raylib_cs;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
namespace Rained.Assets;

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
    RandomFlipX = 128,
    RandomFlipY = 256,
    RandomRotation = 512
}

/// <summary>
/// How a color should be interpreted in a prop.
/// </summary>
enum PropColorTreatment : byte
{
    Unspecified = 0,

    /// <summary>
    /// The color should be interpreted as the pixel being lit, shaded, or normal.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// The color in the prop texture will either be solid or transparent, and the renderer will
    /// procedurally determine the pixel's shade using a bevel algorithm.
    /// </summary>
    Bevel = 2
}

/// <summary>
/// Information used for the coloring of soft props.
/// </summary>
readonly struct SoftPropRenderInfo(float contourExp, float highLightBorder, float shadowBorder)
{
    /// <summary>
    /// Exponent used to determine the sublayer of a pixel given its depth in the texture.
    /// </summary>
    public readonly float ContourExponent = contourExp;

    /// <summary>
    /// The minimum pixel shade value needed to be colored as a highlight.
    /// </summary>
    public readonly float HighlightBorder = highLightBorder;

    /// <summary>
    /// The maximum pixel shade value needed to be colored as a shadow. 
    /// </summary>
    public readonly float ShadowBorder = shadowBorder;
}

// data in propColors.txt, used for custom colors
struct PropColor
{
    public string Name;
    public Color Color;
}

record class PropInit
{
    public readonly string Name;
    public readonly PropCategory Category;
    public readonly PropType Type;
    public readonly PropFlags PropFlags;
    public readonly PropColorTreatment ColorTreatment;
    public readonly SoftPropRenderInfo? SoftPropRender;
    public readonly int Bevel;
    public readonly int Depth;
    public readonly int[] LayerDepths;
    public readonly int VariationCount;
    public readonly string[] Notes;
    public readonly RopeInit? Rope;

    // used for obtaining preview image
    private bool sizeKnown = false;
    private int pixelWidth = 0;
    private int pixelHeight = 0;
    private readonly int layerCount;

    public int PixelWidth
    {
        get
        {
            if (!sizeKnown) GetPropSize();
            return pixelWidth;
        }
    }

    public int PixelHeight
    {
        get
        {
            if (!sizeKnown) GetPropSize();
            return pixelHeight;
        }
    }

    public float Width { get => PixelWidth / 20f; }
    public float Height { get => PixelHeight / 20f; }
    public int LayerCount { get => layerCount; }

    public PropInit(PropCategory category, Lingo.PropertyList init)
    {
        object? tempObject; // used with TryGetValue on init list
        var randVar = false; // if the prop will be placed with a random variation

        Category = category;
        ColorTreatment = PropColorTreatment.Unspecified;
        Bevel = 0;
        SoftPropRender = null;
        LayerDepths = [0];
        Name = (string) init["nm"];
        Type = (string) init["tp"] switch
        {
            "standard" => PropType.Standard,
            "variedStandard" => PropType.VariedStandard,
            "soft" => PropType.Soft,
            "coloredSoft" => PropType.ColoredSoft,
            "variedSoft" => PropType.VariedSoft,
            "simpleDecal" => PropType.SimpleDecal,
            "variedDecal" => PropType.VariedDecal,
            "antimatter" => PropType.Antimatter,
            "rope" or "customRope" => PropType.Rope,
            "long" or "customLong" => PropType.Long,
            _ => throw new Exception($"Invalid prop type '{(string)init["tp"]}'")
        };

        // initialize rope-type prop
        if (Type == PropType.Rope)
        {
            Rope = new RopeInit(init);
            Depth = Lingo.LingoNumber.AsInt(init["depth"]);
            VariationCount = 1;
            layerCount = 1;
        }

        // initialize long-type prop
        else if (Type == PropType.Long)
        {
            Depth = Lingo.LingoNumber.AsInt(init["depth"]);
            VariationCount = 1;
            layerCount = 1;
        }

        // initialize other types of props
        else
        {
            // obtain size of image cel
            if (init.TryGetValue("pxlSize", out tempObject))
            {
                var pxlSize = (Vector2) tempObject;
                pixelWidth = (int) pxlSize.X;
                pixelHeight = (int) pxlSize.Y;
                sizeKnown = true;
            }
            else if (init.TryGetValue("sz", out tempObject))
            {
                var sz = (Vector2) tempObject;
                pixelWidth = (int)sz.X * 20;
                pixelHeight = (int)sz.Y * 20;
                sizeKnown = true;
            }

            // get image layer count and depth
            Depth = 0;
            layerCount = 1;
            
            if (init.TryGetValue("repeatL", out tempObject))
            {
                var list = ((Lingo.LinearList)tempObject);
                layerCount = list.Count;
                LayerDepths = new int[layerCount];
                int i = 0;
                foreach (int n in list.Cast<int>())
                {
                    LayerDepths[i++] = Depth;
                    Depth += n;
                }

            }
            else if (init.TryGetValue("depth", out tempObject))
            {
                Depth = Lingo.LingoNumber.AsInt(tempObject);
            }

            // variation count
            VariationCount = 1;

            if (init.TryGetValue("vars", out tempObject))
            {
                VariationCount = Lingo.LingoNumber.AsInt(tempObject);
            }

            if (init.TryGetValue("random", out tempObject))
            {
                randVar = Lingo.LingoNumber.AsInt(tempObject) != 0;   
            }
        }

        // read notes
        var tags = ((Lingo.LinearList)init["tags"]).Cast<string>();

        PropFlags = 0;
        if (init.TryGetValue("notes", out tempObject))
        {
            var notes = ((Lingo.LinearList)tempObject);
            Notes = notes.Cast<string>().ToArray();
        }
        else
        {
            Notes = Array.Empty<string>();
        }

        // post effects recommended when colorized note
        if (Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.TryGetValue("colorize", out tempObject) && Lingo.LingoNumber.AsInt(tempObject) != 0)
            {
                PropFlags |= PropFlags.Colorize;
            }
        }
        // set flags
        if (init.TryGetValue("colorTreatment", out tempObject))
        {
            var treatmentVal = (string)tempObject;

            if (treatmentVal == "bevel")
            {
                ColorTreatment = PropColorTreatment.Bevel;
                PropFlags |= PropFlags.ProcedurallyShaded;

                if (init.TryGetValue("bevel", out var bevelObj))
                {
                    Bevel = (int)bevelObj;
                }
            }
            else if (treatmentVal == "standard")
            {
                ColorTreatment = PropColorTreatment.Standard;
            }
        }

        // is procedurally shaded?
        if (Type == PropType.Soft || Type == PropType.VariedSoft || Type == PropType.ColoredSoft)
        {
            if (init.TryGetValue("selfShade", out tempObject) && Lingo.LingoNumber.AsInt(tempObject) != 0)
            {
                PropFlags |= PropFlags.ProcedurallyShaded;
            }

            if (init.TryGetValue("contourExp", out var contourExp) && init.TryGetValue("highLightBorder", out var hl) && init.TryGetValue("shadowBorder", out var sh))
            {
                SoftPropRender = new SoftPropRenderInfo(
                    Lingo.LingoNumber.AsFloat(contourExp),
                    Lingo.LingoNumber.AsFloat(hl),
                    Lingo.LingoNumber.AsFloat(sh)
                );
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

        // random parameters on placement
        if (randVar)
        {
            PropFlags |= PropFlags.RandomVariation;
        }

        if (tags.Contains("randomFlipX")) PropFlags |= PropFlags.RandomFlipX;
        if (tags.Contains("randomFlipY")) PropFlags |= PropFlags.RandomFlipY;
        if (tags.Contains("randomRotat")) PropFlags |= PropFlags.RandomRotation;
        
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
        PropFlags = PropFlags.Tile;
        ColorTreatment = PropColorTreatment.Standard;
        Notes = [];

        layerCount = srcTile.LayerCount;
        Depth = srcTile.LayerDepth;
        LayerDepths = srcTile.LayerDepths;
        pixelWidth = (srcTile.Width + srcTile.BfTiles * 2) * 20;
        pixelHeight = (srcTile.Height + srcTile.BfTiles * 2) * 20;
        sizeKnown = true;
        VariationCount = srcTile.VariationCount;

        if (VariationCount > 1)
            PropFlags |= PropFlags.RandomVariation;
    }

    private void GetPropSize()
    {
        if (sizeKnown) return;
        pixelWidth = 20;
        pixelHeight = 20;

        if (RainEd.Instance is not null)
        {
            var texture = RainEd.Instance.AssetGraphics.GetPropTexture(this);

            if (texture is not null)
            {
                pixelWidth = texture.Width;
                pixelHeight = texture.Height;
            }
        }
        else
        {
            // headless image loading (no gpu)
            string texturePath = AssetGraphicsProvider.GetFilePath(
                Path.Combine(AssetDataPath.GetPath(), "Props"),
                Name + ".png");

            if (!File.Exists(texturePath) && DrizzleCast.GetFileName(Name + ".png", out string? castPath))
            {
                texturePath = castPath!;
            }

            try
            {
                using var img = Glib.Image.FromFile(texturePath, Glib.PixelFormat.RGBA);
                pixelWidth = img.Width;
                pixelHeight = img.Height;
            }
            catch (Exception e)
            {
                Log.Error("Could not load image {ImagePath}: {Exception}", texturePath, e.ToString());
            }
        }

        sizeKnown = true;
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
            PixelWidth * variation,
            PixelHeight * layer + oy,
            PixelWidth, PixelHeight
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

    public RopeInit(Lingo.PropertyList init)
    {
        var previewColor = (Lingo.Color)init["previewColor"];

        CollisionDepth = Lingo.LingoNumber.AsInt(init["collisionDepth"]);
        PreviewInterval = Lingo.LingoNumber.AsInt(init["previewEvery"]);
        PreviewColor = new Color(previewColor.R, previewColor.G, previewColor.B, 255);
        PhysicalProperties = new RopePhysicalProperties()
        {
            segmentLength = Lingo.LingoNumber.AsFloat(init["segmentLength"]),
            grav = Lingo.LingoNumber.AsFloat(init["grav"]),
            stiff = Lingo.LingoNumber.AsInt(init["stiff"]) == 1,
            friction = Lingo.LingoNumber.AsFloat(init["friction"]),
            airFric = Lingo.LingoNumber.AsFloat(init["airFric"]),
            segRad = Lingo.LingoNumber.AsFloat(init["segRad"]),
            rigid = Lingo.LingoNumber.AsFloat(init["rigid"]),
            edgeDirection = Lingo.LingoNumber.AsFloat(init["edgeDirection"]),
            selfPush = Lingo.LingoNumber.AsFloat(init["selfPush"]),
            sourcePush = Lingo.LingoNumber.AsFloat(init["sourcePush"])
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
    
    -["LB Rope Props", color(0, 255, 0)]
    [#nm:"Big Big Pipe", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:40, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(50,150,210), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Ring Chain", #tp:"rope", #depth:6, #tags:[], #notes:[], #segmentLength:40, #collisionDepth:3, #segRad:20, #grav:0.9, #friction:0.6, #airFric:0.95, #stiff:1, #previewColor:color(100,200,0), #previewEvery:1, #edgeDirection:0.1, #rigid:0.2, #selfPush:10, #sourcePush:0.1]
    [#nm:"Christmas Wire", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:17, #collisionDepth:0, #segRad:8.5, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:0, #previewColor:color(200,0, 200), #previewEvery:1, #edgeDirection:0, #rigid:0, #selfPush:0, #sourcePush:0]
    [#nm:"Ornate Wire", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:17, #collisionDepth:0, #segRad:8.5, #grav:0.5, #friction:0.5, #airFric:0.9, #stiff:0, #previewColor:color(0,200, 200), #previewEvery:1, #edgeDirection:0, #rigid:0, #selfPush:0, #sourcePush:0]

    -["Alduris Rope Props", color(0, 255, 0)]
    [#nm:"Small Chain", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:22, #collisionDepth:0, #segRad:3, #grav:0.5, #friction:0.65, #airFric:0.95, #stiff:1, #previewColor:color(255,0,150), #previewEvery:2, #edgeDirection:0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    [#nm:"Fat Chain", #tp:"rope", #depth:0, #tags:[], #notes:[], #segmentLength:44, #collisionDepth:0, #segRad:8, #grav:0.5, #friction:0.65, #airFric:0.95, #stiff:1, #previewColor:color(255,0,150), #previewEvery:2, #edgeDirection:0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    
    -["Dakras Rope Props", color(0, 255, 0)]
    [#nm:"Big Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:56, #collisionDepth:3, #segRad:19, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(0,255,40), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    [#nm:"Chunky Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:28, #collisionDepth:3, #segRad:19, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(0,255,40), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:6.5, #sourcePush:0]
    [#nm:"Big Bike Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:76, #collisionDepth:3, #segRad:33, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(100,150,100), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:33, #sourcePush:0]
    [#nm:"Huge Bike Chain", #tp:"rope", #depth:9, #tags:[], #notes:[], #segmentLength:152, #collisionDepth:3, #segRad:66, #grav:0.9, #friction:0.8, #airFric:0.95, #stiff:1, #previewColor:color(100,200,100), #previewEvery:1, #edgeDirection:0.0, #rigid:0.0, #selfPush:66, #sourcePush:0]
    
    -["Long props", color(0, 255, 0)]
    [#nm:"Cabinet Clamp", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Drill Suspender", #tp:"long", #depth:5, #tags:[], #notes:[]]
    [#nm:"Thick Chain", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Drill", #tp:"long", #depth:10, #tags:[], #notes:[]]
    [#nm:"Piston", #tp:"long", #depth:4, #tags:[], #notes:[]]

    -["LB Long Props", color(0, 255, 0)]
    [#nm:"Stretched Pipe", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Twisted Thread", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Stretched Wire", #tp:"long", #depth:0, #tags:[], #notes:[]]
    [#nm:"Long Barbed Wire", #tp:"long", #depth:0, #tags:[], #notes:[]]

    -["April Longs", color(0, 255, 0)]
    [#nm:"Moss Drop", #tp:"long", #depth:3, #tags:[], #notes:["Keep in mind this long will droop in in front of anything solid, if you dont want something to collide with it, render it after this prop"]]
    [#nm:"Moss Drop A", #tp:"long", #depth:3, #tags:["effectColorA"], #notes:["Keep in mind this long will droop in in front of anything solid, if you dont want something to collide with it, render it after this prop"]]
    [#nm:"Moss Drop B", #tp:"long", #depth:3, #tags:["effectColorB"], #notes:["Keep in mind this long will droop in in front of anything solid, if you dont want something to collide with it, render it after this prop"]]
    [#nm:"Moss Hang", #tp:"long", #depth:3, #tags:[], #notes:["For best results you should place this on the back sublayers of whatever layer you're trying to place this on, and allow the moss to kinda 'lerch' forward. The moss starts placing in the middle of the long, and follows gravity."]]
    [#nm:"Moss Hang A", #tp:"long", #depth:3, #tags:["effectColorA"], #notes:["For best results you should place this on the back sublayers of whatever layer you're trying to place this on, and allow the moss to kinda 'lerch' forward. The moss starts placing in the middle of the long, and follows gravity."]]
    """;

    public readonly List<PropCategory> Categories;
    public readonly List<PropTileCategory> TileCategories;
    public readonly List<PropColor> PropColors; // custom colors
    private readonly Dictionary<string, PropInit> allProps;

    /// <summary>
    /// True if any errors were encountered while loading.
    /// </summary>
    public bool HasErrors { get; private set; } = false;

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

    public bool TryGetPropFromName(string name, [NotNullWhen(true)] out PropInit? value)
    {
        return allProps.TryGetValue(name, out value);
    }
    
    private void AddPropToIndex(int lineNo, PropInit prop)
    {
        /*if (allProps.ContainsKey(prop.Name))
        {
            if (lineNo == -2) // dumb ik
            {
                Log.UserLogger.Warning("Tile As Prop: Already added prop {PropName}", prop.Name);
            }
            else
            {
                Log.UserLogger.Warning(ErrorString(lineNo, "Already added prop " + prop.Name));
            }
        }*/
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
    static string ErrorString(int lineNo, string msg)
    {
        if (lineNo == -1)
            return "[EMBEDDED]: " + msg;
        else
            return "Line " + lineNo + ": " + msg;
    }


    private void InitProps(TileDatabase tileDatabase)
    {
        // read prop init file
        var initFilePath = Path.Combine(AssetDataPath.GetPath(), "Props", "Init.txt");
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
                if (lingoParser.Read(line[1..]) is not Lingo.LinearList header)
                {
                    Log.UserLogger.Warning(ErrorString(lineNo, "Malformed category header, ignoring."));
                    continue;
                }

                currentCategory = new PropCategory(catIndex++, (string) header[0], (Lingo.Color) header[1]);
                Categories.Add(currentCategory);
            }

            // read prop
            else
            {
                if (currentCategory is null) throw new Exception(ErrorString(lineNo, "The first category header is missing"));
                
                Lingo.PropertyList? propData = null;
                try // curse you Wryak
                {
                    var parsedLine = lingoParser.Read(line, out Lingo.ParseException? parseErr);
                    if (parseErr is not null)
                    {
                        HasErrors = true;
                        Log.UserLogger.Error(ErrorString(lineNo, parseErr.Message + " (line ignored)"));
                        continue;
                    }
                    
                    if (parsedLine is null)
                    {
                        HasErrors = true;
                        Log.UserLogger.Error(ErrorString(lineNo, "Malformed tile init (line ignored)"));
                        continue;
                    }

                    propData = (Lingo.PropertyList)parsedLine;
                    var propInit = new PropInit(currentCategory, propData);
                    currentCategory.Props.Add(propInit);
                    AddPropToIndex(lineNo, propInit);
                }
                catch (Exception e)
                {
                    HasErrors = true;

                    var nameObj = propData?["nm"];
                    var name = nameObj is string v ? v : "???";
                    Log.UserLogger.Warning(ErrorString(lineNo, "Could not add prop '{PropName}': {ErrorMessage}"), name, e.Message);
                }
            }
        }

        Log.UserLogger.Information("Reading tiles as props...");
        
        // read tile database to create Tiles as Prop
        // the "Tiles as props" categories are not touched by the tile editor;
        // it only touches the TileCategories field
        int tilePropCatIndex = 2;

        currentCategory = new PropCategory(catIndex++, "Tiles as props 1", new Lingo.Color(255, 0, 0))
        {
            IsTileCategory = true
        };
        Categories.Add(currentCategory);

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
                AddPropToIndex(-2, propInit);
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
                }
            }

            if (tilePropCategory.Props.Count > 0)
            {
                TileCategories.Add(tilePropCategory);
            }
        }

        // purge empty categories
        for (int i = Categories.Count - 1; i >= 0; i--)
        {
            if (Categories[i].Props.Count == 0)
            {
                Log.UserLogger.Warning("{Category} was empty", Categories[i].Name);
                Categories.RemoveAt(i);
            }
        }

        // purge empty tile categories
        for (int i = TileCategories.Count - 1; i >= 0; i--)
        {
            if (TileCategories[i].Props.Count == 0)
            {
                Log.UserLogger.Warning("{Category} was empty", TileCategories[i].Name);
                TileCategories.RemoveAt(i);
            }
        }
    }

    private void InitExtraProps()
    {
        Log.Information("Initialize rope-type props...");

        using StringReader reader = new(ExtraPropsInit);
        var lingoParser = new Lingo.LingoParser();

        PropCategory? curGroup = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line[0] == '-')
            {
                var headerData = (Lingo.LinearList)lingoParser.Read(line[1..])!;
                curGroup = new PropCategory(catIndex++, (string)headerData[0], (Lingo.Color)headerData[1]);
                Categories.Add(curGroup);
            }
            else
            {
                var ropeData = (Lingo.PropertyList)lingoParser.Read(line)!;
                var propInit = new PropInit(curGroup!, ropeData);
                curGroup!.Props.Add(propInit);
                AddPropToIndex(-1, propInit);
            }
        }

        Log.Information("Done initializing rope and long props");
    }

    private void InitCustomColors()
    {
        // read propColors.txt
        var initFilePath = Path.Combine(AssetDataPath.GetPath(), "Props", "propColors.txt");
        var lingoParser = new Lingo.LingoParser();

        foreach (var line in File.ReadLines(initFilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var colData = (Lingo.LinearList) (lingoParser.Read(line) ?? throw new Exception("Malformed propColors.txt"));

            var name = (string) colData[0];
            var lingoCol = (Lingo.Color) colData[1];

            PropColors.Add(new PropColor()
            {
                Name = name,
                Color = new Color(lingoCol.R, lingoCol.G, lingoCol.B, 255)
            });
        }
    }
}