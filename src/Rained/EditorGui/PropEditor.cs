using ImGuiNET;
using RainEd.Props;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
namespace RainEd;
class PropEditor : IEditorMode
{
    interface ITransformState
    {
        void Update();
    }

    public string Name { get => "Props"; }
    private readonly EditorWindow window;
    private int selectedGroup = 0;
    private int currentSelectorMode = 0;
    private PropInit? selectedInit = null;
    private readonly List<Prop> selectedProps = new();
    private List<Prop>? initSelectedProps = null; // used for add rect select mode
    private Vector2 prevMousePos;
    private Vector2 dragStartPos;

    private readonly Color HighlightColor = new(0, 0, 255, 255);
    private readonly Color HighlightColorGlow = new(50, 50, 255, 255);
    private readonly Color HighlightColor2 = new(180, 180, 180, 255);
    private readonly Color HighlightColor2Glow = new(255, 255, 255, 255);

    private bool isMouseDragging = false;
    private enum DragMode
    {
        Select,
        Move
    };
    private DragMode dragMode;

#region Transform Modes
    // aaa how do i structure this??

    // A snapshot of a prop's transform
    readonly struct PropTransform
    {
        public readonly bool isAffine;
        public readonly Vector2[] quads;
        public readonly Prop.AffineTransform affine;

        public PropTransform(Prop prop)
        {
            quads = new Vector2[4];
            isAffine = prop.IsAffine;
            if (isAffine)
            {
                affine = prop.Transform;
            }
            else
            {
                var pts = prop.QuadPoints;
                for (int i = 0; i < 4; i++)
                {
                    quads[i] = pts[i];
                }
            }
        }
    }
    
    class ScaleTransformMode
    {
        public readonly int handleId; // even = corner, odd = edge
        public readonly Prop.AffineTransform origRect;
        public readonly PropTransform[] origPropTransforms;

        public readonly Vector2 handleOffset;
        public readonly Vector2 propRight;
        public readonly Vector2 propUp;
        public readonly Matrix3x2 rotationMatrix;
        public readonly bool mustMaintainProportions;

        public ScaleTransformMode(int handleId, List<Prop> props)
        {
            this.handleId = handleId;

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
                origRect = new Prop.AffineTransform()
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
                origRect = prop.Transform;
                propRight = new Vector2(MathF.Cos(prop.Transform.Rotation), MathF.Sin(prop.Transform.Rotation));
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
    }

    class WarpTransformMode
    {
        public readonly int handleId;

        public WarpTransformMode(
            int handleId
        )
        {
            this.handleId = handleId;
        }
    }

    class RotateTransformMode
    {
        public readonly Vector2 rotCenter;
        public readonly PropTransform[] origTransforms;

        public RotateTransformMode(Vector2 rotCenter, List<Prop> props)
        {
            this.rotCenter = rotCenter;
            origTransforms = new PropTransform[props.Count];
            for (int i = 0; i < props.Count; i++)
            {
                origTransforms[i] = new PropTransform(props[i]);
            }
        }
    }

    private enum TransformMode
    {
        None,
        Scale,
        Warp,
        Rotate
    }
    private TransformMode? transformMode;
    private ScaleTransformMode? scaleMode;
    private WarpTransformMode? warpMode;
    private RotateTransformMode? rotateMode;
#endregion

    public PropEditor(EditorWindow window)
    {
        this.window = window;
    }

    public void Load()
    {
        selectedProps.Clear();
        initSelectedProps = null;
        isMouseDragging = false;
    }

