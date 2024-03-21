/**
    Built-in tile/prop/material Init.txt manager

    Storing an extra copy of all the assets may seem weird at first,
    so here are four good reasons why I need this class:
    
        1. Unlike the TileDatabase and PropDatabase class, this class does not
        load the asset graphics, so it can parse init files *significantly* faster.

        2. I don't want to actually change the usable asset database while the user
        is managing them, so I need a store a copy of it anyway.

        3. Some tiles and props are loaded from immutable memory rather than a file.

        4. I forgot the fourth reason. 

        5. Stores the line numbers. I guess. And the actual string.
*/
using Raylib_cs;

namespace RainEd.Assets;

record TileInit
{
    public string Name;
    public string RawData;
    public int LineNumber;

    public TileInit(string name, string data, int line)
    {
        Name = name;
        RawData = data;
        LineNumber = line;
    }
}

record TileInitCategory
{
    public string Name;
    public Lingo.Color Color;
    public List<TileInit> Tiles;
    public int LineNumber;

    public TileInitCategory(string name, Lingo.Color color, int line)
    {
        Name = name;
        Color = color;
        Tiles = [];
        LineNumber = line;
    }
}

class AssetManager
{
    public List<TileInitCategory> TileInit;

    public AssetManager()
    {
        TileInit = [];
        ParseTileInit(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt"), TileInit);
    }

    private static void ParseTileInit(string path, List<TileInitCategory> outputCategories)
    {
        var parser = new Lingo.LingoParser();

        int lineNo = 0;
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                lineNo++;
                continue;
            }

            // category header
            if (line[0] == '-')
            {
                var list = (Lingo.List) parser.Read(line[1..])!;
                outputCategories.Add(new TileInitCategory(
                    name: (string) list.values[0],
                    color: (Lingo.Color) list.values[1],
                    line: lineNo
                ));
            }

            // tile init
            else
            {
                var category = outputCategories[^1];
                var data = (Lingo.List) parser.Read(line)!;

                category.Tiles.Add(new TileInit(
                    name: (string) data.fields["nm"],
                    data: line,
                    line: lineNo
                ));
            }

            lineNo++;
        }
    }
}