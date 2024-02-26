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

    private readonly Color SelectionColor = new(0, 0, 255, 255);

    private bool isMouseDragging = false;
    private enum DragMode
    {
        Select,
        Move
    };
    private DragMode dragMode;

#region Transform Modes
    // transform mode
    // aa how do i structure this??
    readonly struct ScaleTransformState
    {
        public readonly int corner;

        public ScaleTransformState(int corner)
        {
            this.corner = corner;
        }
    }

    readonly struct RotateTransformState
    {
        public readonly Vector2 rotCenter;
        public readonly Vector2[,] origQuads;

        public RotateTransformState(Vector2 rotCenter, List<Prop> props)
        {
            this.rotCenter = rotCenter;

            origQuads = new Vector2[props.Count, 4];
            for (int i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                origQuads[i,0] = prop.Quad[0] - rotCenter;
                origQuads[i,1] = prop.Quad[1] - rotCenter;
                origQuads[i,2] = prop.Quad[2] - rotCenter;
                origQuads[i,3] = prop.Quad[3] - rotCenter;
            }
        }
    }

    private enum TransformMode
    {
        None,
        Scale,
        Rotate
    }
    private TransformMode transformMode;
    private ScaleTransformState scaleState;
    private RotateTransformState rotateState;
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
            if (
                IsPointInTriangle(point, prop.Quad[0], prop.Quad[1], prop.Quad[2]) ||
                IsPointInTriangle(point, prop.Quad[2], prop.Quad[3], prop.Quad[0])    
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
    {
        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var prop in selectedProps)
        {
            for (int i = 0; i < 4; i++)
            {
                minX = Math.Min(minX, prop.Quad[i].X);
                minY = Math.Min(minY, prop.Quad[i].Y);
                maxX = Math.Max(maxX, prop.Quad[i].X);
                maxY = Math.Max(maxY, prop.Quad[i].Y);
            }
        }

        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    private static Vector2 GetPropCenter(Prop prop)
    {
        return (prop.Quad[0] + prop.Quad[1] + prop.Quad[2] + prop.Quad[3]) / 4f;
    }

    // returns true if gizmo is hovered, false if not
    private bool DrawGizmoHandle(Vector2 pos)
    {
        bool isGizmoHovered = window.IsViewportHovered && (window.MouseCellFloat - pos).Length() < 0.5f / window.ViewZoom;
        Raylib.DrawCircleV(
            pos * Level.TileSize,
            (isGizmoHovered ? 8f : 4f) / window.ViewZoom,
            isGizmoHovered ? new Color(50, 50, 255, 255) : SelectionColor
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
            Raylib.DrawLineEx(prop.Quad[0] * Level.TileSize, prop.Quad[1] * Level.TileSize, 1f / window.ViewZoom, SelectionColor);
            Raylib.DrawLineEx(prop.Quad[1] * Level.TileSize, prop.Quad[2] * Level.TileSize, 1f / window.ViewZoom, SelectionColor);
            Raylib.DrawLineEx(prop.Quad[2] * Level.TileSize, prop.Quad[3] * Level.TileSize, 1f / window.ViewZoom, SelectionColor);
            Raylib.DrawLineEx(prop.Quad[3] * Level.TileSize, prop.Quad[0] * Level.TileSize, 1f / window.ViewZoom, SelectionColor);
        }

        // prop transform gizmos
        if (selectedProps.Count > 0)
        {
            var aabb = GetSelectionAABB();

            // draw selection AABB if there is more than
            // one prop selected
            if (selectedProps.Count > 1)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        aabb.Position * Level.TileSize,
                        aabb.Size * Level.TileSize
                    ),
                    1f / window.ViewZoom,
                    SelectionColor
                );
            }

            // scale gizmo (points on corners)
            // don't draw handles if rotating
            if (transformMode == TransformMode.None || transformMode == TransformMode.Scale)
            {
                Vector2[] corners = new Vector2[4]
                {
                    aabb.Position + aabb.Size * new Vector2(0f, 0f),
                    aabb.Position + aabb.Size * new Vector2(1f, 0f),
                    aabb.Position + aabb.Size * new Vector2(1f, 1f),
                    aabb.Position + aabb.Size * new Vector2(0f, 1f),
                };


                for (int i = 0; i < 4; i++)
                {
                    // don't draw this handle if another scale handle is active
                    if (transformMode == TransformMode.Scale && scaleState.corner != i)
                        continue;
                    
                    var handlePos = corners[i];   
                    
                    // draw gizmo handle at corner
                    if (DrawGizmoHandle(handlePos) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        transformMode = TransformMode.Scale;
                        scaleState = new ScaleTransformState(
                            corner: i
                        );
                    }
                }
            }

            // rotation gizmo (don't draw if scaling)
            if (transformMode == TransformMode.None || transformMode == TransformMode.Rotate)
            {
                Vector2 sideDir = Vector2.UnitX;
                Vector2 handleDir = -Vector2.UnitY;
                Vector2 handleCnPos = aabb.Position + new Vector2(aabb.Width / 2f, 0f);

                if (selectedProps.Count == 1)
                {
                    handleCnPos = (selectedProps[0].Quad[0] + selectedProps[0].Quad[1]) / 2f;
                    sideDir = Vector2.Normalize(selectedProps[0].Quad[1] - selectedProps[0].Quad[0]);
                    handleDir = new(sideDir.Y, -sideDir.X);
                }

                Vector2 rotDotPos = handleCnPos + handleDir * 5f;

                // draw line to gizmo handle
                Raylib.DrawLineEx(
                    startPos: handleCnPos * Level.TileSize,
                    endPos: rotDotPos * Level.TileSize,
                    1f / window.ViewZoom,
                    SelectionColor
                );

                // draw gizmo handle
                if (DrawGizmoHandle(rotDotPos) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    transformMode = TransformMode.Rotate;
                    rotateState = new RotateTransformState(
                        rotCenter: aabb.Position + aabb.Size / 2f,
                        props: selectedProps
                    );
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
            Raylib.DrawRectangleRec(rect, new Color(SelectionColor.R, SelectionColor.G, SelectionColor.B, (byte)80));
            Raylib.DrawRectangleLinesEx(rect, 1f / window.ViewZoom, SelectionColor);

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
                if (transformMode == TransformMode.Scale)
                    PropScaleMode();
                else if (transformMode == TransformMode.Rotate)
                    PropRotateMode();
                
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    transformMode = TransformMode.None;
                }
            }
            else
            {
                // in default mode
                PropSelectMode(wasMouseDragging);
            }
        }

        prevMousePos = window.MouseCellFloat;
    }

    public void PropSelectMode(bool wasMouseDragging)
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
                    prop.Quad[0] += mouseDelta;
                    prop.Quad[1] += mouseDelta;
                    prop.Quad[2] += mouseDelta;
                    prop.Quad[3] += mouseDelta;
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
    }

    public void PropRotateMode()
    {
        var startDir = Vector2.Normalize(dragStartPos - rotateState.rotCenter);
        var curDir = Vector2.Normalize(window.MouseCellFloat - rotateState.rotCenter);
        var angleDiff = MathF.Atan2(curDir.Y, curDir.X) - MathF.Atan2(startDir.Y, startDir.X);

        var rotMat = Matrix3x2.CreateRotation(angleDiff);
        
        for (int i = 0; i < selectedProps.Count; i++)
        {
            for (int k = 0; k < 4; k++)
            {
                selectedProps[i].Quad[k] = Vector2.Transform(rotateState.origQuads[i,k], rotMat) + rotateState.rotCenter;
            }
        }
    }

    public void PropScaleMode()
    {

    }
}