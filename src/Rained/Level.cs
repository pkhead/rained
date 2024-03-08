/**
*   Level.cs
*   Definition for the Level class and all related constructs
*   that would be edited in the IEditorMode classes
*/
using System.Numerics;
using RainEd.Light;
using RainEd.Props;
using Raylib_cs;
namespace RainEd;

public enum GeoType : sbyte
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
    public GeoType Geo = GeoType.Air;
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
    public readonly bool Has(LevelObject obj) => (Objects & obj) != 0;

    public readonly override bool Equals(object? obj)
    {        
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        var other = (LevelCell) obj;
        return
            Geo == other.Geo &&
            Objects == other.Objects &&
            Material == other.Material &&
            TileRootX == other.TileRootX &&
            TileRootY == other.TileRootY &&
            TileLayer == other.TileLayer &&
            TileHead == other.TileHead;
    }
    
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(
            Geo.GetHashCode(),
            Objects.GetHashCode(),
            Material.GetHashCode(),
            TileRootX.GetHashCode(),
            TileRootY.GetHashCode(),
            TileLayer.GetHashCode(), 
            TileHead?.GetHashCode()
        );
    }
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

    public readonly override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (RotatedRect) obj; 
        return Center == other.Center && Size == other.Size && Rotation == other.Rotation;       
    }
    
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Center.GetHashCode(), Size.GetHashCode(), Rotation.GetHashCode());
    }

    public static bool operator ==(RotatedRect left, RotatedRect right)
    {
        return left.Center == right.Center && left.Size == right.Size && left.Rotation == right.Rotation;
    }

    public static bool operator !=(RotatedRect left, RotatedRect right)
    {
        return !(left == right);
    }
}

struct PropTransform
{
    // A prop is affine by default
    // The user can then "convert" it to a freeform quad,
    // which then will allow the Quad field to be used
    public bool isAffine;

    // only modify if isAffine is false
    public Vector2[] quad;

    // only usable if isAffine is true
    public RotatedRect rect;

    /*public PropTransform(Prop prop)
    {
        quad = new Vector2[4];
        isAffine = prop.IsAffine;
        if (isAffine)
        {
            rect = prop.Rect;
        }
        else
        {
            var pts = prop.QuadPoints;
            for (int i = 0; i < 4; i++)
            {
                quad[i] = pts[i];
            }
        }
    }*/
}

class PropRope
{
    private readonly PropInit init; 
    private RopeModel? model;
    public RopeReleaseMode ReleaseMode;
    public bool Simulate;
    public Vector2 PointA = Vector2.Zero;
    public Vector2 PointB = Vector2.Zero;
    public float Width;
    public int Layer = 0;
    public float Thickness = 2f;

    // due to the fact that RopeModel's PointA and PointB are converted between units,
    // i cannot check those if they are equal
    private Vector2 lastPointA;
    private Vector2 lastPointB;
    private float lastWidth;
    private bool ignoreMovement = false;

    // when LevelSerialization wants to call LoadPoints, the points will be loaded
    // after the model is created in ResetSimulation rather than directly there
    private Vector2[]? deferredLoadSegments = null;

    public RopeModel? Model { get => model; }

    // this is set by RainEd's UpdateRopeSimulation. it is important that I set this per prop
    // and is only updated while it is simulating, so that ropes don't
    // jitter while their simulation is paused
    public float SimulationTimeRemainder = 0f;

    public PropRope(PropInit init)
    {
        if (init.Rope is null) throw new ArgumentException("Given PropInit is not a rope-type prop", nameof(init));

        this.init = init;
        ReleaseMode = RopeReleaseMode.None;
        Simulate = false;
        Width = init.Height;
        
        lastPointA = PointA;
        lastPointB = PointB;
        lastWidth = Width;
    }

    public void LoadPoints(Vector2[] ptPositions)
    {
        deferredLoadSegments = ptPositions;
    }

    // don't reset the simulation when it moves on this frame
    public void IgnoreMovement()
    {
        ignoreMovement = true;
    }

    public void ResetModel()
    {
        if (model == null)
            model = new RopeModel(PointA, PointB, init.Rope!.PhysicalProperties, Width / init.Height, Layer, ReleaseMode);
        else if (!ignoreMovement)
            model.ResetRopeModel(PointA, PointB, init.Rope!.PhysicalProperties, Width / init.Height, Layer, ReleaseMode);
        
        if (deferredLoadSegments is not null)
        {
            model.SetSegmentPositions(deferredLoadSegments);
            deferredLoadSegments = null;
        }
    }

    public void SimluationStep()
    {
        // if rope properties changed, reset rope model
        if (model == null || Layer != model.Layer ||
            lastPointA != PointA || lastPointB != PointB ||
            ReleaseMode != model.Release || Width != lastWidth
        )
        {
            ResetModel();
        }

        ignoreMovement = false;
        lastPointA = PointA;
        lastPointB = PointB;
        lastWidth = Width;
        
        if (Simulate)
        {
            model!.Update();
        }
    }
}

