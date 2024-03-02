/**
Ported verison of ropeModel.lingo
I commented WARNING in places where I'm not sure Lingo does exactly what I think it does
**/
using System.Numerics;
namespace RainEd.Props;

struct RopePhysicalProperties
{
    public float segmentLength;
    public float grav;
    public bool stiff;
    public float friction;
    public float airFric;
    public float segRad;
    public float rigid;
    public float edgeDirection;
    public float selfPush;
    public float sourcePush;
}

enum RopeReleaseMode
{
    None = 0,
    Left = 1,
    Right = 2
}

class RopeModel
{
    private Vector2 posA, posB;
    private int layer;
    private int release;
    private RopePhysicalProperties physics;
    private Segment[] segments;

    public RopeModel(
        Vector2 pA, Vector2 pB,
        RopePhysicalProperties prop,
        float lengthFac,
        int layer,
        RopeReleaseMode release
    )
    {
        segments = Array.Empty<Segment>();
        ResetRopeModel(pA, pB, prop, lengthFac, layer, release);
    }

    public void ResetRopeModel(
        Vector2 pA, Vector2 pB,
        RopePhysicalProperties prop,
        float lengthFac,
        int layer,
        RopeReleaseMode release
    )
    {
        this.layer = layer;
        this.release = release switch
        {
            RopeReleaseMode.Left => -1,
            RopeReleaseMode.Right => 1,
            RopeReleaseMode.None => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(release))
        };

        // convert RainEd coordinates to a coordinate system where
        // topleft is at (1, 1), and each tile is 20 px large
        posA = (pA + Vector2.One) * 20f;
        posB = (pB + Vector2.One) * 20f;
        physics = prop;

        float numberOfSegments = Vector2.Distance(posA, posB) / physics.segmentLength * lengthFac;
        numberOfSegments = Math.Max(numberOfSegments, 3f);
        var _nSegmentInt = (int) numberOfSegments;

        float step = Vector2.Distance(posA, posB) / numberOfSegments;
        
        if (segments.Length != _nSegmentInt)
            segments = new Segment[_nSegmentInt];
        
        for (int i = 0; i < _nSegmentInt; i++)
        {
            segments[i] = new Segment()
            {
                pos = posA + MoveToPoint(posA, posB, (i+0.5f)*step),
                lastPos = posA + MoveToPoint(posA, posB, (i+0.5f)*step),
                vel = Vector2.Zero
            };
        }
    }

    public int SegmentCount { get => segments.Length; }
    public Vector2 GetSegmentPos(int i)
    {
        return SmoothPos(i) / 20f - Vector2.One;
    }
    public Vector2 GetLastSegmentPos(int i)
    {
        return SmoothPosOld(i) / 20f - Vector2.One;
    }

    public RopeReleaseMode Release {
        get
        {
            return release switch
            {
                -1 => RopeReleaseMode.Left,
                1 => RopeReleaseMode.Right,
                _ => RopeReleaseMode.None
            };
        }
        set
        {
            release = value switch
            {
                RopeReleaseMode.Left => -1,
                RopeReleaseMode.Right => 1,
                RopeReleaseMode.None => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };   
        }
    }

    public Vector2 PointA
    {
        set => posA = (value + Vector2.One) * 20f;
        get => posA / 20f - Vector2.One;
    }

    public Vector2 PointB
    {
        set => posB = (value + Vector2.One) * 20f;
        get => posB / 20f - Vector2.One;
    }

    public int Layer {
        get => layer;
        set => layer = value;
    }

