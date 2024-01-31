using System.Numerics;
namespace RainEd;

public enum CellType : sbyte
{
    Air,
    Solid,
    SlopeRightUp,
    SlopeLeftUp,
    SlopeRightDown,
    SlopeLeftDown,
    Platform,
    ShortcutEntrance,
    // 8 is empty
    Glass = 9,
}

public enum Material : byte
{
    None,
    Standard,
    Concrete,
    RainStone,
    Bricks,
    BigMetal,
    TinySigns,
    Scaffolding,
    DensePipes,
    SuperStructure,
    SuperStructure2,
    TiledStone,
    ChaoticStone,
    SmallPipes,
    Trash,
    Invisible,
    LargeTrash,
    ThreeDBricks,
    RandomMachines,
    Dirt,
    CeramicTile,
    TempleStone,
    Circuits,
    Ridge
};

[Flags]
public enum LevelObject : uint
{
    None = 0u,
    HorizontalBeam = 1u,
    VerticalBeam = 2u,
    Hive = 4u,
    // (always appears with shortcut entrance?) 8
    Shortcut = 16u,
    Entrance = 32u,
    CreatureDen = 64u,
    // (empty slot?) 128,
    Rock = 256u,
    Spear = 512u,
    Crack = 1024u,
    ForbidFlyChain = 2048u,
    GarbageWorm = 4096u,
    // IDs jump from 13 to 18
    Waterfall = 131072u,
    WhackAMoleHole = 262144u,
    WormGrass = 524288u,
    ScavengerHole = 1048576u
}

public struct LevelCell
{
    public CellType Cell = CellType.Air;
    public LevelObject Objects = 0;
    public Material Material = Material.None;

    // X position of the tile root, -1 if there is no tile here
    public int TileRootX = -1;

    // Y position of the tile root, -1 if there is no tile here
    public int TileRootY = -1;
    // Layer of tile root, -1 if there is no tile here
    public int TileLayer = -1;

    // As the tile root, reference to the tile, or null if no tile here
    public Tiles.TileData? TileHead = null;

    public readonly bool HasTile() => TileRootX >= 0 || TileRootY >= 0 || TileHead is not null;

    public LevelCell() {}

    public void Add(LevelObject obj) => Objects |= obj;
    public void Remove(LevelObject obj) => Objects &= ~obj;
    public readonly bool Has(LevelObject obj) => Objects.HasFlag(obj);
}

public class Camera
{
    // camera size is 70x39 tiles
    // camera red border is 52.5x35
    // left black inner border is 1 tile away
    // game resolution is 1040x800 
    // render scales up pixels by 1.25 (each tile is 16 pixels )
    // quad save format: [A, O]
    //  A: clockwise angle in degrees, where 0 is up
    //  O: offset number from 0 to 1 (1.0 translate to 4 tiles) 
    public Vector2 Position;
    public float[] CornerOffsets = new float[4];
    public float[] CornerAngles = new float[4];

    public readonly static Vector2 WidescreenSize = new(70f, 40f);
    public readonly static Vector2 StandardSize = new(52.5f, 40f);

    public Camera(Vector2 position)
    {
        Position = position;
    }

    public Camera() : this(new(1f, 1f))
    {}


    public Vector2 GetCornerOffset(int cornerIndex)
    {
        return new Vector2(
            MathF.Sin(CornerAngles[cornerIndex]),
            -MathF.Cos(CornerAngles[cornerIndex])
        ) * CornerOffsets[cornerIndex] * 4f;
    }

    public Vector2 GetCornerPosition(int cornerIndex, bool offset)
    {
        // bit trick to get the X and Y percentage from the cornerIndex
        int x = cornerIndex & 1;
        int y = (cornerIndex & 2) >> 1;

        return offset ?
            Position + new Vector2(WidescreenSize.X * x, WidescreenSize.Y * y) + GetCornerOffset(cornerIndex)
        :
            Position + new Vector2(WidescreenSize.X * x, WidescreenSize.Y * y);
    }
}

public class Level
{
    private readonly RainEd editor;

    public LevelCell[,,] Layers;
    private int _width, _height;
    public int BufferTilesLeft, BufferTilesTop;
    public int BufferTilesRight, BufferTilesBot;
    public Material DefaultMaterial = Material.Standard;

    public readonly List<Camera> Cameras = new();

    public int Width { get => _width; }
    public int Height { get => _height; }

    public const int LayerCount = 3;
    public const int TileSize = 20;
    public const int MaxCameraCount = 20;

    public static readonly string[] MaterialNames = new string[23]
    {
        "Standard",
        "Concrete",
        "RainStone",
        "Bricks",
        "BigMetal",
        "Tiny Signs",
        "Scaffolding",
        "Dense Pipes",
        "SuperStructure",
        "SuperStructure2",
        "Tiled Stone",
        "Chaotic Stone",
        "Small Pipes",
        "Trash",
        "Invisible",
        "LargeTrash",
        "3D Bricks",
        "Random Machines",
        "Dirt",
        "Ceramic Tile",
        "Temple Stone",
        "Circuits",
        "Ridge"
    };

    public Level(RainEd editor, int width = 72, int height = 43)
    {
        this.editor = editor;

        _width = width;
        _height = height;
        BufferTilesLeft = 12;
        BufferTilesTop = 3;
        BufferTilesRight = 12;
        BufferTilesBot = 5;

        Layers = new LevelCell[LayerCount,Width,Height];

        for (int l = 0; l < LayerCount; l++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Layers[l,x,y] = new LevelCell();
                }
            }
        }
    }

    public static Level NewDefaultLevel(RainEd editor)
    {
        var level = new Level(editor, 72, 43);
        level.Cameras.Add(new Camera());

        for (int l = 0; l < LayerCount; l++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    level.Layers[l,x,y].Cell = l == 2 ? CellType.Air : CellType.Solid;
                }
            }
        }

        return level;
    }

    public bool IsInBounds(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;
    
    public LevelCell GetClamped(int layer, int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Layers[layer, x, y];
    }
}