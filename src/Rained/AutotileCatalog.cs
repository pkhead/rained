namespace RainEd.Autotiles;

using System.Globalization;
using System.Numerics;
using ImGuiNET;

enum AutotileType
{
    Path, Rect
}

struct PathSegment(int x, int y)
{
    public bool Left = false;
    public bool Right = false;
    public bool Up = false;
    public bool Down = false;
    public int X = x;
    public int Y = y;
}

/// <summary>
/// A table of segment directions associated with tile names.
/// </summary>
struct PathTileTable(string ld, string lu, string rd, string ru, string vert, string horiz)
{
    public string LeftDown = ld;
    public string LeftUp = lu;
    public string RightDown = rd;
    public string RightUp = ru;
    public string Vertical = vert;
    public string Horizontal = horiz;

    public string TRight = "";
    public string TLeft = "";
    public string TUp = "";
    public string TDown = "";
    public string XJunct = "";

    public bool Intersections = false;
}

abstract class Autotile
{
    public bool IsReady = true;
    public bool CanActivate = true;

    public string Name;
    public int PathThickness;
    public int SegmentLength;
    public AutotileType Type;

    public Autotile()
    {
        Name = "(unnamed)";
        Type = AutotileType.Rect;
        PathThickness = 1;
        SegmentLength = 1;
    }

    public Autotile(string name) : this()
    {
        Name = name;
    }

    /// <summary>
    /// Run the rectangle autotiler.
    /// </summary>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="rectMin">The position of the top-left corner of the rectangle.</param>
    /// <param name="rectMax">The position of the bottom-right corner of the rectangle.</param>
    /// <param name="force">If the autotiler should force-place</param>
    /// <param name="geometry">If the autotiler should place geometry.</param>
    public virtual void TileRect(int layer, Vector2i rectMin, Vector2i rectMax, bool force, bool geometry) {}

