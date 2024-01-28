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
    public int Width;
    public int Height;
    public byte[,] Requirements;
    public byte[,] Requirements2;
    public int BfTiles = 0;
    public RlManaged.Texture2D PreviewTexture;

    public TileData(string name, TileType type, int width, int height, int bfTiles, List<int>? repeatL)
    {
        Name = name;
        Width = width;
        Height = height;
        BfTiles = bfTiles;

        Requirements = new byte[width, height];
        Requirements2 = new byte[width, height];

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
        
        var fullImage = new RlManaged.Image($"drizzle/Drizzle.Data/Graphics/{name}.png");
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
            fullImage.Dispose();
            throw new Exception("Preview image is out of bounds");
        }

        var previewImage = new RlManaged.Image(fullImage, previewRect);

        // convert black-and-white image to white-and-transparent, respectively
        /*for (int x = 0; x < previewImage.Width; x++)
        {
            for (int y = 0; y < previewImage.Height; y++)
            {
                if (Raylib.GetImageColor(previewImage, x, y).Equals(new Color(255, 255, 255, 255)))
                {
                    previewImage.DrawPixel(x, y, new Color(255, 255, 255, 0));
                }
                else if (Raylib.GetImageColor(previewImage, x, y).Equals(new Color(0, 0, 0, 255)))
                {
                    previewImage.DrawPixel(x, y, new Color(255, 255, 255, 1));
                }
            }
        }*/

        PreviewTexture = new RlManaged.Texture2D(previewImage);

        fullImage.Dispose();
        previewImage.Dispose();
    }
}

public class TileCategory
{
    public string Name;
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

    public Database()
    {
        Categories = new();

        Console.WriteLine("Reading tile init data...");
        var parser = new Lingo.Parser(new StreamReader("drizzle/Drizzle.Data/Graphics/Init.txt"));
        List<List<object>> dataRoot = parser.Read();

        Console.WriteLine("Parsing tile init data...");
        foreach (List<object> categoriesTable in dataRoot)
        {
            var header = (Lingo.List) categoriesTable[0];
            var group = new TileCategory((string) header.values[0], (Lingo.Color) header.values[1]);
            Categories.Add(group);

            Console.WriteLine($"Register category {group.Name}");

            for (int i = 1; i < categoriesTable.Count; i++)
            {
                if (categoriesTable[i] is not Lingo.List tileInit) throw new Exception("Invalid tile init file");

                var name = (string) tileInit.fields["nm"];
                var tp = (string) tileInit.fields["tp"];
                var size = (Vector2) tileInit.fields["sz"];
                var specsData = (Lingo.List) tileInit.fields["specs"];
                var bfTiles = (int) tileInit.fields["bfTiles"];
                Lingo.List? repeatLayerList =
                    tileInit.fields.ContainsKey("repeatL") ? (Lingo.List) tileInit.fields["repeatL"] : null;
                
                List<int>? repeatL = repeatLayerList?.values.Cast<int>().ToList();
                
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
                        type: tileType,
                        width: (int)size.X, height: (int)size.Y,
                        bfTiles: bfTiles,
                        repeatL: repeatL
                    );

                    group.Tiles.Add(tileData);
                } catch (Exception e)
                {
                    Console.WriteLine($"Could not add '{name}': {e.Message}");
                }
            }
        }

        Console.WriteLine("Done!");
    }
}