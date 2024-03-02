using System.Numerics;
using RainEd.Light;
using RainEd.Props;
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
    public Tiles.Tile? TileHead = null;

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
    // render scales up pixels by 1.25 (each tile is 16 pixels) (me smort. i was already aware tiles were 20 px large)
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

    private int width, height;
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

public struct RotatedRect
{
    public Vector2 Center;
    public Vector2 Size;
    public float Rotation;
}

class PropRope
{
    private readonly PropInit init; 
    private RopeModel? model;
    public RopeReleaseMode ReleaseMode;
    public bool Simulate;
    public Vector2 PointA = Vector2.Zero;
    public Vector2 PointB = Vector2.Zero;
    public float LengthFactor = 1f;
    public int Layer = 0;

    // due to the fact that RopeModel's PointA and PointB are converted between units,
    // i cannot check those if they are equal
    private Vector2 lastPointA;
    private Vector2 lastPointB;
    private float lastLengthFac;

    public RopeModel? Model { get => model; }

    public PropRope(PropInit init)
    {
        if (init.Rope is null) throw new ArgumentException("Given PropInit is not a rope-type prop", nameof(init));

        this.init = init;
        ReleaseMode = RopeReleaseMode.None;
        Simulate = true;
        
        lastPointA = PointA;
        lastPointB = PointB;
        lastLengthFac = LengthFactor;
    }

    public void SimluationStep()
    {
        // if rope properties changed, reset rope model
        if (model == null|| Layer != model.Layer ||
            lastPointA != PointA || lastPointB != PointB ||
            ReleaseMode != model.Release || LengthFactor != lastLengthFac
        )
        {
            if (model == null)
                model = new RopeModel(PointA, PointB, init.Rope!.PhysicalProperties, LengthFactor, Layer, ReleaseMode);
            else
                model.ResetRopeModel(PointA, PointB, init.Rope!.PhysicalProperties, LengthFactor, Layer, ReleaseMode);
        }

        lastPointA = PointA;
        lastPointB = PointB;
        lastLengthFac = LengthFactor;
        
        model.Update();
    }
}
class Prop
{
    public readonly PropInit PropInit;

    // A prop is affine by default
    // The user can then "convert" it to a freeform quad,
    // which then will allow the Quad field to be used
    private bool isAffine;

    // only use if Affine is false
    private readonly Vector2[] quad;

    // only use if Affine is true
    private RotatedRect affineTransform;

    private PropRope? rope;
    public PropRope? Rope { get => rope; }

    public Vector2[] QuadPoints
    {
        get
        {
            if (isAffine)
                UpdateQuadPointsFromAffine();
            
            return quad;
        }
    }

    public ref RotatedRect Rect
    {
        get
        {
            if (!isAffine)
                throw new Exception("Attempt to get affine transformation of a freeform-mode prop");
            return ref affineTransform;
        }
    }

    public bool IsAffine { get => isAffine; }

    public enum PropRenderTime
    {
        PreEffects, PostEffects
    };

    public int DepthOffset = 0; // 0-29
    public int CustomDepth;
    public int CustomColor = 0; // index into the PropDatabase.PropColors list
    public int RenderOrder = 0;
    public int Variation = 0; // A variation of -1 means Random Variation
    public int Seed;
    public PropRenderTime RenderTime = PropRenderTime.PreEffects;

    private Prop(PropInit init)
    {
        PropInit = init;
        
        isAffine = true;
        quad = new Vector2[4];

        if (init.PropFlags.HasFlag(PropFlags.RandomVariation)) Variation = -1;
        Seed = (int)(DateTime.Now.Ticks % 1000);
        CustomDepth = init.Depth;

        if (init.Type == PropType.Rope)
        {
            rope = new PropRope(init);
        }
    }

    public Prop(PropInit init, Vector2 center, Vector2 size) : this(init)
    {
        affineTransform.Center = center;
        affineTransform.Size = size;
        affineTransform.Rotation = 0f;   
    }

    public Prop(PropInit init, Vector2[] points) : this(init)
    {
        isAffine = false;

        for (int i = 0; i < 4; i++)
        {
            quad[i] = points[i];
        }
    }

    private void UpdateQuadPointsFromAffine()
    {
        Matrix3x2 transformMat = Matrix3x2.CreateRotation(affineTransform.Rotation);
        quad[0] = affineTransform.Center + Vector2.Transform(affineTransform.Size * new Vector2(-1f, -1f) / 2f, transformMat);
        quad[1] = affineTransform.Center + Vector2.Transform(affineTransform.Size * new Vector2(1f, -1f) / 2f, transformMat);
        quad[2] = affineTransform.Center + Vector2.Transform(affineTransform.Size * new Vector2(1f, 1f) / 2f, transformMat);
        quad[3] = affineTransform.Center + Vector2.Transform(affineTransform.Size * new Vector2(-1f, 1f) / 2f, transformMat);
    }

