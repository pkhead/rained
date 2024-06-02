using ImGuiNET;
using RainEd.Props;
using Raylib_cs;

using System.Numerics;
namespace RainEd;

partial class PropEditor : IEditorMode
{
    public string Name { get => "Props"; }
    private readonly LevelView window;
    
    private readonly List<Prop> selectedProps = new();
    private List<Prop>? initSelectedProps = null; // used for add rect select mode
    private Prop[] propSelectionList = Array.Empty<Prop>(); // used for being able to select props that are behind others
    private Prop? highlightedProp = null; // used for prop selection list
    
    private bool isWarpMode = false;
    private Vector2 prevMousePos;
    private Vector2 dragStartPos;
    private int snappingMode = 1; // 0 = off, 1 = precise snap, 2 = snap to grid
    
    private ChangeHistory.PropChangeRecorder changeRecorder;

    private readonly Color[] OutlineColors =
    [
        new(0, 0, 255, 255),
        new(180, 180, 180, 255),
        new(0, 255, 0, 255),
        new(255, 0, 0, 255),
    ];

    private readonly Color[] OutlineGlowColors =
    [
        new(100, 100, 255, 255),
        new(255, 255, 255, 255),
        new(100, 255, 100, 255),
        new(255, 100, 100, 255)
    ];
    
    private readonly List<string> propColorNames;

    private bool isMouseDragging = false;
    private readonly List<PropTransform> dragInitPositions = [];
    private enum DragMode
    {
        Select,
        Move
    };
    private DragMode dragMode;

    private ITransformMode? transformMode;

    public PropEditor(LevelView window)
    {
        this.window = window;
        selectedInit = RainEd.Instance.PropDatabase.Categories[selectedPropGroup].Props[0];

        // register prop color names to be displayed in the custom color dropdown 
        propColorNames = new List<string>()
        {
            Capacity = RainEd.Instance.PropDatabase.PropColors.Count
        };

        foreach (var col in RainEd.Instance.PropDatabase.PropColors)
        {
            propColorNames.Add(col.Name);
        }

        // setup change history
        changeRecorder = new ChangeHistory.PropChangeRecorder();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder = new ChangeHistory.PropChangeRecorder();
        };

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            changeRecorder.TakeSettingsSnapshot();
            
            // remove props from selection that no longer exist
            // when the user undos or redos
            var propsList = RainEd.Instance.Level.Props;

