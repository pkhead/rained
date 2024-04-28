namespace RainEd.Autotiles;
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
    }

    public override void ConfigGui()
    {
        var btnSize = new Vector2(ImGui.GetTextLineHeight() * 10f, 0f);

        ImGui.Button(TileTable.LeftDown.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Left-Down");
        
        ImGui.Button(TileTable.LeftUp.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Left-Up");

        ImGui.Button(TileTable.RightDown.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Right-Down");
        
        ImGui.Button(TileTable.RightUp.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Right-Up");
        
        ImGui.Button(TileTable.Vertical.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Vertical");

        ImGui.Button(TileTable.Horizontal.Name, btnSize);
        ImGui.SameLine();
        ImGui.Text("Horizontal");
    }

    public override string[] MissingTiles { get => []; }

    public override void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry)
    {
        var modifier = TilePlacementMode.Normal;
        if (geometry)   modifier = TilePlacementMode.Geometry;
        else if (force) modifier = TilePlacementMode.Force;

        StandardTilePath(TileTable, layer, pathSegments, modifier);
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