using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Rained.WorldData;

enum NodeType
{
    Exit,
    Den,

    /// <summary>
    /// I don't know what this is.
    /// </summary>
    RegionTransportation,

    /// <summary>
    /// Exit/entrance for Miros Birds.
    /// </summary>
    SideExit,

    /// <summary>
    /// Exit/entrance for vultures.
    /// </summary>
    SkyExit,

    /// <summary>
    /// Exit/entrance for Goliaths (i forgot).
    /// </summary>
    SeaExit,

    BatHive,
    GarbageHoles
}

[Flags]
enum CellPlayFeature : ushort
{ 
    VerticalBeam = 1,
    HorizontalBeam = 2,
    Shortcut = 4,
    RoomExit = 8,
    DragonDen = 16,
    WhackAMoleHole = 32,
    ScavengerHole = 64,
    Hive = 128,
    GarbageHole = 256,
    WallBehind = 512,
    Waterfall = 1024,
    WormGrass = 2048,
}

enum CellPlayGeo : byte
{
    Air = 0,
    Wall = 1,
    Slope = 2,
    Platform = 3,
    ShortcutEntrance = 4
}

record struct RoomNode(Vector2i Position, NodeType Type);
record struct LevelPlayCell(CellPlayGeo Geo, CellPlayFeature Features);

/// <summary>
/// Loaded representation of a rendered level .txt file.
/// </summary>
class Room
{
    public readonly int Width;
    public readonly int Height;
    public readonly int WaterLevel;
    public readonly bool IsWaterInFront;
    public readonly string Name;

    public bool HasWater => WaterLevel >= 0;

    public LevelPlayCell[,] Cells;
    public RoomNode[] Nodes;

    private RlManaged.Texture2D? _texture = null;
    public RlManaged.Texture2D Texture;

