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

[Serializable]
public class MergeException : Exception
{
    public MergeException() { }
    public MergeException(string message) : base(message) { }
    public MergeException(string message, System.Exception inner) : base(message, inner) { }
}

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

interface IAssetGraphicsManager
{
    public void CopyGraphics(InitData init, string srcDir, string destDir, bool expect);
    public void DeleteGraphics(InitData init, string dir);
}

enum PromptResult { Yes, No, YesToAll, NoToAll }

class PromptOptions
{
    public string Text = "";
    public readonly string[] CheckboxText;
    public readonly bool[] CheckboxValues;

    public PromptOptions(string text)
    {
        Text = text;
        CheckboxText = [];
        CheckboxValues = [];
    }

    public PromptOptions(string text, string[] checkboxes)
    {
        Text = text;
        CheckboxText = checkboxes;
        CheckboxValues = new bool[checkboxes.Length];

        for (int i = 0; i < checkboxes.Length; i++)
            CheckboxValues[i] = false;
    }
}

record CategoryList
{
    public readonly string FilePath;
    public readonly List<string> Lines;
    private readonly List<object> parsedLines;
    private bool isColored;
    private readonly IAssetGraphicsManager graphicsManager;

    public readonly List<InitCategory> Categories = [];
    public readonly Dictionary<string, List<InitData>> ItemDictionary = [];

    private record EmptyLine;

