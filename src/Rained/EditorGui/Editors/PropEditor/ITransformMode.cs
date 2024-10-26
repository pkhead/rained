using System.Numerics;
using ImGuiNET;
using Rained.LevelData;
using Raylib_cs;
namespace Rained.EditorGui.Editors;

partial class PropEditor : IEditorMode
{
    interface ITransformMode
    {
        void Update(Vector2 mouseDragStart, Vector2 mousePos);
        bool Deactivated()
            => EditorWindow.IsMouseReleased(ImGuiMouseButton.Left);
    }

    class MoveTransformMode : ITransformMode
    {
        private readonly PropTransform[] dragInitPositions;
        private readonly Prop[] props;
        private readonly Dictionary<Prop, Vector2[]> initRopePoints;
        private readonly float snap;

        public MoveTransformMode(List<Prop> props, float snap)
        {
            this.props = props.ToArray();
            this.snap = snap;

            // record initial drag positions
            dragInitPositions = new PropTransform[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                dragInitPositions[i] = props[i].Transform.Clone();
            }

            // record initial rope points
            initRopePoints = new Dictionary<Prop, Vector2[]>();
            for (int i = 0; i < props.Count; i++)
            {
                var rope = props[i].Rope;
                if (rope is not null && rope.Model is not null)
                {
                    var ptArr = new Vector2[rope.Model.SegmentCount];
                    for (int j = 0; j < rope.Model.SegmentCount; j++)
                    {
                        ptArr[j] = rope.Model.GetSegmentPos(j);
                    }

                    initRopePoints.Add(props[i], ptArr);
                }
            }
        }

        public void Update(Vector2 dragStartPos, Vector2 mousePos)
        {
            bool posSnap = props.Length == 1 && props[0].IsAffine;

            var mouseDelta = mousePos - dragStartPos;
            
            if (snap > 0 && !posSnap)
            {
                mouseDelta = Snap(mouseDelta, snap);
            }

            for (int i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                if (!prop.IsMovable) continue;

                if (prop.IsAffine)
                    prop.Rect.Center = dragInitPositions[i].rect.Center + mouseDelta;

                    if (snap > 0 && posSnap)
                    {
                        prop.Rect.Center = Snap(prop.Rect.Center, snap);
                    }

                    // move rope points as well without resetting it
                    var rope = prop.Rope;
                    if (rope is not null && rope.Model is not null)
                    {
                        var initPts = initRopePoints[prop];

                        rope.IgnoreMovement();
                        for (int j = 0; j < rope.Model.SegmentCount; j++)
                        {
                            rope.Model.SetSegmentPosition(j, initPts[j] + (prop.Rect.Center - dragInitPositions[i].rect.Center));
                        }
                    }
                else
                {
                    var pts = prop.QuadPoints;
                    pts[0] = dragInitPositions[i].quad[0] + mouseDelta;
                    pts[1] = dragInitPositions[i].quad[1] + mouseDelta;
                    pts[2] = dragInitPositions[i].quad[2] + mouseDelta;
                    pts[3] = dragInitPositions[i].quad[3] + mouseDelta;
                }
            }
        }
    }
    
    class ScaleTransformMode : ITransformMode
    {
        public readonly int handleId; // even = corner, odd = edge
        private readonly RotatedRect origRect;
        private readonly PropTransform[] origPropTransforms;

        private readonly Vector2 handleOffset;
        private readonly Vector2 propRight;
        private readonly Vector2 propUp;
        private readonly Matrix3x2 rotationMatrix;
        private readonly bool mustMaintainProportions;
        private readonly float snap;