    public void ConvertToFreeform()
    {
        if (!isAffine) return;
        isAffine = false;
        UpdateQuadPointsFromAffine();
    }

    public void ResetTransform()
    {
        if (!isAffine)
        {
            // calculate center of quad
            var ct = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;

            // convert to affine
            isAffine = true;
            affineTransform.Center = ct;
        }

        affineTransform.Size = new Vector2(PropInit.Width, PropInit.Height);
        affineTransform.Rotation = 0f;
    }

    public void FlipX()
    {
        if (isAffine)
        {
            affineTransform.Size.X = -affineTransform.Size.X;
        }
        else
        {
            var ct = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;
            for (int i = 0; i < 4; i++)
                quad[i].X = -(quad[i].X - ct.X) + ct.X;
        }
    }

    public void FlipY()
    {
        if (isAffine)
        {
            affineTransform.Size.Y = -affineTransform.Size.Y;
        }
        else
        {
            var ct = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;
            for (int i = 0; i < 4; i++)
                quad[i].Y = -(quad[i].Y - ct.Y) + ct.Y;
        }
    }

    public void TickRopeSimulation()
    {
        if (rope is null) return;

        var cos = MathF.Cos(affineTransform.Rotation);
        var sin = MathF.Sin(affineTransform.Rotation);
        rope.PointA = affineTransform.Center + new Vector2(cos, sin) * -affineTransform.Size.X / 2f;
        rope.PointB = affineTransform.Center + new Vector2(cos, sin) * affineTransform.Size.X / 2f;
        rope.Layer = DepthOffset / 10;
        rope.SimluationStep();
    }
}

/*class RopeProp
{
    public readonly RopePropInit Init;
    public Vector2 PointA;
    public Vector2 PointB;
    private int lengthFac;
    private int layer;
    public RopeReleaseMode Release;

    private RopeModel ropeModel;

    public RopeProp(RopePropInit init, int layer, Vector2 center, float width)
    {
        Init = init;
        PointA = center - Vector2.UnitX * (width / 2f);
        PointB = center + Vector2.UnitX * (width / 2f);
        
        lengthFac = 1;
        this.layer = layer;
        Release = RopeReleaseMode.None;

        ropeModel = new RopeModel(PointA, PointB, init.PhysicalProperties, lengthFac, layer, Release);
    }
    
    public RopeModel Model { get => ropeModel; }

    public void SimulationStep()
    {
        ropeModel.Update();
    }
}*/

class Level
{
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

    private LightMap lightMap;
    public LightMap LightMap { get => lightMap; }
    public float LightAngle = MathF.PI;
    public float LightDistance = 1f;
    public const float MaxLightDistance = 10f;

    public readonly List<Effect> Effects = new();
    public readonly List<Prop> Props = new();
    
    public int TileSeed = 200;
    public bool DefaultMedium = false; // idk what the hell this does
    public bool HasSunlight = true;
    public bool HasWater = false;
    public int WaterLevel = -1;
    public bool IsWaterInFront = true; 

    public Level(int width = 72, int height = 43)
    {
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
        lightMap = new LightMap(Width, Height);
    }

    public static Level NewDefaultLevel()
    {
        var level = new Level(72, 43);
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
                            var rootX = oldCell.TileRootX + dstOriginX;
                            var rootY = oldCell.TileRootY + dstOriginY;
                            
                            // if the tile head is out of bounds, clear tile data here
                            if (rootX < 0 || rootY < 0 || rootX >= newWidth || rootY >= newHeight)
                            {
                                Layers[l,x,y].TileLayer = -1;
                                Layers[l,x,y].TileRootX = -1;
                                Layers[l,x,y].TileRootY = -1;
                            }
                            else
                            {
                                Layers[l,x,y].TileRootX = rootX;
                                Layers[l,x,y].TileRootY = rootY;
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
        LightMap.Resize(
            newWidth, newHeight,
            dstOriginX, dstOriginY
        );

        // resize effect matrices
        foreach (var effect in Effects)
        {
            effect.Resize(newWidth, newHeight, dstOriginX, dstOriginY);
        }

        // update cameras
        foreach (var camera in Cameras)
        {
            camera.Position.X += dstOriginX;
            camera.Position.Y += dstOriginY;
        }

        // TODO: resize props

        _width = newWidth;
        _height = newHeight;
    }

    public void LoadLightMap(Image image)
    {
        lightMap.Dispose();
        lightMap = new LightMap(image);
    }
}