    /// <summary>
    /// Run the path autotiler.
    /// </summary>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="pathSegments">An array of path segments.</param>
    /// <param name="force">If the autotiler should force-place.</param>
    /// <param name="geometry">If the autotiler should place geometry.</param>
    public virtual void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry) {}

    public virtual void ConfigGui() {}

    public virtual bool AllowIntersections { get => false; }

    enum Direction
    {
        Right, Up, Left, Down
    };

    // C# version of lua autotilePath.
    // I suppose I could just make it so you can call this function directly within Lua,
    // but I don't feel like it. Also, the Lua version is probably a good
    // example on how to use autotiling
    public static void StandardTilePath(
        PathTileTable tileTable,
        int layer,
        PathSegment[] pathSegments,
        TilePlacementMode modifier,
        int startIndex, int endIndex
    )
    {
        // abort if at least one tile is invalid
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.LeftDown)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.LeftUp)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.RightDown)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.RightUp)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.Horizontal)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.Vertical)) return;
        
        // obtain tile inits from the names
        var ld = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftDown);
        var lu = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftUp);
        var rd = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightDown);
        var ru = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightUp);
        var horiz = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Horizontal);
        var vert = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Vertical);
        Tiles.Tile? tRight, tLeft, tUp, tDown, xInt;

        bool JoinOutsideTile(Vector2i pos, int layer, Direction dir)
        {
            Vector2i newPos = pos + dir switch
            {
                Direction.Right => Vector2i.UnitX,
                Direction.Up => -Vector2i.UnitY,
                Direction.Left => -Vector2i.UnitX,
                Direction.Down => Vector2i.UnitY,
                _ => throw new ArgumentOutOfRangeException(nameof(dir))
            };

            if (!RainEd.Instance.Level.IsInBounds(newPos.X, newPos.Y)) return false;

            // check if the cell has a tile that is in the tile table
            // if so, modify that tile to join with the this tile
            // and return true. (assuming that the tile size is 1x1)
            ref var cell = ref RainEd.Instance.Level.Layers[layer, newPos.X, newPos.Y];
            if (cell.TileHead is null) return false;

            // devilish if-else switch to switch it to the proper
            // tile based on the tile it already is.
            // it does follow a logic. however, i am too lazy
            // to actually write it so the logic is more explicit
            // (aka making better code.)
            if (cell.TileHead == horiz)
            {
                cell.TileHead = dir switch
                {
                    Direction.Up => tUp,
                    Direction.Down => tDown,
                    _ => horiz
                };
            }
            else if (cell.TileHead == vert)
            {
                cell.TileHead = dir switch
                {
                    Direction.Right => tRight,
                    Direction.Left => tLeft,
                    _ => vert
                };
            }
            // turns
            else if (cell.TileHead == ld)
            {
                cell.TileHead = dir switch
                {
                    Direction.Left => tUp,
                    Direction.Down => tRight,
                    _ => ld
                };
            }
            else if (cell.TileHead == lu)
            {
                cell.TileHead = dir switch
                {
                    Direction.Left => tDown,
                    Direction.Up => tRight,
                    _ => lu
                };
            }
            else if (cell.TileHead == rd)
            {
                cell.TileHead = dir switch
                {
                    Direction.Right => tUp,
                    Direction.Down => tLeft,
                    _ => rd
                };
            }
            else if (cell.TileHead == ru)
            {
                cell.TileHead = dir switch
                {
                    Direction.Right => tDown,
                    Direction.Up => tLeft,
                    _ => ru
                };
            }
            // t-junctions, which may convert to x junctions
            else if (cell.TileHead == tRight)
            {
                cell.TileHead = dir switch
                {
                    Direction.Left => xInt,
                    _ => tRight 
                };
            }
            else if (cell.TileHead == tUp)
            {
                cell.TileHead = dir switch
                {
                    Direction.Down => xInt,
                    _ => tUp 
                };
            }
            else if (cell.TileHead == tLeft)
            {
                cell.TileHead = dir switch
                {
                    Direction.Right => xInt,
                    _ => tLeft
                };
            }
            else if (cell.TileHead == tDown)
            {
                cell.TileHead = dir switch
                {
                    Direction.Up => xInt,
                    _ => tDown
                };
            }

            return true;
        }

        if (tileTable.Intersections)
        {
            // abort if at least one tile is invalid
            if (!RainEd.Instance.TileDatabase.HasTile(tileTable.TRight)) return;
            if (!RainEd.Instance.TileDatabase.HasTile(tileTable.TLeft)) return;
            if (!RainEd.Instance.TileDatabase.HasTile(tileTable.TUp)) return;
            if (!RainEd.Instance.TileDatabase.HasTile(tileTable.TDown)) return;
            if (!RainEd.Instance.TileDatabase.HasTile(tileTable.XJunct)) return;

            // obtain tile inits from names
            tRight = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TRight);
            tLeft = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TLeft);
            tUp = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TUp);
            tDown = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TDown);
            xInt = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.XJunct);

            Tiles.Tile[]? tileTableList = null;

            for (int i = startIndex; i < endIndex; i++)
            {
                var seg = pathSegments[i];
                var segPos = new Vector2i(seg.X, seg.Y);
                if (!RainEd.Instance.Level.IsInBounds(seg.X, seg.Y)) continue;

                // if there is already a tile here that is in the tile table,
                // then instead of placing a tile, turn the current tile into
                // an X junction
                {
                    ref var cellHere = ref RainEd.Instance.Level.Layers[layer, seg.X, seg.Y];
                    if (cellHere.TileHead is not null)
                    {
                        tileTableList ??= [ld, lu, rd, ru, horiz, vert, tRight, tLeft, tUp, tDown, xInt];
                        if (tileTableList.Contains(cellHere.TileHead))
                        {
                            cellHere.TileHead = xInt;
                            continue;
                        }
                    }
                }

                // join outside tiles
                if (!seg.Right && JoinOutsideTile(segPos, layer, Direction.Right)) seg.Right = true;
                if (!seg.Up && JoinOutsideTile(segPos, layer, Direction.Up)) seg.Up = true;
                if (!seg.Left && JoinOutsideTile(segPos, layer, Direction.Left)) seg.Left = true;
                if (!seg.Down && JoinOutsideTile(segPos, layer, Direction.Down)) seg.Down = true;

                // obtain the number of connections
                int count = 0;
                if (seg.Left) count++;
                if (seg.Right) count++;
                if (seg.Up) count++;
                if (seg.Down) count++;

                // four connections = X intersection
                if (count == 4)
                {
                    RainEd.Instance.Level.SafePlaceTile(xInt, layer, seg.X, seg.Y, modifier);
                }

                // three connections = T intersection
                else if (count == 3)
                {
                    if (seg.Left && seg.Right)
                    {
                        RainEd.Instance.Level.SafePlaceTile(seg.Up ? tDown : tUp, layer, seg.X, seg.Y, modifier);
                    }
                    else if (seg.Up && seg.Down)
                    {
                        RainEd.Instance.Level.SafePlaceTile(seg.Right ? tLeft : tRight, layer, seg.X, seg.Y, modifier);
                    }
                }

                // two connections, normal
                else
                {
                    if (seg.Left && seg.Down)
                    {
                        RainEd.Instance.Level.SafePlaceTile(ld, layer, seg.X, seg.Y, modifier);
                    }
                    else if (seg.Left && seg.Up)
                    {
                        RainEd.Instance.Level.SafePlaceTile(lu, layer, seg.X, seg.Y, modifier);
                    }
                    else if (seg.Right && seg.Down)
                    {
                        RainEd.Instance.Level.SafePlaceTile(rd, layer, seg.X, seg.Y, modifier);
                    }
                    else if (seg.Right && seg.Up)
                    {
                        RainEd.Instance.Level.SafePlaceTile(ru, layer, seg.X, seg.Y, modifier);
                    }

                    // straight
                    else if (seg.Down || seg.Up)
                    {
                        RainEd.Instance.Level.SafePlaceTile(vert, layer, seg.X, seg.Y, modifier);
                    }
                    else if (seg.Right || seg.Left)
                    {
                        RainEd.Instance.Level.SafePlaceTile(horiz, layer, seg.X, seg.Y, modifier);
                    }
                }
            }
        }
        else
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                var seg = pathSegments[i];

                // turns
                if (seg.Left && seg.Down)
                {
                    RainEd.Instance.Level.SafePlaceTile(ld, layer, seg.X, seg.Y, modifier);
                }
                else if (seg.Left && seg.Up)
                {
                    RainEd.Instance.Level.SafePlaceTile(lu, layer, seg.X, seg.Y, modifier);
                }
                else if (seg.Right && seg.Down)
                {
                    RainEd.Instance.Level.SafePlaceTile(rd, layer, seg.X, seg.Y, modifier);
                }
                else if (seg.Right && seg.Up)
                {
                    RainEd.Instance.Level.SafePlaceTile(ru, layer, seg.X, seg.Y, modifier);
                }

                // straight
                else if (seg.Down || seg.Up)
                {
                    RainEd.Instance.Level.SafePlaceTile(vert, layer, seg.X, seg.Y, modifier);
                }
                else if (seg.Right || seg.Left)
                {
                    RainEd.Instance.Level.SafePlaceTile(horiz, layer, seg.X, seg.Y, modifier);
                }
            }
        }
    }

    public static void StandardTilePath(
        PathTileTable tileTable,
        int layer,
        PathSegment[] pathSegments,
        TilePlacementMode modifier
    ) => StandardTilePath(tileTable, layer, pathSegments, modifier, 0, pathSegments.Length);
}

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
                !tileDict.TryGetValue("intersections", out string? intersections) ||
                !tileDict.TryGetValue("tr", out string? tr) ||
                !tileDict.TryGetValue("tu", out string? tu) ||
                !tileDict.TryGetValue("tl", out string? tl) ||
                !tileDict.TryGetValue("td", out string? td) ||
                !tileDict.TryGetValue("x", out string? x)
            )
            {
                RainEd.Logger.Error("Standard autotile {AutotileName} does not have a complete definition!", autotileName);
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

                if (intersections == "true")
                    autotile.TileTable.Intersections = true;
                else if (intersections == "false")
                    autotile.TileTable.Intersections = false;
                else
                    throw new AutotileParseException($"Line {lineNo}: Expected true or false for the value of the key 'intersections', got '{intersections}'.");

                autotile.TileTable.TRight = tr;
                autotile.TileTable.TUp = tu;
                autotile.TileTable.TLeft = tl;
                autotile.TileTable.TDown = td;
                autotile.TileTable.XJunct = x;
                                
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