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
    public Lingo.Color? Color;

    public InitCategory(string name, int line, Lingo.Color? color = null)
    {
        Name = name;
        LineNumber = line;
        Color = color;
        Items = [];
    }
}

record CategoryList
{
    public readonly string FilePath;
    public readonly List<string> Lines;
    private bool isColored;

    public readonly List<InitCategory> Categories = [];
    public readonly Dictionary<string, InitData> Dictionary = [];

    public CategoryList(string path, bool colored)
    {
        isColored = colored;
        FilePath = path;
        Lines = new List<string>(File.ReadAllLines(path));

        var parser = new Lingo.LingoParser();
        int lineNo = 0;
        foreach (var line in Lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                lineNo++;
                continue;
            }

            // category header
            if (line[0] == '-')
            {
                if (colored)
                {
                    var list = (Lingo.List) parser.Read(line[1..])!;
                    Categories.Add(new InitCategory(
                        name: (string) list.values[0],
                        color: (Lingo.Color) list.values[1],
                        line: lineNo
                    ));
                }
                else
                {
                    Categories.Add(new InitCategory(
                        name: line[1..],
                        line: lineNo
                    ));
                }
            }

            // item
            else
            {
                var data = parser.Read(line) as Lingo.List;

                if (data is not null)
                {
                    AddInit(new InitData(
                        name: (string) data.fields["nm"],
                        data: line,
                        line: lineNo
                    ));
                }
            }

            lineNo++;
        }
    }

    public void AddInit(InitData data, InitCategory? category = null)
    {
        (category ?? Categories[^1]).Items.Add(data);
        Dictionary[data.Name] = data;
    }

    private InitCategory? GetCategory(string name)
    {
        foreach (var cat in Categories)
        {
            if (cat.Name == name)
                return cat;
        }

        return null;
    }

    public void Merge(string otherPath)
    {
        RainEd.Logger.Information("Merge {Path}", otherPath);
        var parentDir = Path.Combine(FilePath, "..");
        var otherDir = Path.Combine(otherPath, "..");

        var parser = new Lingo.LingoParser();
        InitCategory? targetCategory = null; 

        // add extra lines for readability
        Lines.Add("");
        Lines.Add("");

        int otherLineNo = -1;

        foreach (var line in File.ReadLines(otherPath))
        {
            otherLineNo++;
            int lineNo = Lines.Count;

            if (string.IsNullOrWhiteSpace(line))
            {
                Lines.Add("");
                continue;
            }

            // category
            if (line[0] == '-')
            {
                InitCategory category;

                if (isColored)
                {
                    var list = (Lingo.List) parser.Read(line[1..])!;
                    category = new InitCategory(
                        name: (string) list.values[0],
                        color: (Lingo.Color) list.values[1],
                        line: lineNo
                    );
                }
                else
                {
                    category = new InitCategory(
                        name: line[1..],
                        line: lineNo
                    );
                }

                // error if category already exists
                if (GetCategory(category.Name) is not null)
                {
                    throw new Exception($"Category '{category.Name}' already exists!");
                }
                else
                {
                    Lines.Add(line);
                    Categories.Add(category);
                    targetCategory = category;
                }
            }

            // item
            else
            {
                if (targetCategory is null)
                {
                    throw new Exception("Category definition expected, got item definition");
                }

                var data = parser.Read(line) as Lingo.List;

                if (data is not null)
                {
                    var init = new InitData(
                        name: (string) data.fields["nm"],
                        data: line,
                        line: lineNo
                    );

                    // error if name already exists
                    if (Dictionary.ContainsKey(init.Name))
                    {
                        throw new Exception($"Item '{init.Name}' already exists!");
                    }
                    else
                    {
                        var pngName = init.Name + ".png";

                        // copy graphics
                        RainEd.Logger.Information("Copy {ImageName}", pngName);
                        
                        var graphicsData = File.ReadAllBytes(Path.Combine(otherDir, pngName));
                        File.WriteAllBytes(Path.Combine(parentDir, pngName), graphicsData);

                        Lines.Add(line);
                        AddInit(init, targetCategory);
                    }
                }
            }
        }

        RainEd.Logger.Information("Writing merge result to {Path}...", FilePath);
        
        // force \r newlines
        File.WriteAllText(FilePath, string.Join("\r", Lines));

        RainEd.Logger.Information("Merge successful!");
    }
}

class AssetManager
{
    public readonly CategoryList TileInit;
    public readonly CategoryList PropInit;
    public readonly CategoryList MaterialsInit;
    
    public AssetManager()
    {
        TileInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt"), true);
        PropInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Props", "Init.txt"), true);
        MaterialsInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Materials", "Init.txt"), false);
    }
}