        private readonly Prop[] props;
        public ScaleTransformMode(int handleId, List<Prop> props, float snap)
        {
            this.props = props.ToArray();
            this.handleId = handleId;
            this.snap = snap;

            handleOffset = handleId switch
            {
                0 => new Vector2(-1f, -1f),
                1 => new Vector2( 0f, -1f),
                2 => new Vector2( 1f, -1f),
                3 => new Vector2( 1f,  0f),
                4 => new Vector2( 1f,  1f),
                5 => new Vector2( 0f,  1f),
                6 => new Vector2(-1f,  1f),
                7 => new Vector2(-1f,  0f),
                _ => throw new Exception("Invalid handleId")
            };

            if (props.Count > 1 || !props[0].IsAffine)
            {
                var extents = CalcPropExtents(props);
                origRect = new RotatedRect()
                {
                    Center = extents.Position + extents.Size / 2f,
                    Size = extents.Size,
                    Rotation = 0f
                };

                propRight = Vector2.UnitX;
                propUp = -Vector2.UnitY;
                rotationMatrix = Matrix3x2.Identity;
            }
            else
            {
                var prop = props[0];
                origRect = prop.Rect;
                propRight = new Vector2(MathF.Cos(prop.Rect.Rotation), MathF.Sin(prop.Rect.Rotation));
                propUp = new Vector2(propRight.Y, -propRight.X);
                rotationMatrix = Matrix3x2.CreateRotation(origRect.Rotation);
            }

            origPropTransforms = new PropTransform[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                origPropTransforms[i] = props[i].Transform.Clone();
            }

            // proportion maintenance is required if there is more than
            // one prop selected, and at least one affine prop in the selection
            mustMaintainProportions = false;
            if (props.Count > 1)
            {
                foreach (var prop in props)
                {
                    if (prop.IsAffine)
                    {
                        mustMaintainProportions = true;
                        break;
                    }
                }
            }
        }

        public void Update(Vector2 dragStartPos, Vector2 mousePos)
        {
            Vector2 scaleAnchor;

            if (EditorWindow.IsKeyDown(ImGuiKey.ModCtrl))
            {
                scaleAnchor = origRect.Center;
            }
            else
            {
                // the side opposite to the active handle
                scaleAnchor =
                    Vector2.Transform(origRect.Size / 2f * -handleOffset, rotationMatrix)
                    + origRect.Center;
            }

            // calculate vector deltas from scale anchor to original handle pos and mouse position
            // these take the prop's rotation into account
            var origDx = Vector2.Dot(propRight, dragStartPos - scaleAnchor);
            var origDy = Vector2.Dot(propUp, dragStartPos - scaleAnchor);
            var mouseDx = Vector2.Dot(propRight, mousePos - scaleAnchor);
            var mouseDy = Vector2.Dot(propUp, mousePos - scaleAnchor);
            var scale = new Vector2(
                Snap(mouseDx, snap) / Snap(origDx, snap),
                Snap(mouseDy, snap) / Snap(origDy, snap)
            );

            // lock on axis if dragging an edge handle
            if (handleOffset.X == 0f)
                scale.X = 1f;
            if (handleOffset.Y == 0f)
                scale.Y = 1f;
            
            // maintain a minimum size, because if the length at one axis
            // is 0, and the user resizes it afterwards, it will cause the length at that axis
            // to be NaN, which would make the prop disappear and, more urgently, crash the
            // program if it's a rope simulation
            if (MathF.Abs(origRect.Size.X * scale.X) < 0.5f)
            {
                scale.X = MathF.Abs(0.5f / origRect.Size.X) * (scale.X >= 0f ? 1f : -1f);
            }

            if (MathF.Abs(origRect.Size.Y * scale.Y) < 0.5f)
            {
                scale.Y = MathF.Abs(0.5f / origRect.Size.Y) * (scale.Y >= 0f ? 1f : -1f);
            }
            
            // hold shift to maintain proportions
            // if scaling multiple props at once, this is the only valid mode. curse you, rotation!!!  
            if (mustMaintainProportions || EditorWindow.IsKeyDown(ImGuiKey.ModShift))
            {
                if (handleOffset.X == 0f)
                {
                    scale.X = scale.Y;
                }
                else if (handleOffset.Y == 0f)
                {
                    scale.Y = scale.X;
                }
                else
                {
                    if (scale.X > scale.Y)
                        scale.Y = scale.X;
                    else
                        scale.X = scale.Y;
                }
            }

            // apply size scale
            var newRect = origRect;
            newRect.Size *= scale;

            // anchor the prop to the anchor point
            if (EditorWindow.IsKeyDown(ImGuiKey.ModCtrl))
            {
                newRect.Center = origRect.Center;
            }
            else
            {
                newRect.Center = scaleAnchor + Vector2.Transform(newRect.Size / 2f * handleOffset, rotationMatrix);
            }
            
            // scale selected props
            for (int i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                if (!prop.IsMovable) continue;

                if (prop.IsAffine)
                {
                    ref var propTransform = ref prop.Rect;
                    var origTransform = origPropTransforms[i].rect;

                    propTransform.Size = origTransform.Size * scale;
                    
                    // clamp size
                    //propTransform.Size.X = MathF.Max(0.1f, propTransform.Size.X);
                    //propTransform.Size.Y = MathF.Max(0.1f, propTransform.Size.Y);

                    // position prop
                    var propOffset = (origTransform.Center - origRect.Center) / origRect.Size;
                    propTransform.Center = Vector2.Transform(propOffset * newRect.Size, rotationMatrix) + newRect.Center;
                }
                else
                {
                    var propQuad = prop.QuadPoints;
                    var origQuad = origPropTransforms[i].quad;

                    for (int k = 0; k < 4; k++)
                    {
                        var scaleOffset = (origQuad[k] - origRect.Center) / origRect.Size;
                        propQuad[k] = Vector2.Transform(scaleOffset * newRect.Size, rotationMatrix) + newRect.Center;
                    }
                }
            }
        }
    }

