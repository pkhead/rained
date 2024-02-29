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
    private readonly int layer;
    private readonly int release;
    private readonly RopePhysicalProperties physics;
    private readonly Segment[] segments;

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
        if (diff.LengthSquared() == 0) return Vector2.Zero;
        return Vector2.Normalize(diff) * t;
    }

    private static Vector2 Direction(Vector2 from, Vector2 to)
    {
        if (to == from) return Vector2.Zero;
        return Vector2.Normalize(to - from);
    }

    private static Vector2 GiveGridPos(Vector2 pos)
    {
        return new Vector2(
            MathF.Floor((pos.X / 20f) + 0.4999f),
            MathF.Floor((pos.Y / 20f) + 0.4999f)
        );
    }

    private static Vector2 GiveMiddleOfTile(Vector2 pos)
    {
        return new Vector2(
            (pos.X * 20f) - 10f,
            (pos.Y * 20f) - 10
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

    public RopeModel(
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
        posA = pA / Level.TileSize * 20f + Vector2.One;
        posB = pB / Level.TileSize * 20f + Vector2.One;
        physics = prop;

        float numberOfSegments = Vector2.Distance(pA, pB) / physics.segmentLength * lengthFac;
        numberOfSegments = Math.Max(numberOfSegments, 3);

        float step = Vector2.Distance(pA, pB) / numberOfSegments;
        
        segments = new Segment[(int) numberOfSegments];
        for (int i = 0; i < numberOfSegments; i++)
        {
            segments[i] = new Segment()
            {
                pos = pA + MoveToPoint(pA, pB, (i+0.5f)*step),
                lastPos = pA + MoveToPoint(pA, pB, (i+0.5f)*step),
                vel = Vector2.Zero
            };
        }
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
                segments[1].pos = new Vector2(
                    Lerp(segments[0].pos.X, idealFirstPos.X, physics.edgeDirection),
                    Lerp(segments[0].pos.Y, idealFirstPos.Y, physics.edgeDirection)
                );
            }

            if (release < 1)
            {
                // WARNING - indexing
                for (int A = 0; A <= segments.Length / 2f - 1; A++)
                {
                    var fac = 1f - A / (segments.Length / 2f);
                    fac *= fac;
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
            segments[1].pos = posA;
            segments[1].vel = Vector2.Zero;
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

        for (int i = 1; i < segments.Length; i++)
        {
            var a = segments.Length - i;
            ConnectRopePoints(a, a+1);
            if (physics.rigid > 0)
                ApplyRigidity(i);
        }

        if (physics.selfPush > 0)
        {
            for (int A = 0; A < segments.Length; A++)
            {
                for (int B = 1; B < segments.Length; B++)
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

    public void ConnectRopePoints(int A, int B)
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

    public void ApplyRigidity(int A)
    {
        void func(int B2)
        {
            var B = A + B2;
            if (B >= 0 && B < segments.Length)
            {
                var dir = Direction(segments[A].pos, segments[B].pos);
                segments[A].vel -= (dir * physics.rigid * physics.segmentLength)
                    / (Vector2.Distance(segments[A].pos, segments[B].pos) + 0.1f + MathF.Abs(B2));
                segments[B].vel += (dir * physics.rigid * physics.segmentLength)
                    / (Vector2.Distance(segments[A].pos, segments[B].pos) + 0.1f + MathF.Abs(B2)); 
            }
        };

        // WARNING - if there is an error, check to see if this indexing is correct
        func(-1);
        func(1);
        func(-2);
        func(2);
        func(-3);
        func(3);
    }

    public Vector2 SmoothPos(int A)
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

    public void PushRopePointOutOfTerrain(int A)
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
        var _list = new Vector2[]
        {
            new(0f, 0f),
            new(-1f, 0f),
            new(-1f, -1f),
            new(0f, -1),
            new(1f, -1),
            new(1f, 0f),
            new(1f, 1f),
            new(0f, 1f),
            new(-1f, 1f)
        };

        foreach (var dir in _list)
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
                    // WARNING - does <> mean not equal to?
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

    // wtf is this function name?
    private static int AfaMvLvlEdit(Vector2 p, int layer)
    {
        int x = (int)p.X - 1;
        int y = (int)p.Y - 1;
        var level = RainEd.Instance.Level;
        if (level.IsInBounds(x, y))
            return (int) RainEd.Instance.Level.Layers[layer, (int)p.X, (int)p.Y].Cell;
        else
            return 1;
    }
}