            for (int i = selectedProps.Count - 1; i >= 0; i--)
            {
                if (propsList.IndexOf(selectedProps[i]) == -1)
                    selectedProps.RemoveAt(i);
            }
        };

        // this function is defined in PropEditorToolbar.cs
        ProcessSearch();

        // load preferences
        switch (RainEd.Instance.Preferences.PropSnap)
        {
            case "off":
                snappingMode = 0;
                break;
            
            case "0.5x":
                snappingMode = 1;
                break;
            
            case "1x":
                snappingMode = 2;
                break;

            default:
                RainEd.Logger.Error("Invalid prop snap '{PropSnap}' in preferences.json", RainEd.Instance.Preferences.PropSnap);
                break;
        }
    }

    public void Load()
    {
        selectedProps.Clear();
        transformMode = null;
        initSelectedProps = null;
        isMouseDragging = false;
    }

    public void SavePreferences(UserPreferences prefs)
    {
        if (snappingMode == 0)
            prefs.PropSnap = "off";
        else if (snappingMode == 1)
            prefs.PropSnap = "0.5x";
        else if (snappingMode == 2)
            prefs.PropSnap = "1x";
        else
            RainEd.Logger.Error("Invalid prop snap mode {SnapMode}", snappingMode);
    }

    public void Unload()
    {
        if (changeRecorder.IsTransformActive)
            changeRecorder.PushTransform();
        
        if (transformMode is WarpTransformMode)
        {
            selectedProps[0].TryConvertToAffine();
        }

        transformMode = null;
        propSelectionList = [];

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.Rope is not null)
                prop.Rope.Simulate = false;
        }

        isRopeSimulationActive = false;
        wasRopeSimulationActive = false;
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

    private static Prop? GetPropAt(Vector2 point, int layer)
    {
        for (int i = RainEd.Instance.Level.Props.Count - 1; i >= 0; i--)
        {
            var prop = RainEd.Instance.Level.Props[i];
            if (prop.DepthOffset < layer * 10) continue;

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

    private static Prop[] GetPropsAt(Vector2 point, int layer)
    {
        var list = new List<Prop>();

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.DepthOffset < layer * 10) continue;

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

    private Rectangle GetSelectionAABB(bool excludeNonmovable = false)
        => CalcPropExtents(selectedProps, excludeNonmovable);

    private static Rectangle CalcPropExtents(List<Prop> props, bool excludeNonmovable = false)
    {
        var minX = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var minY = float.PositiveInfinity;
        var maxY = float.NegativeInfinity;

        foreach (var prop in props)
        {
            if (excludeNonmovable && !prop.IsMovable) continue;
            
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
    private bool DrawGizmoHandle(Vector2 pos, int colorIndex)
    {
        bool isGizmoHovered = window.IsViewportHovered && (window.MouseCellFloat - pos).Length() < 0.5f / window.ViewZoom;
        
        Color color = isGizmoHovered ? OutlineGlowColors[colorIndex] : OutlineColors[colorIndex];
        
        Raylib.DrawCircleV(
            pos * Level.TileSize,
            (isGizmoHovered ? 8f : 4f) / window.ViewZoom,
            color
        );

        return isGizmoHovered;
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;

        level.SortPropsByDepth();

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, LevelView.BackgroundColor);

        // draw geometry/tile layers
        var drawTiles = RainEd.Instance.Preferences.ViewTiles;
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrames[l]);

            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            levelRender.RenderGeometry(l, LevelView.GeoColor(255));
            if (drawTiles)
                levelRender.RenderTiles(l, 100);
        }

        // draw alpha-blended result into main frame
        Raylib.BeginTextureMode(mainFrame);
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            Rlgl.PushMatrix();
                Rlgl.LoadIdentity();

                var alpha = l == window.WorkLayer ? 255 : 50;
                Raylib.DrawTextureRec(
                    layerFrames[l].Texture,
                    new Rectangle(
                        new Vector2(0f, layerFrames[l].Texture.Height),
                        new Vector2(layerFrames[l].Texture.Width, -layerFrames[l].Texture.Height)
                    ),
                    Vector2.Zero,
                    new Color(255, 255, 255, alpha)
                );
            Rlgl.PopMatrix();
        }

        // draw props
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrames[l]);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            levelRender.RenderProps(l, 255);
        }

        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw alpha-blended result into main frame
            Raylib.BeginTextureMode(mainFrame);
            Rlgl.PushMatrix();
                Rlgl.LoadIdentity();

                var alpha = l == window.WorkLayer ? 255 : 50;
                Raylib.DrawTextureRec(
                    layerFrames[l].Texture,
                    new Rectangle(
                        new Vector2(0f, layerFrames[l].Texture.Height),
                        new Vector2(layerFrames[l].Texture.Width, -layerFrames[l].Texture.Height)
                    ),
                    Vector2.Zero,
                    new Color(255, 255, 255, alpha)
                );
            Rlgl.PopMatrix();
        }
        
        levelRender.RenderGrid();
        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();
        
        // highlight selected props
        if (isWarpMode)
        {
            foreach (var prop in level.Props)
            {
                if (prop == highlightedProp) continue;

                Color col;
                if (!prop.IsMovable)
                {
                    col = OutlineColors[3]; // red
                }
                else if (prop.IsLong)
                {
                    col = OutlineColors[2]; // green
                }
                else if (prop.IsAffine)
                {
                    col = OutlineColors[0]; // blue
                }
                else
                {
                    col = OutlineColors[1]; // white
                }
                
                var pts = prop.QuadPoints;
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
                var col = prop.IsMovable ? OutlineColors[0] : OutlineColors[3];;
                Raylib.DrawLineEx(pts[0] * Level.TileSize, pts[1] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[1] * Level.TileSize, pts[2] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[2] * Level.TileSize, pts[3] * Level.TileSize, 1f / window.ViewZoom, col);
                Raylib.DrawLineEx(pts[3] * Level.TileSize, pts[0] * Level.TileSize, 1f / window.ViewZoom, col);
            }
        }

        if (highlightedProp is not null)
        {
            var pts = highlightedProp.QuadPoints;
            var col = OutlineGlowColors[0];
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
            
            var aabb = GetSelectionAABB(excludeNonmovable: transformMode is not null);

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
                    OutlineColors[0]
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
                    if (DrawGizmoHandle(handlePos, 0) && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        changeRecorder.BeginTransform();
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
                    OutlineColors[0]
                );

                // draw gizmo handle
                if (DrawGizmoHandle(rotDotPos, 0) && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    changeRecorder.BeginTransform();
                    transformMode = new RotateTransformMode(
                        rotCenter: aabb.Position + aabb.Size / 2f,
                        props: selectedProps
                    );
                }
            }

            // freeform warp gizmo
            if ((transformMode is null && canWarp) || transformMode is WarpTransformMode || transformMode is LongTransformMode)
            {
                if (selectedProps[0].IsMovable)
                {
                    // normal free-form 
                    if (!selectedProps[0].IsLong)
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
                            if (DrawGizmoHandle(handlePos, 1) && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                changeRecorder.BeginTransform();
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
                            if (transformMode is LongTransformMode ropeMode && ropeMode.handleId != i)
                                continue;
                            
                            if (DrawGizmoHandle(i == 1 ? pB : pA, 2) && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                changeRecorder.BeginTransform();
                                transformMode = new LongTransformMode(
                                    handleId: i,
                                    prop: prop,
                                    snap: snappingMode / 2f
                                );
                            }
                        }
                    }
                }
            }
        }

        // draw drag rect
        if (isMouseDragging && dragMode == DragMode.Select)
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
            Raylib.DrawRectangleRec(rect, new Color(OutlineColors[0].R, OutlineColors[0].G, OutlineColors[0].B, (byte)80));
            Raylib.DrawRectangleLinesEx(rect, 1f / window.ViewZoom, OutlineColors[0]);

            // select all props within selection rectangle
            selectedProps.Clear();
            
            if (initSelectedProps is not null)
            {
                foreach (var prop in initSelectedProps)
                    selectedProps.Add(prop);
            }

            foreach (var prop in level.Props)
            {
                if (selectedProps.Contains(prop)) continue;
                if (prop.DepthOffset < window.WorkLayer * 10) continue;
                
                var pc = GetPropCenter(prop);
                if (pc.X >= minX && pc.Y >= minY && pc.X <= maxX && pc.Y <= maxY)
                    selectedProps.Add(prop);
            }
        }

        if (window.IsViewportHovered)
        {
            if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            {
                dragStartPos = window.MouseCellFloat;
            }

            // in prop transform mode
            if (transformMode is null)
            {
                // in default mode
                PropSelectUpdate();
            }
        }

        // update transform mode
        if (transformMode is not null)
        {
            transformMode.Update(dragStartPos, window.MouseCellFloat);
            if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
            {
                changeRecorder.PushTransform();

                if (transformMode is WarpTransformMode)
                {
                    selectedProps[0].TryConvertToAffine();
                }

                transformMode = null;
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

    public void PropSelectUpdate()
    {
        if (EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
        {
            if (!isMouseDragging)
            {
                // drag had begun
                var hoverProp = GetPropAt(dragStartPos, window.WorkLayer);

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
                    // if dragging over a prop, drag all currently selected props
                    // if active prop is in selection. if not, then set selection
                    // to this prop
                    dragMode = DragMode.Move;
                    if (!selectedProps.Contains(hoverProp))
                    {
                        selectedProps.Clear();
                        selectedProps.Add(hoverProp);
                    }

                    changeRecorder.BeginTransform();
                    transformMode = new MoveTransformMode(
                        selectedProps,
                        snappingMode / 2f
                    );
                }
            }
            
            isMouseDragging = true;
        }

        // user clicked a prop, so add it to the selection
        if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left) && !isMouseDragging)
        {
            if (!EditorWindow.IsKeyDown(ImGuiKey.ModShift))
                selectedProps.Clear();
            
            var prop = GetPropAt(window.MouseCellFloat, window.WorkLayer);
            if (prop is not null)
            {
                SelectProp(prop);
            }
        }

        // right-click opens menu to select one of multiple props under the cursor
        // useful for when props overlap (which i assume is common)
        if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Right) && !isMouseDragging)
        {
            propSelectionList = GetPropsAt(window.MouseCellFloat, window.WorkLayer);
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

        if (!EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
            isMouseDragging = false;

        // when N is pressed, create new selected prop
        // TODO: drag and drop from props list
        if (KeyShortcuts.Activated(KeyShortcut.NewObject) || EditorWindow.IsMouseDoubleClicked(ImGuiMouseButton.Left))
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
                changeRecorder.BeginListChange();

                var prop = new Prop(selectedInit, createPos, new Vector2(selectedInit.Width, selectedInit.Height))
                {
                    DepthOffset = window.WorkLayer * 10
                };

                RainEd.Instance.Level.Props.Add(prop);
                selectedProps.Clear();
                selectedProps.Add(prop);

                changeRecorder.PushListChange();
            }
        }

        // when E is pressed, sample prop
        if (KeyShortcuts.Activated(KeyShortcut.Eyedropper))
        {
            var prop = GetPropAt(window.MouseCellFloat, window.WorkLayer);
            if (prop is not null)
            {
                // if prop is a tile as prop
                if (prop.PropInit.PropFlags.HasFlag(PropFlags.Tile))
                {
                    for (int i = 0; i < RainEd.Instance.PropDatabase.TileCategories.Count; i++)
                    {
                        var group = RainEd.Instance.PropDatabase.TileCategories[i];
                        var idx = group.Props.IndexOf(prop.PropInit);

                        if (idx >= 0)
                        {
                            forceSelection = SelectionMode.Tiles;
                            selectedTileGroup = i;
                            selectedTileIdx = idx;
                            break;
                        }
                    }
                }

                // prop is a regular prop
                else
                {
                    forceSelection = SelectionMode.Props;
                    selectedPropGroup = prop.PropInit.Category.Index;
                    selectedPropIdx = prop.PropInit.Category.Props.IndexOf(prop.PropInit);
                }
            }
        }

        // delete key to delete selected props
        if (KeyShortcuts.Activated(KeyShortcut.RemoveObject))
        {
            changeRecorder.BeginListChange();
                foreach (var prop in selectedProps)
                {
                    RainEd.Instance.Level.Props.Remove(prop);
                }
            changeRecorder.PushListChange();

            selectedProps.Clear();
            isMouseDragging = false;
        }

        // duplicate props
        if (KeyShortcuts.Activated(KeyShortcut.Duplicate))
        {
            changeRecorder.BeginListChange();

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

                RainEd.Instance.Level.Props.Add(newProp);
                selectedProps.Add(newProp);
            }

            changeRecorder.PushListChange();
        }
    }
}