namespace RainEd.Autotiles;

using System.Globalization;
using ImGuiNET;

[Serializable]
public class AutotileParseException : Exception
{
    public AutotileParseException() { }
    public AutotileParseException(string message) : base(message) { }
    public AutotileParseException(string message, Exception inner) : base(message, inner) { }
}

class AutotileCatalog
{
    public readonly List<string> AutotileCategories = ["Misc"];
    private readonly List<List<Autotile>> Autotiles = [[]];
    private readonly Dictionary<Autotile, string> autotileCategoryMap = [];

    private static readonly string ConfigPath = Path.Combine(Boot.AppDataPath, "config", "autotiles.txt");

    /// <summary>
    /// Adds the given autotile to the catalog.
    /// </summary>
    /// <param name="autotile">The autotile to add.</param>
    /// <param name="category">The category to add it in.</param>
    public void AddAutotile(Autotile autotile, string category = "Misc")
    {   
        var catIndex = AutotileCategories.IndexOf(category);
        if (catIndex == -1)
        {
            catIndex = AutotileCategories.Count;
            AutotileCategories.Add(category);
            Autotiles.Add([]);
        }

        Autotiles[catIndex].Add(autotile);
        autotileCategoryMap.Add(autotile, category);
    }

    public List<Autotile> GetAutotilesInCategory(string category)
        => Autotiles[AutotileCategories.IndexOf(category)];

    public List<Autotile> GetAutotilesInCategory(int index)
        => Autotiles[index];
    
    public bool HasAutotile(Autotile autotile)
        => autotileCategoryMap.ContainsKey(autotile);

    public string GetCategoryNameOf(Autotile autotile)
    {
        return autotileCategoryMap[autotile];
    }

    public void RemoveAutotile(Autotile autotile)
    {
        var catIndex = AutotileCategories.IndexOf(autotileCategoryMap[autotile]);

        // remove references to the autotile
        Autotiles[catIndex].Remove(autotile);
        autotileCategoryMap.Remove(autotile);

        // if there are no autotiles left in the category,
        // remove that also.
        if (Autotiles[catIndex].Count == 0)
        {
            Autotiles.RemoveAt(catIndex);
            AutotileCategories.RemoveAt(catIndex);
        }
    }

