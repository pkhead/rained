using System.Numerics;
using Raylib_cs;
using RlManaged;

namespace Tiles;

public enum TileType
{
    VoxelStruct,
    VoxelStructRockType,
    VoxelStructRandomDisplaceVertical,
    VoxelStructRandomDisplaceHorizontal,
    Box
}

public class TileData
{
    public string Name;
    public readonly TileCategory Category;
    public int Width;
    public int Height;
    public sbyte[,] Requirements;
    public sbyte[,] Requirements2;
    public readonly bool HasSecondLayer;
    public int BfTiles = 0;
    public RlManaged.Texture2D PreviewTexture;

    public readonly int CenterX;
    public readonly int CenterY;

    public TileData(string name, TileCategory category, TileType type, int width, int height, int bfTiles, List<int>? repeatL, List<int> specs, List<int>? specs2)
    {
        Name = name;
        Width = width;
        Height = height;
        BfTiles = bfTiles;
        Category = category;

        CenterX = (int)MathF.Ceiling((float)Width / 2) - 1;
        CenterY = (int)MathF.Ceiling((float)Height / 2) - 1;

        Requirements = new sbyte[width, height];
        Requirements2 = new sbyte[width, height];
        HasSecondLayer = false;

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
                if (repeatL is null) throw new NullReferenceException();
                rowCount *= repeatL.Count;
                break;
            
            case TileType.VoxelStructRockType:
                break;
            
            case TileType.Box:
                rowCount = height * width + height + bfTiles * 2;
                imageOffset = 0;
                break;
        }
        
        var fullImage = RlManaged.Image.Load($"drizzle/Drizzle.Data/Graphics/{name}.png");
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
            Console.WriteLine($"Warning: '{name}' preview image is out of bounds");
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
    }
}

public class TileCategory
{
    public string Name;
    public int Index;
    public Color Color;
    public List<TileData> Tiles = new();

    public TileCategory(string name, Lingo.Color color)
    {
        Name = name;
        Color = new Color(color.R, color.G, color.B, 255);
    }
}

public class Database
{
    public readonly List<TileCategory> Categories;
    private readonly Dictionary<string, TileData> stringToTile = new();

    public Database()
    {
        Categories = new();

        Console.WriteLine("Reading tile init data...");
        var lingoParser = new Lingo.LingoParser();

        TileCategory? curGroup = null;
        int groupIndex = 0;
        foreach (var line in File.ReadLines("drizzle/Drizzle.Data/Graphics/Init.txt"))
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
                
                Console.WriteLine($"Register category {curGroup.Name}");
            }
            else
            {
                if (curGroup is null) throw new Exception("Invalid tile init file");

                var tileInit = (Lingo.List) (lingoParser.Read(line) ?? throw new Exception("Invalid tile init file"));

                var name = (string) tileInit.fields["nm"];
                var tp = (string) tileInit.fields["tp"];
                var size = (Vector2) tileInit.fields["sz"];
                var specsData = (Lingo.List) tileInit.fields["specs"];
                var bfTiles = (int) tileInit.fields["bfTiles"];
                Lingo.List? specs2Data = null;
                Lingo.List? repeatLayerList =
                    tileInit.fields.ContainsKey("repeatL") ? (Lingo.List) tileInit.fields["repeatL"] : null;
                
                if (tileInit.fields.ContainsKey("specs2") && tileInit.fields["specs2"] is Lingo.List specs2List)
                {
                    specs2Data = specs2List;
                }

                List<int>? repeatL = repeatLayerList?.values.Cast<int>().ToList();
                List<int> specs = specsData.values.Cast<int>().ToList();
                List<int>? specs2 = specs2Data?.values.Cast<int>().ToList();

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
                    var tileData = new TileData(
                        name: name,
                        category: curGroup,
                        type: tileType,
                        width: (int)size.X, height: (int)size.Y,
                        bfTiles: bfTiles,
                        repeatL: repeatL,
                        specs: specs,
                        specs2: specs2
                    );

                    curGroup.Tiles.Add(tileData);
                    stringToTile.Add(name, tileData);
                } catch (Exception e)
                {
                    Console.WriteLine($"Could not add '{name}': {e.Message}");
                }
            }
        }

        Console.WriteLine("Done!");
    }

    public bool HasTile(string name) => stringToTile.ContainsKey(name);
    public TileData GetTileFromName(string name) => stringToTile[name];
}