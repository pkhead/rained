namespace RainEd.Autotiles;
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
    public Tiles.Tile LeftDown = RainEd.Instance.TileDatabase.GetTileFromName(ld);
    public Tiles.Tile LeftUp = RainEd.Instance.TileDatabase.GetTileFromName(lu);
    public Tiles.Tile RightDown = RainEd.Instance.TileDatabase.GetTileFromName(rd);
    public Tiles.Tile RightUp = RainEd.Instance.TileDatabase.GetTileFromName(ru);
    public Tiles.Tile Vertical = RainEd.Instance.TileDatabase.GetTileFromName(vert);
    public Tiles.Tile Horizontal = RainEd.Instance.TileDatabase.GetTileFromName(horiz);
}

abstract class Autotile
{
    public bool IsReady = true;

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
        for (int i = startIndex; i < endIndex; i++)
        {
            var seg = pathSegments[i];

            // turns
            if (seg.Left && seg.Down)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.LeftDown, layer, seg.X, seg.Y, modifier);
            }
            else if (seg.Left && seg.Up)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.LeftUp, layer, seg.X, seg.Y, modifier);
            }
            else if (seg.Right && seg.Down)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.RightDown, layer, seg.X, seg.Y, modifier);
            }
            else if (seg.Right && seg.Up)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.RightUp, layer, seg.X, seg.Y, modifier);
            }

            // straight
            else if (seg.Down || seg.Up)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.Vertical, layer, seg.X, seg.Y, modifier);
            }
            else if (seg.Right || seg.Left)
            {
                RainEd.Instance.Level.SafePlaceTile(tileTable.Horizontal, layer, seg.X, seg.Y, modifier);
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

    public StandardPathAutotile()
    {
        Name = "My Autotile";
        Type = AutotileType.Path;

        TileTable.LeftDown = RainEd.Instance.TileDatabase.GetTileFromName("Pipe WS");
        TileTable.LeftUp = RainEd.Instance.TileDatabase.GetTileFromName("Pipe WN");
        TileTable.RightDown = RainEd.Instance.TileDatabase.GetTileFromName("Pipe ES");
        TileTable.RightUp = RainEd.Instance.TileDatabase.GetTileFromName("Pipe EN");
        TileTable.Vertical = RainEd.Instance.TileDatabase.GetTileFromName("Vertical Pipe");
        TileTable.Horizontal = RainEd.Instance.TileDatabase.GetTileFromName("Horizontal Pipe");

        ProcessSearch();
    }

    enum TileType
    {
        Horizontal,
        Vertical,
        Turn
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
            PathThickness = Math.Clamp(PathThickness, 1, 10);
        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Thickness");

        if (ImGui.InputInt("##Length", ref SegmentLength))
            SegmentLength = Math.Clamp(SegmentLength, 1, 10);
        ImGui.SameLine(); // WHY DOESN'T THE TEXT ALIGN!!!
        ImGui.Text("Segment Length");

        ImGui.PopItemWidth();
    }

    private void TileButton(ref Tiles.Tile tile, string label, TileType tileType)
    {
        ImGui.PushID(label);
        if (ImGui.Button(tile.Name, new Vector2(ImGui.GetTextLineHeight() * 10f, 0f)))
        {
            ImGui.OpenPopup("PopupTileSelector");
        }

        ImGui.SameLine();
        ImGui.Text(label);

        if (ImGui.BeginPopup("PopupTileSelector"))
        {
            TileSelectorGui();

            if (selectedTile != null)
            {
                // determine that the tile dimensions are valid given
                // the tile direction type
                bool dimValid = tileType switch
                {
                    TileType.Horizontal => selectedTile.Width == SegmentLength && selectedTile.Height == PathThickness,
                    TileType.Vertical => selectedTile.Width == PathThickness && selectedTile.Height == SegmentLength,
                    TileType.Turn => selectedTile.Width == selectedTile.Height && selectedTile.Width == PathThickness,
                    _ => false
                };
                
                if (dimValid)
                {
                    tile = selectedTile;
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

    public override string[] MissingTiles { get => []; }

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
}