    public AutotileCatalog()
    {
        // read from config/autotiles.txt
        var lineNo = 0;

        string autotileName = "";
        string groupName = "Misc";

        int? thickness = null;
        int? length = null;
        Dictionary<string, string> tileDict = [];

        void SubmitAutotile()
        {
            if (autotileName == "") return;
            
            if (
                //thickness is null ||
                //length is null ||
                !tileDict.TryGetValue("ld", out string? ld) ||
                !tileDict.TryGetValue("lu", out string? lu) ||
                !tileDict.TryGetValue("rd", out string? rd) ||
                !tileDict.TryGetValue("ru", out string? ru) ||
                !tileDict.TryGetValue("vertical", out string? vertical) ||
                !tileDict.TryGetValue("horizontal", out string? horizontal) ||
                !tileDict.TryGetValue("allowJunctions", out string? allowJunctions) ||
                !tileDict.TryGetValue("tr", out string? tr) ||
                !tileDict.TryGetValue("tu", out string? tu) ||
                !tileDict.TryGetValue("tl", out string? tl) ||
                !tileDict.TryGetValue("td", out string? td) ||
                !tileDict.TryGetValue("x", out string? x) ||
                !tileDict.TryGetValue("placeCaps", out string? placeCaps) ||
                !tileDict.TryGetValue("capRight", out string? capRight) ||
                !tileDict.TryGetValue("capUp", out string? capUp) ||
                !tileDict.TryGetValue("capLeft", out string? capLeft) ||
                !tileDict.TryGetValue("capDown", out string? capDown)
            )
            {
                Log.Error("Standard autotile {AutotileName} does not have a complete definition!", autotileName);
            }
            else
            {
                var autotile = new StandardPathAutotile(
                    1, 1,
                    ld, lu, rd, ru,
                    vertical, horizontal
                ) {
                    Name = autotileName
                };

                if (allowJunctions == "true")
                    autotile.TileTable.AllowJunctions = true;
                else if (allowJunctions == "false")
                    autotile.TileTable.AllowJunctions = false;
                else
                    throw new AutotileParseException($"Line {lineNo}: Expected true or false for the value of the key 'allowJunctions', got '{allowJunctions}'.");

                if (placeCaps == "true")
                    autotile.TileTable.PlaceCaps = true;
                else if (placeCaps == "false")
                    autotile.TileTable.PlaceCaps = false;
                else
                    throw new AutotileParseException($"Line {lineNo}: Expected true or false for the value of the key 'placeCaps', got '{placeCaps}'");
                
                autotile.TileTable.TRight = tr;
                autotile.TileTable.TUp = tu;
                autotile.TileTable.TLeft = tl;
                autotile.TileTable.TDown = td;
                autotile.TileTable.XJunct = x;
                autotile.TileTable.CapRight = capRight;
                autotile.TileTable.CapUp = capUp;
                autotile.TileTable.CapLeft = capLeft;
                autotile.TileTable.CapDown = capDown;
                                
                AddAutotile(autotile, groupName);
            }

            // reset values
            thickness = null;
            length = null;
            tileDict.Clear();
            autotileName = "";
            groupName = "Misc";
        }

        foreach (var line in File.ReadLines(ConfigPath))
        {
            lineNo++;

            // skip empty lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            // ignore comments
            if (line[0] == '#') continue;

            // read header
            if (line[0] == '[')
            {
                // header lines always end with a closing bracket
                if (line[^1] != ']')
                    throw new AutotileParseException($"Line {lineNo}: Expected ']', got newline.");
                
                SubmitAutotile();
                
                var sepIndex = line.IndexOf(':');
                autotileName = "(unknown)";
                groupName = "Misc";

                // no colon separator was found
                if (sepIndex == -1)
                {
                    autotileName = line[1..^1];
                }
                // colon separator was found
                else
                {
                    autotileName = line[1..sepIndex];
                    groupName = line[(sepIndex+1)..^1];
                }
            }

            // normal line
            else
            {
                var sepIndex = line.IndexOf('=');
                if (sepIndex == -1) throw new AutotileParseException($"Line {lineNo}: Expected '=', got newline.");

                var key = line[0..sepIndex];
                var value = line[(sepIndex+1)..];

                switch (key)
                {
                    case "thickness":
                        thickness = int.Parse(value, CultureInfo.InvariantCulture);
                        break;

                    case "length":
                        length = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    
                    // other key/value pair
                    default:
                        tileDict.Add(key, value);
                        break;
                }
            }
        }

        SubmitAutotile();
    }

    private string createName = "My Autotile";
    private string createCategory = "Misc";
    private string createError = "";
    private Autotile? renameTarget = null;

    private void CheckCreateError()
    {
        // check that there are no characters in the names that would mess up saving
        // (just newline characters and colons. closing brackets are actually fine.)
        if (createName.Contains('\n') || createName.Contains('\r') || createName.Contains(':'))
        {
            createError = "Invalid character in name!";
            return;
        }

        if (createCategory.Contains('\n') || createCategory.Contains('\r'))
        {
            createError = "Invalid character in name!";
            return;
        }

        // check if autotile in the same category does not already exist
        var catIndex = AutotileCategories.IndexOf(createCategory);
        if (catIndex >= 0)
        {
            foreach (var t in Autotiles[catIndex])
                if (t.Name == createName)
                {
                    createError = "An autotile with the same name and category already exists!";
                    break;
                }
        }
    }