class Prop
{
    public readonly PropInit PropInit;

    // this is just used for prop depth sorting
    // if both RenderOrder and DepthOffset is the same on a stack of props
    // it might cause a "z-fighting" coming from the
    // sorted list being re-sorted and stuf...
    // so I can't return 0 in the comparer function
    // i need a number unique to each prop
    public readonly uint ID;
    private static uint nextId = 0;
    
    private PropTransform transform;
    public PropTransform Transform { get => transform; set => transform = value; }

    private readonly PropRope? rope;
    public PropRope? Rope { get => rope; }

    public Vector2[] QuadPoints
    {
        get
        {
            if (transform.isAffine)
                UpdateQuadPointsFromAffine();
            
            return transform.quad;
        }
    }

    public ref RotatedRect Rect
    {
        get
        {
            if (!transform.isAffine)
                throw new Exception("Attempt to get affine transformation of a freeform-mode prop");
            return ref transform.rect;
        }
    }

    public bool IsAffine { get => transform.isAffine; }

    public bool IsMovable
    {
        get => rope == null || transform.isAffine;
    }

    public enum PropRenderTime
    {
        PreEffects, PostEffects
    };

    public int DepthOffset = 0; // 0-29
    public int CustomDepth;
    public int CustomColor = 0; // index into the PropDatabase.PropColors list
    public int RenderOrder = 0;
    public int Variation = 0;
    public int Seed;
    public PropRenderTime RenderTime = PropRenderTime.PreEffects;
    public bool ApplyColor = false;

    private Prop(PropInit init)
    {
        PropInit = init;
        ID = nextId++;
        
        transform.isAffine = true;
        transform.quad = new Vector2[4];

        Seed = (int)(DateTime.Now.Ticks % 1000);
        CustomDepth = init.Depth;

        if (init.PropFlags.HasFlag(PropFlags.RandomVariation))
        {
            var rand = new Random(Seed);
            Variation = rand.Next(0, init.VariationCount);
        }
        else
        {
            Variation = 0;
        }

        if (init.Type == PropType.Rope)
        {
            rope = new PropRope(init);
        }
    }

    public Prop(PropInit init, Vector2 center, Vector2 size) : this(init)
    {
        transform.rect.Center = center;
        transform.rect.Size = size;
        transform.rect.Rotation = 0f;   
    }

    public Prop(PropInit init, Vector2[] points) : this(init)
    {
        transform.isAffine = false;

        for (int i = 0; i < 4; i++)
        {
            transform.quad[i] = points[i];
        }
    }

    private void UpdateQuadPointsFromAffine()
    {
        Matrix3x2 transformMat = Matrix3x2.CreateRotation(transform.rect.Rotation);
        transform.quad[0] = transform.rect.Center + Vector2.Transform(transform.rect.Size * new Vector2(-1f, -1f) / 2f, transformMat);
        transform.quad[1] = transform.rect.Center + Vector2.Transform(transform.rect.Size * new Vector2(1f, -1f) / 2f, transformMat);
        transform.quad[2] = transform.rect.Center + Vector2.Transform(transform.rect.Size * new Vector2(1f, 1f) / 2f, transformMat);
        transform.quad[3] = transform.rect.Center + Vector2.Transform(transform.rect.Size * new Vector2(-1f, 1f) / 2f, transformMat);
    }

    public void ConvertToFreeform()
    {
        if (rope is not null) throw new Exception("Cannot warp rope-type props");
        if (!transform.isAffine) return;
        transform.isAffine = false;
        UpdateQuadPointsFromAffine();
    }

    public bool TryConvertToAffine()
    {
        if (transform.isAffine) return true;

        // check if all the interior angles of this quad are 90 degrees
        for (int i = 0; i < 4; i++)
        {
            // form a triangle with (pA, pB, pc)
            var pA = transform.quad[i];
            var pB = transform.quad[(i+1)%4];
            var pC = transform.quad[(i+2)%4];

            if (pA == pB || pB == pC || pA == pC)
                return false;
            
            // if the triangle is not right
            // this prop cannot be expressed as an affine rect transformation
            var pythagLeft = Vector2.DistanceSquared(pA, pB) + Vector2.DistanceSquared(pB, pC);
            var hyp = Vector2.DistanceSquared(pA, pC);
            
            // there can be a small margin of error
            if (MathF.Abs(pythagLeft - hyp) > 0.5f)
            {
                return false;
            }
        }

        // calculate dimensions of rotated rect
        transform.isAffine = true;
        transform.rect.Center = (transform.quad[0] + transform.quad[1] + transform.quad[2] + transform.quad[3]) / 4f;
        transform.rect.Size.X = Vector2.Distance(transform.quad[0], transform.quad[1]);
        transform.rect.Size.Y = Vector2.Distance(transform.quad[3], transform.quad[0]);
        
        var dir = transform.quad[1] - transform.quad[0];
        transform.rect.Rotation = MathF.Atan2(dir.Y, dir.X);

        return true;
    }

