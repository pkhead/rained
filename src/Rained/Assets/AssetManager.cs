using Drizzle.Lingo.Runtime;
using Rained.EditorGui;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Linq;
using static Silk.NET.Core.Native.WinString;


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

    internal List<InitCategory> Categories = [];
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
                        Name: (string) list.values[0],
                        Color: (Lingo.Color) list.values[1]
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
                        Name: (string) data.fields["nm"]
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

	public void UpdateCategoryInit(InitCategory category)
	{
		if (!Categories.Contains(category))
			throw new ArgumentException("The given category was not created by this CategoryList", nameof(category));

		// Replaces the category init line with the new updated information
		int lineIndex = lines.IndexOf(categoryHeaders[category.Name]);
		if (lineIndex == -1) throw new Exception($"Failed to find line index of category '{category.Name}'");
		lines[lineIndex].RawLine = $"-[\"{category.Name}\"{(category.Color == null ? "" : $", color({category.Color.Value.R}, {category.Color.Value.G}, {category.Color.Value.B})")}]";
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

	internal void MoveItemWithinCategory(InitCategory category, InitItem draggingItem, InitItem replaceItem)
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

	internal void MoveCategory(InitCategory draggedCategory, InitCategory replaceCategory)
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

	internal string GetCategoryHeader(InitCategory header)
	{
		if (categoryHeaders.TryGetValue(header.Name, out var initCategoryHeader))
			return initCategoryHeader.RawLine;

		return "";
	}

	//public delegate Task<PromptResult> PromptRequest(PromptOptions promptState);

	//public async Task Merge(string otherPath, PromptRequest promptOverwrite)
	//{
	//	try
	//	{
	//		// automatically overwrite items that are only defined one time
	//		// (LOOKING AT YOU, "INSIDE HUGE PIPE HORIZONTAL" DEFINED IN BOTH MISC AND LB INTAKE SYSTEM.)
	//		bool? autoOverwrite = null;

	//		Log.Information("Merge {Path}", otherPath);
	//		var parentDir = Path.Combine(FilePath, "..");
	//		var otherDir = Path.Combine(otherPath, "..");

	//		var parser = new Lingo.LingoParser();
	//		InitCategory? targetCategory = null;

	//		int lineAccum = 2;
	//		int otherLineNo = -1;

	//		int FlushLineAccum(int insert)
	//		{
	//			int oldAccum = lineAccum;

	//			for (int i = 0; i < lineAccum; i++)
	//			{
	//				Lines.Insert(insert, "");
	//				parsedLines.Insert(insert, new EmptyLine());
	//			}
	//			lineAccum = 0;

	//			return oldAccum;
	//		}

	//		foreach (var line in File.ReadLines(otherPath))
	//		{
	//			otherLineNo++;
	//			int lineNo = Lines.Count;

	//			if (string.IsNullOrWhiteSpace(line))
	//			{
	//				if (targetCategory is not null) lineAccum++;
	//				continue;
	//			}

	//			// category
	//			if (line[0] == '-')
	//			{
	//				InitCategory? category = null;

	//				if (isColored)
	//				{
	//					var list = parser.Read(line[1..]) as Lingo.LinearList;

	//					if (list is not null)
	//					{
	//						category = new InitCategory(
	//							Name: (string)list[0],
	//							Color: (Lingo.Color)list[1]
	//						);
	//					}
	//					else
	//					{
	//						Log.Information("Ignore malformed category header at '{Line}'", line);
	//					}
	//				}
	//				else
	//				{
	//					category = new InitCategory(line[1..], null);
	//				}

	//				// don't write new line if overwriting a category with the same name and color
	//				if (category is not null)
	//				{
	//					InitCategory? oldCategory = null;
	//					if ((oldCategory = GetCategory(category.Name)) is not null)
	//					{
	//						// throw error on color mismatch
	//						if (oldCategory.Color != category.Color)
	//							throw new Exception($"Category '{category.Name}' already exists!");

	//						targetCategory = oldCategory;

	//						// this will insert a newline when adding
	//						// new tiles to the category
	//						lineAccum = 1;
	//					}
	//					else
	//					{
	//						// register the category
	//						FlushLineAccum(Lines.Count);
	//						Lines.Add(line);
	//						parsedLines.Add(category);
	//						Categories.Add(category);
	//						targetCategory = category;
	//					}
	//				}
	//			}

	//			// item
	//			else
	//			{
	//				if (targetCategory is null)
	//				{
	//					throw new Exception("Category definition expected, got item definition");
	//				}

	//				var data = parser.Read(line) as Lingo.PropertyList;

	//				if (data is not null)
	//				{
	//					var init = new InitItem(
	//						RawLine: line,
	//						Name: (string)data["nm"]
	//					);

	//					bool doInsert = true;
	//					bool expectGraphics = true;

	//					// check with user if tile with same name already exists
	//					if (ItemDictionary.TryGetValue(init.Name, out List<InitData>? initItems))
	//					{
	//						doInsert = false;

	//						// if there is more than one item with the same name, or
	//						// if the item is defined in a different category,
	//						// it is a merge conflict that needs Advanced User Intervention.

	//						// (IM TALKING ABOUT YOU INSIDE HUGE PIPE HORIZINTAL WHICH IS IN BOTH MISC AND LB INTAKE SYSTEM)
	//						if (initItems.Count > 1 || initItems[0].Category != targetCategory)
	//						{
	//							Log.Information("Merge conflict with asset '{Name}'", init.Name);

	//							// there will be an extra checkbox which will insert the new init
	//							// into a new line
	//							var options = new string[initItems.Count + 1];
	//							for (int i = 0; i < initItems.Count; i++)
	//							{
	//								options[i] = "Overwrite old definition in " + initItems[i].Category.Name;
	//							}
	//							options[^1] = "Add to " + targetCategory.Name;

	//							var prompt = new PromptOptions($"Merge conflict with asset \"{init.Name}\"\nNew definition is in {targetCategory.Name}", options);
	//							await promptOverwrite(prompt);

	//							for (int i = 0; i < initItems.Count; i++)
	//							{
	//								if (prompt.CheckboxValues[i])
	//								{
	//									Log.Information("Overwrite def in '{Category}'", initItems[0].Category);
	//									ReplaceInit(initItems[i], init);
	//								}
	//							}

	//							if (prompt.CheckboxValues[^1])
	//							{
	//								Log.Information("Add to '{Category}'", targetCategory);

	//								doInsert = true;
	//								expectGraphics = false;
	//							}
	//						}

	//						// simple merge conflict - item is only defined once and
	//						// both new and old items are in the same category
	//						else
	//						{
	//							bool doOverwrite;

	//							if (autoOverwrite.HasValue)
	//							{
	//								doOverwrite = autoOverwrite.Value;
	//							}
	//							else
	//							{
	//								// ask the user if they want to overwrite
	//								var opt = new PromptOptions($"Overwrite \"{init.Name}\"?");

	//								switch (await promptOverwrite(opt))
	//								{
	//									case PromptResult.Yes:
	//										doOverwrite = true;
	//										break;

	//									case PromptResult.No:
	//										doOverwrite = false;
	//										break;

	//									case PromptResult.YesToAll:
	//										doOverwrite = true;
	//										autoOverwrite = true;
	//										break;

	//									case PromptResult.NoToAll:
	//										doOverwrite = false;
	//										autoOverwrite = false;
	//										break;

	//									default:
	//										throw new Exception();
	//								}
	//							}

	//							if (doOverwrite)
	//							{
	//								// go ahead and overwrite
	//								Log.Information("Overwrite tile '{TileName}'", init.Name);
	//								ReplaceInit(initItems[0], init);

	//								graphicsManager.CopyGraphics(init, otherDir, parentDir, false);
	//							}
	//							else
	//							{
	//								Log.Information("Ignore tile '{TileName}'", init.Name);
	//							}
	//						}
	//					}

	//					if (doInsert)
	//					{
	//						// insert the new item at the end of the category tile list
	//						// by inserting it where the next init category definition is,
	//						// or the EOF
	//						int i = parsedLines.IndexOf(targetCategory);
	//						if (i == -1) throw new Exception("Failed to find line number of category");

	//						// find where the next category starts (or the EOF)
	//						int insertLoc = -1;

	//						i++;
	//						while (i < parsedLines.Count && parsedLines[i] is not InitCategory)
	//						{
	//							if (parsedLines[i] is not EmptyLine)
	//								insertLoc = i;

	//							i++;
	//						}

	//						// if at EOF
	//						if (i == Lines.Count)
	//						{
	//							i = Lines.Count;
	//							insertLoc = i;
	//						}

	//						else
	//						{
	//							// prefer to insert it after the last item def in this category
	//							if (insertLoc >= 0)
	//							{
	//								insertLoc++;
	//							}
	//							else
	//							{
	//								insertLoc = i;
	//							}
	//						}

	//						// flush line accumulator
	//						insertLoc += FlushLineAccum(insertLoc);

	//						// insert item
	//						Lines.Insert(insertLoc, line);
	//						parsedLines.Insert(insertLoc, init);
	//						AddInit(init, targetCategory);

	//						graphicsManager.CopyGraphics(init, otherDir, parentDir, expectGraphics);
	//					}
	//				}
	//			}
	//		}

	//		Log.Information("Writing merge result to {Path}...", FilePath);
	//		WriteToFile();
	//		Log.Information("Merge successful!");
	//	}
	//	catch (Exception e)
	//	{
	//		Log.Error("Error while merging:\n{Error}", e);
	//		throw;
	//	}
	//}
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

	public void UpdateCategory(CategoryListIndex index, int categoryIndex)
	{
		var list = GetCategoryList(index)!;
		var category = list.Categories[categoryIndex];
		list.UpdateCategoryInit(category);
	}

	public List<CategoryList.InitCategory> GetCategories(CategoryListIndex index)
    {
        return GetCategoryList(index)!.Categories;
    }

    public void Commit()
    {
        TileInit.Commit();
        PropInit.Commit();
        MaterialsInit?.Commit();

        /*foreach (var action in gfxManagerActionQueue)
        {
            if (action is GfxManagerCopyAction copyAction)
                copyAction.GraphicsManager.CopyGraphics(copyAction.Name, copyAction.SourceDirectory, copyAction.DestDirectory, copyAction.Expect);
            
            else if (action is GfxManagerDeleteAction delAction)
                delAction.GraphicsManager.DeleteGraphics(delAction.Name, delAction.Directory);
        }*/
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

	internal void MoveItem(CategoryListIndex index, CategoryList.InitItem? draggingItem, CategoryList.InitCategory destCategory)
	{
		var catList = GetCategoryList(index)!;
		catList.MoveItem(draggingItem!, destCategory, false);
	}

	internal void MoveItem(CategoryListIndex index, CategoryList.InitCategory category, CategoryList.InitItem draggingItem, CategoryList.InitItem replaceItem)
	{
		if (!category.Items.Contains(draggingItem))
			throw new ArgumentException("Dragged item was not found in the current category.", draggingItem.Name);
		if (!category.Items.Contains(replaceItem))
			throw new ArgumentException("Replace item was not found in the current category.", replaceItem.Name);

		var catList = GetCategoryList(index)!;
		catList.MoveItemWithinCategory(category, draggingItem, replaceItem);
	}

	internal void MoveItem(CategoryListIndex index, CategoryList.InitItem draggingItem, CategoryList.InitCategory destCategory, CategoryList.InitItem? replaceItem)
	{
		//Applies the move item and then also rearranges the item inside of the class
		var catList = GetCategoryList(index)!;
		catList.MoveItem(draggingItem!, destCategory, true, replaceItem);
	}

	internal void MoveCategory(CategoryListIndex index, CategoryList.InitCategory draggedCategory, CategoryList.InitCategory dragToCategory)
	{
		var catList = GetCategoryList(index)!;
		catList.MoveCategory(draggedCategory, dragToCategory);
	}

	internal async Task? Export(CategoryListIndex index, string filePath)
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

	/*public async Task Merge(CategoryListIndex initIndex, CategoryList srcInit, PromptRequest promptOverwrite)
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
                
                foreach (var item in srcCategory.Items)
                {
                    if (!processedItemNames.Add(item.Name)) continue;
                    
                    bool doInsert = true;
                    bool expectGraphics = true;

                    // check with user if the same tile appears multiple times in the
                    // source
                    var srcItems = srcInit.GetItemsByName(item.Name);
                    if (srcInit.Length > 0)
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

                            var prompt = new PromptOptions($"Merge conflict with asset \"{item.Name}\"\nNew definition is in {destCategory.Name}", options);
                            await promptOverwrite(prompt);
                            
                            for (int i = 0; i < dstItems.Length; i++)
                            {
                                if (prompt.CheckboxValues[i])
                                {
                                    Log.Information("Overwrite def in '{Category}'", destInit.GetCategoryOfItem(dstItems[0]).Name);
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
                            }

                            if (prompt.CheckboxValues[^1])
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
                                var opt = new PromptOptions($"Overwrite \"{item.Name}\"?");
                                
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
    }*/
}