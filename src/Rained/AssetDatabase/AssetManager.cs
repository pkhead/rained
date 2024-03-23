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
    public InitCategory Category = null!;

    public InitData(string name, string data)
    {
        Name = name;
        RawData = data;
    }
}

record InitCategory
{
    public string Name;
    public List<InitData> Items;
    public Lingo.Color? Color;

    public InitCategory(string name, Lingo.Color? color = null)
    {
        Name = name;
        Color = color;
        Items = [];
    }
}

record CategoryList
{
    public readonly string FilePath;
    public readonly List<string> Lines;
    private readonly List<object> parsedLines;
    private bool isColored;

    public readonly List<InitCategory> Categories = [];
    public readonly Dictionary<string, InitData> Dictionary = [];

    private record EmptyLine;

    public CategoryList(string path, bool colored)
    {
        isColored = colored;
        FilePath = path;
        Lines = new List<string>(File.ReadAllLines(path));
        parsedLines = [];

        var parser = new Lingo.LingoParser();
        foreach (var line in Lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                parsedLines.Add(new EmptyLine());
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
                        color: (Lingo.Color) list.values[1]
                    ));
                }
                else
                {
                    Categories.Add(new InitCategory(line[1..]));
                }

                parsedLines.Add(Categories[^1]);
            }

            // item
            else
            {
                var data = parser.Read(line) as Lingo.List;

                if (data is not null)
                {
                    var init = new InitData(
                        name: (string) data.fields["nm"],
                        data: line
                    );
                    AddInit(init);

                    parsedLines.Add(init);
                }
                else
                {
                    parsedLines.Add(new EmptyLine());
                }
            }
        }
    }

    public void AddInit(InitData data, InitCategory? category = null)
    {
        category ??= Categories[^1];
        
        data.Category = category;
        category.Items.Add(data);
        Dictionary[data.Name] = data;
    }

    public void ReplaceInit(InitData oldData, InitData newData)
    {
        if (oldData.Name != newData.Name)
            throw new ArgumentException("Mismatched names");
        
        int i = parsedLines.IndexOf(oldData);
        if (i == -1) throw new Exception("Could not find line index");

        if (parsedLines[i] is not InitData v || v != oldData || v.RawData != Lines[i])
            throw new Exception("Line index points to incorrect item");
        
        oldData.Category.Items[oldData.Category.Items.IndexOf(oldData)] = newData;
        newData.Category = oldData.Category;

        Dictionary[newData.Name] = newData;
        parsedLines[i] = newData;
        Lines[i] = newData.RawData;
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

    public async Task Merge(string otherPath, Func<string, Task<bool>> promptOverwrite)
    {
        try
        {
            RainEd.Logger.Information("Merge {Path}", otherPath);
            var parentDir = Path.Combine(FilePath, "..");
            var otherDir = Path.Combine(otherPath, "..");

            var parser = new Lingo.LingoParser();
            InitCategory? targetCategory = null; 

            int lineAccum = 2;
            int otherLineNo = -1;

            int FlushLineAccum(int insert)
            {
                int oldAccum = lineAccum;

                for (int i = 0; i < lineAccum; i++)
                {
                    Lines.Insert(insert, "");
                    parsedLines.Insert(insert, new EmptyLine());
                }
                lineAccum = 0;

                return oldAccum;
            }

            foreach (var line in File.ReadLines(otherPath))
            {
                otherLineNo++;
                int lineNo = Lines.Count;

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (targetCategory is not null) lineAccum++;
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
                            color: (Lingo.Color) list.values[1]
                        );
                    }
                    else
                    {
                        category = new InitCategory(line[1..]);
                    }

                    // don't write new line if overwriting a category with the same name and color
                    InitCategory? oldCategory = null;
                    if ((oldCategory = GetCategory(category.Name)) is not null)
                    {
                        // throw error on color mismatch
                        if (oldCategory.Color != category.Color)
                            throw new Exception($"Category '{category.Name}' already exists!");
                        
                        targetCategory = oldCategory;

                        // this will insert a newline when adding
                        // new tiles to the category
                        lineAccum = 1;
                    }
                    else
                    {
                        // register the category
                        FlushLineAccum(Lines.Count);
                        Lines.Add(line);
                        parsedLines.Add(category);
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
                            data: line
                        );

                        bool success = true;
                        bool expectImageExistence = true;

                        // check with user if tile with same name already exists
                        if (Dictionary.TryGetValue(init.Name, out InitData? oldInit))
                        {
                            if (oldInit.Category != targetCategory)
                                throw new Exception($"Item '{init.Name}' was imported into '{targetCategory.Name}', but was already in '{oldInit.Name}'");

                            if (await promptOverwrite(init.Name))
                            {
                                // go ahead and overwrite
                                RainEd.Logger.Information("Overwrite tile '{TileName}'", init.Name);
                                ReplaceInit(oldInit, init);
                                expectImageExistence = false;
                            }
                            else
                            {
                                RainEd.Logger.Information("Ignore tile '{TileName}'", init.Name);
                                success = false;
                            }
                        }
                        else
                        {
                            // insert the new item at the end of the category tile list
                            // by inserting it where the next init category definition is,
                            // or the EOF
                            int i = parsedLines.IndexOf(targetCategory);
                            if (i == -1) throw new Exception("Failed to find line number of category");

                            // find where the next category starts (or the EOF)
                            int insertLoc = -1;

                            i++;
                            while (i < parsedLines.Count && parsedLines[i] is not InitCategory)
                            {
                                if (parsedLines[i] is not EmptyLine)
                                    insertLoc = i;
                                
                                i++;
                            }
                            
                            // if at EOF
                            if (i == Lines.Count)
                            {   
                                i = Lines.Count;
                                insertLoc = i;
                            }

                            else
                            {
                                // prefer to insert it after the last item def in this category
                                if (insertLoc >= 0)
                                {
                                    insertLoc++;
                                }
                                else
                                {
                                    insertLoc = i;
                                }
                            }
                            
                            // flush line accumulator
                            insertLoc += FlushLineAccum(insertLoc);

                            // insert item
                            Lines.Insert(insertLoc, line);
                            parsedLines.Insert(insertLoc, init);
                            AddInit(init, targetCategory);
                        }
                        
                        // copy image
                        if (success)
                        {
                            var pngName = init.Name + ".png";

                            // if expectImageExistence is false, only copy the image if it exists.
                            // it is set to false when overwriting an item, in case the new
                            // Init.txt just wants to change the item data and not its graphics.
                            if (expectImageExistence || File.Exists(Path.Combine(otherDir, pngName)))
                            {
                                // copy graphics
                                RainEd.Logger.Information("Copy {ImageName}", pngName);
                                
                                var graphicsData = File.ReadAllBytes(Path.Combine(otherDir, pngName));
                                File.WriteAllBytes(Path.Combine(parentDir, pngName), graphicsData);
                            }
                        }
                    }
                }
            }

            RainEd.Logger.Information("Writing merge result to {Path}...", FilePath);
            
            // force \r newlines
            File.WriteAllText(FilePath, string.Join("\r", Lines));

            RainEd.Logger.Information("Merge successful!");
        }
        catch (Exception e)
        {
            RainEd.Logger.Error("Error while merging:\n{Error}", e);
            throw;
        }
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