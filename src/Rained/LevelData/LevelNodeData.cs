namespace Rained.LevelData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

enum NodeType
{
    Exit,
    Den,
    RegionTransportation,

    /// <summary>
    /// Exit/entrance for Miros Birds.
    /// </summary>
    SideExit,

    /// <summary>
    /// Exit/entrance for vultures and Miro's Vultures.
    /// </summary>
    SkyExit,

    /// <summary>
    /// Exit/Entrance for Leviathans
    /// </summary>
    SeaExit,

    BatHive,

    GarbageHoles,
}

/// <summary>
/// Class holding node data for a level.
/// </summary>
class LevelNodeData
{
    private readonly HashSet<Vector2i> shortcutLocsSet = [];
    private readonly Level level;

    private readonly List<(Vector2i pos, NodeType type)> nodes = [];
    public IEnumerable<(Vector2i pos, NodeType type)> Nodes => nodes;
    private bool dirty = false;
    private bool hadWater;

    public LevelNodeData(Level level)
    {
        this.level = level;

        // initial update
        Reset();
    }

    public void Reset()
    {
        shortcutLocsSet.Clear();

        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                InvalidateCell(x, y);
            }
        }

        Update();
        hadWater = level.HasWater;
    }

    /// <summary>
    /// Marks a cell on the first layer as dirty.
    /// </summary>
    /// <param name="x">The X position of the cell.</param>
    /// <param name="y">The Y position of the cell.</param>
    public void InvalidateCell(int x, int y)
    {
        if (!level.IsInBorder(x, y)) return;

        ref var cell = ref level.Layers[0,x,y];
        if (cell.Geo == GeoType.ShortcutEntrance || (cell.Objects & (LevelObject.GarbageWorm | LevelObject.Hive)) != 0)
            shortcutLocsSet.Add(new Vector2i(x, y));
        else
            shortcutLocsSet.Remove(new Vector2i(x, y));
        
        dirty = true;
    }

    /// <summary>
    /// Recalculate the nodes list if needed.
    /// </summary>
    public void Update()
    {
        if (hadWater != level.HasWater) dirty = true;
        hadWater = level.HasWater;
        
        if (!dirty) return;

        var stopwatch = new Stopwatch();

        var shortcutLocs = shortcutLocsSet.ToArray();
        Array.Sort(shortcutLocs, (Vector2i pos0, Vector2i pos1) =>
        {
            var idx0 = pos0.Y * level.Width + pos0.X;
            var idx1 = pos1.Y * level.Width + pos1.X;
            return idx0 - idx1;
        });

        nodes.Clear();
        List<(Vector2i pos, NodeType type)> shortcuts = [];
        List<Vector2i> hives = [];
        List<Vector2i> garbageHoles = [];

        // fill shortcuts, hives, and garbageHole lists
        foreach (var shortcutLoc in shortcutLocs)
        {
            var x = shortcutLoc.X;
            var y = shortcutLoc.Y;
            ref var cell = ref level.Layers[0,x,y];

            // garbage worm detection
            if (cell.Has(LevelObject.GarbageWorm))
            {
                garbageHoles.Add(shortcutLoc);
            }

            // hive detection
            if (IsHive(x, y) && !IsHive(x-1, y))
                hives.Add(new Vector2i(x, y));

            // shortcut nodes
            if (cell.Geo == GeoType.ShortcutEntrance && ProcessShortcut(x, y, out Vector2i shortcutPos, out var nodeType))
                shortcuts.Add((shortcutPos, nodeType));
        }

        // sort shortcuts by position
        shortcuts.Sort(((Vector2i pos, NodeType type) s0, (Vector2i pos, NodeType type) s1) =>
        {
            var idx0 = s0.pos.Y * level.Width + s0.pos.X;
            var idx1 = s1.pos.Y * level.Width + s1.pos.X;
            return idx0 - idx1;
        });

        // room node priority:
        //   1. room exits (white)
        //   2. creature dens (pink)
        //   3. region transportation (black)
        //   4. side exits (beige-green)
        //   5. sky exit (cyan)
        //   6. sea exit (deep blue)
        //   7. hives (green)
        //   8. garbage hole (orange)

        // room exits
        foreach (var node in shortcuts)
        {
            if (node.type == NodeType.Exit)
                nodes.Add(node);
        }

        // creature dens
        foreach (var node in shortcuts)
        {
            if (node.type == NodeType.Den)
                nodes.Add(node);
        }

        // region transporatation (scav holes)
        foreach (var node in shortcuts)
        {
            if (node.type == NodeType.RegionTransportation)
                nodes.Add(node);
        }

        // side exits
        for (int side = 0; side < 2; side++)
        {
            int x = side == 0 ? level.BufferTilesLeft : level.Width - level.BufferTilesRight - 1;

            var lastSolid = true;
            for (int y = level.BufferTilesTop; y < level.Height - level.BufferTilesBot; y++)
            {
                var solid = level.Layers[0,x,y].Geo == GeoType.Solid;
                if (solid != lastSolid && !solid)
                    nodes.Add((new Vector2i(x, y), NodeType.SideExit));
                
                lastSolid = solid;
            }
        }

        // sky exits
        {
            var lastSolid = true;
            var y = level.BufferTilesTop;
            for (int x = level.BufferTilesLeft; x < level.Width - level.BufferTilesRight; x++)
            {
                var solid = level.Layers[0,x,y].Geo == GeoType.Solid;
                if (solid != lastSolid && !solid)
                    nodes.Add((new Vector2i(x, y), NodeType.SkyExit));
                
                lastSolid = solid;
            }
        }

        // sea exits
        if (level.HasWater)
        {
            var lastSolid = true;
            var y = level.Height - level.BufferTilesBot - 1;
            for (int x = level.BufferTilesLeft; x < level.Width - level.BufferTilesRight; x++)
            {
                var solid = level.Layers[0, x, y].Geo == GeoType.Solid;
                if (solid != lastSolid && !solid)
                    nodes.Add((new Vector2i(x, y), NodeType.SeaExit));
                
                lastSolid = solid;
            }
        }

        // hives
        foreach (var pos in hives)
        {
            nodes.Add((pos, NodeType.BatHive));
        }

        // garbage hole
        foreach (var pos in garbageHoles)
        {
            nodes.Add((pos, NodeType.GarbageHoles));
        }

        stopwatch.Stop();
        Log.Debug("nodedata update: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);

        dirty = false;
    }

    enum ShortcutContinuationType {
        None, Straight, Curve
    }

    private bool ProcessShortcut(int x, int y, out Vector2i nodePos, out NodeType type)
    {        
        type = NodeType.Exit;
        nodePos = Vector2i.Zero;
        if (!IsValidShortcutEntrance(x, y, out int _, out int _)) return false;
        
        int lastX = x;
        int lastY = y;
        int steps = 0;
        var visitedCurves = new HashSet<Vector2i>();

        while (true)
        {
            if (++steps > 1000)
            {
                Log.Warning("Shortcut tracer steps exceeded!");
                break;
            }

            int oldX = x;
            int oldY = y;
            var continueType = NextShortcutPosition(ref x, ref y, lastX, lastY);
            if (continueType == ShortcutContinuationType.None) break;

            // loop detection process -- flags certain cells as visited if there was a curve there.
            // if it reaches that cell again as a curve, an infinite loop would occur so we need
            // to stop processing there.
            if (continueType == ShortcutContinuationType.Curve)
            {
                if (visitedCurves.Contains(new Vector2i(oldX, oldY)))
                {
                    Log.Debug("Loop detected, escaping...");
                    return false;
                }
                else
                {
                    visitedCurves.Add(new Vector2i(oldX, oldY));
                }
            }

            lastX = oldX;
            lastY = oldY;

            ref var cell = ref level.Layers[0,x,y];
            
            if (cell.Objects.HasFlag(LevelObject.Entrance))
            {
                type = NodeType.Exit;
                nodePos = new Vector2i(x, y);
                return true;
            }
            else if (cell.Objects.HasFlag(LevelObject.CreatureDen))
            {
                type = NodeType.Den;
                nodePos = new Vector2i(x, y);
                return true;
            }
            else if (cell.Objects.HasFlag(LevelObject.ScavengerHole))
            {
                type = NodeType.RegionTransportation;
                nodePos = new Vector2i(x, y);
                return true;
            }
        }
        
        return false;
    }

    private ShortcutContinuationType NextShortcutPosition(ref int x, ref int y, int lastX, int lastY)
    {
        // implementation referenced from decompiled game code
        // ShortcutHandler.NextShortcutPosition(IntVector2 pos, IntVector2 lastPos, Room room)
        ReadOnlySpan<(int x, int y)> fourDirections = [
            (-1, 0),
            (0, -1),
            (1, 0),
            (0, 1)
        ];

        var dx = x - lastX;
        var dy = y - lastY;
        Debug.Assert((dx == 0 && dy == 0) || Math.Abs(dx) + Math.Abs(dy) == 1);

        // first check if the shortcut can continue in a straight path
        // if so, pick that position
        if ((dx != 0 || dy != 0) && IsShortcut(GetCellOrDefault(x + dx, y + dy)))
        {
            x += dx;
            y += dy;
            return ShortcutContinuationType.Straight;
        }
        else // the shortcut curves here, find out where to go
        {
            for (int i = 0; i < 4; i++)
            {
                var dir = fourDirections[i];
                if (!(dir.x == -dx && dir.y == -dy) && IsShortcut(GetCellOrDefault(x + dir.x, y + dir.y)))
                {
                    x += dir.x;
                    y += dir.y;
                    return ShortcutContinuationType.Curve;
                }
            }

            return ShortcutContinuationType.None;
        }
    }

    private const LevelObject objectMask = LevelObject.Shortcut | LevelObject.Entrance | LevelObject.CreatureDen | LevelObject.WhackAMoleHole | LevelObject.ScavengerHole;

    private static bool IsShortcut(in LevelCell cell)
    {
        return cell.Geo == GeoType.ShortcutEntrance || (cell.Objects & objectMask) != 0;
    }

    private LevelCell GetCellOrDefault(int x, int y)
    {
        if (x < level.BufferTilesLeft || y < level.BufferTilesTop || x >= level.Width - level.BufferTilesRight || y >= level.Height - level.BufferTilesBot)
            return new LevelCell();
        
        return level.Layers[0,x,y];
    }

    private bool IsValidShortcutEntrance(int x, int y, out int dx, out int dy)
    {
        dx = 0;
        dy = 0;
        
        var layers = level.Layers;
        if (layers[0,x,y].Geo != GeoType.ShortcutEntrance) return false;

        // check if all four corners are solid blocks
        if (layers[0,x-1,y-1].Geo != GeoType.Solid) return false;
        if (layers[0,x+1,y-1].Geo != GeoType.Solid) return false;
        if (layers[0,x+1,y+1].Geo != GeoType.Solid) return false;
        if (layers[0,x-1,y+1].Geo != GeoType.Solid) return false;
        
        // check if all but one of 4 neighbors are solid
        int numSolid = 0;
        if (layers[0,x-1,y].Geo == GeoType.Solid) numSolid++;
        if (layers[0,x+1,y].Geo == GeoType.Solid) numSolid++;
        if (layers[0,x,y-1].Geo == GeoType.Solid) numSolid++;
        if (layers[0,x,y+1].Geo == GeoType.Solid) numSolid++;
        if (numSolid != 3) return false;


        int sdx = 0;
        int sdy = 0;
        int shortcutCount = 0;

        void checkDir(int pdx, int pdy)
        {
            if ((layers[0, x+pdx, y+pdy].Geo is GeoType.Solid or GeoType.Platform) && (layers[0, x-pdx, y-pdy].Objects & objectMask) != 0)
            {
                sdx = pdx;
                sdy = pdy;
                shortcutCount++;
            }
        }

        checkDir(-1, 0);
        checkDir(1, 0);
        if (shortcutCount > 1) return false;
        checkDir(0, -1);
        if (shortcutCount > 1) return false;
        checkDir(0, 1);
        if (shortcutCount > 1) return false;

        dx = sdx;
        dy = sdy;
        return true;
    }

    private bool IsHive(int x, int y)
    {
        ref var cell = ref level.Layers[0,x,y];
        return cell.Has(LevelObject.Hive) && cell.Geo == GeoType.Air && level.GetBorderClamped(0, x, y+1).Geo == GeoType.Solid;
    }
}