    class WarpTransformMode : ITransformMode
    {
        public readonly int handleId;
        public readonly Prop prop;
        public readonly float snap;

        public WarpTransformMode(
            int handleId,
            Prop prop,
            float snap
        )
        {
            this.prop = prop;
            this.handleId = handleId;
            this.snap = snap;

            if (prop.IsAffine)
            {
                prop.ConvertToFreeform();
            }
        }

        public void Update(Vector2 dragStart, Vector2 mousePos)
        {
            prop.QuadPoints[handleId] = Snap(mousePos, snap);

            // hold shift for vertex snapping
            if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
            {
                float minDist = 1.0f / RainEd.Instance.LevelView.ViewZoom;
                minDist *= minDist;

                Vector2 snapPos = prop.QuadPoints[handleId];
                foreach (var prop in RainEd.Instance.Level.Props)
                {
                    if (prop == this.prop) continue;
                    var quadPts = prop.QuadPoints;

                    for (int i = 0; i < 4; i++)
                    {
                        var d = Vector2.DistanceSquared(quadPts[i], mousePos);
                        if (d < minDist)
                        {
                            snapPos = quadPts[i];
                            minDist = d;
                        }
                    }
                }

                prop.QuadPoints[handleId] = snapPos;
            }
        }
    }

    class LongTransformMode : ITransformMode
    {
        public readonly int handleId;
        public readonly Prop prop;
        public readonly float snap;

        private readonly Vector2 origPA;
        private readonly Vector2 origPB;

        public LongTransformMode(
            int handleId,
            Prop prop,
            float snap
        )
        {
            this.prop = prop;
            this.handleId = handleId;
            this.snap = snap;

            var rope = prop.Rope;
            if (rope is not null)
            {
                origPA = rope.PointA;
                origPB = rope.PointB;
            }
            else
            {
                var cos = MathF.Cos(prop.Rect.Rotation);
                var sin = MathF.Sin(prop.Rect.Rotation);
                origPA = prop.Rect.Center + new Vector2(cos, sin) * -prop.Rect.Size.X / 2f;
                origPB = prop.Rect.Center + new Vector2(cos, sin) * prop.Rect.Size.X / 2f;
            }
        }

        private static Vector2 DirectionTo(Vector2 from, Vector2 to)
        {
            if (from == to)
                return Vector2.UnitX;
            else
                return Vector2.Normalize(to - from);
        }

