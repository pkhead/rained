namespace RainEd.Autotiles;

using System.Globalization;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;

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

    public abstract string[] MissingTiles { get; }

    public void CheckMissingTiles()
    {
        var tiles = MissingTiles;
        if (tiles.Length > 0)
        {
            LuaInterface.LogWarning($"missing required tiles for autotile '{Name}': {string.Join(", ", tiles)}");
        }
    }

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
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.LeftDown)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.LeftUp)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.RightDown)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.RightUp)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.Horizontal)) return;
        if (!RainEd.Instance.TileDatabase.HasTile(tileTable.Vertical)) return;
        var ld = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftDown);
        var lu = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftUp);
        var rd = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightDown);
        var ru = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightUp);
        var horiz = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Horizontal);
        var vert = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Vertical);

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

    public static void StandardTilePath(
        PathTileTable tileTable,
        int layer,
        PathSegment[] pathSegments,
        TilePlacementMode modifier
    ) => StandardTilePath(tileTable, layer, pathSegments, modifier, 0, pathSegments.Length);
}

class StandardPathAutotile : Autotile
{
    public PathTileTable TileTable;
    public override string[] MissingTiles { get => []; }

    public StandardPathAutotile(int thickness, int length, string ld, string lu, string rd, string ru, string vert, string horiz)
    {
        Name = "My Autotile";
        Type = AutotileType.Path;
        PathThickness = thickness;
        SegmentLength = length;

        TileTable.LeftDown = ld;
        TileTable.LeftUp = lu;
        TileTable.RightDown = rd;
        TileTable.RightUp = ru;
        TileTable.Vertical = vert;
        TileTable.Horizontal = horiz;

        CheckTiles();
        ProcessSearch();
    }

    private bool IsInvalid(string tileName, TileType tileType)
    {
        return
            !RainEd.Instance.TileDatabase.HasTile(tileName) ||
            !CheckDimensions(RainEd.Instance.TileDatabase.GetTileFromName(tileName), tileType);
    }

    private void CheckTiles()
    {
        CanActivate = !(
            IsInvalid(TileTable.LeftDown, TileType.Turn) ||
            IsInvalid(TileTable.LeftUp, TileType.Turn) ||
            IsInvalid(TileTable.RightDown, TileType.Turn) ||
            IsInvalid(TileTable.RightUp, TileType.Turn) ||
            IsInvalid(TileTable.Vertical, TileType.Vertical) ||
            IsInvalid(TileTable.Horizontal, TileType.Horizontal)
        );
    }

    enum TileType
    {
        Horizontal,
        Vertical,
        Turn
    }

    private bool CheckDimensions(Tiles.Tile tile, TileType tileType)
        => tileType switch
        {
            TileType.Horizontal => tile.Width == SegmentLength && tile.Height == PathThickness,
            TileType.Vertical => tile.Width == PathThickness && tile.Height == SegmentLength,
            TileType.Turn => tile.Width == tile.Height && tile.Width == PathThickness,
            _ => false
        };

    public override void ConfigGui()
    {
        ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 10f);
        
        TileButton(ref TileTable.LeftDown, "Left-Down", TileType.Turn);
        TileButton(ref TileTable.LeftUp, "Left-Up", TileType.Turn);
        TileButton(ref TileTable.RightDown, "Right-Down", TileType.Turn);
        TileButton(ref TileTable.RightUp, "Right-Up", TileType.Turn);
        TileButton(ref TileTable.Vertical, "Vertical", TileType.Vertical);
        TileButton(ref TileTable.Horizontal, "Horizontal", TileType.Horizontal);

        if (ImGui.InputInt("##Thickness", ref PathThickness))
        {
            PathThickness = Math.Clamp(PathThickness, 1, 10);
            CheckTiles();
        }

        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Thickness");

        if (ImGui.InputInt("##Length", ref SegmentLength))
        {
            SegmentLength = Math.Clamp(SegmentLength, 1, 10);
            CheckTiles();
        }

        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Segment Length");

