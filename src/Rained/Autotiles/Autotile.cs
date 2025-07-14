namespace Rained.Autotiles;
using LevelData;
using System.Numerics;

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

[Flags]
enum PathDirection
{
    Right = 1,
    Up = 2,
    Left = 4,
    Down = 8
};

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

    public string CapRight = "";
    public string CapUp = "";
    public string CapLeft = "";
    public string CapDown = "";

    public bool AllowJunctions = false;
    public bool PlaceCaps = false;
}

/// <summary>
/// The base class for autotiles.
/// </summary>
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
    public virtual bool AutoHistory { get => true; }
    public virtual bool ConstrainToSquare { get => false; }

    enum Direction : int
    {
        Right = 0,
        Up = 1,
        Left = 2,
        Down = 3
    };

    readonly struct InstancedPathTileTable
    {
        public readonly Assets.Tile? LeftDown;
        public readonly Assets.Tile? LeftUp;
        public readonly Assets.Tile? RightDown;
        public readonly Assets.Tile? RightUp;
        public readonly Assets.Tile? Vertical;
        public readonly Assets.Tile? Horizontal;

        public readonly Assets.Tile? TRight;
        public readonly Assets.Tile? TLeft;
        public readonly Assets.Tile? TUp;
        public readonly Assets.Tile? TDown;
        public readonly Assets.Tile? XJunct;

        public readonly Assets.Tile? CapRight;
        public readonly Assets.Tile? CapUp;
        public readonly Assets.Tile? CapLeft;
        public readonly Assets.Tile? CapDown;

        public readonly bool AllowJunctions = false;
        public readonly bool PlaceCaps = false;

        public InstancedPathTileTable(PathTileTable tileTable)
        {
            AllowJunctions = tileTable.AllowJunctions;
            PlaceCaps = tileTable.PlaceCaps;

            LeftDown = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftDown);
            LeftUp = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.LeftUp);
            RightDown = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightDown);
            RightUp = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.RightUp);
            Horizontal = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Horizontal);
            Vertical = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.Vertical);

            if (AllowJunctions)
            {
                TRight = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TRight);
                TLeft = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TLeft);
                TUp = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TUp);
                TDown  = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.TDown);
                XJunct = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.XJunct);
            }

            if (PlaceCaps)
            {
                CapRight = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.CapRight);
                CapUp = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.CapUp);
                CapLeft = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.CapLeft);
                CapDown = RainEd.Instance.TileDatabase.GetTileFromName(tileTable.CapDown);
            }
        }
    }

    private static PathDirection GetDirectionsFromTile(InstancedPathTileTable tileTable, Assets.Tile tile)
    {
        if (tile == tileTable.LeftDown)
            return PathDirection.Left | PathDirection.Down;
        if (tile == tileTable.LeftUp)
            return PathDirection.Left | PathDirection.Up;
        if (tile == tileTable.RightDown)
            return PathDirection.Right | PathDirection.Down;
        if (tile == tileTable.RightUp)
            return PathDirection.Right | PathDirection.Up;
        
        if (tile == tileTable.Horizontal)
            return PathDirection.Left | PathDirection.Right;
        if (tile == tileTable.Vertical)
            return PathDirection.Up | PathDirection.Down;
        
        if (tile == tileTable.TRight)
            return PathDirection.Up | PathDirection.Left | PathDirection.Down;
        if (tile == tileTable.TUp)
            return PathDirection.Right | PathDirection.Left | PathDirection.Down;
        if (tile == tileTable.TLeft)
            return PathDirection.Right | PathDirection.Up | PathDirection.Down;
        if (tile == tileTable.TDown)
            return PathDirection.Right | PathDirection.Up | PathDirection.Left;
        
        if (tile == tileTable.XJunct)
            return PathDirection.Right | PathDirection.Up | PathDirection.Left | PathDirection.Down;
        
        if (tile == tileTable.CapRight)
            return PathDirection.Left;
        if (tile == tileTable.CapUp)
            return PathDirection.Down;
        if (tile == tileTable.CapLeft)
            return PathDirection.Right;
        if (tile == tileTable.CapDown)
            return PathDirection.Up;
        
        return 0;
    }

    private static Assets.Tile? GetTileFromDirections(InstancedPathTileTable tiles, PathDirection dirs)
    {
        var right = dirs.HasFlag(PathDirection.Right);
        var up = dirs.HasFlag(PathDirection.Up);
        var left = dirs.HasFlag(PathDirection.Left);
        var down = dirs.HasFlag(PathDirection.Down);

        // obtain the number of connections
        int count = 0;
        if (left) count++;
        if (right) count++;
        if (up) count++;
        if (down) count++;

        // four connections = X intersection
        if (count == 4)
        {
            return tiles.XJunct;
        }

        // three connections = T intersection
        else if (count == 3)
        {
            if (left && right)
            {
                return up ? tiles.TDown : tiles.TUp;
            }
            else if (up && down)
            {
                return right ? tiles.TLeft : tiles.TRight;
            }
        }

        // two connections, normal
        else
        {
            if (left && down)
            {
                return tiles.LeftDown;
            }
            else if (left && up)
            {
                return tiles.LeftUp;
            }
            else if (right && down)
            {
                return tiles.RightDown;
            }
            else if (right && up)
            {
                return tiles.RightUp;
            }

            // cap segments
            else if (tiles.PlaceCaps && count == 1)
            {
                if (right)  return tiles.CapLeft;
                if (up)     return tiles.CapDown;
                if (left)   return tiles.CapRight;
                if (down)   return tiles.CapUp; 
            }

            // straight segments
            else
            {
                if (down || up)
                {
                    return tiles.Vertical;
                }
                else if (right || left)
                {
                    return tiles.Horizontal;
                }

            }
        }

        return null;
    }
    
    /// <summary>
    /// Autotile from a segment list and a tile table. 
    /// </summary>
    /// <param name="tileTable"></param>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="pathSegments"></param>
    /// <param name="modifier"></param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void StandardTilePath(
        PathTileTable tileTable,
        int layer,
        PathSegment[] pathSegments,
        TilePlacementMode modifier,
        int startIndex, int endIndex
    )
    {
        // obtain tile inits from the names
        var tiles = new InstancedPathTileTable(tileTable);

        // abort if at least one tile is invalid
        if (tiles.LeftDown is null) return;
        if (tiles.LeftUp is null) return;
        if (tiles.RightDown is null) return;
        if (tiles.RightUp is null) return;
        if (tiles.Horizontal is null) return;
        if (tiles.Vertical is null) return;

        var tileRenderer = RainEd.Instance.LevelView.Renderer;

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

            var tileDirs = GetDirectionsFromTile(tiles, cell.TileHead);
            if (tileDirs == 0) return false;

            tileDirs |= (PathDirection)((4 << (int)dir) % 15);
            cell.TileHead = GetTileFromDirections(tiles, tileDirs);
            tileRenderer.InvalidateTileHead(newPos.X, newPos.Y, layer);

            return true;
        }

        if (tileTable.AllowJunctions)
        {
            // abort if at least one tile is invalid
            if (tiles.TRight is null) return;
            if (tiles.TLeft is null) return;
            if (tiles.TUp is null) return;
            if (tiles.TDown is null) return;
            if (tiles.XJunct is null) return;

            for (int i = startIndex; i < endIndex; i++)
            {
                var seg = pathSegments[i];
                var segPos = new Vector2i(seg.X, seg.Y);
                PathDirection segDirs = 0;
                if (seg.Right) segDirs |= PathDirection.Right;
                if (seg.Up) segDirs |= PathDirection.Up;
                if (seg.Left) segDirs |= PathDirection.Left;
                if (seg.Down) segDirs |= PathDirection.Down;

                if (!RainEd.Instance.Level.IsInBounds(seg.X, seg.Y)) continue;

                // if there is already a tile here that is in the tile table,
                // replace that tile with the proper intersection
                // instead of placing over it
                {
                    ref var cellHere = ref RainEd.Instance.Level.Layers[layer, seg.X, seg.Y];
                    if (cellHere.TileHead is not null)
                    {
                        var tileDirs = GetDirectionsFromTile(tiles, cellHere.TileHead);
                        if (tileDirs != 0)
                        {
                            tileDirs |= segDirs;
                            cellHere.TileHead = GetTileFromDirections(tiles, tileDirs);
                            tileRenderer.InvalidateTileHead(seg.X, seg.Y, layer);
                            continue;
                        }
                    }
                }

                // join outside tiles
                if (!seg.Right && JoinOutsideTile(segPos, layer, Direction.Right)) segDirs |= PathDirection.Right;
                if (!seg.Up && JoinOutsideTile(segPos, layer, Direction.Up)) segDirs |= PathDirection.Up;
                if (!seg.Left && JoinOutsideTile(segPos, layer, Direction.Left)) segDirs |= PathDirection.Left;
                if (!seg.Down && JoinOutsideTile(segPos, layer, Direction.Down)) segDirs |= PathDirection.Down;

                var tile = GetTileFromDirections(tiles, segDirs)!;
                if (tile is not null)
                    RainEd.Instance.Level.SafePlaceTile(tile, layer, seg.X, seg.Y, modifier);
            }
        }
        else
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                var seg = pathSegments[i];

                PathDirection dirs = 0;
                if (seg.Right) dirs |= PathDirection.Right;
                if (seg.Up) dirs |= PathDirection.Up;
                if (seg.Left) dirs |= PathDirection.Left;
                if (seg.Down) dirs |= PathDirection.Down;

                var tile = GetTileFromDirections(tiles, dirs)!;
                if (tile is not null)
                    RainEd.Instance.Level.SafePlaceTile(tile, layer, seg.X, seg.Y, modifier);
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