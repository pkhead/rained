using System.Numerics;
using Raylib_cs;

namespace Tiles;

public struct TileData
{
    public string Name;
    public int Width;
    public int Height;
    public byte[,] Requirements;
    public byte[,] Requirements2;

    public TileData(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;

        Requirements = new byte[width, height];
        Requirements2 = new byte[width, height];
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
        var parser = new Lingo.Parser(new StreamReader("data/tileinit.txt"));
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
                var size = (Vector2) tileInit.fields["sz"];
                var specsData = (Lingo.List) tileInit.fields["specs"];
                //var specs2Data = tileInit.fields["specs2"];

                var tileData = new TileData(name, (int)size.X, (int)size.Y);
                group.Tiles.Add(tileData);
            }
        }

        Console.WriteLine("Done!");
    }
}