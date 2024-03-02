using ImGuiNET;
using RainEd.Props;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
namespace RainEd;

partial class PropEditor : IEditorMode
{
    public string Name { get => "Props"; }
    private readonly EditorWindow window;
    private readonly List<Prop> selectedProps = new();
    private List<Prop>? initSelectedProps = null; // used for add rect select mode
    private Prop[] propSelectionList = Array.Empty<Prop>(); // used for being able to select props that are behind others
    private Prop? highlightedProp = null; // used for prop selection list
    private bool isWarpMode = false;
    private Vector2 prevMousePos;
    private Vector2 dragStartPos;
    private int snappingMode = 1; // 0 = off, 1 = precise snap, 2 = snap to grid

    private readonly Color HighlightColor = new(0, 0, 255, 255);
    private readonly Color HighlightColorGlow = new(50, 50, 255, 255);
    private readonly Color HighlightColor2 = new(180, 180, 180, 255);
    private readonly Color HighlightColor2Glow = new(255, 255, 255, 255);
    private readonly List<string> propColorNames;

    private bool isMouseDragging = false;
    private readonly List<PropTransform> dragInitPositions = new();
    private enum DragMode
    {
        Select,
        Move
    };
    private DragMode dragMode;

    private ITransformMode? transformMode;

    public PropEditor(EditorWindow window)
    {
        this.window = window;

        propColorNames = new List<string>()
        {
            Capacity = RainEd.Instance.PropDatabase.PropColors.Count
        };

        foreach (var col in RainEd.Instance.PropDatabase.PropColors)
        {
            propColorNames.Add(col.Name);
        }
    }