    public void DrawToolbar()
    {
        var propDb = RainEd.Instance.PropDatabase;

        if (ImGui.Begin("Props", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.SeparatorText("Create Prop");

            if (ImGui.BeginTabBar("PropSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetTextLineHeight() * 50.0f;

                if (ImGui.BeginTabItem("Props"))
                {
                    // if tab changed, reset selected group back to 0
                    if (currentSelectorMode != 0)
                    {
                        currentSelectorMode = 0;
                        selectedGroup = 0;
                    }

                    // group list box
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        for (int i = 0; i < propDb.Categories.Count; i++)
                        {
                            var group = propDb.Categories[i];
                            if (group.IsTileCategory) continue; // skip Tiles as props categories

                            if (ImGui.Selectable(group.Name, selectedGroup == i))
                                selectedGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.Categories[selectedGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            if (ImGui.Selectable(prop.Name, prop == selectedInit))
                            {
                                selectedInit = prop;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                var previewRect = prop.GetPreviewRectangle(0, prop.LayerCount / 2);
                                rlImGui.ImageRect(
                                    prop.Texture,
                                    (int)previewRect.Width, (int)previewRect.Height,
                                    previewRect
                                );
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Tiles"))
                {
                    // if tab changed, reset selected group back to 0
                    if (currentSelectorMode != 1)
                    {
                        currentSelectorMode = 1;
                        selectedGroup = 0;
                    }

                    // group list box
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        for (int i = 0; i < propDb.TileCategories.Count; i++)
                        {
                            if (ImGui.Selectable(propDb.TileCategories[i].Name, selectedGroup == i))
                                selectedGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.TileCategories[selectedGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            if (ImGui.Selectable(prop.Name, prop == selectedInit))
                            {
                                selectedInit = prop;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                var previewRect = prop.GetPreviewRectangle(0, prop.LayerCount / 2);
                                rlImGui.ImageRect(
                                    prop.Texture,
                                    (int)previewRect.Width, (int)previewRect.Height,
                                    previewRect
                                );
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.SeparatorText("Options");

            // work layer
            {
                int workLayerV = window.WorkLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("Work Layer", ref workLayerV);
                window.WorkLayer = Math.Clamp(workLayerV, 1, 3) - 1;
            }

            // prop transformation mode
            if (selectedProps.Count > 0)
            {
                bool canConvert = false;

                foreach (var prop in selectedProps)
                {
                    if (prop.IsAffine)
                    {
                        canConvert = true;
                        break;
                    }
                }

                if (!canConvert)
                    ImGui.BeginDisabled();
                
                if (ImGui.Button("Convert to Warpable Prop"))
                {
                    foreach (var prop in selectedProps)
                        prop.ConvertToFreeform();
                }

                if (!canConvert)
                    ImGui.EndDisabled();
            }

            // sublayer
            // prop settings
            // notes + synopses
            
            ImGui.End();
        }
    }

    private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        static float sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        float d1 = sign(pt, v1, v2);
        float d2 = sign(pt, v2, v3);
        float d3 = sign(pt, v3, v1);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static Prop? GetPropAt(Vector2 point)
    {
        foreach (var prop in RainEd.Instance.Level.Props)
        {
            var pts = prop.QuadPoints;
            if (
                IsPointInTriangle(point, pts[0], pts[1], pts[2]) ||
                IsPointInTriangle(point, pts[2], pts[3], pts[0])    
            )
            {
                return prop;
            }
        }

        return null;
    }

    /*private static Rectangle GetPropAABB(Prop prop)
    {
        var minX = Math.Min(prop.Quad[0].X, Math.Min(prop.Quad[1].X, Math.Min(prop.Quad[2].X, prop.Quad[3].X)));
        var minY = Math.Min(prop.Quad[0].Y, Math.Min(prop.Quad[1].Y, Math.Min(prop.Quad[2].Y, prop.Quad[3].Y)));
        var maxX = Math.Max(prop.Quad[0].X, Math.Min(prop.Quad[1].X, Math.Min(prop.Quad[2].X, prop.Quad[3].X)));
        var maxY = Math.Max(prop.Quad[0].Y, Math.Min(prop.Quad[1].Y, Math.Min(prop.Quad[2].Y, prop.Quad[3].Y)));
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }*/
    private Rectangle GetSelectionAABB()
        => CalcPropExtents(selectedProps);

    private static Rectangle CalcPropExtents(List<Prop> props)
    {
        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var prop in props)
        {
            for (int i = 0; i < 4; i++)
            {
                var pts = prop.QuadPoints;
                minX = Math.Min(minX, pts[i].X);
                minY = Math.Min(minY, pts[i].Y);
                maxX = Math.Max(maxX, pts[i].X);
                maxY = Math.Max(maxY, pts[i].Y);
            }
        }

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    private static Vector2 GetPropCenter(Prop prop)
    {
        if (prop.IsAffine)
            return prop.Transform.Center;
        
        var pts = prop.QuadPoints;
        return (pts[0] + pts[1] + pts[2] + pts[3]) / 4f;
    }

    // returns true if gizmo is hovered, false if not
    private bool DrawGizmoHandle(Vector2 pos, bool secondaryColor = false)
    {
        bool isGizmoHovered = window.IsViewportHovered && (window.MouseCellFloat - pos).Length() < 0.5f / window.ViewZoom;
        
        Color color;
        if (secondaryColor)
        {
            color = isGizmoHovered ? HighlightColor2Glow : HighlightColor2;
        }
        else
        {
            color = isGizmoHovered ? HighlightColorGlow : HighlightColor;
        }

        Raylib.DrawCircleV(
            pos * Level.TileSize,
            (isGizmoHovered ? 8f : 4f) / window.ViewZoom,
            color
        );

        return isGizmoHovered;
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        bool wasMouseDragging = isMouseDragging;
        isMouseDragging = false;

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));

        // draw layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrame);

            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            levelRender.RenderGeometry(l, new Color(0, 0, 0, 255));
            levelRender.RenderTiles(l, 255);
            levelRender.RenderProps(l, 255);
            
            // draw alpha-blended result into main frame
            Raylib.BeginTextureMode(mainFrame);
            Rlgl.PushMatrix();
            Rlgl.LoadIdentity();

            int offset = l * 2;
            var alpha = l == window.WorkLayer ? 255 : 50;
            Raylib.DrawTextureRec(
                layerFrame.Texture,
                new Rectangle(0f, layerFrame.Texture.Height, layerFrame.Texture.Width, -layerFrame.Texture.Height),
                Vector2.One * offset,
                new Color(255, 255, 255, alpha)
            );
            Rlgl.PopMatrix();
        }

        levelRender.RenderGrid();
        levelRender.RenderBorder();

        // highlight selected props
        foreach (var prop in selectedProps)
        {
            var pts = prop.QuadPoints;
            var col = prop.IsAffine ? HighlightColor : HighlightColor2;
            Raylib.DrawLineEx(pts[0] * Level.TileSize, pts[1] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[1] * Level.TileSize, pts[2] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[2] * Level.TileSize, pts[3] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[3] * Level.TileSize, pts[0] * Level.TileSize, 1f / window.ViewZoom, col);
        }

        // prop transform gizmos
        if (selectedProps.Count > 0)
        {
            bool canWarp = transformMode == TransformMode.Warp ||
                (ImGui.IsKeyDown(ImGuiKey.F) && selectedProps.Count == 1 && !selectedProps[0].IsAffine);
            
            var aabb = GetSelectionAABB();

            // draw selection AABB if there is more than
            // one prop selected
            if (!canWarp && (selectedProps.Count > 1 || !selectedProps[0].IsAffine))
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        aabb.Position * Level.TileSize,
                        aabb.Size * Level.TileSize
                    ),
                    1f / window.ViewZoom,
                    HighlightColor
                );
            }
            
            // scale gizmo (points on corners/edges)
            // don't draw handles if rotating
            if ((transformMode == TransformMode.None && !canWarp) || transformMode == TransformMode.Scale)
            {
                Vector2[] corners;

                if (selectedProps.Count == 1 && selectedProps[0].IsAffine)
                {
                    corners = selectedProps[0].QuadPoints;
                }
                else
                {
                    corners = new Vector2[4]
                    {
                        aabb.Position + aabb.Size * new Vector2(0f, 0f),
                        aabb.Position + aabb.Size * new Vector2(1f, 0f),
                        aabb.Position + aabb.Size * new Vector2(1f, 1f),
                        aabb.Position + aabb.Size * new Vector2(0f, 1f),
                    };
                };

                // even i's are corner points
                // odd i's are edge points
                for (int i = 0; i < 8; i++)
                {
                    // don't draw this handle if another scale handle is active
                    if (scaleMode is not null && scaleMode.handleId != i)
                    {
                        continue;
                    }
                    
                    var handle1 = corners[i / 2]; // position of left corner
                    var handle2 = corners[(i + 1) / 2 % 4]; // position of right corner
                    var handlePos = (handle1 + handle2) / 2f;
                    
                    // draw gizmo handle at corner
                    if (DrawGizmoHandle(handlePos) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        transformMode = TransformMode.Scale;
                        scaleMode = new ScaleTransformMode(
                            handleId: i,
                            props: selectedProps
                        );
                    }
                }
            }

            // rotation gizmo (don't draw if scaling or rotating) 
            if (transformMode == TransformMode.None && !canWarp)
            {
                Vector2 sideDir = Vector2.UnitX;
                Vector2 handleDir = -Vector2.UnitY;
                Vector2 handleCnPos = aabb.Position + new Vector2(aabb.Width / 2f, 0f);

                if (selectedProps.Count == 1 && selectedProps[0].IsAffine)
                {
                    handleCnPos = (selectedProps[0].QuadPoints[0] + selectedProps[0].QuadPoints[1]) / 2f;
                    sideDir = Vector2.Normalize(selectedProps[0].QuadPoints[1] - selectedProps[0].QuadPoints[0]);
                    handleDir = new(sideDir.Y, -sideDir.X);
                }

                Vector2 rotDotPos = handleCnPos + handleDir * 5f;

                // draw line to gizmo handle
                Raylib.DrawLineEx(
                    startPos: handleCnPos * Level.TileSize,
                    endPos: rotDotPos * Level.TileSize,
                    1f / window.ViewZoom,
                    HighlightColor
                );

                // draw gizmo handle
                if (DrawGizmoHandle(rotDotPos) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    transformMode = TransformMode.Rotate;
                    rotateMode = new RotateTransformMode(
                        rotCenter: aabb.Position + aabb.Size / 2f,
                        props: selectedProps
                    );
                }
            }

            // freeform warp gizmo
            if ((transformMode == TransformMode.None && canWarp) || transformMode == TransformMode.Warp)
            {
                Vector2[] corners = selectedProps[0].QuadPoints;
                
                for (int i = 0; i < 4; i++)
                {
                    // don't draw this handle if another scale handle is active
                    if (warpMode is not null && warpMode.handleId != i)
                    {
                        continue;
                    }

                    var handlePos = corners[i];
                    
                    // draw gizmo handle at corner
                    if (DrawGizmoHandle(handlePos, true) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        transformMode = TransformMode.Warp;
                        warpMode = new WarpTransformMode(
                            handleId: i
                        );
                    }
                }
            }
        }

        // draw drag rect
        if (wasMouseDragging && dragMode == DragMode.Select)
        {
            var minX = Math.Min(dragStartPos.X, window.MouseCellFloat.X);
            var maxX = Math.Max(dragStartPos.X, window.MouseCellFloat.X);
            var minY = Math.Min(dragStartPos.Y, window.MouseCellFloat.Y);
            var maxY = Math.Max(dragStartPos.Y, window.MouseCellFloat.Y);

            var rect = new Rectangle(
                minX * Level.TileSize,
                minY * Level.TileSize,
                (maxX - minX) * Level.TileSize,
                (maxY - minY) * Level.TileSize
            );
            Raylib.DrawRectangleRec(rect, new Color(HighlightColor.R, HighlightColor.G, HighlightColor.B, (byte)80));
            Raylib.DrawRectangleLinesEx(rect, 1f / window.ViewZoom, HighlightColor);

            // select all props within selection rectangle
            selectedProps.Clear();
            
            if (initSelectedProps is not null)
            {
                foreach (var prop in initSelectedProps)
                    selectedProps.Add(prop);
            }

            foreach (var prop in level.Props)
            {
                var pc = GetPropCenter(prop);
                if (pc.X >= minX && pc.Y >= minY && pc.X <= maxX && pc.Y <= maxY)
                    selectedProps.Add(prop);
            }
        }

        if (window.IsViewportHovered)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                dragStartPos = window.MouseCellFloat;
            }

            // in prop transform mode
            if (transformMode != TransformMode.None)
            {
                if (transformMode == TransformMode.Warp)
                    TransformWarpUpdate();
                else if (transformMode == TransformMode.Scale)
                    TransformScaleAffineUpdate();
                else if (transformMode == TransformMode.Rotate)
                    TransformRotateUpdate();
                
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    transformMode = TransformMode.None;
                    warpMode = null;
                    scaleMode = null;
                    rotateMode = null;
                }
            }
            else
            {
                // in default mode
                PropSelectUpdate(wasMouseDragging);
            }
        }

        prevMousePos = window.MouseCellFloat;
    }

    public void PropSelectUpdate(bool wasMouseDragging)
    {
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            if (!wasMouseDragging)
            {
                // drag had begun
                var hoverProp = GetPropAt(dragStartPos);

                // if dragging over an empty space, begin rect select
                if (hoverProp is null)
                {
                    dragMode = DragMode.Select;

                    // if shift is held, rect select Adds instead of Replace
                    if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                        initSelectedProps = selectedProps.ToList(); // clone selection list
                    else
                        initSelectedProps = null;
                }
                else
                {
                    // if draggging over a prop, drag all currently selected props
                    // if active prop is in selection. if not, then set selection
                    // to this prop
                    dragMode = DragMode.Move;
                    if (!selectedProps.Contains(hoverProp))
                    {
                        selectedProps.Clear();
                        selectedProps.Add(hoverProp);
                    }
                }
            }
            isMouseDragging = true;

            // move drag
            if (dragMode == DragMode.Move)
            {
                var mouseDelta = window.MouseCellFloat - prevMousePos;
                foreach (var prop in selectedProps)
                {
                    if (prop.IsAffine)
                        prop.Transform.Center += mouseDelta;
                    else
                    {
                        var pts = prop.QuadPoints;
                        pts[0] += mouseDelta;
                        pts[1] += mouseDelta;
                        pts[2] += mouseDelta;
                        pts[3] += mouseDelta;
                    }
                }
            }
        }

        // user clicked a prop, so add it to the selection
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !wasMouseDragging)
        {
            if (!ImGui.IsKeyDown(ImGuiKey.ModShift))
                selectedProps.Clear();
            
            var prop = GetPropAt(window.MouseCellFloat);
            if (prop is not null)
            {
                if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                {
                    // if prop is in selection, remove it from selection
                    // if prop is not in selection, add it to the selection
                    if (!selectedProps.Remove(prop))
                        selectedProps.Add(prop);
                }
                else
                {
                    selectedProps.Add(prop);
                }
            }
        }

        // when N is pressed, create new selected prop
        // TODO: drag and drop from props list
        if (ImGui.IsKeyPressed(ImGuiKey.N) || ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            if (selectedInit is not null)
            {
                var prop = new Prop(selectedInit, window.MouseCellFloat, new Vector2(selectedInit.Width, selectedInit.Height))
                {
                    Depth = window.WorkLayer * 10
                };

                RainEd.Instance.Level.Props.Add(prop);
                selectedProps.Clear();
                selectedProps.Add(prop);
            }
        }

        // delete key to delete selected props
        if (ImGui.IsKeyPressed(ImGuiKey.Delete) || ImGui.IsKeyPressed(ImGuiKey.Backspace))
        {
            foreach (var prop in selectedProps)
            {
                RainEd.Instance.Level.Props.Remove(prop);
            }

            selectedProps.Clear();
        }

        // duplicate props
        if (ImGui.IsKeyPressed(ImGuiKey.D) && ImGui.IsKeyDown(ImGuiKey.ModCtrl))
        {
            var propsToDup = selectedProps.ToArray();
            selectedProps.Clear();

            foreach (var srcProp in propsToDup)
            {
                Prop newProp;
                if (srcProp.IsAffine)
                {
                    newProp = new Prop(srcProp.PropInit, srcProp.Transform.Center + Vector2.One, srcProp.Transform.Size);
                    newProp.Transform.Rotation = srcProp.Transform.Rotation;
                }
                else
                {
                    newProp = new Prop(srcProp.PropInit, srcProp.QuadPoints);
                    newProp.QuadPoints[0] += Vector2.One;
                    newProp.QuadPoints[1] += Vector2.One;
                    newProp.QuadPoints[2] += Vector2.One;
                    newProp.QuadPoints[3] += Vector2.One;
                }

                RainEd.Instance.Level.Props.Add(newProp);
                selectedProps.Add(newProp);
            }
        }
    }