    private void ShowCreateError()
    {
        if (createError != "" && !ImGui.IsPopupOpen("Error"))
        {
            ImGui.OpenPopup("Error");
        }

        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGuiExt.BeginPopupModal("Error", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text(createError);

            ImGui.Separator();
            if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
            {
                createError = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Open the Create Autotile popup.
    /// </summary>
    public void OpenCreatePopup()
    {
        ImGui.OpenPopup("Create Autotile");
        createName = "My Autotile";
        createCategory = "Misc";
    }

    /// <summary>
    /// Open the Rename Autotile popup.
    /// </summary>
    /// <param name="autotile">The autotile to rename</param>
    public void OpenRenamePopup(Autotile autotile)
    {
        ImGui.OpenPopup("Rename Autotile");
        createName = autotile.Name;
        createCategory = GetCategoryNameOf(autotile);
        renameTarget = autotile;
    }

    /// <summary>
    /// Render the Create Autotile popup.
    /// </summary>
    public void RenderCreatePopup()
    {
        bool p_open = true;
        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("Create Autotile", ref p_open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 12.0f);
            ImGui.InputText("Name", ref createName, 128);
            ImGui.InputText("Category", ref createCategory, 128);
            ImGui.PopItemWidth();

            ImGui.Separator();
            if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btnPressed))
            {
                if (btnPressed == 0 && !string.IsNullOrWhiteSpace(createName) && !string.IsNullOrWhiteSpace(createCategory)) // OK
                {
                    CheckCreateError();

                    // if there was no error, create the autotile
                    if (createError == "")
                    {
                        var autotile = new StandardPathAutotile(1, 1, "Pipe WS", "Pipe WN", "Pipe ES", "Pipe EN", "Vertical Pipe", "Horizontal Pipe")
                        {
                            Name = createName
                        };

                        AddAutotile(autotile, createCategory);
                        ImGui.CloseCurrentPopup();
                    }
                }

                else if (btnPressed == 1) // cancel
                {
                    ImGui.CloseCurrentPopup();
                }
            }

            // show any errors
            ShowCreateError();

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Render the Rename Autotile popup.
    /// </summary>
    public void RenderRenamePopup()
    {
        bool p_open = true;
        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("Rename Autotile", ref p_open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 12.0f);
            ImGui.InputText("Name", ref createName, 128);
            ImGui.InputText("Category", ref createCategory, 128);
            ImGui.PopItemWidth();

            ImGui.Separator();
            if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btnPressed))
            {
                if (btnPressed == 0 && !string.IsNullOrWhiteSpace(createName) && !string.IsNullOrWhiteSpace(createCategory)) // OK
                {
                    CheckCreateError();

                    // if there was no error, rename the autotile
                    if (createError == "")
                    {
                        // record renaming to config file
                        if (renameTarget is StandardPathAutotile std)
                        {
                            RenameStandard(std, createName, createCategory);
                        }

                        renameTarget!.Name = createName;

                        // move categories if needed
                        var oldCategory = GetCategoryNameOf(renameTarget);
                        if (oldCategory != createCategory)
                        {
                            RemoveAutotile(renameTarget);
                            AddAutotile(renameTarget, createCategory);
                        }

                        ImGui.CloseCurrentPopup();

                        renameTarget = null;
                    }
                }

                else if (btnPressed == 1) // cancel
                {
                    ImGui.CloseCurrentPopup();
                    renameTarget = null;
                }
            }

            // show any errors
            ShowCreateError();

            ImGui.EndPopup();
        }

        if (!p_open)
            renameTarget = null;
    }

    /// <summary>
    /// Save user-created autotiles.
    /// </summary>
    public void SaveConfig()
    {
        var fileLines = new List<string>(File.ReadAllLines(ConfigPath));

        foreach (var category in Autotiles)
        {
            foreach (var genericAutotile in category)
            {
                if (genericAutotile is StandardPathAutotile autotile)
                {
                    autotile.Save(fileLines);
                }
            }
        }

        File.WriteAllLines(ConfigPath, fileLines);
    }

    private void RenameStandard(StandardPathAutotile autotile, string newName, string newCategory)
    {
        var fileLines = new List<string>(File.ReadAllLines(ConfigPath));
        autotile.Rename(fileLines, newName, newCategory);
        File.WriteAllLines(ConfigPath, fileLines);
    }

    public void DeleteStandard(StandardPathAutotile autotile)
    {
        var fileLines = new List<string>(File.ReadAllLines(ConfigPath));
        autotile.Delete(fileLines);
        File.WriteAllLines(ConfigPath, fileLines);

        RemoveAutotile(autotile);
    }
}