    public void ResetTransform()
    {
        if (!transform.isAffine)
        {
            // calculate center of quad
            var ct = (transform.quad[0] + transform.quad[1] + transform.quad[2] + transform.quad[3]) / 4f;

            // convert to affine
            transform.isAffine = true;
            transform.rect.Center = ct;
        }

        transform.rect.Size = new Vector2(PropInit.Width, PropInit.Height);
        transform.rect.Rotation = 0f;
    }

    public void FlipX()
    {
        if (transform.isAffine)
        {
            transform.rect.Size.X = -transform.rect.Size.X;
        }
        else
        {
            var ct = (transform.quad[0] + transform.quad[1] + transform.quad[2] + transform.quad[3]) / 4f;
            for (int i = 0; i < 4; i++)
                transform.quad[i].X = -(transform.quad[i].X - ct.X) + ct.X;
        }
    }

    public void FlipY()
    {
        if (transform.isAffine)
        {
            transform.rect.Size.Y = -transform.rect.Size.Y;
        }
        else
        {
            var ct = (transform.quad[0] + transform.quad[1] + transform.quad[2] + transform.quad[3]) / 4f;
            for (int i = 0; i < 4; i++)
                transform.quad[i].Y = -(transform.quad[i].Y - ct.Y) + ct.Y;
        }
    }

    public Rectangle CalcAABB()
    {
        if (transform.isAffine)
            UpdateQuadPointsFromAffine();
        
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);

        for (int i = 0; i < 4; i++)
        {
            var pt = transform.quad[i];

            if (pt.X > max.X) max.X = pt.X;
            if (pt.Y > max.Y) max.Y = pt.Y;
            if (pt.X < min.X) min.X = pt.X;
            if (pt.Y < min.Y) min.Y = pt.Y;
        }

        return new Rectangle(min, max - min);
    }

    public void TickRopeSimulation()
    {
        if (rope is null) return;

        if (IsAffine)
        {
            var cos = MathF.Cos(transform.rect.Rotation);
            var sin = MathF.Sin(transform.rect.Rotation);
            rope.PointA = transform.rect.Center + new Vector2(cos, sin) * -transform.rect.Size.X / 2f;
            rope.PointB = transform.rect.Center + new Vector2(cos, sin) * transform.rect.Size.X / 2f;
            rope.Layer = DepthOffset / 10;
            rope.Width = transform.rect.Size.Y;
            rope.SimluationStep();
        }
        else
        {
            rope.PointA = (transform.quad[0] + transform.quad[3]) / 2f;
            rope.PointB = (transform.quad[1] + transform.quad[2]) / 2f;
            rope.Layer = DepthOffset / 10;
            rope.Width = PropInit.Height;

            if (rope.Model is null)
                rope.ResetModel();
        }
    }
}

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
    public readonly List<Prop> SortedProps = new();
    
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
                    level.Layers[l,x,y].Geo = l == 2 ? GeoType.Air : GeoType.Solid;
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

    private class PropDepthSorter : IComparer<Prop>
    {
        int IComparer<Prop>.Compare(Prop? a, Prop? b)
        {
            if (a!.DepthOffset == b!.DepthOffset)
            {
                if (a!.RenderOrder == b!.RenderOrder)
                    return a!.ID.CompareTo(b!.ID);
                else
                    return b!.RenderOrder.CompareTo(a!.RenderOrder);
            }
            else
            {
                return b!.DepthOffset.CompareTo(a!.DepthOffset);
            }
        }
    }

    public void SortPropsByDepth()
    {
        Props.Sort(new PropDepthSorter());
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
                            Geo = DefaultMedium ? GeoType.Solid : GeoType.Air
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

        // resize props
        foreach (var prop in Props)
        {
            // move prop
            if (prop.IsAffine)
            {
                prop.Rect.Center.X += dstOriginX;
                prop.Rect.Center.Y += dstOriginY;
            }
            else
            {
                var quadPts = prop.QuadPoints;
                for (int i = 0; i < 4; i++)
                {
                    quadPts[i].X += dstOriginX;
                    quadPts[i].Y += dstOriginY;
                }
            }

            // move rope segments
            if (prop.Rope is not null && prop.Rope.Model is not null)
            {
                var model = prop.Rope.Model;
                prop.Rope.IgnoreMovement(); // don't reset prop when it moves on this frame

                for (int i = 0; i < model.SegmentCount; i++)
                {
                    var pos = model.GetSegmentPos(i);
                    pos.X += dstOriginX;
                    pos.Y += dstOriginY;
                    model.SetSegmentPosition(i, pos);
                }
            }
        }

        _width = newWidth;
        _height = newHeight;
    }

    public void LoadLightMap(Image image)
    {
        lightMap.Dispose();
        lightMap = new LightMap(image);
    }
}