    public void TransformRotateUpdate()
    {
        RotateTransformMode modeState = rotateMode!;

        var startDir = Vector2.Normalize(dragStartPos - modeState.rotCenter);
        var curDir = Vector2.Normalize(window.MouseCellFloat - modeState.rotCenter);
        var angleDiff = MathF.Atan2(curDir.Y, curDir.X) - MathF.Atan2(startDir.Y, startDir.X);

        if (ImGui.IsKeyDown(ImGuiKey.ModShift))
        {
            var snap = MathF.PI / 8f;
            angleDiff = MathF.Round(angleDiff / snap) * snap; 
        }

        var rotMat = Matrix3x2.CreateRotation(angleDiff);

        for (int i = 0; i < selectedProps.Count; i++)
        {
            var prop = selectedProps[i];
            if (prop.IsAffine)
            {
                var origTransform = modeState.origTransforms[i].affine;
                prop.Transform.Rotation = origTransform.Rotation + angleDiff;
                prop.Transform.Center = Vector2.Transform(origTransform.Center - modeState.rotCenter, rotMat) + modeState.rotCenter;
            }
            else
            {
                var pts = prop.QuadPoints;
                var origPts = modeState.origTransforms[i].quads;
                for (int k = 0; k < 4; k++)
                {
                    pts[k] = Vector2.Transform(origPts[k] - modeState.rotCenter, rotMat) + modeState.rotCenter;
                }
            }
        }
    }

