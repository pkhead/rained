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
        const LevelObject nodeMask = LevelObject.Entrance | LevelObject.CreatureDen | LevelObject.GarbageWorm | LevelObject.ScavengerHole | LevelObject.Hive;

        var cell = level.Layers[0,x,y];
        var cellPos = new Vector2i(x, y);

        if ((cell.Objects & nodeMask) != 0)
            shortcutLocsSet.Add(cellPos);
        else
            shortcutLocsSet.Remove(cellPos);
        
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
            if ((cell.Objects & (LevelObject.Entrance | LevelObject.CreatureDen | LevelObject.ScavengerHole)) != 0)
            {
                NodeType nodeType = NodeType.Exit;
                bool valid = true;

                switch (cell.Objects)
                {
                    case LevelObject.Entrance:
                        nodeType = NodeType.Exit;
                        break;
                    
                    case LevelObject.CreatureDen:
                        nodeType = NodeType.Den;
                        break;
                    
                    case LevelObject.ScavengerHole:
                        nodeType = NodeType.RegionTransportation;
                        break;
                    
                    default:
                        valid = false;
                        break;
                }
                
                if (valid && ValidShortcut(x, y))
                    shortcuts.Add((shortcutLoc, nodeType));
            }
        }

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
            int x = side == 0 ? 0 : level.Width-1;

            var lastSolid = true;
            for (int y = 0; y < level.Height; y++)
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
            for (int x = 0; x < level.Width; x++)
            {
                var solid = level.Layers[0,x,0].Geo == GeoType.Solid;
                if (solid != lastSolid && !solid)
                    nodes.Add((new Vector2i(x, 0), NodeType.SkyExit));
                
                lastSolid = solid;
            }
        }

        // sea exits
        if (level.HasWater)
        {
            var lastSolid = true;
            for (int x = 0; x < level.Width; x++)
            {
                var solid = level.Layers[0, x, level.Height-1].Geo == GeoType.Solid;
                if (solid != lastSolid && !solid)
                    nodes.Add((new Vector2i(x, level.Height-1), NodeType.SeaExit));
                
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

    // TODO: implement this
    private bool ValidShortcut(int x, int y)
    {
        return true;
    }

    private LevelCell GetCellOrDefault(int x, int y)
    {
        if (x < 0 || y < 0 || x >= level.Width || y >= level.Height)
            return new LevelCell();
        
        return level.Layers[0,y,x];
    }

    private bool IsHive(int x, int y)
    {
        ref var cell = ref level.Layers[0,x,y];
        return cell.Has(LevelObject.Hive) && cell.Geo == GeoType.Air && level.GetClamped(0, x, y+1).Geo == GeoType.Solid;
    }
}