    public CategoryList(string path, bool colored, IAssetGraphicsManager graphicsManager)
    {
        isColored = colored;
        this.graphicsManager = graphicsManager;
        FilePath = path;
        parsedLines = [];

        if (!File.Exists(path))
        {
            Log.Error("{Path} not found!", path);
            Lines = [];
            return;
        }

        Lines = new List<string>(File.ReadAllLines(path));

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
                    if (parser.Read(line[1..]) is not Lingo.List list) continue;

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

    private void WriteToFile()
    {
        // force \r newlines
        if (File.Exists(FilePath))
            File.WriteAllText(FilePath, string.Join("\r", Lines));
    }

    public void AddInit(InitData data, InitCategory? category = null)
    {
        category ??= Categories[^1];
        
        data.Category = category;
        category.Items.Add(data);

        if (!ItemDictionary.TryGetValue(data.Name, out List<InitData>? list))
        {
            list = [];
            ItemDictionary.Add(data.Name, list);
        }
        list.Add(data);
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

        var list = ItemDictionary[newData.Name];
        list[list.IndexOf(oldData)] = newData;
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

    public void DeleteCategory(InitCategory category)
    {
        int lineIndex = parsedLines.IndexOf(category);
        if (lineIndex == -1) throw new Exception("Failed to find line index");

        if (!Categories.Remove(category))
            throw new Exception("The category was not in the list");
        
        // remove items in the category
        var parentDir = Path.Combine(FilePath, "..");
        foreach (var tile in category.Items)
        {
            ItemDictionary.Remove(tile.Name);
            graphicsManager.DeleteGraphics(tile, parentDir);
        }

        // remove lines and tiles relating to the category
        parsedLines.RemoveAt(lineIndex);
        Lines.RemoveAt(lineIndex);

        // keep removing lines until another category is found or the EOF is reached
        while (lineIndex < Lines.Count && parsedLines[lineIndex] is not InitCategory)
        {
            parsedLines.RemoveAt(lineIndex);
            Lines.RemoveAt(lineIndex);
        }

        WriteToFile();
    }

    public void DeleteItem(InitData item, bool write = true)
    {
        int lineIndex = parsedLines.IndexOf(item);
        if (lineIndex == -1) throw new Exception("Failed to find line index");

        // remove from category data
        if (!item.Category.Items.Remove(item))
            throw new Exception("Item was already removed!");

        var parentDir = Path.Combine(FilePath, "..");
        ItemDictionary.Remove(item.Name);
        if (write) graphicsManager.DeleteGraphics(item, parentDir);

        // delete the line
        parsedLines.RemoveAt(lineIndex);
        Lines.RemoveAt(lineIndex);

        if (write) WriteToFile();
    }

    public delegate Task<PromptResult> PromptRequest(PromptOptions promptState);

    public async Task Merge(string otherPath, PromptRequest promptOverwrite)
    {
        try
        {
            // automatically overwrite items that are only defined one time
            // (LOOKING AT YOU, "INSIDE HUGE PIPE HORIZONTAL" DEFINED IN BOTH MISC AND LB INTAKE SYSTEM.)
            bool? autoOverwrite = null;

            Log.Information("Merge {Path}", otherPath);
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
                    InitCategory? category = null;

                    if (isColored)
                    {
                        var list = parser.Read(line[1..]) as Lingo.List;

                        if (list is not null)
                        {
                            category = new InitCategory(
                                name: (string) list.values[0],
                                color: (Lingo.Color) list.values[1]
                            );
                        }
                        else
                        {
                            Log.Information("Ignore malformed category header at '{Line}'", line);
                        }
                    }
                    else
                    {
                        category = new InitCategory(line[1..]);
                    }

                    // don't write new line if overwriting a category with the same name and color
                    if (category is not null)
                    {
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
                        
                        bool doInsert = true;
                        bool expectGraphics = true;

                        // check with user if tile with same name already exists
                        if (ItemDictionary.TryGetValue(init.Name, out List<InitData>? initItems))
                        {
                            doInsert = false;

                            // if there is more than one item with the same name, or
                            // if the item is defined in a different category,
                            // it is a merge conflict that needs Advanced User Intervention.

                            // (IM TALKING ABOUT YOU INSIDE HUGE PIPE HORIZINTAL WHICH IS IN BOTH MISC AND LB INTAKE SYSTEM)
                            if (initItems.Count > 1 || initItems[0].Category != targetCategory)
                            {
                                Log.Information("Merge conflict with asset '{Name}'", init.Name);
                                
                                // there will be an extra checkbox which will insert the new init
                                // into a new line
                                var options = new string[initItems.Count + 1];
                                for (int i = 0; i < initItems.Count; i++)
                                {
                                    options[i] = "Overwrite old definition in " + initItems[i].Category.Name;
                                }
                                options[^1] = "Add to " + targetCategory.Name;

                                var prompt = new PromptOptions($"Merge conflict with asset \"{init.Name}\"\nNew definition is in {targetCategory.Name}", options);
                                await promptOverwrite(prompt);
                                
                                for (int i = 0; i < initItems.Count; i++)
                                {
                                    if (prompt.CheckboxValues[i])
                                    {
                                        Log.Information("Overwrite def in '{Category}'", initItems[0].Category);
                                        ReplaceInit(initItems[i], init);
                                    }
                                }

                                if (prompt.CheckboxValues[^1])
                                {
                                    Log.Information("Add to '{Category}'", targetCategory);

                                    doInsert = true;
                                    expectGraphics = false;
                                }
                            }

                            // simple merge conflict - item is only defined once and
                            // both new and old items are in the same category
                            else
                            {
                                bool doOverwrite;

                                if (autoOverwrite.HasValue)
                                {
                                    doOverwrite = autoOverwrite.Value;
                                }
                                else
                                {
                                    // ask the user if they want to overwrite
                                    var opt = new PromptOptions($"Overwrite \"{init.Name}\"?");
                                    
                                    switch (await promptOverwrite(opt))
                                    {
                                        case PromptResult.Yes:
                                            doOverwrite = true;
                                            break;

                                        case PromptResult.No:
                                            doOverwrite = false;
                                            break;

                                        case PromptResult.YesToAll:
                                            doOverwrite = true;
                                            autoOverwrite = true;
                                            break;

                                        case PromptResult.NoToAll:
                                            doOverwrite = false;
                                            autoOverwrite = false;
                                            break;

                                        default:
                                            throw new Exception();
                                    }
                                }

                                if (doOverwrite)
                                {
                                    // go ahead and overwrite
                                    Log.Information("Overwrite tile '{TileName}'", init.Name);
                                    ReplaceInit(initItems[0], init);
                                    
                                    graphicsManager.CopyGraphics(init, otherDir, parentDir, false);
                                }
                                else
                                {
                                    Log.Information("Ignore tile '{TileName}'", init.Name);
                                }
                            }
                        }
                        
                        if (doInsert)
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

                            graphicsManager.CopyGraphics(init, otherDir, parentDir, expectGraphics);
                        }
                    }
                }
            }

            Log.Information("Writing merge result to {Path}...", FilePath);
            WriteToFile();
            Log.Information("Merge successful!");
        }
        catch (Exception e)
        {
            Log.Error("Error while merging:\n{Error}", e);
            throw;
        }
    }
}