#region Lingo Ported
    struct Segment
    {
        public Vector2 pos;
        public Vector2 lastPos;
        public Vector2 vel;
    }

    struct RopePoint
    {
        public Vector2 Loc;
        public Vector2 LastLoc;
        public Vector2 Frc;
        public Vector2 SizePnt;
    }

    private static Vector2 MoveToPoint(Vector2 a, Vector2 b, float t)
    {
        var diff = b - a;
        if (diff.LengthSquared() == 0) return Vector2.UnitY * t;
        return Vector2.Normalize(diff) * t;
    }

    // simplification of a specialized version of MoveToPoint where t = 1
    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        if (to == from) return Vector2.UnitY; // why is MoveToPoint defined like this??
        return Vector2.Normalize(to - from);
    }

    private static Vector2 GiveGridPos(Vector2 pos)
    {
        /*return new Vector2(
            MathF.Floor((pos.X / 20f) + 0.4999f),
            MathF.Floor((pos.Y / 20f) + 0.4999f)
        );*/
        return new Vector2(
            MathF.Floor(pos.X / 20f),
            MathF.Floor(pos.Y / 20f)
        );
    }

    private static Vector2 GiveMiddleOfTile(Vector2 pos)
    {
        return new Vector2(
            (pos.X * 20f) - 10f,
            (pos.Y * 20f) - 10f
        );
    }

    private static float Lerp(float A, float B, float val)
    {
        val = Math.Clamp(val, 0f, 1f);
        if (B < A)
        {
            (B, A) = (A, B);
            val = 1f - val;
        }
        return Math.Clamp(A + (B-A)*val, A, B);
    }

    private static bool DiagWI(Vector2 point1, Vector2 point2, float dig)
    {
        var RectHeight = MathF.Abs(point1.Y - point2.Y);
        var RectWidth = MathF.Abs(point1.X - point2.X);
        return (RectHeight * RectHeight) + (RectWidth * RectWidth) < dig*dig;
    }

    // wtf is this function name?
    private static int AfaMvLvlEdit(Vector2 p, int layer)
    {
        int x = (int)p.X - 2;
        int y = (int)p.Y - 2;
        var level = RainEd.Instance.Level;
        if (level.IsInBounds(x, y))
            return (int) RainEd.Instance.Level.Layers[layer, x, y].Cell;
        else
            return 1;
    }

    public void Update() 
    {
        if (physics.edgeDirection > 0f)
        {
            var dir = Direction(posA, posB);
            if (release > -1)
            {
                // WARNING - indexing
                for (int A = 0; A <= segments.Length / 2f - 1; A++)
                {
                    var fac = 1f - A / (segments.Length / 2f);
                    fac *= fac;

                    segments[A].vel += dir*fac*physics.edgeDirection;
                }

                var idealFirstPos = posA + dir * physics.segmentLength;
                segments[0].pos = new Vector2(
                    Lerp(segments[0].pos.X, idealFirstPos.X, physics.edgeDirection),
                    Lerp(segments[0].pos.Y, idealFirstPos.Y, physics.edgeDirection)
                );
            }

            if (release < 1)
            {
                // WARNING - indexing
                for (int A1 = 0; A1 <= segments.Length / 2f - 1; A1++)
                {
                    var fac = 1f - A1 / (segments.Length / 2f);
                    fac *= fac;
                    var A = segments.Length + 1 - (A1+1) - 1;
                    segments[A].vel -= dir*fac*physics.edgeDirection;
                }

                var idealFirstPos = posB - dir * physics.segmentLength;
                segments[^1].pos = new Vector2(
                    Lerp(segments[^1].pos.X, idealFirstPos.X, physics.edgeDirection),
                    Lerp(segments[^1].pos.Y, idealFirstPos.Y, physics.edgeDirection)
                );
            }
        }

        if (release > -1)
        {
            segments[0].pos = posA;
            segments[0].vel = Vector2.Zero;
        }

        if (release < 1)
        {
            segments[^1].pos = posB;
            segments[^1].vel = Vector2.Zero;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].lastPos = segments[i].pos;
            segments[i].pos += segments[i].vel;
            segments[i].vel *= physics.airFric;
            segments[i].vel.Y += physics.grav;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            ConnectRopePoints(i, i-1);
            if (physics.rigid > 0)
                ApplyRigidity(i);
        }

        for (int i = 2; i <= segments.Length; i++)
        {
            var a = segments.Length - i + 1;
            ConnectRopePoints(a-1, a);
            if (physics.rigid > 0)
                ApplyRigidity(i-1);
        }

        if (physics.selfPush > 0)
        {
            for (int A = 0; A < segments.Length; A++)
            {
                for (int B = 0; B < segments.Length; B++)
                {
                    if (A != B && DiagWI(segments[A].pos, segments[B].pos, physics.selfPush))
                    {
                        var dir = Direction(segments[A].pos, segments[B].pos);
                        var dist = Vector2.Distance(segments[A].pos, segments[B].pos);
                        var mov = dir * (dist - physics.selfPush);

                        segments[A].pos += mov * 0.5f;
                        segments[A].vel += mov * 0.5f;
                        segments[B].pos -= mov * 0.5f;
                        segments[B].vel -= mov * 0.5f;
                    }
                }
            }
        }

        if (physics.sourcePush > 0)
        {
            for (int A = 0; A < segments.Length; A++)
            {
                segments[A].vel += MoveToPoint(posA, segments[A].pos, physics.sourcePush) * Math.Clamp((A / (segments.Length - 1f)) - 0.7f, 0f, 1f);
                segments[A].vel += MoveToPoint(posB, segments[A].pos, physics.sourcePush) * Math.Clamp((1f - (A / (segments.Length - 1f))) - 0.7f, 0f, 1f);

            }
        }

        for (int i = 1 + (release > -1?1:0); i <= segments.Length - (release < 1?1:0); i++)
        {
            PushRopePointOutOfTerrain(i-1);
        }

        /*
        if(preview)then
            member("ropePreview").image.copyPixels(member("pxl").image,  member("ropePreview").image.rect, rect(0,0,1,1), {#color:color(255, 255, 255)})
            repeat with i = 1 to ropeModel.segments.count then
                adaptedPos = me.SmoothedPos(i)
                adaptedPos = adaptedPos - cameraPos*20.0
                adaptedPos = adaptedPos * previewScale
                member("ropePreview").image.copyPixels(member("pxl").image, rect(adaptedPos-point(1,1), adaptedPos+point(2,2)), rect(0,0,1,1), {#color:color(0, 0, 0)})
            end repeat
        end if
        */
    }

    private void ConnectRopePoints(int A, int B)
    {
        var dir = Direction(segments[A].pos, segments[B].pos);
        var dist = Vector2.Distance(segments[A].pos, segments[B].pos);

        if (physics.stiff || dist > physics.segmentLength)
        {
            var mov = dir * (dist - physics.segmentLength);

            segments[A].pos += mov * 0.5f;
            segments[A].vel += mov * 0.5f;
            segments[B].pos -= mov * 0.5f;
            segments[B].vel -= mov * 0.5f;
        }
    }

    private void ApplyRigidity(int A)
    {
        void func(int B2)
        {
            var B = A+1 + B2;
            if (B > 0 && B <= segments.Length)
            {
                var dir = Direction(segments[A].pos, segments[B-1].pos);
                segments[A].vel -= (dir * physics.rigid * physics.segmentLength)
                    / (Vector2.Distance(segments[A].pos, segments[B-1].pos) + 0.1f + MathF.Abs(B2));
                segments[B-1].vel += (dir * physics.rigid * physics.segmentLength)
                    / (Vector2.Distance(segments[A].pos, segments[B-1].pos) + 0.1f + MathF.Abs(B2)); 
            }
        };

        func(-2);
        func(2);
        func(-3);
        func(3);
        func(-4);
        func(4);
    }

    private Vector2 SmoothPos(int A)
    {
        if (A == 0)
        {
            if (release > -1)
                return posA;
            else
                return segments[A].pos;
        }
        else if (A == segments.Length - 1)
        {
            if (release < 1)
                return posB;
            else
                return segments[A].pos;
        }
        else
        {
            var smoothpos = (segments[A-1].pos + segments[A+1].pos) / 2f;
            return (segments[A].pos + smoothpos) / 2f;
        }
    }

    // not in the lingo source code
    private Vector2 SmoothPosOld(int A)
    {
        if (A == 0)
        {
            if (release > -1)
                return posA;
            else
                return segments[A].lastPos;
        }
        else if (A == segments.Length - 1)
        {
            if (release < 1)
                return posB;
            else
                return segments[A].lastPos;
        }
        else
        {
            var smoothpos = (segments[A-1].lastPos + segments[A+1].lastPos) / 2f;
            return (segments[A].lastPos + smoothpos) / 2f;
        }
    }

    private void PushRopePointOutOfTerrain(int A)
    {
        var p = new RopePoint()
        {
            Loc = segments[A].pos,
            LastLoc = segments[A].lastPos,
            Frc = segments[A].vel,
            SizePnt = Vector2.One * physics.segRad
        };
        p = SharedCheckVCollision(p, physics.friction, layer);
        segments[A].pos = p.Loc;
        segments[A].vel = p.Frc;

        var gridPos = GiveGridPos(segments[A].pos);
        
        loopFunc(new Vector2(0f, 0f));
        loopFunc(new Vector2(-1f, 0f));
        loopFunc(new Vector2(-1f, -1f));
        loopFunc(new Vector2(0f, -1));
        loopFunc(new Vector2(1f, -1));
        loopFunc(new Vector2(1f, 0f));
        loopFunc(new Vector2(1f, 1f));
        loopFunc(new Vector2(0f, 1f));
        loopFunc(new Vector2(-1f, 1f));

        void loopFunc(Vector2 dir)
        {
            if (AfaMvLvlEdit(gridPos+dir, layer) == 1)
            {
                var midPos = GiveMiddleOfTile(gridPos + dir);
                var terrainPos = new Vector2(
                    Math.Clamp(segments[A].pos.X, midPos.X-10f, midPos.X+10f),
                    Math.Clamp(segments[A].pos.Y, midPos.Y-10f, midPos.Y+10f)
                );
                terrainPos = ((terrainPos * 10f) + midPos) / 11f;

                var dir2 = Direction(segments[A].pos, terrainPos);
                var dist = Vector2.Distance(segments[A].pos, terrainPos);
                if (dist < physics.segRad)
                {
                    var mov = dir2 * (dist-physics.segRad);
                    segments[A].pos += mov;
                    segments[A].vel += mov;
                }
            }
        }
    }

    private RopePoint SharedCheckVCollision(RopePoint p, float friction, int layer)
    {
        var bounce = 0f;

        if (p.Frc.Y > 0)
        {
            var lastGridPos = GiveGridPos(p.LastLoc);
            var feetPos = GiveGridPos(p.Loc + new Vector2(0f, p.SizePnt.Y + 0.01f));
            var lastFeetPos = GiveGridPos(p.LastLoc + new Vector2(0f, p.SizePnt.Y));
            var leftPos = GiveGridPos(p.Loc + new Vector2(-p.SizePnt.X + 1f, p.SizePnt.Y + 0.01f));
            var rightPos = GiveGridPos(p.Loc + new Vector2(p.SizePnt.X - 1f, p.SizePnt.Y + 0.01f));

            // WARNING - idk if lingo calculate the loop direction
            for (float q = lastFeetPos.Y; q <= feetPos.Y; q++)
            {
                for (float c = leftPos.X; c <= rightPos.X; c++)
                {
                    if (AfaMvLvlEdit(new Vector2(c, q), layer) == 1 && AfaMvLvlEdit(new Vector2(c, q-1f), layer) != 1)
                    {
                        if (lastGridPos.Y >= q && AfaMvLvlEdit(lastGridPos, layer) == 1)
                        {}
                        else
                        {
                            p.Loc.Y = ((q-1f)*20f) - p.SizePnt.Y;
                            p.Frc.X *= friction;
                            p.Frc.Y = -p.Frc.Y * bounce;
                            return p;
                        }
                    }
                }
            }
        }
        else if (p.Frc.Y < 0)
        {
            var lastGridPos = GiveGridPos(p.LastLoc);
            var headPos = GiveGridPos(p.Loc - new Vector2(0f, p.SizePnt.Y + 0.01f));
            var lastHeadPos = GiveGridPos(p.LastLoc - new Vector2(0, p.SizePnt.Y));
            var leftPos = GiveGridPos(p.Loc + new Vector2(-p.SizePnt.X + 1f, p.SizePnt.Y + 0.01f));
            var rightPos = GiveGridPos(p.Loc + new Vector2(p.SizePnt.X - 1f, p.SizePnt.Y + 0.01f));

            // WARNING - idk if lingo calculates the loop direction
            for (float d = headPos.Y; d <= lastHeadPos.Y; d++)
            {
                var q = (lastHeadPos.Y) - (d-headPos.Y);
                for (float c = leftPos.X; c <= rightPos.X; c++)
                {
                    // WARNING - does <> mean not equal to?
                    if (AfaMvLvlEdit(new(c, q), layer) == 1 && AfaMvLvlEdit(new(c, q+1), layer) != 1)
                    {
                        if (lastGridPos.Y <= q && AfaMvLvlEdit(lastGridPos, layer) != 1)
                        {}
                        else
                        {
                            p.Loc.Y = (q*20f)+p.SizePnt.Y;
                            p.Frc.X *= friction;
                            p.Frc.Y = -p.Frc.Y * bounce;
                            return p;
                        }
                    }
                }
            }
        }

        return p;
    }
#endregion
}