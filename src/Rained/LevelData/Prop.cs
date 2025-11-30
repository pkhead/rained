using System.Diagnostics;
using System.Numerics;
using Rained.Assets;
using Raylib_cs;
namespace Rained.LevelData;

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

class PropTransform
{
    // A prop is affine by default
    // The user can then "convert" it to a freeform quad,
    // which then will allow the Quad field to be used
    public bool isAffine;

    // only modify if isAffine is false
    public Vector2[] quad;

    // only usable if isAffine is true
    public RotatedRect rect;

    public PropTransform()
    {
        quad = new Vector2[4];
    }

    public PropTransform Clone()
    {
        var clone = new PropTransform()
        {
            isAffine = isAffine,
            rect = rect
        };

        for (int i = 0; i < 4; i++)
        {
            clone.quad[i] = quad[i];
        }
        
        return clone;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (PropTransform) obj;        
        return isAffine == other.isAffine && rect == other.rect &&
            quad[0] == other.quad[0] &&
            quad[1] == other.quad[1] &&
            quad[2] == other.quad[2] &&
            quad[3] == other.quad[3];
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(isAffine.GetHashCode(), quad.GetHashCode(), rect.GetHashCode());
    }
}

class PropRope
{
    private readonly PropInit init; 
    private RopeModel? model;
    public RopeReleaseMode ReleaseMode;

    /// <summary>
    /// The speed at which the rope is being simulated.
    /// If set to 0, simulation will be deactivated.
    /// </summary>
    public float SimulationSpeed;

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
    public float SimulationTimeStacker = 0f;

    public PropRope(PropInit init)
    {
        if (init.Rope is null) throw new ArgumentException("Given PropInit is not a rope-type prop", nameof(init));

        this.init = init;
        ReleaseMode = RopeReleaseMode.None;
        SimulationSpeed = 0;
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
        
        model.PointA = PointA;
        model.PointB = PointB;
        
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

        while (SimulationTimeStacker >= 1f)
        {
            model!.Update();
            SimulationTimeStacker -= 1f;
        }
    }
}

public enum PropFezTreeEffectColor
{
    Dead = 0,
    Color1 = 1,
    Color2 = 2
};

class PropFezTree
{
    public float LeafDensity = 1f; // [0.0, 1.0]
    public PropFezTreeEffectColor EffectColor = PropFezTreeEffectColor.Dead;

    public Vector2 TrunkPosition = new(0f, 0f);
    public float TrunkAngle = 0f; // radians, clockwise
    // public Vector2 LeafPosition = new(0f, 0f); // position of prop bottom-middle
    // public float LeafAngle = 0f; // radians, counterclockwise
}