    public void Load()
    {
        selectedProps.Clear();
        initSelectedProps = null;
        isMouseDragging = false;
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

    private static Vector2 Snap(Vector2 vector, float snap)
    {
        if (snap == 0) return vector;
        return new Vector2(
            MathF.Round(vector.X / snap) * snap,
            MathF.Round(vector.Y / snap) * snap
        );
    }

    private static float Snap(float number, float snap)
    {
        if (snap == 0) return number;
        return MathF.Round(number / snap) * snap;
    }

    private static Prop? GetPropAt(Vector2 point)
    {
        for (int i = RainEd.Instance.Level.Props.Count - 1; i >= 0; i--)
        {
            var prop = RainEd.Instance.Level.Props[i];

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

    private static Prop[] GetPropsAt(Vector2 point)
    {
        var list = new List<Prop>();

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            var pts = prop.QuadPoints;
            if (
                IsPointInTriangle(point, pts[0], pts[1], pts[2]) ||
                IsPointInTriangle(point, pts[2], pts[3], pts[0])    
            )
            {
                list.Add(prop);
            }
        }

        return list.ToArray();
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
            return prop.Rect.Center;
        
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
        if (isWarpMode)
        {
            foreach (var prop in level.Props)
            {
                if (prop == highlightedProp) continue;

                var pts = prop.QuadPoints;
                var col = prop.IsAffine ? HighlightColor : HighlightColor2;
                Raylib.DrawLineEx(pts[0] * Level.TileSize, pts[1] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[1] * Level.TileSize, pts[2] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[2] * Level.TileSize, pts[3] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[3] * Level.TileSize, pts[0] * Level.TileSize, 1f / window.ViewZoom, col);
            }
        }
        else
        {
            foreach (var prop in selectedProps)
            {
                if (prop == highlightedProp) continue;

                var pts = prop.QuadPoints;
                var col = HighlightColor;
                Raylib.DrawLineEx(pts[0] * Level.TileSize, pts[1] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[1] * Level.TileSize, pts[2] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[2] * Level.TileSize, pts[3] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[3] * Level.TileSize, pts[0] * Level.TileSize, 1f / window.ViewZoom, col);
            }
        }

        if (highlightedProp is not null)
        {
            var pts = highlightedProp.QuadPoints;
            var col = HighlightColorGlow;
            Raylib.DrawLineEx(pts[0] * Level.TileSize, pts[1] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[1] * Level.TileSize, pts[2] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[2] * Level.TileSize, pts[3] * Level.TileSize, 1f / window.ViewZoom, col);
            Raylib.DrawLineEx(pts[3] * Level.TileSize, pts[0] * Level.TileSize, 1f / window.ViewZoom, col);
        }

        // prop transform gizmos
        if (selectedProps.Count > 0)
        {
            bool canWarp = transformMode is WarpTransformMode ||
                (isWarpMode && selectedProps.Count == 1);
            
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
            if ((transformMode is null && !canWarp) || transformMode is ScaleTransformMode)
            {
                ScaleTransformMode? scaleMode = transformMode as ScaleTransformMode;

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
                    if (scaleMode != null && scaleMode.handleId != i)
                    {
                        continue;
                    }
                    
                    var handle1 = corners[i / 2]; // position of left corner
                    var handle2 = corners[(i + 1) / 2 % 4]; // position of right corner
                    var handlePos = (handle1 + handle2) / 2f;
                    
                    // draw gizmo handle at corner
                    if (DrawGizmoHandle(handlePos) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        transformMode = new ScaleTransformMode(
                            handleId: i,
                            props: selectedProps,
                            snap: snappingMode / 2f
                        );
                    }
                }
            }

            // rotation gizmo (don't draw if scaling or rotating) 
            if (transformMode is null && !canWarp)
            {
                Vector2 handleDir = -Vector2.UnitY;
                Vector2 handleCnPos = aabb.Position + new Vector2(aabb.Width / 2f, 0f);

                if (selectedProps.Count == 1 && selectedProps[0].IsAffine)
                {
                    var prop = selectedProps[0];
                    var sideDir = new Vector2(MathF.Cos(prop.Rect.Rotation), MathF.Sin(prop.Rect.Rotation));
                    handleDir = new(sideDir.Y, -sideDir.X);
                    handleCnPos = prop.Rect.Center + handleDir * Math.Abs(prop.Rect.Size.Y) / 2f; 
                }

                Vector2 rotDotPos = handleCnPos + handleDir * 5f / window.ViewZoom;

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
                    transformMode = new RotateTransformMode(
                        rotCenter: aabb.Position + aabb.Size / 2f,
                        props: selectedProps
                    );
                }
            }

            // freeform warp gizmo
            if ((transformMode is null && canWarp) || transformMode is WarpTransformMode || transformMode is RopePointTransformMode)
            {
                // normal free-form 
                if (selectedProps[0].Rope is null)
                {
                    Vector2[] corners = selectedProps[0].QuadPoints;

                    for (int i = 0; i < 4; i++)
                    {
                        // don't draw this handle if another scale handle is active
                        if (transformMode is WarpTransformMode warpMode && warpMode.handleId != i)
                        {
                            continue;
                        }

                        var handlePos = corners[i];
                        
                        // draw gizmo handle at corner
                        if (DrawGizmoHandle(handlePos, true) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            transformMode = new WarpTransformMode(
                                handleId: i,
                                prop: selectedProps[0],
                                snap: snappingMode / 2f
                            );
                        }
                    }
                }

                // on rope-type props, freeform will instead only allow you to drag
                // point A and point B, and will just modify the RotatedRect so that
                // the left and right sides touch A and B
                else
                {
                    var prop = selectedProps[0];
                    var cos = MathF.Cos(prop.Rect.Rotation);
                    var sin = MathF.Sin(prop.Rect.Rotation);
                    var pA = prop.Rect.Center + new Vector2(cos, sin) * -prop.Rect.Size.X / 2f;
                    var pB = prop.Rect.Center + new Vector2(cos, sin) * prop.Rect.Size.X / 2f;

                    for (int i = 0; i < 2; i++)
                    {
                        if (transformMode is RopePointTransformMode ropeMode && ropeMode.handleId != i)
                            continue;
                        
                        if (DrawGizmoHandle(i == 1 ? pB : pA) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            transformMode = new RopePointTransformMode(
                                handleId: i,
                                prop: prop,
                                snap: snappingMode / 2f
                            );
                        }
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
            if (transformMode is not null)
            {
                transformMode.Update(dragStartPos, window.MouseCellFloat);
                
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    transformMode = null;
                }
            }
            else
            {
                // in default mode
                PropSelectUpdate(wasMouseDragging);
            }
        }

        prevMousePos = window.MouseCellFloat;

        // props selection popup (opens when right-clicking over an area with multiple props)
        highlightedProp = null;
        if (ImGui.IsPopupOpen("PropSelectionList") && ImGui.BeginPopup("PropSelectionList"))
        {
            for (int i = propSelectionList.Length - 1; i >= 0; i--)
            {
                var prop = propSelectionList[i];

                ImGui.PushID(i);
                if (ImGui.Selectable(prop.PropInit.Name))
                {
                    ImGui.CloseCurrentPopup();
                    if (!EditorWindow.IsKeyDown(ImGuiKey.ModShift))
                        selectedProps.Clear();
                    SelectProp(prop);
                }

                if (ImGui.IsItemHovered())
                    highlightedProp = prop;
                
                ImGui.PopID();

            }

            ImGui.EndPopup();
        }
    }

    private void SelectProp(Prop prop)
    {
        if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
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
                    if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
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

                    // record initial drag positions
                    dragInitPositions.Clear();
                    foreach (var prop in selectedProps)
                    {
                        dragInitPositions.Add(new PropTransform(prop));
                    }
                }
            }
            isMouseDragging = true;

            // move drag
            if (dragMode == DragMode.Move)
            {
                float snap = snappingMode / 2f;
                bool posSnap = selectedProps.Count == 1 && selectedProps[0].IsAffine;

                var mouseDelta = window.MouseCellFloat - dragStartPos;
                
                if (snap > 0 && !posSnap)
                {
                    mouseDelta = Snap(mouseDelta, snap);
                }

                for (int i = 0; i < selectedProps.Count; i++)
                {
                    var prop = selectedProps[i];

                    if (prop.IsAffine)
                        prop.Rect.Center = dragInitPositions[i].rect.Center + mouseDelta;

                        if (snap > 0 && posSnap)
                        {
                            prop.Rect.Center = Snap(prop.Rect.Center, snap);
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

        // user clicked a prop, so add it to the selection
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !wasMouseDragging)
        {
            if (!EditorWindow.IsKeyDown(ImGuiKey.ModShift))
                selectedProps.Clear();
            
            var prop = GetPropAt(window.MouseCellFloat);
            if (prop is not null)
            {
                SelectProp(prop);
            }
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && !wasMouseDragging)
        {
            propSelectionList = GetPropsAt(window.MouseCellFloat);
            if (propSelectionList.Length == 1)
            {
                if (!EditorWindow.IsKeyDown(ImGuiKey.ModShift))
                    selectedProps.Clear();
                
                SelectProp(propSelectionList[0]);
            } else if (propSelectionList.Length > 1)
            {
                ImGui.OpenPopup("PropSelectionList");
            }
        }

        // when N is pressed, create new selected prop
        // TODO: drag and drop from props list
        if (RainEd.Instance.IsShortcutActivated(RainEd.ShortcutID.NewObject) || ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            var createPos = window.MouseCellFloat;
            
            var snap = snappingMode / 2f;
            if (snap > 0)
            {
                createPos.X = MathF.Round(createPos.X / snap) * snap;
                createPos.Y = MathF.Round(createPos.Y / snap) * snap;
            
            }
            if (selectedInit is not null)
            {
                var prop = new Prop(selectedInit, createPos, new Vector2(selectedInit.Width, selectedInit.Height))
                {
                    DepthOffset = window.WorkLayer * 10
                };

                RainEd.Instance.Level.Props.Add(prop);
                selectedProps.Clear();
                selectedProps.Add(prop);
            }
        }

        // delete key to delete selected props
        if (EditorWindow.IsKeyPressed(ImGuiKey.Delete) || EditorWindow.IsKeyPressed(ImGuiKey.Backspace))
        {
            foreach (var prop in selectedProps)
            {
                RainEd.Instance.Level.Props.Remove(prop);
            }

            selectedProps.Clear();
            isMouseDragging = false;
        }

        // duplicate props
        if (EditorWindow.IsKeyPressed(ImGuiKey.D) && EditorWindow.IsKeyDown(ImGuiKey.ModCtrl))
        {
            var propsToDup = selectedProps.ToArray();
            selectedProps.Clear();

            foreach (var srcProp in propsToDup)
            {
                Prop newProp;
                if (srcProp.IsAffine)
                {
                    newProp = new Prop(srcProp.PropInit, srcProp.Rect.Center + Vector2.One, srcProp.Rect.Size);
                    newProp.Rect.Rotation = srcProp.Rect.Rotation;
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
}