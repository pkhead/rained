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
namespace RainEd.Assets;

record InitData
{
    public string Name;
    public string RawData;
    public int LineNumber;

    public InitData(string name, string data, int line)
    {
        Name = name;
        RawData = data;
        LineNumber = line;
    }
}

record InitCategory
{
    public string Name;
    public List<InitData> Items;
    public int LineNumber;

    public InitCategory(string name, int line)
    {
        Name = name;
        LineNumber = line;
        Items = [];
    }
}

record ColoredInitCategory : InitCategory
{
    public Lingo.Color Color;

    public ColoredInitCategory(string name, Lingo.Color color, int line) : base(name, line)
    {
        Name = name;
        Color = color;
        Items = [];
        LineNumber = line;
    }
}

record CategoryList<T>
    where T : InitCategory
{
    public List<T> Categories = [];
    public Dictionary<string, InitData> Dictionary = [];

    public void AddInit(InitData data)
    {
        Categories[^1].Items.Add(data);
        Dictionary[data.Name] = data;
    }
}

class AssetManager
{
    public readonly CategoryList<ColoredInitCategory> TileInit;
    public readonly CategoryList<ColoredInitCategory> PropInit;
    public readonly CategoryList<InitCategory> MaterialsInit;
    
    public AssetManager()
    {
        TileInit = new();
        PropInit = new();
        MaterialsInit = new();

        // parse tile Init.txt
        ParseColoredInit(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt"), TileInit);
        
        // parse prop Init.txt
        ParseColoredInit(Path.Combine(RainEd.Instance.AssetDataPath, "Props", "Init.txt"), PropInit);

        // parse materials Init.txt
        ParseMaterialInit(Path.Combine(RainEd.Instance.AssetDataPath, "Materials", "Init.txt"), MaterialsInit);
    }

    private static void ParseColoredInit(
        string path,
        CategoryList<ColoredInitCategory> outputCategories
    )
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
                outputCategories.Categories.Add(new ColoredInitCategory(
                    name: (string) list.values[0],
                    color: (Lingo.Color) list.values[1],
                    line: lineNo
                ));
            }

            // tile init
            else
            {
                var data = parser.Read(line) as Lingo.List;

                if (data is not null)
                {
                    outputCategories.AddInit(new InitData(
                        name: (string) data.fields["nm"],
                        data: line,
                        line: lineNo
                    ));
                }
            }

            lineNo++;
        }
    }

    private static void ParseMaterialInit(string path, CategoryList<InitCategory> outputCategories)
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
                outputCategories.Categories.Add(new InitCategory(
                    name: line[1..],
                    line: lineNo
                ));
            }

            // tile init
            else
            {
                var data = (Lingo.List) parser.Read(line)!;

                outputCategories.AddInit(new InitData(
                    name: (string) data.fields["nm"],
                    data: line,
                    line: lineNo
                ));
            }

            lineNo++;
        }
    }
}