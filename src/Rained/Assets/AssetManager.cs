using Rained.EditorGui;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;


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
namespace Rained.Assets;

[Serializable]
public class MergeException : Exception
{
    public MergeException() { }
    public MergeException(string message) : base(message) { }
    public MergeException(string message, System.Exception inner) : base(message, inner) { }
}

interface IAssetGraphicsManager
{
    public void CopyGraphics(string name, string srcDir, string destDir, bool expect);
    public void DeleteGraphics(string name, string dir);
    public void RenameGraphics(string oldName, string newName, string dir);
}

enum PromptYesNoResult { Yes, No, YesToAll, NoToAll }
enum PromptInputMode { YesNo, Checkbox, Radio }

abstract record PromptResult;
record PromptResultYesNo(PromptYesNoResult Result) : PromptResult;
record PromptResultCheckbox(bool[] Values) : PromptResult;
record PromptResultRadio(int Value) : PromptResult;

class PromptOptions(PromptInputMode inputMode, string text, string[]? options = null)
{
    public readonly PromptInputMode InputMode = inputMode;
    public string Text = text;
    public string[]? OptionText = options;
}

partial class CategoryList
{
    public readonly string FilePath;
    //public readonly List<string> Lines;
    private readonly List<InitLineData> lines;
    private readonly bool isColored;

    // structs to store information directly from lines in the init file
    public record class InitLineData(string RawLine)
    {
        public string RawLine = RawLine;
    }
    
    private record class InitIrrelevantLine(string RawLine) : InitLineData(RawLine); // can represent comments or empty lines
    private record class InitCategoryHeader(string RawLine, string Name, Lingo.Color? Color) : InitLineData(RawLine)
    {
        public string Name = Name;
        public Lingo.Color? Color = Color;
    }

    public record class InitItem(string RawLine, string Name) : InitLineData(RawLine)
    {
        public string Name = Name;
    }

    public class InitCategory(string Name, Lingo.Color? Color)
    {
        public string Name = Name;
        public Lingo.Color? Color = Color;
        public List<InitItem> Items = [];
    }

    public List<InitCategory> Categories = [];
    private readonly Dictionary<string, InitCategoryHeader> categoryHeaders = [];
    private readonly Dictionary<InitItem, InitCategory> itemCategories = [];

    private record EmptyLine;

    // helper function to create error string with line information
    static string ErrorString(int lineNo, string msg)
        => "Line " + (lineNo == -1 ? "[UNKNOWN]" : lineNo) + ": " + msg;    

    public CategoryList(string path, bool colored)
    {
        isColored = colored;
        FilePath = path;
        lines = [];

        if (!File.Exists(path))
        {
            Log.Error("{Path} not found!", path);
            return;
        }

        // first, parse all of the lines
        uint nextId = 1;
        var parser = new Lingo.LingoParser();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                lines.Add(new InitIrrelevantLine(line));
                continue;
            }

            // category header
            if (line[0] == '-')
            {
                if (colored)
                {
                    if (parser.Read(line[1..]) is not Lingo.LinearList list) continue;

                    lines.Add(new InitCategoryHeader(
                        RawLine: line,
                        Name: (string) list[0],
                        Color: (Lingo.Color) list[1]
                    ));
                }
                else
                {
                    lines.Add(new InitCategoryHeader(
                        RawLine: line,
                        Name: line[1..],
                        Color: null
                    ));
                }
            }