        ImGui.PopItemWidth();
    }

    private void TileButton(ref string tile, string label, TileType tileType)
    {
        bool invalid = IsInvalid(tile, tileType);

        // if invalid, make button red
        if (invalid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.95f, 0.32f, 0.32f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.95f, 0.32f, 0.32f, 0.52f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.95f, 0.32f, 0.32f, 1.00f));
        }

        ImGui.PushID(label);
        if (ImGui.Button(tile, new Vector2(ImGui.GetTextLineHeight() * 10f, 0f)))
        {
            ImGui.OpenPopup("PopupTileSelector");
        }

        if (invalid)
        {
            ImGui.PopStyleColor(3);
        }

        ImGui.SameLine();
        ImGui.Text(label);

        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        if (ImGui.BeginPopup("PopupTileSelector"))
        {
            ImGui.TextUnformatted("Select Tile");
            ImGui.Separator();

            TileSelectorGui();

            if (selectedTile != null)
            {
                if (CheckDimensions(selectedTile, tileType))
                {
                    tile = selectedTile.Name;

                    CanActivate = true;
                    CheckTiles();
                }
                else
                {
                    RainEd.Instance.ShowNotification("Tile dimensions do not match autotile dimensions");
                }

                selectedTile = null;
                ImGui.CloseCurrentPopup();
            }

            if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.PopID();
    }

    public override void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry)
    {
        var modifier = TilePlacementMode.Normal;
        if (geometry)   modifier = TilePlacementMode.Geometry;
        else if (force) modifier = TilePlacementMode.Force;

        StandardTilePath(TileTable, layer, pathSegments, modifier);
    }

    // copied from TileEditorToolbar.cs
    private string searchQuery = "";
    private int selectedTileGroup = 0;
    private Tiles.Tile? selectedTile = null;

    // available groups (available = passes search)
    private readonly List<int> matSearchResults = [];
    private readonly List<int> tileSearchResults = [];

    private void ProcessSearch()
    {
        var tileDb = RainEd.Instance.TileDatabase;
        var matDb = RainEd.Instance.MaterialDatabase;

        tileSearchResults.Clear();
        matSearchResults.Clear();

        // find material groups that have any entires that pass the searchq uery
        for (int i = 0; i < matDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the results
            if (searchQuery == "")
            {
                matSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the materials in this group
            // if there is one material that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < matDb.Categories[i].Materials.Count; j++)
            {
                // this material passes the search, so add this group to the search results
                if (matDb.Categories[i].Materials[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    matSearchResults.Add(i);
                    break;
                }
            }
        }

        // find groups that have any entries that pass the search query
        for (int i = 0; i < tileDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the search query
            if (searchQuery == "")
            {
                tileSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the tiles in this group
            // if there is one tile that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < tileDb.Categories[i].Tiles.Count; j++)
            {
                // this tile passes the search, so add this group to the search results
                if (tileDb.Categories[i].Tiles[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    tileSearchResults.Add(i);
                    break;
                }
            }
        }
    }

    private void TileSelectorGui()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
        {
            ProcessSearch();
        }

        var halfWidth = ImGui.GetTextLineHeight() * 16f;
        var boxHeight = ImGui.GetTextLineHeight() * 25f;
        if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
        {
            var drawList = ImGui.GetWindowDrawList();
            float textHeight = ImGui.GetTextLineHeight();

            foreach (var i in tileSearchResults)
            {
                var group = tileDb.Categories[i];
                var cursor = ImGui.GetCursorScreenPos();

                if (ImGui.Selectable("  " + group.Name, selectedTileGroup == i) || tileSearchResults.Count == 1)
                    selectedTileGroup = i;
                
                drawList.AddRectFilled(
                    p_min: cursor,
                    p_max: cursor + new Vector2(10f, textHeight),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(group.Color.R / 255f, group.Color.G / 255f, group.Color.B / 255f, 1f))
                );
            }
            
            ImGui.EndListBox();
        }
        
        // group listing (effects) list box
        ImGui.SameLine();
        if (ImGui.BeginListBox("##Tiles", new Vector2(halfWidth, boxHeight)))
        {
            var tileList = tileDb.Categories[selectedTileGroup].Tiles;

            for (int i = 0; i < tileList.Count; i++)
            {
                var tile = tileList[i];

                // don't show this prop if it doesn't pass search test
                if (!tile.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                
                if (ImGui.Selectable(tile.Name))
                {
                    selectedTile = tile;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();

                    if (tile.PreviewTexture is not null)
                        rlImGui.Image(tile.PreviewTexture, tile.Category.Color);
                    else
                        rlImGui.ImageSize(RainEd.Instance.PlaceholderTexture, 16, 16);

                    ImGui.EndTooltip();
                }
            }
            
            ImGui.EndListBox();
        }
    }
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
    }

    public List<Autotile> GetAutotilesInCategory(string category)
        => Autotiles[AutotileCategories.IndexOf(category)];

    public List<Autotile> GetAutotilesInCategory(int index)
        => Autotiles[index];
    
    public void CheckMissingTiles()
    {
        foreach (var group in Autotiles)
        {
            foreach (var tile in group)
            {
                tile.CheckMissingTiles();
            }
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
        string? ld = null;
        string? lu = null;
        string? rd = null;
        string? ru = null;
        string? vertical = null;
        string? horizontal = null;

        void SubmitAutotile()
        {
            if (autotileName == "") return;

            if (
                thickness is null ||
                length is null ||
                ld is null ||
                lu is null ||
                rd is null ||
                ru is null ||
                vertical is null ||
                horizontal is null
            )
            {
                RainEd.Logger.Error("Standard autotile {AutotileName} does not have a complete definition!");
                return;
            }

            var autotile = new StandardPathAutotile(
                thickness.Value, length.Value,
                ld, lu, rd, ru,
                vertical, horizontal
            ) {
                Name = autotileName
            };

            AddAutotile(autotile, groupName);

            // reset values
            thickness = null;
            length = null;
            ld = null;
            lu = null;
            rd = null;
            ru = null;
            vertical = null;
            horizontal = null;
            autotileName = "";
            groupName = "Misc";
        }

        foreach (var rawLine in File.ReadLines(Path.Combine(Boot.AppDataPath, "config", "autotiles.txt")))
        {
            lineNo++;

            // skip empty lines
            if (string.IsNullOrWhiteSpace(rawLine)) continue;

            var line = rawLine.Trim();

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

                    case "ld":
                        ld = value;
                        break;

                    case "lu":
                        lu = value;
                        break;

                    case "rd":
                        rd = value;
                        break;

                    case "ru":
                        ru = value;
                        break;

                    case "vertical":
                        vertical = value;
                        break;

                    case "horizontal":
                        horizontal = value;
                        break;
                    
                    // unknown key
                    default:
                        throw new AutotileParseException($"Line {lineNo}: Unknown key '{key}'");
                }
            }
        }

        SubmitAutotile();
    }
}