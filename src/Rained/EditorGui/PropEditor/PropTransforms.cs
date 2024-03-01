using System.Numerics;
using ImGuiNET;

namespace RainEd;

partial class PropEditor : IEditorMode
{
    interface ITransformMode
    {
        void Update(Vector2 mouseDragStart, Vector2 mousePos);
    }

    // A copy of a prop's transform
    readonly struct PropTransform
    {
        public readonly bool isAffine;
        public readonly Vector2[] quad;
        public readonly RotatedRect rect;

        public PropTransform(Prop prop)
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
                origPropTransforms[i] = new PropTransform(props[i]);
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

            // clamp size
            //newRect.Size.X = MathF.Max(0.1f, newRect.Size.X);
            //newRect.Size.Y = MathF.Max(0.1f, newRect.Size.Y);

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
                origTransforms[i] = new PropTransform(props[i]);
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
}