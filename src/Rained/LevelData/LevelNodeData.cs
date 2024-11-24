namespace Rained.LevelData;
using System.Collections.Generic;
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

    public LevelNodeData(Level level)
    {
        this.level = level;

        // initial update
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                RegisterObject(x, y, 0);
            }
        }

        Update();
    }

    /// <summary>
    /// Register a newly placed geometry object into the NodeData system.
    /// If it is not a relevant shortcut object, it will be ignored.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="layer"></param>
    public void RegisterObject(int x, int y, int layer)
    {
        const LevelObject nodeMask = LevelObject.Entrance | LevelObject.CreatureDen | LevelObject.GarbageWorm | LevelObject.ScavengerHole | LevelObject.Hive;
        if (layer != 0) return; // only objects added to the first layer can be scanned for node creation

        var cell = level.Layers[0,x,y];
        var cellPos = new Vector2i(x, y);

        if ((cell.Objects & nodeMask) != 0)
            shortcutLocsSet.Add(cellPos);
        else
            shortcutLocsSet.Remove(cellPos);
    }

    /// <summary>
    /// Recalculate the nodes list.
    /// </summary>
    public void Update()
    {
        var shortcutLocs = shortcutLocsSet.ToArray();
        var level = RainEd.Instance.Level;
        Array.Sort(shortcutLocs, (Vector2i pos0, Vector2i pos1) =>
        {
            var idx0 = pos0.Y * level.Width + pos0.X;
            var idx1 = pos1.Y * level.Width + pos1.Y;
            return idx0 - idx1;
        });

        nodes.Clear();
        List<(Vector2i pos, NodeType type)> shortcuts = [];
        List<Vector2i> hives = [];
        List<Vector2i> garbageHoles = [];
        var lastHive = false;
        var lastY = 0;

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
            if (y != lastY) lastHive = false;
            var hive = cell.Has(LevelObject.Hive) && cell.Geo == GeoType.Air && level.GetClamped(0, x, y+1).Geo == GeoType.Solid;
            if (hive != lastHive && hive)
                hives.Add(new Vector2i(x, y));
            
            lastHive = hive;
            if (cell.Has(LevelObject.Hive))
            {
                hives.Add(shortcutLoc);
            }

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

            lastY = y;
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
        
        for (int i = 0; i < nodes.Count; i++)
        {
            Log.Debug("{Index}: {NodeType}", i, nodes[i].type);
        }
    }

    private bool ValidShortcut(int x, int y)
    {
        return true;
    }
}