    public void TransformWarpUpdate()
    {
        var modeState = warpMode!;
        
        if (selectedProps.Count == 1)
        {
            selectedProps[0].QuadPoints[modeState.handleId] = window.MouseCellFloat;
        }
    }

    public void TransformScaleAffineUpdate()
    {
        var modeState = scaleMode!;
        var origRect = modeState.origRect;
        
        Vector2 scaleAnchor;

        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
        {
            scaleAnchor = origRect.Center;
        }
        else
        {
            // the side opposite to the active handle
            scaleAnchor =
                Vector2.Transform(origRect.Size / 2f * -modeState.handleOffset, modeState.rotationMatrix)
                + origRect.Center;
        }

        // calculate vector deltas from scale anchor to original handle pos and mouse position
        // these take the prop's rotation into account
        var origDx = Vector2.Dot(modeState.propRight, dragStartPos - scaleAnchor);
        var origDy = Vector2.Dot(modeState.propUp, dragStartPos - scaleAnchor);
        var mouseDx = Vector2.Dot(modeState.propRight, window.MouseCellFloat - scaleAnchor);
        var mouseDy = Vector2.Dot(modeState.propUp, window.MouseCellFloat - scaleAnchor);
        var scale = new Vector2(mouseDx / origDx, mouseDy / origDy);

        // lock on axis if dragging an edge handle
        if (modeState.handleOffset.X == 0f)
            scale.X = 1f;
        if (modeState.handleOffset.Y == 0f)
            scale.Y = 1f;
        
        // hold shift to maintain proportions
        // if scaling multiple props at once, this is the only valid mode. curse you, rotation!!!  
        if (modeState.mustMaintainProportions || ImGui.IsKeyDown(ImGuiKey.ModShift))
        {
            if (modeState.handleOffset.X == 0f)
            {
                scale.X = scale.Y;
            }
            else if (modeState.handleOffset.Y == 0f)
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
        newRect.Size.X = MathF.Max(0.1f, newRect.Size.X);
        newRect.Size.Y = MathF.Max(0.1f, newRect.Size.Y);

        // anchor the prop to the anchor point
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
        {
            newRect.Center = origRect.Center;
        }
        else
        {
            newRect.Center = scaleAnchor + Vector2.Transform(newRect.Size / 2f * modeState.handleOffset, modeState.rotationMatrix);
        }
        
        // scale selected props
        for (int i = 0; i < selectedProps.Count; i++)
        {
            var prop = selectedProps[i];

            if (prop.IsAffine)
            {
                ref var propTransform = ref prop.Transform;
                var origTransform = modeState.origPropTransforms[i].affine;

                propTransform.Size = origTransform.Size * scale;
                
                // clamp size
                propTransform.Size.X = MathF.Max(0.1f, propTransform.Size.X);
                propTransform.Size.Y = MathF.Max(0.1f, propTransform.Size.Y);

                // position prop
                var propOffset = (origTransform.Center - origRect.Center) / origRect.Size;
                propTransform.Center = Vector2.Transform(propOffset * newRect.Size, modeState.rotationMatrix) + newRect.Center;
            }
            else
            {
                var propQuad = prop.QuadPoints;
                var origQuad = modeState.origPropTransforms[i].quads;

                for (int k = 0; k < 4; k++)
                {
                    var scaleOffset = (origQuad[k] - origRect.Center) / origRect.Size;
                    propQuad[k] = Vector2.Transform(scaleOffset * newRect.Size, modeState.rotationMatrix) + newRect.Center;
                }
            }
        }
    }
}