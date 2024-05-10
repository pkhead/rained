namespace RainEd.Autotiles;

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

    public bool AllowJunctions = false;
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

        if (tileTable.AllowJunctions)
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