public enum PropRenderTime
{
    PreEffects, PostEffects
};

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

    private readonly PropFezTree? fezTree;
    public PropFezTree? FezTree { get => fezTree; }
    
    // returns true if it's a rope or a long-type prop
    public bool IsLong { get => PropInit.Type == PropType.Rope || PropInit.Type == PropType.Long; }

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

    // this is here so that the user can't edit warped rope or long props
    // because that is invalid
    public bool IsMovable
    {
        get => !IsLong || transform.isAffine;
    }

    public bool CanVertexEdit
    {
        get => PropInit.Type != PropType.FezTree;
    }

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
        transform = new PropTransform
        {
            isAffine = true,
            quad = new Vector2[4]
        };

        Seed = (int)(DateTime.Now.Ticks % 1000);
        CustomDepth = init.Depth;
        Variation = 0;

        if (init.Type == PropType.Rope)
        {
            rope = new PropRope(init);
        }
        else if (init.Type == PropType.FezTree)
        {
            fezTree = new PropFezTree();
        }
    }

    public Prop(PropInit init, Vector2 center, Vector2 size) : this(init)
    {
        transform.rect.Center = center;
        transform.rect.Size = size;
        transform.rect.Rotation = 0f;

        if (fezTree is not null)
        {
            fezTree.TrunkPosition = center + new Vector2(0f, size.Y / 2f + 1f);
        }
    }

    public Prop(PropInit init, Vector2[] points) : this(init)
    {
        transform.isAffine = false;

        for (int i = 0; i < 4; i++)
        {
            transform.quad[i] = points[i];
        }
    }

    public Prop Clone(Vector2 cloneOffset)
    {
        Prop newProp;
        var srcProp = this;
        if (srcProp.IsAffine)
        {
            newProp = new Prop(srcProp.PropInit, srcProp.Rect.Center + cloneOffset, srcProp.Rect.Size);
            newProp.Rect.Rotation = srcProp.Rect.Rotation;
        }
        else
        {
            newProp = new Prop(srcProp.PropInit, srcProp.QuadPoints);
            newProp.QuadPoints[0] += cloneOffset;
            newProp.QuadPoints[1] += cloneOffset;
            newProp.QuadPoints[2] += cloneOffset;
            newProp.QuadPoints[3] += cloneOffset;
        }

        // copy other properties to the new prop
        // (this is the best way to do this)
        newProp.DepthOffset = srcProp.DepthOffset;
        newProp.ApplyColor = srcProp.ApplyColor;
        newProp.CustomColor = srcProp.CustomColor;
        newProp.CustomDepth = srcProp.CustomDepth;
        newProp.RenderOrder = srcProp.RenderOrder;
        newProp.Variation = srcProp.Variation;
        newProp.Seed = srcProp.Seed;
        newProp.RenderTime = srcProp.RenderTime;

        // copy fez tree properties
        if (newProp.FezTree is not null)
        {
            Debug.Assert(FezTree is not null);
            newProp.FezTree.EffectColor = FezTree.EffectColor;
            newProp.FezTree.LeafDensity = FezTree.LeafDensity;
            newProp.FezTree.TrunkPosition = FezTree.TrunkPosition + cloneOffset;
            newProp.FezTree.TrunkAngle = FezTree.TrunkAngle;
        }

        return newProp;
    }

    public Prop Clone() => Clone(Vector2.Zero);

    /// <summary>
    /// Apply random modifications to prop as specified by its RandomVariation,
    /// RandomFlipX, RandomFlipY, and RandomRotation flags, or the absence of them.
    /// </summary>
    public void Randomize()
    {
        var rand = new Random(Seed);

        if (PropInit.PropFlags.HasFlag(PropFlags.RandomVariation))
        {
            Variation = rand.Next(0, PropInit.VariationCount);
        }

        if (PropInit.PropFlags.HasFlag(PropFlags.RandomFlipX))
            if (rand.NextSingle() > 0.5) FlipX();

        if (PropInit.PropFlags.HasFlag(PropFlags.RandomFlipY))
            if (rand.NextSingle() > 0.5) FlipY();

        if (PropInit.PropFlags.HasFlag(PropFlags.RandomRotation))
            transform.rect.Rotation = rand.NextSingle() * MathF.PI * 2f;
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
        
        var right = transform.quad[1] - transform.quad[0];
        var down = transform.quad[3] - transform.quad[0];
        var crossZ = right.X * down.Y - right.Y * down.X;
        transform.rect.Rotation = MathF.Atan2(right.Y, right.X);

        // Z component of the cross product is used to
        // detect if the prop has been flipped
        if (MathF.Sign(crossZ) < 0f)
        {
            transform.rect.Size.Y = -transform.rect.Size.Y;
        }

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

    public (Vector2 pos, Vector2 leafSize, float leafAngle) CalcFezTreeLeafParameters()
    {
        // calculate leaf position, size, and angle
        // leaf position is the midpoint of the bottom rect side
        // leaf angle is just the rotation of the prop
        Vector2 leafPos;
        Vector2 propStretch;
        float leafAngle;
        if (IsAffine)
        {
            var rect = Rect;
            leafPos = rect.Center + new Vector2(-MathF.Sin(rect.Rotation), MathF.Cos(rect.Rotation)) * (rect.Size.Y / 2f);
            propStretch = rect.Size / new Vector2(PropInit.Width, PropInit.Height);
            leafAngle = rect.Rotation;
        }
        else
        {
            var pts = QuadPoints;

            var propWidth = Vector2.Distance((pts[0] + pts[3]) / 2f, (pts[1] + pts[2]) / 2f);
            var propHeight = Vector2.Distance((pts[0] + pts[1]) / 2f, (pts[2] + pts[3]) / 2f);
            propStretch = new Vector2(propWidth, propHeight) / new Vector2(PropInit.Width, PropInit.Height);

            leafPos = (pts[2] + pts[3]) / 2f;
            leafAngle = MathF.Atan2(pts[2].Y - pts[3].Y, pts[2].X - pts[3].X);
        }

        var leafSize = propStretch * 80f * new Vector2(0.6875f, 0.84375f);
        return (leafPos, leafSize, leafAngle);
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