using System.Numerics;
using Raylib_cs;

namespace RainEd.Tiles;

public enum TileType
{
    VoxelStruct,
    VoxelStructRockType,
    VoxelStructRandomDisplaceVertical,
    VoxelStructRandomDisplaceHorizontal,
    Box,
    VoxelStructSandType,
}

class Tile
{
    public readonly string Name;
    public readonly TileCategory Category;
    public readonly int Width;
    public readonly int Height;
    public readonly sbyte[,] Requirements;
    public readonly sbyte[,] Requirements2;
    public readonly bool HasSecondLayer;
    public readonly int BfTiles = 0;
    public readonly RlManaged.Texture2D PreviewTexture;
    public readonly bool CanBeProp;
    public readonly int LayerCount;
    public readonly int LayerDepth;
    public readonly int VariationCount;

    public readonly int CenterX;
    public readonly int CenterY;

    public readonly string GraphicsPath;

    public Tile(
        string name,
        TileCategory category,
        TileType type,
        int width, int height,
        int bfTiles,
        List<int>? repeatL,
        List<int> specs, List<int>? specs2,
        int rnd,
        bool noPropTag
    )
    {
        Name = name;
        Width = width;
        Height = height;
        BfTiles = bfTiles;
        Category = category;
        CanBeProp = false;
        LayerCount = 1;
        VariationCount = rnd;

        CenterX = (int)MathF.Ceiling((float)Width / 2) - 1;
        CenterY = (int)MathF.Ceiling((float)Height / 2) - 1;

        Requirements = new sbyte[width, height];
        Requirements2 = new sbyte[width, height];
        HasSecondLayer = false;
        LayerDepth = 10;

        // fill requirements table
        int i = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Requirements[x,y] = (sbyte) specs[i];
                Requirements2[x,y] = -1;
                i++;
            }
        }

        if (specs2 is not null)
        {
            LayerDepth = 20;
            HasSecondLayer = true;
            
            i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Requirements2[x, y] = (sbyte) specs2[i];
                    i++;
                }
            }
        }

        // retrieve Y location of preview image
        int rowCount = height + bfTiles * 2;
        int imageOffset = 1;
        switch (type)
        {
            case TileType.VoxelStruct:
            case TileType.VoxelStructRandomDisplaceHorizontal:
            case TileType.VoxelStructRandomDisplaceVertical:
                LayerCount = repeatL!.Count;
                rowCount *= LayerCount;
                CanBeProp = true;

                break;
            
            case TileType.VoxelStructRockType:
            case TileType.VoxelStructSandType:
                break;
            
            case TileType.Box:
                rowCount = height * width + height + bfTiles * 2;
                imageOffset = 0;
                break;
        }
        
        // find path to image
        // if it doesn't exist in Data/Graphics, check in assets/internal
        GraphicsPath = Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", name + ".png");
        if (!File.Exists(GraphicsPath) && DrizzleCastMap.TryGetValue(name, out string? castPath))
        {
            GraphicsPath = Path.Combine(Boot.AppDataPath, "assets", "internal", castPath!);
        }

        using var fullImage = RlManaged.Image.Load(GraphicsPath);
        var previewRect = new Rectangle(
            0,
            rowCount * 20 + imageOffset,
            width * 16,
            height * 16
        );

        if (previewRect.X < 0 || previewRect.Y < 0 ||
            previewRect.X >= fullImage.Width || previewRect.Y >= fullImage.Height ||
            previewRect.X + previewRect.Width > fullImage.Width ||
            previewRect.Y + previewRect.Height > fullImage.Height
        )
        {
            RainEd.Logger.Warning($"Tile '{name}' preview image is out of bounds");
        }

        using var previewImage = RlManaged.Image.GenColor(width * 16, height * 16, Color.White);
        previewImage.Format(PixelFormat.UncompressedR8G8B8A8);

        Raylib.ImageDraw(
            ref previewImage.Ref(),
            fullImage,
            previewRect,
            new Rectangle(0, 0, previewRect.Width, previewRect.Height),
            Color.White
        );

        // convert black-and-white image to white-and-transparent, respectively
        for (int x = 0; x < previewImage.Width; x++)
        {
            for (int y = 0; y < previewImage.Height; y++)
            {
                if (Raylib.GetImageColor(previewImage, x, y).Equals(new Color(255, 255, 255, 255)))
                {
                    previewImage.DrawPixel(x, y, new Color(255, 25, 255, 0));
                }
                else
                {
                    previewImage.DrawPixel(x, y, new Color(255, 255, 255, 255));
                }
            }
        }

        PreviewTexture = RlManaged.Texture2D.LoadFromImage(previewImage);

        if (noPropTag)
            CanBeProp = false;
    }
}