class AssetManager
{
    public readonly CategoryList TileInit;
    public readonly CategoryList PropInit;
    public readonly CategoryList? MaterialsInit;

    class StandardGraphicsManager : IAssetGraphicsManager
    {
        public void CopyGraphics(InitData init, string srcDir, string destDir, bool expect)
        {
            var pngName = init.Name + ".png";
            var pngPath = AssetGraphicsProvider.GetFilePath(srcDir, pngName);

            // if expectImageExistence is false, only copy the image if it exists.
            // it is set to false when overwriting an item, in case the new
            // Init.txt just wants to change the item data and not its graphics.
            if (expect || File.Exists(pngPath))
            {
                // copy graphics
                Log.Information("Copy {ImageName}", pngName);
                
                var graphicsData = File.ReadAllBytes(pngPath);
                File.WriteAllBytes(Path.Combine(destDir, pngName), graphicsData);
            }
        }

        public void DeleteGraphics(InitData init, string dir)
        {
            var filePath = AssetGraphicsProvider.GetFilePath(dir, init.Name + ".png");

            if (File.Exists(filePath))
            {
                Log.Information("Delete {FilePath}", filePath);
                File.Delete(filePath);
            }
        }
    }

    class MaterialGraphicsManager : IAssetGraphicsManager
    {
        private static readonly string[] PossibleExtensions = [
            ".png",
            "Texture.png",
            "Floor.png",
            "Slopes.png",
            "Texture.png",
        ];

        public void CopyGraphics(InitData init, string srcDir, string destDir, bool expect)
        {
            foreach (var ext in PossibleExtensions)
            {
                var pngName = init.Name + ext;
                var pngPath = AssetGraphicsProvider.GetFilePath(srcDir, pngName);

                if (File.Exists(pngPath))
                {
                    // copy graphics
                    Log.Information("Copy {ImageName}", pngName);
                    
                    var graphicsData = File.ReadAllBytes(pngPath);
                    File.WriteAllBytes(Path.Combine(destDir, pngName), graphicsData);
                }
            }
        }

        public void DeleteGraphics(InitData init, string dir)
        {
            foreach (var ext in PossibleExtensions)
            {
                var filePath = AssetGraphicsProvider.GetFilePath(dir, init.Name + ext);
                
                if (File.Exists(filePath))
                {
                    Log.Information("Delete {FilePath}", filePath);
                    File.Delete(filePath);
                }
            }
        }
    }
    
    public AssetManager()
    {
        var graphicsManager1 = new StandardGraphicsManager();
        var graphicsManager2 = new MaterialGraphicsManager();
        var matInitPath = Path.Combine(RainEd.Instance.AssetDataPath, "Materials", "Init.txt");

        TileInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt"), true, graphicsManager1);
        PropInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Props", "Init.txt"), true, graphicsManager1);

        if (File.Exists(matInitPath))
        {
            MaterialsInit = new(matInitPath, false, graphicsManager2);
        }
        else
        {
            MaterialsInit = null;
        }
    }
}