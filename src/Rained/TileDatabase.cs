using System.Numerics;
using Raylib_cs;

namespace RainEd.Tiles;

public enum TileType
{
    VoxelStruct,
    VoxelStructRockType,
    VoxelStructRandomDisplaceVertical,
    VoxelStructRandomDisplaceHorizontal,
    Box
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
                break;
            
            case TileType.Box:
                rowCount = height * width + height + bfTiles * 2;
                imageOffset = 0;
                break;
        }
        
        var fullImage = RlManaged.Image.Load(Path.Combine(Boot.AppDataPath, "Data", "Graphics", name + ".png"));
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

        var previewImage = RlManaged.Image.GenColor(width * 16, height * 16, Color.White);
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

        fullImage.Dispose();
        previewImage.Dispose();

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
        foreach (var line in File.ReadLines(Path.Combine(Boot.AppDataPath, "Data","Graphics","Init.txt")))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
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
    }

    public bool HasTile(string name) => stringToTile.ContainsKey(name);
    public Tile GetTileFromName(string name) => stringToTile[name];
}