class TileCategory
{
    public string Name;
    public int Index;
    public Color Color;
    public List<Tile> Tiles = new();

    public TileCategory(string name, Lingo.Color color)
    {
        Name = name;
        Color = new Color(color.R, color.G, color.B, 255);
    }
}

class TileDatabase
{
    public readonly List<TileCategory> Categories;
    private readonly Dictionary<string, Tile> stringToTile = new();

    public TileDatabase()
    {
        Categories = new();
        
        var lingoParser = new Lingo.LingoParser();

        TileCategory? curGroup = null;
        int groupIndex = 0;

        void ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            
            // read header
            if (line[0] == '-')
            {
                var header = (Lingo.List) (lingoParser.Read(line[1..]) ?? throw new Exception("Invalid header"));
                curGroup = new TileCategory((string) header.values[0], (Lingo.Color) header.values[1])
                {
                    Index = groupIndex
                };

                groupIndex++;
                Categories.Add(curGroup);
                
                RainEd.Logger.Information("Register tile category {GroupName}", curGroup.Name);
            }
            else
            {
                if (curGroup is null) throw new Exception("Invalid tile init file");

                var tileInit = (Lingo.List) (lingoParser.Read(line) ?? throw new Exception("Invalid tile init file"));

                object? tempValue = null;
                var name = (string) tileInit.fields["nm"];
                var tp = (string) tileInit.fields["tp"];
                var size = (Vector2) tileInit.fields["sz"];
                var specsData = (Lingo.List) tileInit.fields["specs"];
                var bfTiles = (int) tileInit.fields["bfTiles"];
                Lingo.List? specs2Data = null;
                Lingo.List? repeatLayerList =
                    tileInit.fields.TryGetValue("repeatL", out tempValue) ? (Lingo.List) tempValue : null;
                int rnd =
                    tileInit.fields.TryGetValue("rnd", out tempValue) ? (int)tempValue : 1;
                
                if (tileInit.fields.TryGetValue("specs2", out tempValue) && tempValue is Lingo.List specs2List)
                {
                    specs2Data = specs2List;
                }

                List<int>? repeatL = repeatLayerList?.values.Cast<int>().ToList();
                List<int> specs = specsData.values.Cast<int>().ToList();
                List<int>? specs2 = specs2Data?.values.Cast<int>().ToList();
                List<string> tags = ((Lingo.List)tileInit.fields["tags"]).values.Cast<string>().ToList();

                TileType tileType = tp switch
                {
                    "voxelStruct" => TileType.VoxelStruct,
                    "voxelStructRockType" => TileType.VoxelStructRockType,
                    "voxelStructSandType" => TileType.VoxelStructSandType,
                    "voxelStructRandomDisplaceHorizontal" => TileType.VoxelStructRandomDisplaceHorizontal,
                    "voxelStructRandomDisplaceVertical" => TileType.VoxelStructRandomDisplaceVertical,
                    "box" => TileType.Box,
                    _ => throw new Exception($"Invalid tile type '{tp}'")
                };

                try {
                    var tileData = new Tile(
                        name: name,
                        category: curGroup,
                        type: tileType,
                        width: (int)size.X, height: (int)size.Y,
                        bfTiles: bfTiles,
                        repeatL: repeatL,
                        specs: specs,
                        specs2: specs2,
                        rnd: rnd,
                        noPropTag: tags.Contains("notProp")
                    );

                    curGroup.Tiles.Add(tileData);
                    stringToTile.Add(name, tileData);
                } catch (Exception e)
                {
                    RainEd.Logger.Warning("Could not add tile '{Name}': {ErrorMessage}", name, e.Message);
                }
            }
        }

