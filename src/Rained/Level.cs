using System.Numerics;
using Raylib_cs;
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

struct LevelCell
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

class Camera
{
    // camera size is 70x39 tiles
    // camera red border is 52.5x35
    // left black inner border is 1 tile away
    // game resolution is 1040x800 
    // render scales up pixels by 1.25 (each tile is 16 pixels )
    // quad save format: [A, O]
    //  A: clockwise angle in degrees, where 0 is up
    //  O: offset number from 0 to 1 (1.0 translate to 4 tiles) 
    // corner order is: TL, TR, BR, BL
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
        int x = (cornerIndex == 1 || cornerIndex == 2) ? 1 : 0;
        int y = (cornerIndex & 2) >> 1;
        
        return offset ?
            Position + new Vector2(WidescreenSize.X * x, WidescreenSize.Y * y) + GetCornerOffset(cornerIndex)
        :
            Position + new Vector2(WidescreenSize.X * x, WidescreenSize.Y * y);
    }
}

class Effect
{
    public enum LayerMode
    {
        All = 0,
        First, Second, Third,
        FirstAndSecond, SecondAndThird
    };

    public readonly EffectInit Data;
    public LayerMode Layer = LayerMode.All;
    public bool Is3D = false;
    public int CustomValue = 0;
    public int PlantColor = 1; // 0 = Color1, 1 = Color2, 2 = Dead
    public int Seed;

    public int width, height;
    public int Width { get => width; }
    public int Height { get => height; }
    public float[,] Matrix;

    public Effect(Level level, EffectInit init)
    {
        Data = init;
        Seed = Raylib.GetRandomValue(0, 500);

        if (!string.IsNullOrEmpty(init.customSwitchName))
        {
            // find index of default value
            for (int i = 0; i < init.customSwitchOptions.Length; i++)
            {
                if (init.customSwitchOptions[i] == init.customSwitchDefault)
                {
                    CustomValue = i;
                    break;
                }
            }
        }

        // create matrix
        width = level.Width;
        height = level.Height;
        Matrix = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Matrix[x,y] = init.fillWith;
            }
        }
    }

    public void Resize(int newWidth, int newHeight, int oldDstX, int oldDstY)
    {
        var oldMatrix = Matrix;
        Matrix = new float[newWidth, newHeight];

        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                int oldX = x - oldDstX;
                int oldY = y - oldDstY;

                if (oldX >= 0 && oldY >= 0 && oldX < width && oldY < height)
                {
                    Matrix[x,y] = oldMatrix[oldX,oldY];
                }
                else
                {
                    Matrix[x,y] = Data.fillWith;
                }
            }
        }

        width = newWidth;
        height = newHeight;
    }
}

class Level
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
        "3DBricks",
        "Random Machines",
        "Dirt",
        "Ceramic Tile",
        "Temple Stone",
        "Circuits",
        "Ridge"
    };

    private RlManaged.Image lightMap;

    public float LightAngle = MathF.PI;
    public float LightDistance = 1f;
    public const float MaxLightDistance = 10f;

    public readonly List<Effect> Effects = new();
    
    public RlManaged.Image LightMap {
        get => GetLightImage();
        set => SetLightImage(value);
    }

    public ref Image LightMapRef { get => ref lightMap.Ref(); }

    public int TileSeed = 200;
    public bool DefaultMedium = false; // idk what the hell this does
    public bool HasSunlight = true;
    public bool HasWater = false;
    public int WaterLevel = -1;
    public bool IsWaterInFront = true; 

    public Level(RainEd editor, int width = 72, int height = 43)
    {
        this.editor = editor;

        _width = width;
        _height = height;
        BufferTilesLeft = 12;
        BufferTilesTop = 3;
        BufferTilesRight = 12;
        BufferTilesBot = 5;

        WaterLevel = height / 2;

        // initialize layers
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

        // initialize light map
        int lightMapWidth = Width * 20 + 300;
        int lightMapHeight = Height * 20 + 300;
        lightMap = RlManaged.Image.GenColor(lightMapWidth, lightMapHeight, new Color(0, 0, 0, 255));
        Raylib.ImageFormat(ref lightMap.Ref(), PixelFormat.UncompressedGrayscale);
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

    // get light map as a Raylib image
    private RlManaged.Image GetLightImage()
    {
        return lightMap;
    }

    private void SetLightImage(RlManaged.Image srcImage)
    {
        if (srcImage == lightMap) return;
        if (srcImage.Width != lightMap.Width || srcImage.Height != lightMap.Height)
        {
            Console.WriteLine("WARNING: Light map size is not LevelSize * 20 + (300, 300)");
        }
        
        if (srcImage.PixelFormat != lightMap.PixelFormat)
            throw new Exception("Mismatched format");

        lightMap = srcImage;
    }

    public void Resize(int newWidth, int newHeight, int anchorX = -1, int anchorY = -1)
    {
        if (newWidth == _width && newHeight == _height) return;

        // resize geometry
        var oldLayers = Layers;
        Layers = new LevelCell[LayerCount, newWidth, newHeight];
        
        int dstOriginX = (int)((newWidth - _width) * ((anchorX + 1) / 2f));
        int dstOriginY = (int)((newHeight - _height) * ((anchorY + 1) / 2f));
        
        for (int l = 0; l < LayerCount; l++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    var oldX = x - dstOriginX;
                    var oldY = y - dstOriginY;

                    // copy the cell data from the old level
                    if (IsInBounds(oldX, oldY))
                    {
                        ref var oldCell = ref oldLayers[l,oldX,oldY];
                        Layers[l,x,y] = oldCell;

                        // completely remove any tiles where the tile head
                        // is now out of bounds
                        // first, check if this is a tile body
                        if (oldCell.HasTile() && oldCell.TileHead is null)
                        {
                            var rootX = oldCell.TileRootX;
                            var rootY = oldCell.TileRootY;
                            
                            // if the tile head is out of bounds, clear tile data here
                            if (rootX < 0 || rootY < 0 || rootX >= newWidth || rootY >= newHeight)
                            {
                                Layers[l,x,y].TileLayer = -1;
                                Layers[l,x,y].TileRootX = -1;
                                Layers[l,x,y].TileRootY = -1;
                            }
                        }
                    }

                    // this cell is not in the bounds of the old level,
                    // so put the default medium here
                    else
                    {
                        Layers[l,x,y] = new LevelCell()
                        {
                            Cell = DefaultMedium ? CellType.Solid : CellType.Air
                        };
                    }
                }
            }
        }

        // resize light map
        Raylib.ImageResizeCanvas(
            ref lightMap.Ref(),
            newWidth * 20 + 300, newHeight * 20 + 300,
            dstOriginX * 20, dstOriginY * 20,
            Color.White
        );

        // resize effect matrices
        foreach (var effect in Effects)
        {
            effect.Resize(newWidth, newHeight, dstOriginX, dstOriginY);
        }

        // TODO: resize props

        _width = newWidth;
        _height = newHeight;
    }
}