        public void Update(Vector2 dragStart, Vector2 mousePos)
        {
            var pA = origPA;
            var pB = origPB;
            Vector2 anchor;

            if (handleId == 0)
            {
                pA = Snap(mousePos, snap);
                anchor = pB;
            }
            else
            {
                pB = Snap(mousePos, snap);
                anchor = pA;
            }

            // minimum size of 0.5 units
            {
                var diff = pB - pA;
                if (diff.LengthSquared() < 0.5f * 0.5f)
                {
                    if (handleId == 0)
                    {
                        pA = DirectionTo(anchor, pA) * 0.5f + anchor;
                    }
                    else
                    {
                        pB = DirectionTo(anchor, pB) * 0.5f + anchor;
                    }
                }
            }

            // enforce constraint
            var dir = DirectionTo(pA, pB);
            prop.Rect.Center = (pA + pB) / 2f;
            prop.Rect.Rotation = MathF.Atan2(dir.Y, dir.X);
            prop.Rect.Size.X = Vector2.Distance(pA, pB);
        }
    }

    class RotateTransformMode : ITransformMode
    {
        private readonly Vector2 rotCenter;
        private readonly PropTransform[] origTransforms;
        private readonly Prop[] props;

        public RotateTransformMode(Vector2 rotCenter, List<Prop> props)
        {
            this.props = props.ToArray();
            this.rotCenter = rotCenter;
            origTransforms = new PropTransform[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                origTransforms[i] = props[i].Transform.Clone();
            }
        }

        public void Update(Vector2 dragStartPos, Vector2 mousePos)
        {
            var startDir = Vector2.Normalize(dragStartPos - rotCenter);
            var curDir = Vector2.Normalize(mousePos - rotCenter);
            var angleDiff = MathF.Atan2(curDir.Y, curDir.X) - MathF.Atan2(startDir.Y, startDir.X);

            if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
            {
                var snap = MathF.PI / 8f;
                angleDiff = MathF.Round(angleDiff / snap) * snap; 
            }

            var rotMat = Matrix3x2.CreateRotation(angleDiff);

            for (int i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                if (!prop.IsMovable) continue;

                if (prop.IsAffine)
                {
                    var origTransform = origTransforms[i].rect;
                    prop.Rect.Rotation = origTransform.Rotation + angleDiff;
                    prop.Rect.Center = Vector2.Transform(origTransform.Center - rotCenter, rotMat) + rotCenter;
                }
                else
                {
                    var pts = prop.QuadPoints;
                    var origPts = origTransforms[i].quad;
                    for (int k = 0; k < 4; k++)
                    {
                        pts[k] = Vector2.Transform(origPts[k] - rotCenter, rotMat) + rotCenter;
                    }
                }
            }
        }
    }

    class KeyboardRotateTransformMode : ITransformMode
    {
        private readonly Vector2 rotCenter;
        private readonly PropTransform[] origTransforms;
        private readonly Prop[] props;
        private float rotationAngle = 0f;

        public KeyboardRotateTransformMode(Vector2 rotCenter, List<Prop> props)
        {
            this.props = [..props];
            this.rotCenter = rotCenter;
            origTransforms = new PropTransform[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                origTransforms[i] = props[i].Transform.Clone();
            }
        }

        public void Update(Vector2 dragStartPos, Vector2 mousePos)
        {
            var rotSpeed = Raylib.GetFrameTime() * (60f / 180f * MathF.PI);;

            if (KeyShortcuts.Active(KeyShortcut.RotatePropCCW))
                rotationAngle -= rotSpeed;

            if (KeyShortcuts.Active(KeyShortcut.RotatePropCW))
                rotationAngle += rotSpeed;
            
            var rotMat = Matrix3x2.CreateRotation(rotationAngle);

            for (int i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                if (!prop.IsMovable) continue;

                if (prop.IsAffine)
                {
                    var origTransform = origTransforms[i].rect;
                    prop.Rect.Rotation = origTransform.Rotation + rotationAngle;
                    prop.Rect.Center = Vector2.Transform(origTransform.Center - rotCenter, rotMat) + rotCenter;
                }
                else
                {
                    var pts = prop.QuadPoints;
                    var origPts = origTransforms[i].quad;
                    for (int k = 0; k < 4; k++)
                    {
                        pts[k] = Vector2.Transform(origPts[k] - rotCenter, rotMat) + rotCenter;
                    }
                }
            }
        }

        public bool Deactivated()
        {
            return !(KeyShortcuts.Active(KeyShortcut.RotatePropCCW) || KeyShortcuts.Active(KeyShortcut.RotatePropCW));
        }
    }
}