        // read Init.txt
        foreach (var line in File.ReadLines(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt")))
        {
            ProcessLine(line);
        }

        // read internal extra tiles
        using var reader = new StringReader(ExtraTiles);
        string? line2;
        while ((line2 = reader.ReadLine()) is not null)
        {
            ProcessLine(line2);
        }
    }

    public bool HasTile(string name) => stringToTile.ContainsKey(name);
    public Tile GetTileFromName(string name) => stringToTile[name];

    // From Cast/Drought_393439_Drought Needed Init.txt
    private const string ExtraTiles = """
    -["Drought 4Mosaic", color(227, 76, 13)]
    [#nm:"4Mosaic Square", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"4Mosaic Slope NE", #sz:point(1,1), #specs:[2], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"4Mosaic Slope NW", #sz:point(1,1), #specs:[3], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"4Mosaic Slope SW", #sz:point(1,1), #specs:[5], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"4Mosaic Slope SE", #sz:point(1,1), #specs:[4], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"4Mosaic Floor", #sz:point(1,1), #specs:[6], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,8], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]


    -["Drought Missing 3DBricks", color(255, 150, 0)]
    [#nm:"3DBrick Square", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"3DBrick Slope NE", #sz:point(1,1), #specs:[2], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"3DBrick Slope NW", #sz:point(1,1), #specs:[3], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"3DBrick Slope SW", #sz:point(1,1), #specs:[5], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"3DBrick Slope SE", #sz:point(1,1), #specs:[4], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"3DBrick Floor", #sz:point(1,1), #specs:[6], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,7], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]


    -["Drought Alt Grates", color(75, 75, 240)]
    [#nm:"AltGrateA", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateB1", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateB2", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateB3", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateB4", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateC1", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateC2", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateE1", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateE2", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateF1", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateF2", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateF3", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateF4", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateG1", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateG2", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateH", #sz:point(3,4), #specs:[0,0,0, 0,0,0, 0,0,0, 0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]

    [#nm:"AltGrateI", #sz:point(1,1), #specs:[0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateJ1", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateJ2", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateJ3", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateJ4", #sz:point(1,2), #specs:[0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateK1", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateK2", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateK3", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateK4", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateL", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateM", #sz:point(2,2), #specs:[0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateN", #sz:point(4,4), #specs:[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]
    [#nm:"AltGrateO", #sz:point(5,5), #specs:[0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,6,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["notTrashProp", "notProp", "INTERNAL"]]


    -["Drought Missing Stone", color(200, 165, 135)]
    [#nm:"Small Stone Slope NE", #sz:point(1,1), #specs:[2], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Stone Slope NW", #sz:point(1,1), #specs:[3], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Stone Slope SW", #sz:point(1,1), #specs:[5], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Stone Slope SE", #sz:point(1,1), #specs:[4], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Stone Floor", #sz:point(1,1), #specs:[6], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]

    [#nm:"Small Stone Marked", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "chaoticStone2 : very rare", "INTERNAL"]]
    [#nm:"Square Stone Marked", #sz:point(2,2), #specs:[1, 1, 1, 1], #specs2:0, #tp:"voxelStructRockType", #bfTiles:1, #rnd:3, #ptPos:0, #tags:["chaoticStone2 : rare", "INTERNAL"]]


    -["Drought Missing Machine", color(230, 160, 230)]
    [#nm:"Small Machine Slope NE", #sz:point(1,1), #specs:[2], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,1,1,1,1,1,1,1], #bfTiles:1, #rnd:1, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Machine Slope NW", #sz:point(1,1), #specs:[3], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,1,1,1,1,1,1,1], #bfTiles:1, #rnd:1, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Machine Slope SW", #sz:point(1,1), #specs:[5], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,1,1,1,1,1,1,1], #bfTiles:1, #rnd:1, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Machine Slope SE", #sz:point(1,1), #specs:[4], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,1,1,1,1,1,1,1], #bfTiles:1, #rnd:1, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    [#nm:"Small Machine Floor", #sz:point(1,1), #specs:[6], #specs2:0, #tp:"voxelStruct", #repeatL:[1,1,1,1,1,1,1,1,1,1], #bfTiles:1, #rnd:1, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]


    -["Drought Metal", color(100, 185, 245)]
    [#nm:"Small Metal Alt", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Small Metal Marked", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Small Metal X", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Metal Floor Alt", #sz:point(2,1), #specs:[1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Metal Wall", #sz:point(1,2), #specs:[1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Metal Wall Alt", #sz:point(1,2), #specs:[1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Square Metal Marked", #sz:point(2,2), #specs:[1, 1, 1, 1], #specs2:0, #tp:"box", #bfTiles:1, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Square Metal X", #sz:point(2,2), #specs:[1, 1, 1, 1], #specs2:0, #tp:"box", #bfTiles:1, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Wide Metal", #sz:point(3,2), #specs:[1,1, 1,1, 1,1], #specs2:0, #tp:"box", #bfTiles:1, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Tall Metal", #sz:point(2,3), #specs:[1,1,1, 1,1,1], #specs2:0, #tp:"box", #bfTiles:1, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Big Metal X", #sz:point(3,3), #specs:[1, 1, 1, 1, 1, 1, 1, 1, 1], #specs2:0, #tp:"box", #bfTiles:1, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]

    [#nm:"Large Big Metal", #sz:point(4,4), #specs:[1,1,1,1, 1,1,1,1, 1,1,1,1, 1,1,1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Large Big Metal Marked", #sz:point(4,4), #specs:[1,1,1,1, 1,1,1,1, 1,1,1,1, 1,1,1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]
    [#nm:"Large Big Metal X", #sz:point(4,4), #specs:[1,1,1,1, 1,1,1,1, 1,1,1,1, 1,1,1,1], #specs2:0, #tp:"box", #bfTiles:0, #rnd:1, #ptPos:0, #tags:["randomMetal", "INTERNAL"]]


    -["Drought Missing Metal", color(180, 10, 10)]
    [#nm:"Missing Metal Slope NE", #sz:point(1,1), #specs:[2], #specs2:0, #tp:"voxelStruct", #repeatL:[1,9], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"Missing Metal Slope NW", #sz:point(1,1), #specs:[3], #specs2:0, #tp:"voxelStruct", #repeatL:[1,9], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"Missing Metal Slope SW", #sz:point(1,1), #specs:[5], #specs2:0, #tp:"voxelStruct", #repeatL:[1,9], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"Missing Metal Slope SE", #sz:point(1,1), #specs:[4], #specs2:0, #tp:"voxelStruct", #repeatL:[1,9], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]
    [#nm:"Missing Metal Floor", #sz:point(1,1), #specs:[6], #specs2:0, #tp:"voxelStruct", #repeatL:[5,1,1,1,1,1], #bfTiles:0, #rnd:1, #ptPos:0, #tags:["INTERNAL"]]

    -["Dune", color(255, 255, 180)]
    [#nm:"Dune Sand", #sz:point(1,1), #specs:[1], #specs2:0, #tp:"voxelStructSandType", #bfTiles:1, #rnd:4, #ptPos:0, #tags:["nonSolid", "INTERNAL"]]
    """;
}