    /// <summary>
    /// Load a room.
    /// </summary>
    /// <param name="fileName">Path the room's .txt file.</param>
    public Room(string fileName)
    {
        var lines = File.ReadAllLines(fileName);
        Name = Path.GetFileNameWithoutExtension(fileName);
        Log.Information("Load {RoomName}", Name);

        var stopwatch = Stopwatch.StartNew();

        // line 2:
        // <Width>*<Height>|<WaterLevel>|<IsWaterInFront>
        {
            var data = lines[1].Split('|');

            // read room dimensions
            var roomDim = data[0].Split('*');
            Width = int.Parse(roomDim[0], CultureInfo.InvariantCulture);
            Height = int.Parse(roomDim[1], CultureInfo.InvariantCulture);

            // read water data
            WaterLevel = -1;
            IsWaterInFront = false;
            if (data.Length > 1)
            {
                WaterLevel = int.Parse(data[1], CultureInfo.InvariantCulture);
                IsWaterInFront = int.Parse(data[2], CultureInfo.InvariantCulture) == 1;
            }
        }

        // line 12: play matrix
        {
            var line = lines[11];
            Cells = new LevelPlayCell[Height, Width];

            var cursor = 0;
            StringBuilder cellStr = new();
            List<string> cellData = [];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cellStr.Clear();
                    cellData.Clear();

                    while (true)
                    {
                        if (line[cursor] == ',')
                        {
                            cellData.Add(cellStr.ToString());
                            cellStr.Clear();
                        }
                        else if (line[cursor] == '|')
                        {
                            cellData.Add(cellStr.ToString());
                            break;
                        }
                        else
                        {
                            cellStr.Append(line[cursor]);
                        }

                        cursor++;
                    }
                    cursor++;

                    var geo = (CellPlayGeo) int.Parse(cellData[0], CultureInfo.InvariantCulture);
                    CellPlayFeature features = 0;

                    for (int j = 1; j < cellData.Count; j++)
                    {
                        features |= int.Parse(cellData[j], CultureInfo.InvariantCulture) switch
                        {
                            1 => CellPlayFeature.VerticalBeam,
                            2 => CellPlayFeature.HorizontalBeam,
                            3 => CellPlayFeature.Shortcut,
                            4 => CellPlayFeature.RoomExit,
                            5 => CellPlayFeature.DragonDen,
                            6 => CellPlayFeature.WallBehind,
                            8 => CellPlayFeature.Waterfall,
                            7 => CellPlayFeature.Hive,
                            9 => CellPlayFeature.WhackAMoleHole,
                            12 => CellPlayFeature.ScavengerHole,
                            10 => CellPlayFeature.GarbageHole,
                            11 => CellPlayFeature.WormGrass,
                            _ => throw new Exception("Play matrix has invalid value " + cellData[j])
                        };
                    }

                    Cells[y,x] = new LevelPlayCell(geo, features);
                }
            }
        }

        Log.Debug("Read play matrix: {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        // parse nodes
        List<RoomNode> nodes = [];

        // first: collect shortcuts, hives, and garbage holes
        List<RoomNode> shortcuts = [];
        List<Vector2i> hives = [];
        List<Vector2i> garbageHoles = [];

        for (int y = 0; y < Height; y++)
        {
            var lastHive = false;

            for (int x = 0; x < Width; x++)
            {
                var cell = GetCell(x, y);

                if (cell.Geo == CellPlayGeo.ShortcutEntrance)
                {
                    if (FollowShortcut(x, y, out NodeType nodeType))
                        shortcuts.Add(new RoomNode(new Vector2i(x, y), nodeType));
                }

                if (cell.Features.HasFlag(CellPlayFeature.GarbageHole))
                    garbageHoles.Add(new Vector2i(x, y));

                var hive = cell.Features.HasFlag(CellPlayFeature.Hive);
                if (hive != lastHive && hive)
                    hives.Add(new Vector2i(x, y));
                
                lastHive = hive;
            }
        }

        Log.Debug("Parse play matrix: {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

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
            if (node.Type == NodeType.Exit)
                nodes.Add(node);
        }

        // creature dens
        foreach (var node in shortcuts)
        {
            if (node.Type == NodeType.Den)
                nodes.Add(node);
        }

        // region transporatation (scav holes)
        foreach (var node in shortcuts)
        {
            if (node.Type == NodeType.RegionTransportation)
                nodes.Add(node);
        }

        // side exits
        for (int side = 0; side < 2; side++)
        {
            int x = side == 0 ? 0 : Width-1;

            var lastSolid = true;
            for (int y = 0; y < Height; y++)
            {
                var solid = GetCell(x, y).Geo == CellPlayGeo.Wall;
                if (solid != lastSolid && !solid)
                    nodes.Add(new RoomNode(new Vector2i(x, y), NodeType.SideExit));
                
                lastSolid = solid;
            }
        }

        // sky exits
        {
            var lastSolid = true;
            for (int x = 0; x < Width; x++)
            {
                var solid = GetCell(x, 0).Geo == CellPlayGeo.Wall;
                if (solid != lastSolid && !solid)
                    nodes.Add(new RoomNode(new Vector2i(x, 0), NodeType.SkyExit));
                
                lastSolid = solid;
            }
        }

        // sea exits
        if (HasWater)
        {
            var lastSolid = true;
            for (int x = 0; x < Width; x++)
            {
                var solid = GetCell(x, Height-1).Geo == CellPlayGeo.Wall;
                if (solid != lastSolid && !solid)
                    nodes.Add(new RoomNode(new Vector2i(x, Height-1), NodeType.SeaExit));
                
                lastSolid = solid;
            }
        }

        // hives
        foreach (var pos in hives)
        {
            nodes.Add(new RoomNode(pos, NodeType.BatHive));
        }

        // garbage hole
        foreach (var pos in garbageHoles)
        {
            nodes.Add(new RoomNode(pos, NodeType.GarbageHoles));
        }

        Nodes = [..nodes];

        stopwatch.Stop();
        Log.Debug("Node process: {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        for (int i = 0; i < Nodes.Length; i++)
        {
            Log.Debug("{Index}: {NodeType}", i, Nodes[i].Type);
        }
    }

    private bool FollowShortcut(int x, int y, out NodeType type)
    {
        type = NodeType.Exit;

        Debug.Assert(GetCell(x, y).Geo == CellPlayGeo.ShortcutEntrance);

        // get initial direction of shortcut
        Vector2i dir = Vector2i.Zero;

        if (GetCellOrDefault(x-1, y).Geo is CellPlayGeo.Air or CellPlayGeo.Platform)
        {
            dir = new Vector2i(1, 0);
        }
        else if (GetCellOrDefault(x+1, y).Geo is CellPlayGeo.Air or CellPlayGeo.Platform)
        {
            dir = new Vector2i(-1, 0);
        }
        else if (GetCellOrDefault(x, y-1).Geo is CellPlayGeo.Air or CellPlayGeo.Platform)
        {
            dir = new Vector2i(0, 1);
        }
        else if (GetCellOrDefault(x, y+1).Geo is CellPlayGeo.Air or CellPlayGeo.Platform)
        {
            dir = new Vector2i(0, -1);
        }
        else
        {
            throw new Exception("Invalid shortcut entrance?");
        }

        x += dir.X;
        y += dir.Y;

        Span<Vector2i> possibleDirs = stackalloc Vector2i[4];
        int possibleDirs_i;

        // follow shortcut
        while (true)
        {
            var cell = GetCellOrDefault(x, y);
            
            if (cell.Features.HasFlag(CellPlayFeature.DragonDen))
            {
                type = NodeType.Den;
                return true;
            }
            else if (cell.Features.HasFlag(CellPlayFeature.RoomExit) && cell.Geo != CellPlayGeo.ShortcutEntrance)
            {
                type = NodeType.Exit;
                return true;
            }
            else if (cell.Features.HasFlag(CellPlayFeature.ScavengerHole))
            {
                type = NodeType.RegionTransportation;
                return true;
            }
            else if (cell.Features.HasFlag(CellPlayFeature.Shortcut))
            {
                // first case: either straight line or four-way intersection
                if (IsShortcut(GetCellOrDefault(x + dir.X, y + dir.Y).Features))
                {
                    // dir remains unchanged
                }

                // second case: curve
                else
                {
                    // get next direction
                    possibleDirs_i = 0;

                    // TODO: test shortcut direction ordering
                    if (IsShortcut(GetCellOrDefault(x-1, y).Features))
                        possibleDirs[possibleDirs_i++] = new Vector2i(-1, 0);

                    if (IsShortcut(GetCellOrDefault(x+1, y).Features))
                        possibleDirs[possibleDirs_i++] = new Vector2i(1, 0);

                    if (IsShortcut(GetCellOrDefault(x, y-1).Features))
                        possibleDirs[possibleDirs_i++] = new Vector2i(0, -1);

                    if (IsShortcut(GetCellOrDefault(x, y+1).Features))
                        possibleDirs[possibleDirs_i++] = new Vector2i(0, 1);

                    // TODO: test t-junction
                    if (possibleDirs_i == 0)
                    {
                        Log.UserLogger.Error("{RoomName}: Invalid shortcut", Name);
                        return false;
                    }

                    for (int i = 0; i < possibleDirs_i; i++)
                    {
                        if (-dir != possibleDirs[i])
                        {
                            dir = possibleDirs[i];
                            break;
                        }
                    }
                }
            }
            else return false;

            x += dir.X;
            y += dir.Y;
        }
    }

    public LevelPlayCell GetCell(int x, int y) => Cells[y,x];
    public LevelPlayCell GetCellOrDefault(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return new LevelPlayCell(CellPlayGeo.Air, 0);
        
        return Cells[y,x];
    }

    private static bool IsShortcut(CellPlayFeature feature)
    {
        const CellPlayFeature Mask =
            CellPlayFeature.Shortcut |
            CellPlayFeature.RoomExit |
            CellPlayFeature.DragonDen |
            CellPlayFeature.WhackAMoleHole |
            CellPlayFeature.ScavengerHole;
        
        return (feature & Mask) != 0;
    }

    static readonly Glib.Color BackgroundColor = Glib.Color.FromRGBA(153, 153, 153);
    static readonly Glib.Color WallBehindColor = Glib.Color.FromRGBA(128, 128, 128);
    static readonly Glib.Color WallColor = Glib.Color.FromRGBA(77, 77, 77);
    static readonly Glib.Color FeatureColor = Glib.Color.FromRGBA(128, 77, 77);
    static readonly Glib.Color ShortcutColor = Glib.Color.FromRGBA(0, 100, 255);
    static readonly Glib.Color WaterColor = Glib.Color.FromRGBA(0, 0, 255, 127);
    
    static readonly Glib.Color[] NodeColors = [
        // exit
        Glib.Color.FromRGBA(255, 255, 255),

        // den
        Glib.Color.FromRGBA(255, 0, 255),

        // region transportation
        Glib.Color.FromRGBA(52, 50, 52),

        // side exit
        Glib.Color.FromRGBA(128, 216, 128),

        // sky exit
        Glib.Color.FromRGBA(52, 216, 255),

        // sea exit
        Glib.Color.FromRGBA(0, 0, 255),

        // batfly hive
        Glib.Color.FromRGBA(0, 255, 0),

        // garbage worm
        Glib.Color.FromRGBA(255, 128, 0)
    ];

    public void GenerateTexture()
    {
        var stopwatch = Stopwatch.StartNew();
        using var img = Glib.Image.FromColor(Width, Height, BackgroundColor);
        
        // geo
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var col = BackgroundColor;
                var cell = GetCell(x, y);
                
                var beamMask = CellPlayFeature.VerticalBeam | CellPlayFeature.HorizontalBeam;
                if (cell.Geo is CellPlayGeo.Slope or CellPlayGeo.Platform || (cell.Features & beamMask) != 0)
                    col = FeatureColor;
                
                else if (cell.Geo == CellPlayGeo.ShortcutEntrance)
                    col = ShortcutColor;

                else if (cell.Features.HasFlag(CellPlayFeature.WallBehind))
                    col = WallBehindColor;

                else if (cell.Geo == CellPlayGeo.Wall)
                    col = WallColor;
                
                img.SetPixel(x, y, col);
            }
        }

        // node connections
        foreach (var node in Nodes)
        {
            if (node.Type is NodeType.Exit or NodeType.Den or NodeType.RegionTransportation or NodeType.GarbageHoles)
            {
                img.SetPixel(node.Position.X, node.Position.Y, NodeColors[(int)node.Type]);
            }
        }

        // water
        if (HasWater)
        {
            for (int y = Height - WaterLevel - 1; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var col = img.GetPixel(x, y);

                    float invA = WaterColor.A;
                    var r = col.R * invA + WaterColor.R * WaterColor.A;
                    var g = col.G * invA + WaterColor.G * WaterColor.A;
                    var b = col.B * invA + WaterColor.B * WaterColor.A;

                    img.SetPixel(x, y, new Glib.Color(r, g, b));
                }
            }
        }

        stopwatch.Stop();
        Log.Debug("Render time: {Elapsed} ms", stopwatch.ElapsedMilliseconds);

        img.ExportPng("test.png");
    }
}