            // item
            else
            {
                var data = parser.Read(line) as Lingo.PropertyList;

                if (data is not null)
                {
                    lines.Add(new InitItem(
                        RawLine: line,
                        Name: (string) data["nm"]
                    ));
                    // AddInit();
                }
                else
                {
                    lines.Add(new InitIrrelevantLine(line));
                }
            }
        }

        // then, generate the category and items list stuff
        ParseLines();
    }

    private void ParseLines()
    {
        Categories.Clear();
        categoryHeaders.Clear();

        InitCategory? currentCategory = null;
        int lineNum = 0;

        foreach (var lineInfo in lines)
        {
            lineNum++;
            
            if (lineInfo is InitCategoryHeader groupInfo)
            {
                if (!categoryHeaders.TryAdd(groupInfo.Name, groupInfo))
                    throw new Exception(ErrorString(lineNum, $"A category with the name '{groupInfo.Name}' was already created."));
                
                currentCategory = new InitCategory(groupInfo.Name, groupInfo.Color);
                Categories.Add(currentCategory);
            }
            else if (lineInfo is InitItem itemInfo)
            {
                if (currentCategory is null)
                    throw new Exception(ErrorString(lineNum, "The first category header is missing"));
                
                currentCategory.Items.Add(itemInfo);
                itemCategories[itemInfo] = currentCategory;
            }
        }
    }

    public void Commit()
    {
        // force \r newlines
        if (File.Exists(FilePath))
            File.WriteAllText(FilePath, string.Join("\r", lines.Select(x => x.RawLine)));
    }

    public enum CategoryHeaderTypeEnum
    {
        /// <summary>
        /// `-Example Category`
        /// </summary>
        NameLiteral,

        /// <summary>
        /// `-["Example Category", color(255, 0, 0)]`
        /// </summary>
        LingoList
    }

    public CategoryHeaderTypeEnum CategoryHeaderType => isColored ? CategoryHeaderTypeEnum.LingoList : CategoryHeaderTypeEnum.NameLiteral;

    public InitCategory? GetCategoryByName(string name)
    {
        foreach (var cat in Categories)
        {
            if (cat.Name == name)
                return cat;
        }

        return null;
    }

    public int GetCategoryIndex(InitCategory category)
    {
        return Categories.IndexOf(category);
    }

    public InitItem[] GetItemsByName(string itemName)
    {
        List<InitItem> outList = [];

        foreach (var cat in Categories)
        {
            foreach (var item in cat.Items)
            {
                if (item.Name == itemName)
                    outList.Add(item);
            }
        }

        return [..outList];
    }

    public InitCategory GetCategoryOfItem(InitItem item)
    {
        return itemCategories[item];
    }

    /// <summary>
    /// Add an item to a category. The given category must have been created by this CategoryList.
    /// </summary>
    /// <param name="category">The category to add the item to. </param>
    /// <param name="itemData">The item to add.</param>
    /// <exception cref="ArgumentException">Thrown if the given category is not in this CategoryList.</exception>
    /// <returns>The clone of the InitItem that was registered.</returns>
    public InitItem AddItem(InitCategory category, InitItem itemData)
    {
        if (!Categories.Contains(category)) throw new ArgumentException("The given category was not created by this CategoryList", nameof(category));
        if (category.Items.Contains(itemData)) return itemData;
        itemData = new InitItem(itemData.RawLine, itemData.Name);

        category.Items.Add(itemData);

        // add line to file

        // first, find the last line of the section of this category in the file
        int lineIndex = lines.IndexOf(categoryHeaders[category.Name]) + 1;
        while (lineIndex < lines.Count && lines[lineIndex] is not InitCategoryHeader)
            lineIndex++;
        
        // then, go up until a newline is not found
        lineIndex--;
        while (lineIndex > 0 && string.IsNullOrWhiteSpace(lines[lineIndex].RawLine))
            lineIndex--;
        
        // write the new line into this space
        lineIndex++;
        lines.Insert(lineIndex, itemData);
        itemCategories[itemData] = category;
        return itemData;
    }

    public InitCategory AddCategory(string name, Lingo.Color? color, int index)
    {
        // add category header to file. the position where the header is placed
        // is dependent on the index
        int lineIndex;

        if (index < Categories.Count)
        {
            lineIndex = lines.IndexOf(categoryHeaders[Categories[index].Name]);
            if (lineIndex == -1) throw new Exception("Could not determine where to place new category");
        }
        else
        {
            // if last line isn't empty, then add one line of padding
            if (lines.Count > 0 && lines[^1] is InitIrrelevantLine l && string.IsNullOrWhiteSpace(l.RawLine))
                lines.Add(new InitIrrelevantLine(""));
            
            lineIndex = lines.Count;
        }

        var category = new InitCategory(name, color);
        Categories.Insert(index, category);
        lines.Insert(lineIndex, new InitIrrelevantLine("")); // this will add an empty line after the category header

        InitCategoryHeader header;

        if (isColored)
        {
            if (color is null) Log.Warning("Expected color argument in AddCategory. Defaulting to black...");
            Lingo.Color cv = color ?? new Lingo.Color(0, 0, 0);
            header = new InitCategoryHeader($"-[\"{name}\", color({cv.R}, {cv.G}, {cv.B})]", name, cv);
        }
        else
        {
            if (color is not null) Log.Warning("Unexpected color argument in AddCategory.");
            header = new InitCategoryHeader($"-{name}", name, null);
        }

        lines.Insert(lineIndex, header);
        categoryHeaders.Add(category.Name, header);

        return category;
    }

    public InitCategory AddCategory(string name, Lingo.Color? color) => AddCategory(name, color, Categories.Count);

    /// <summary>
    /// Move an item from one category to another.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="dstCategory">The category to move the item to.</param>
    /// <exception cref="ArgumentException">Thrown if the given category is not in this CategoryList.</exception>
    public void MoveItem(InitItem item, InitCategory dstCategory, bool moveItemAfterAdding, InitItem? replaceItem = null)
    {
        if (!Categories.Contains(dstCategory))
            throw new ArgumentException("The given category was not created by this CategoryList", nameof(dstCategory));
        if (dstCategory.Items.Contains(item)) return;
        
        // if this item already exists in the file, remove it
        if (itemCategories.TryGetValue(item, out InitCategory? srcCategory))
        {
            srcCategory.Items.Remove(item);

            // try to remove it from the file
            int lineIndex = lines.IndexOf(categoryHeaders[srcCategory.Name]) + 1;
            bool success = false;
            while (lineIndex < lines.Count && lines[lineIndex] is not InitCategoryHeader)
            {
                if (lines[lineIndex] == item)
                {
                    success = true;
                    lines.RemoveAt(lineIndex);
                    break;
                }
                lineIndex++;
            }

            if (!success) Log.Warning("Could not remove item {Item} from category {Category} in the init file", item.Name, srcCategory.Name);
        }

        // now, add it to the new category
        if (moveItemAfterAdding && replaceItem != null)
        {
            MoveItemWithinCategory(dstCategory, AddItem(dstCategory, item), replaceItem);
        }
        else
        {
            AddItem(dstCategory, item);
        }
    }

    public void DeleteCategory(InitCategory category)
    {
        if (!Categories.Contains(category))
            throw new ArgumentException("The given category was not created by this CategoryList", nameof(category));
        
        // remove category header as well as the lines of the items contained in the category
        // this is done by removing all lines in the same index until EOF or a different category header is reached.
        int lineIndex = lines.IndexOf(categoryHeaders[category.Name]);
        if (lineIndex == -1) throw new Exception($"Failed to find line index of category '{category.Name}'");
        categoryHeaders.Remove(category.Name);

        // the elusive do...while loop...
        do
        {
            if (lines[lineIndex] is InitItem item && !category.Items.Contains(item))
                throw new Exception("Attempt to remove item in file that isn't in the category item list.");
            lines.RemoveAt(lineIndex);
        }
        while (lineIndex < lines.Count && lines[lineIndex] is not InitCategoryHeader);

        if (!Categories.Remove(category))
            throw new UnreachableException();

        // extra sanity check
        foreach (var item in category.Items)
        {
            if (lines.Contains(item))
                throw new Exception("Removed item in category list still exists in the file!");
        }
    }

    /// <summary>
    /// Updates the name and color of a category header.
    /// If this CategoryList does not support colors in headers, the newColor field
    /// will be ignored. Otherwise, it is mandatory.
    /// </summary>
    public void ChangeCategoryHeader(InitCategory category, string newName, Lingo.Color newColor)
    {
        if (!Categories.Contains(category))
            throw new ArgumentException("The given category was not created by this CategoryList", nameof(category));

        // Replaces the category init line with the new updated information
        var line = categoryHeaders[category.Name];
        int lineIndex = lines.IndexOf(line);
        if (lineIndex == -1) throw new Exception($"Failed to find line index of category '{category.Name}'");

        line.Name = newName;
        category.Name = newName;
        if (isColored)
        {
            line.Color = newColor;
            category.Color = newColor;
        }

        var sb = new StringBuilder();
        sb.Append("-[\"");
        sb.Append(category.Name);
        sb.Append('"');

        if (isColored)
            sb.AppendFormat(", color({0}, {1}, {2})", newColor.R, newColor.G, newColor.B);

        sb.Append(']');

        lines[lineIndex].RawLine = sb.ToString();
    }

    [GeneratedRegex("#nm.*?:\\s*\".*?\"")]
    private static partial Regex NameReplaceRegex();

    public void RenameItem(InitItem item, string newName)
    {
        if (!itemCategories.TryGetValue(item, out _))
            throw new ArgumentException("The given item does not exist within the CategoryList", nameof(item));

        item.Name = newName;
        item.RawLine = NameReplaceRegex().Replace(item.RawLine, "#nm:\"" + newName + "\"");
    }

    public void DeleteItem(InitItem item)
    {
        if (!itemCategories.TryGetValue(item, out InitCategory? category))
            throw new ArgumentException("The given item does not exist within the CategoryList", nameof(item));
        itemCategories.Remove(item);
        
        // remove from category
        if (!category.Items.Remove(item))
            throw new ArgumentException("The given item does not exist within the category", nameof(item));
        
        // remove line
        lines.Remove(item);
    }

    public void MoveItemWithinCategory(InitCategory category, InitItem draggingItem, InitItem replaceItem)
    {
        if (!Categories.Contains(category))
            throw new ArgumentException("The given category was not created by this CategoryList", nameof(category));

        int dragIndex = lines.IndexOf(draggingItem);

        if (dragIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitLineData");

        int replaceIndex = lines.IndexOf(replaceItem);
        lines.RemoveAt(dragIndex);
        int postReplaceIndex = lines.IndexOf(replaceItem);
        if (replaceIndex == -1 || postReplaceIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitLineData");

        // Changes the behavior so the target destination is before or after an item depending if the user drags the item up or down
        int displace = replaceIndex - postReplaceIndex;
        displace += postReplaceIndex;

        if (displace >= 0 && displace < lines.Count)
        {
            lines.Insert(displace, draggingItem);
        }
        else if (displace >= lines.Count)
        {
            lines.Add(draggingItem);
        }

        // Handle the visual category part
        int catDragIndex = category.Items.IndexOf(draggingItem);

        if (catDragIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategory");

        int catReplaceIndex = category.Items.IndexOf(replaceItem);
        category.Items.RemoveAt(catDragIndex);
        int catPostReplaceIndex = category.Items.IndexOf(replaceItem);
        if (catReplaceIndex == -1 || catPostReplaceIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategory");

        displace = catReplaceIndex - catPostReplaceIndex;
        displace += catPostReplaceIndex;

        if (displace >= 0 && displace < category.Items.Count)
        {
            category.Items.Insert(displace, draggingItem);
        }
        else if (displace >= category.Items.Count)
        {
            category.Items.Add(draggingItem);
        }
    }

    public void MoveCategory(InitCategory draggedCategory, InitCategory replaceCategory)
    {
        if (!Categories.Contains(replaceCategory) || !Categories.Contains(draggedCategory))
            throw new ArgumentException("InitCategory does not contain one or more of the categories");

        // This is the tedious part, editing the init lines to follow the category :(
        int dragIndex = lines.IndexOf(categoryHeaders[draggedCategory.Name]);
        if (dragIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategoryHeader");
        List<InitLineData> dragCategoryLines = [];

        int replaceIndex = lines.IndexOf(categoryHeaders[replaceCategory.Name]);

        // Move the index back in case there are comments in the category, so the index starts at the end of the last category
        while (dragIndex > 0 && ((lines[dragIndex] is not InitCategoryHeader && lines[dragIndex] is not InitItem) || lines[dragIndex] == categoryHeaders[draggedCategory.Name]))
            dragIndex--;

        // Move after the last line
        if (dragIndex != 0)
            dragIndex++;

        bool shouldContinue = true;
        // We need to take into account InitIrrelevantLine that are in the middle of a category because some people add them despite not having any purpose
        while (dragIndex < lines.Count && shouldContinue && ((lines[dragIndex] is not InitCategoryHeader) || lines[dragIndex] == categoryHeaders[draggedCategory.Name]))
        {
            // This is our check to see if this InitIrrelevantLine is a break case or not
            int cont = 0;
            while (dragIndex + cont < lines.Count && lines[dragIndex + cont] is not InitItem)
            {
                // So if the next valid line is a InitCategoryHeader, we should escape
                if (lines[dragIndex + cont] is InitCategoryHeader && lines[dragIndex + cont] != categoryHeaders[draggedCategory.Name])
                {
                    shouldContinue = false;
                    break;
                }
                    
                cont++;
            }

            if (shouldContinue)
            {
                // Moving categories should likely also delete dups by default, I guess that's a win
                if (lines[dragIndex] is InitLineData data && !dragCategoryLines.Contains(data))
                {
                    dragCategoryLines.Add(data);
                }
                Log.Information("Removing {string}", lines[dragIndex].RawLine);
                lines.RemoveAt(dragIndex);


                if (dragCategoryLines.Count == 1 && dragCategoryLines[0] is not InitIrrelevantLine)
                {
                    // Insert a blank line at the beginning if the category is at the top
                    dragCategoryLines.Insert(0, new(""));
                }
            }
            else if (dragIndex == 0 && lines[dragIndex] is InitIrrelevantLine && string.IsNullOrEmpty(lines[dragIndex].RawLine))
            {
                // Remove the lingering line at the top if the line is not a comment
                lines.RemoveAt(dragIndex);
            }
        }
        int postReplaceIndex = lines.IndexOf(categoryHeaders[replaceCategory.Name]);
        if (replaceIndex == -1 || postReplaceIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategoryHeader");

        int lineDisplace = replaceIndex - postReplaceIndex;
        lineDisplace += postReplaceIndex;

        bool shouldPlaceAtBottom = false;
        if (lineDisplace > 0 && lineDisplace < lines.Count)
        {
            // Moving a category down doesn't properly index, moving it up does however
            // So if the lineDisplace is positive, it should check for the InitIrrelevantLine at the beginning and then place it before that
            Log.Information("Initial insert line at {string}", lines[lineDisplace].RawLine);

            if (lineDisplace > 0)
            {
                bool displaced = false;
                // We need to check which direction is the correct index
                if (lines[lineDisplace] != categoryHeaders[replaceCategory.Name])
                {
                    displaced = true;
                    while (lineDisplace > 0 && lines[lineDisplace] is not InitCategoryHeader)
                        lineDisplace--;
                }

                // So if the last category head is our category we want to *move* past, lets move to the next category
                if (displaced && lines[lineDisplace] == categoryHeaders[replaceCategory.Name])
                {
                    Log.Information("Last category is our replaced category!");
                    while (lineDisplace < lines.Count && (lines[lineDisplace] is not InitCategoryHeader || lines[lineDisplace] == categoryHeaders[replaceCategory.Name]))
                        lineDisplace++;
                }
            }
        }
        else
        {
            // If nothing else, place them at the bottom so you don't lose them
            shouldPlaceAtBottom = true;
            lines.AddRange(dragCategoryLines);
        }

        if (!shouldPlaceAtBottom)
        {
            // Move the line behind the last category if we are not at the top
            while (lineDisplace > 0 && lines[lineDisplace] is not InitItem)
                lineDisplace--;

            // Move after the last line of the last category
            lineDisplace++;

            Log.Information("Final insert line before {string}", lines[lineDisplace + 1].RawLine);
            lines.InsertRange(lineDisplace, dragCategoryLines);
        }

        // This is the easy part
        int catDragIndex = Categories.IndexOf(draggedCategory);
        if (catDragIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategory");

        int catReplaceIndex = Categories.IndexOf(replaceCategory);
        Categories.RemoveAt(catDragIndex);
        int postCatReplaceIndex = Categories.IndexOf(replaceCategory);

        if (catReplaceIndex == -1 || postCatReplaceIndex == -1)
            throw new ArgumentException("Index of an argument was not found in the InitCategory");

        // Changes the behavior so the target destination is before or after an item depending if the user drags the item up or down
        int displace = catReplaceIndex - postCatReplaceIndex;
        displace += postCatReplaceIndex;

        if (displace >= 0 && displace < Categories.Count && !shouldPlaceAtBottom)
        {
            Categories.Insert(displace, draggedCategory);
        }
        else if (displace >= Categories.Count || shouldPlaceAtBottom)
        {
            Categories.Add(draggedCategory);
        }
    }

    public string GetCategoryHeader(InitCategory header)
    {
        if (categoryHeaders.TryGetValue(header.Name, out var initCategoryHeader))
            return initCategoryHeader.RawLine;

        return "";
    }
}

class AssetManager
{
    private readonly CategoryList TileInit;
    private readonly CategoryList PropInit;
    private readonly CategoryList? MaterialsInit;

    private readonly StandardGraphicsManager graphicsManager1;
    private readonly MaterialGraphicsManager graphicsManager2;

    public enum CategoryListIndex
    {
        Tile, Prop, Materials
    };

    private record GfxManagerAction;
    private record GfxManagerDeleteAction(IAssetGraphicsManager GraphicsManager, string Name, string Directory) : GfxManagerAction;
    private record GfxManagerCopyAction(IAssetGraphicsManager GraphicsManager, string SourceDirectory, string DestDirectory, string Name, bool Expect) : GfxManagerAction;
    private record GfxManagerRenameAction(IAssetGraphicsManager GraphicsManager, string Directory, string OldName, string NewName) : GfxManagerAction;
    private readonly Queue<GfxManagerAction> gfxManagerActionQueue = [];

    class StandardGraphicsManager : IAssetGraphicsManager
    {
        public void CopyGraphics(string name, string srcDir, string destDir, bool expect)
        {
            var pngName = name + ".png";
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

        public void DeleteGraphics(string name, string dir)
        {
            var filePath = AssetGraphicsProvider.GetFilePath(dir, name + ".png");

            if (File.Exists(filePath))
            {
                Log.Information("Delete {FilePath}", filePath);
                File.Delete(filePath);
            }
        }

        public void RenameGraphics(string oldName, string newName, string dir)
        {
            var filePath = AssetGraphicsProvider.GetFilePath(dir, oldName + ".png");

            if (File.Exists(filePath))
            {
                Log.Information("Rename {OldPath} to {NewBaseName}", filePath, newName + ".png");
                File.Move(filePath, Path.Combine(dir, newName + ".png"));
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

        public void CopyGraphics(string name, string srcDir, string destDir, bool expect)
        {
            foreach (var ext in PossibleExtensions)
            {
                var pngName = name + ext;
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

        public void DeleteGraphics(string name, string dir)
        {
            foreach (var ext in PossibleExtensions)
            {
                var filePath = AssetGraphicsProvider.GetFilePath(dir, name + ext);
                
                if (File.Exists(filePath))
                {
                    Log.Information("Delete {FilePath}", filePath);
                    File.Delete(filePath);
                }
            }
        }

        public void RenameGraphics(string oldName, string newName, string dir)
        {
            foreach (var ext in PossibleExtensions)
            {
                var filePath = AssetGraphicsProvider.GetFilePath(dir, oldName + ext);

                if (File.Exists(filePath))
                {
                    Log.Information("Rename {OldPath} to {NewBaseName}", filePath, newName + ext);
                    File.Move(filePath, Path.Combine(dir, newName + ext));
                }
            }
        }
    }
    
    public AssetManager()
    {
        graphicsManager1 = new StandardGraphicsManager();
        graphicsManager2 = new MaterialGraphicsManager();
        var matInitPath = Path.Combine(RainEd.Instance.AssetDataPath, "Materials", "Init.txt");

        TileInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt"), true);
        PropInit = new(Path.Combine(RainEd.Instance.AssetDataPath, "Props", "Init.txt"), true);

        if (File.Exists(matInitPath))
        {
            MaterialsInit = new(matInitPath, false);
        }
        else
        {
            MaterialsInit = null;
        }
    }

    private CategoryList? GetCategoryList(CategoryListIndex index) =>
        index switch
        {
            CategoryListIndex.Tile => TileInit,
            CategoryListIndex.Prop => PropInit,
            CategoryListIndex.Materials => MaterialsInit,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

    private IAssetGraphicsManager GetGraphicsManager(CategoryListIndex index) =>
        index switch
        {
            CategoryListIndex.Tile => graphicsManager1,
            CategoryListIndex.Prop => graphicsManager1,
            CategoryListIndex.Materials => graphicsManager2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    
    public void DeleteCategory(CategoryListIndex index, int categoryIndex)
    {
        var list = GetCategoryList(index)!;
        var category = list.Categories[categoryIndex];
        list.DeleteCategory(category);

        // queue deletion of all the items in the list
        // which will be flushed on commit
        var gfx = GetGraphicsManager(index);
        var dir = Path.GetDirectoryName(list.FilePath)!;
        foreach (var item in category.Items)
        {
            gfxManagerActionQueue.Enqueue(new GfxManagerDeleteAction(gfx, item.Name, dir));
        }
    }

    public void DeleteItem(CategoryListIndex index, int categoryIndex, int assetIndex)
    {
        var list = GetCategoryList(index)!;
        var item = list.Categories[categoryIndex].Items[assetIndex];
        list.DeleteItem(item);

        // queue deletion of item
        var gfx = GetGraphicsManager(index);
        var dir = Path.GetDirectoryName(list.FilePath)!;
        gfxManagerActionQueue.Enqueue(new GfxManagerDeleteAction(gfx, item.Name, dir));
    }

    public void RenameItem(CategoryListIndex index, CategoryList.InitItem item, string newName)
    {
        var oldName = item.Name;
        var list = GetCategoryList(index)!;
        list.RenameItem(item, newName);

        var gfx = GetGraphicsManager(index);
        var dir = Path.Combine(list.FilePath, "..");
        gfxManagerActionQueue.Enqueue(new GfxManagerRenameAction(gfx, dir, oldName, newName));
    }

    public void ChangeCategoryHeader(CategoryListIndex index, int categoryIndex, string newName, Lingo.Color newColor)
    {
        var list = GetCategoryList(index)!;
        var category = list.Categories[categoryIndex];
        list.ChangeCategoryHeader(category, newName, newColor);
    }

    public List<CategoryList.InitCategory> GetCategories(CategoryListIndex index)
    {
        return GetCategoryList(index)!.Categories;
    }

    public void MoveItem(CategoryListIndex index, CategoryList.InitItem? draggingItem, CategoryList.InitCategory destCategory)
    {
        var catList = GetCategoryList(index)!;
        catList.MoveItem(draggingItem!, destCategory, false);
    }

    public void MoveItem(CategoryListIndex index, CategoryList.InitCategory category, CategoryList.InitItem draggingItem, CategoryList.InitItem replaceItem)
    {
        if (!category.Items.Contains(draggingItem))
            throw new ArgumentException("Dragged item was not found in the current category.", draggingItem.Name);
        if (!category.Items.Contains(replaceItem))
            throw new ArgumentException("Replace item was not found in the current category.", replaceItem.Name);

        var catList = GetCategoryList(index)!;
        catList.MoveItemWithinCategory(category, draggingItem, replaceItem);
    }

    public void MoveItem(CategoryListIndex index, CategoryList.InitItem draggingItem, CategoryList.InitCategory destCategory, CategoryList.InitItem? replaceItem)
    {
        //Applies the move item and then also rearranges the item inside of the class
        var catList = GetCategoryList(index)!;
        catList.MoveItem(draggingItem!, destCategory, true, replaceItem);
    }

    public void MoveCategory(CategoryListIndex index, CategoryList.InitCategory draggedCategory, CategoryList.InitCategory dragToCategory)
    {
        var catList = GetCategoryList(index)!;
        catList.MoveCategory(draggedCategory, dragToCategory);
    }

    public async Task? Export(CategoryListIndex index, string filePath)
    {
        string imagePath = RainEd.Instance.AssetDataPath;
        switch (index)
        {
            case CategoryListIndex.Tile:
                imagePath = Path.Combine(imagePath, "Graphics");
                break;

            case CategoryListIndex.Prop:
                imagePath = Path.Combine(imagePath, "Props");
                break;

            case CategoryListIndex.Materials:
                imagePath = Path.Combine(imagePath, "Materials");
                break;
        }
        string[] imageFiles = Directory.GetFiles(imagePath, "*.png");

        var catList = GetCategoryList(index);
        using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
        using var zipInit = new ZipArchive(fileStream, ZipArchiveMode.Create, true);

        var initEntry = zipInit.CreateEntry("Copy_To_Init.txt", CompressionLevel.Optimal);
        using var initStream = new StreamWriter(initEntry.Open());
        foreach (var key in AssetManagerGUI.pendingExportFiles.Keys)
        {
            if (catList.Categories.Contains(key))
            {
                // Implement the init
                await initStream.WriteLineAsync(catList.GetCategoryHeader(key));

                int items = AssetManagerGUI.pendingExportFiles[key].Count;
                foreach (var item in AssetManagerGUI.pendingExportFiles[key])
                {
                    await initStream.WriteLineAsync(item.RawLine);
                }
                await initStream.WriteLineAsync("");
            }
        }
        await initStream.DisposeAsync();

        foreach (var key in AssetManagerGUI.pendingExportFiles.Keys)
        {
            // Then implement images
            foreach (var item in AssetManagerGUI.pendingExportFiles[key])
            {
                await WritePngToZip(imageFiles, item, zipInit);
            }
        }

        AssetManagerGUI.pendingExportFiles.Clear();
    }

    public static async Task WritePngToZip(string[] imageFiles, CategoryList.InitItem item, ZipArchive zipInit)
    {
        if (imageFiles.Any(x => x.Contains(item.Name)))
        {
            string source = imageFiles.Where(x => x.Contains(item.Name)).First();
            if (File.Exists(source))
            {
                var entry = zipInit.CreateEntry(Path.GetFileName(source));
                if (entry is not null)
                {
                    using var imageFile = entry.Open();
                    using var originalImage = new FileStream(source, FileMode.Open, FileAccess.Read);

                    if (originalImage is not null && originalImage.CanRead && imageFile.CanWrite)
                    {
                        await originalImage.CopyToAsync(imageFile);
                    }

                    await imageFile.DisposeAsync();
                }
            }
        }
    }

    public void Commit()
    {
        TileInit.Commit();
        PropInit.Commit();
        MaterialsInit?.Commit();

        foreach (var action in gfxManagerActionQueue)
        {
            switch (action)
            {
                case GfxManagerCopyAction copyAction:
                    copyAction.GraphicsManager.CopyGraphics(copyAction.Name, copyAction.SourceDirectory, copyAction.DestDirectory, copyAction.Expect);
                    break;

                case GfxManagerDeleteAction deleteAction:
                    deleteAction.GraphicsManager.DeleteGraphics(deleteAction.Name, deleteAction.Directory);
                    break;
                
                case GfxManagerRenameAction renameAction:
                    renameAction.GraphicsManager.RenameGraphics(renameAction.OldName, renameAction.NewName, renameAction.Directory);
                    break;
            }
        }
        gfxManagerActionQueue.Clear();
    }

    public delegate Task<PromptResult> PromptRequest(PromptOptions promptState);

    public void Replace(CategoryListIndex initIndex, CategoryList srcInit)
    {
        var destInit = GetCategoryList(initIndex)!;
        var gfxManager = GetGraphicsManager(initIndex);

        if (destInit.CategoryHeaderType != srcInit.CategoryHeaderType)
            throw new MergeException("Incompatible init types");

        Log.Information("Replace {Path}", srcInit.FilePath);
        var dstDir = Path.Combine(destInit.FilePath, "..");
        var srcDir = Path.Combine(srcInit.FilePath, "..");

        // graphic copy/deletion will be deferred so that it doesn't perform
        // redudant file operations
        HashSet<string> itemsToDelete = [];
        HashSet<string> itemsToCopy = [];

        // first, delete all items and categories
        while (destInit.Categories.Count > 0)
        {
            var dstCategory = destInit.Categories[0];
            foreach (var dstItem in dstCategory.Items)
            {
                itemsToDelete.Add(dstItem.Name);
            }

            destInit.DeleteCategory(dstCategory);
        }

        // then, add all items and categories from source
        foreach (var srcCategory in srcInit.Categories)
        {
            var dstCategory = destInit.AddCategory(srcCategory.Name, srcCategory.Color);
            foreach (var srcItem in srcCategory.Items)
            {
                destInit.AddItem(dstCategory, srcItem);

                itemsToCopy.Add(srcItem.Name);
                itemsToDelete.Remove(srcItem.Name);
            }
        }

        // now, act on deferred file copies/deletes
        foreach (var name in itemsToDelete)
        {
            gfxManagerActionQueue.Enqueue(new GfxManagerDeleteAction(
                GraphicsManager: gfxManager,
                Name: name,
                Directory: dstDir
            ));
        }

        foreach (var name in itemsToCopy)
        {
            gfxManagerActionQueue.Enqueue(new GfxManagerCopyAction(
                GraphicsManager: gfxManager,
                SourceDirectory: srcDir,
                DestDirectory: dstDir,
                Name: name,
                Expect: true
            ));
        }
    }
    
    public void Append(CategoryListIndex initIndex, CategoryList srcInit)
    {
        var destInit = GetCategoryList(initIndex)!;
        var gfxManager = GetGraphicsManager(initIndex);

        if (destInit.CategoryHeaderType != srcInit.CategoryHeaderType)
            throw new MergeException("Incompatible init types");

        Log.Information("Append {Path}", srcInit.FilePath);
        var dstDir = Path.Combine(destInit.FilePath, "..");
        var srcDir = Path.Combine(srcInit.FilePath, "..");

        // graphic copy operations will be deferred so that it doesn't perform
        // redudant operations
        HashSet<string> itemsToCopy = [];

        // add all items and categories from source
        foreach (var srcCategory in srcInit.Categories)
        {
            var dstCategory = destInit.GetCategoryByName(srcCategory.Name);

            // don't create new category if appending a category from dest with the same name
            if (dstCategory is not null)
            {
                // throw error on color mismatch
                if (srcCategory.Color != dstCategory.Color)
                        throw new Exception($"Category '{srcCategory.Name}' have different colors from source and destination init files.");
            }
            else // register the category
            {
                dstCategory = destInit.AddCategory(srcCategory.Name, srcCategory.Color);
            }

            foreach (var srcItem in srcCategory.Items)
            {
                destInit.AddItem(dstCategory, srcItem);
                itemsToCopy.Add(srcItem.Name);
            }
        }

        // now, act on deferred file copies
        foreach (var name in itemsToCopy)
        {
            gfxManagerActionQueue.Enqueue(new GfxManagerCopyAction(
                GraphicsManager: gfxManager,
                SourceDirectory: srcDir,
                DestDirectory: dstDir,
                Name: name,
                Expect: true
            ));
        }
    }

    // this will try to import an init such that an item will only appear once in the merged init.
    //
    // simple merge conflict:
    //  - item already exists in dest init. Will simply ask user if they want to overwrite it.
    //
    // complex merge conflict:
    //  - imported item is in a different category than in the dest init
    //  - item is defined multiple times in the source init
    //  will give user a list of choices.
    //
    // if no merge conflicts occur with a certain item, then it will simply find the category in the
    // dest init that the source item belonged in, or create a new one, and append it there.
    public async Task Merge(CategoryListIndex initIndex, CategoryList srcInit, PromptRequest promptReq)
    {
        try
        {
            // automatically overwrite items that are only defined one time
            // (LOOKING AT YOU, "INSIDE HUGE PIPE HORIZONTAL" DEFINED IN BOTH MISC AND LB INTAKE SYSTEM.)
            bool? autoOverwrite = null;

            var destInit = GetCategoryList(initIndex)!;
            var gfxManager = GetGraphicsManager(initIndex);

            if (destInit.CategoryHeaderType != srcInit.CategoryHeaderType)
                throw new MergeException("Incompatible init types");

            Log.Information("Merge {Path}", srcInit.FilePath);
            var dstDir = Path.Combine(destInit.FilePath, "..");
            var srcDir = Path.Combine(srcInit.FilePath, "..");

            HashSet<string> processedItemNames = [];

            foreach (var srcCategory in srcInit.Categories)
            {
                var destCategory = destInit.GetCategoryByName(srcCategory.Name);

                // don't create new category if overwriting a category from dest with the same name
                if (destCategory is not null)
                {
                    // throw error on color mismatch
                    if (srcCategory.Color != destCategory.Color)
                        throw new Exception($"Attempt to merge category '{destCategory.Name}' with a different color");
                }
                else // register the category
                {
                    destCategory = destInit.AddCategory(srcCategory.Name, srcCategory.Color);
                }
                
                foreach (var candidateItem in srcCategory.Items)
                {
                    if (!processedItemNames.Add(candidateItem.Name)) continue;
                    
                    bool doInsert = true;
                    bool expectGraphics = true;

                    var srcItems = srcInit.GetItemsByName(candidateItem.Name);
                    var dstItems = destInit.GetItemsByName(candidateItem.Name);
                    Debug.Assert(srcItems.Length > 0);

                    // check with user if the same tile appears multiple times in the
                    // source
                    var item = candidateItem;
                    if (srcItems.Length > 1)
                    {
                        var options = new string[srcItems.Length];
                        for (int i = 0; i < srcItems.Length; i++)
                        {
                            options[i] = $"Use definition in \"{srcInit.GetCategoryOfItem(srcItems[i]).Name}\"";
                        }
                        var prompt = new PromptOptions(PromptInputMode.Radio, $"Multiple definitions of \"{item.Name}\"", options);
                        var res = (PromptResultRadio) await promptReq(prompt);

                        Debug.Assert(res.Value >= 0);
                        item = srcItems[res.Value];
                    }

                    if (dstItems.Length > 0)
                    {
                        doInsert = false;

                        // if there is more than one item with the same name, or
                        // if the item is defined in a different category,
                        // it is a merge conflict that needs Advanced User Intervention.

                        // (IM TALKING ABOUT YOU INSIDE HUGE PIPE HORIZINTAL WHICH IS IN BOTH MISC AND LB INTAKE SYSTEM)
                        if (dstItems.Length > 1 || destInit.GetCategoryOfItem(dstItems[0]) != destCategory)
                        {
                            Log.Information("Merge conflict with asset '{Name}'", item.Name);
                            
                            // there will be an extra checkbox which will insert the new init
                            // into a new line
                            var options = new string[dstItems.Length + 1];
                            for (int i = 0; i < dstItems.Length; i++)
                            {
                                options[i] = "Overwrite old definition in " + destInit.GetCategoryOfItem(dstItems[i]).Name;
                            }
                            options[^1] = "Add to " + destCategory.Name;

                            var prompt = new PromptOptions(PromptInputMode.Checkbox, $"Merge conflict with asset \"{item.Name}\"\nNew definition is in {destCategory.Name}", options);
                            var res = (PromptResultCheckbox) await promptReq(prompt);
                            
                            for (int i = 0; i < dstItems.Length; i++)
                            {
                                if (res.Values[i])
                                {
                                    Log.Information("Overwrite def in '{Category}'", destInit.GetCategoryOfItem(dstItems[i]).Name);
                                    dstItems[i].RawLine = item.RawLine;
                                    dstItems[i].Name = item.Name;
                                    gfxManagerActionQueue.Enqueue(new GfxManagerCopyAction(
                                        GraphicsManager: gfxManager,
                                        SourceDirectory: srcDir,
                                        DestDirectory: dstDir,
                                        item.Name,
                                        expectGraphics
                                    ));
                                }
                            }

                            if (res.Values[^1])
                            {
                                Log.Information("Add to '{Category}'", destCategory.Name);

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
                                var opt = new PromptOptions(PromptInputMode.YesNo, $"Overwrite \"{item.Name}\"?");
                                
                                var res = (PromptResultYesNo) await promptReq(opt);
                                switch (res.Result)
                                {
                                    case PromptYesNoResult.Yes:
                                        doOverwrite = true;
                                        break;

                                    case PromptYesNoResult.No:
                                        doOverwrite = false;
                                        break;

                                    case PromptYesNoResult.YesToAll:
                                        doOverwrite = true;
                                        autoOverwrite = true;
                                        break;

                                    case PromptYesNoResult.NoToAll:
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
                                Log.Information("Overwrite tile '{TileName}'", item.Name);
                                dstItems[0].RawLine = item.RawLine;
                                dstItems[0].Name = item.Name;
                                gfxManagerActionQueue.Enqueue(new GfxManagerCopyAction(
                                    GraphicsManager: gfxManager,
                                    SourceDirectory: srcDir,
                                    DestDirectory: dstDir,
                                    item.Name,
                                    expectGraphics
                                ));
                            }
                            else
                            {
                                Log.Information("Ignore tile '{TileName}'", item.Name);
                            }
                        }
                    }
                    
                    if (doInsert)
                    {
                        destInit.AddItem(destCategory, item);

                        gfxManagerActionQueue.Enqueue(new GfxManagerCopyAction(
                            GraphicsManager: gfxManager,
                            SourceDirectory: srcDir,
                            DestDirectory: dstDir,
                            item.Name,
                            expectGraphics
                        ));
                    }
                }
            }

            //Log.Information("Writing merge result to {Path}...", FilePath);
            //WriteToFile();
            Log.Information("Merge successful!");
        }
        catch (Exception e)
        {
            Log.Error("Error while merging:\n{Error}", e);
            throw;
        }
    }
}