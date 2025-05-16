using ImGuiNET;
using Raylib_cs;
using System.Numerics;
using Rained.Assets;
using Rained.LevelData;
namespace Rained.EditorGui.Editors;

partial class PropEditor : IEditorMode
{
    public string Name { get => "Props"; }
    public bool SupportsCellSelection => false;
    public List<Prop> SelectedProps => selectedProps;
    public ChangeHistory.PropChangeRecorder ChangeRecorder => changeRecorder;

    private enum PropSnapMode
    {
        None,
        Quarter,
        Half,
        Whole
    }
    
    private readonly LevelWindow window;
    
    private readonly List<Prop> selectedProps = new();
    private List<Prop>? initSelectedProps = null; // used for add rect select mode
    private Prop[] propSelectionList = Array.Empty<Prop>(); // used for being able to select props that are behind others
    private Prop? highlightedProp = null; // used for prop selection list
    
    private bool isWarpMode = false;
    private Vector2 prevMousePos;
    private Vector2 dragStartPos;
    private PropSnapMode snappingMode = PropSnapMode.Whole; // 0 = off, 1 = precise snap, 2 = snap to grid
    
    private int initDepth = -1; // the depth of the last selected prop(s)
    private bool isDoubleClick = false; // if mouse double clicks, wait until mouse release to open the selector popup
    
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
    private bool isModeMouseDown = false;

    public PropEditor(LevelWindow window)
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
                snappingMode = PropSnapMode.None;
                break;

            case "0.25x":
                snappingMode = PropSnapMode.Quarter;
                break;
            
            case "0.5x":
                snappingMode = PropSnapMode.Half;
                break;
            
            case "1x":
                snappingMode = PropSnapMode.Whole;
                break;

            default:
                Log.Error("Invalid prop snap '{PropSnap}' in preferences.json", RainEd.Instance.Preferences.PropSnap);
                break;
        }
    }

    public void Load()
    {
        isDoubleClick = false;
        selectedProps.Clear();
        transformMode = null;
        initSelectedProps = null;
        isMouseDragging = false;
    }

    public void ReloadLevel()
    {
        changeRecorder.TakeSettingsSnapshot();
    }

    public void SavePreferences(UserPreferences prefs)
    {
        if (snappingMode == PropSnapMode.None)
            prefs.PropSnap = "off";
        else if (snappingMode == PropSnapMode.Quarter)
            prefs.PropSnap = "0.25x";
        else if (snappingMode == PropSnapMode.Half)
            prefs.PropSnap = "0.5x";
        else if (snappingMode == PropSnapMode.Whole)
            prefs.PropSnap = "1x";
        else
            Log.Error("Invalid prop snap mode {SnapMode}", snappingMode);
    }

    public void Unload()
    {
        if (changeRecorder.IsTransformActive)
            changeRecorder.PushChanges();
        
        if (transformMode is WarpTransformMode)
        {
            selectedProps[0].TryConvertToAffine();
        }

        transformMode = null;
        propSelectionList = [];

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.Rope is not null)
                prop.Rope.SimulationSpeed = 0f;
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

    private static float GetSnapValue(PropSnapMode mode)
    {
        return mode switch
        {
            PropSnapMode.None => 0f,
            PropSnapMode.Quarter => 0.25f,
            PropSnapMode.Half => 0.5f,
            PropSnapMode.Whole => 1.0f,
            _ => 0f,
        };
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

    private static void GetSelectionLayerFilter(int layer, out int layerMin, out int layerMax)
    {
        var mode = RainEd.Instance.Preferences.PropSelectionLayerFilter;
        switch (mode)
        {
            case UserPreferences.PropSelectionLayerFilterOption.All:
                layerMin = 0;
                layerMax = 2;
                break;
            
            case UserPreferences.PropSelectionLayerFilterOption.Current:
                layerMin = layer;
                layerMax = layer;
                break;
            
            case UserPreferences.PropSelectionLayerFilterOption.InFront:
                layerMin = layer;
                layerMax = 2;
                break;
            
            default:
                layerMin = layer;
                layerMax = layer;
                break;
        }
    }

    private static bool IsSublayerWithinFilter(int depthOffset, int layerMin, int layerMax)
    {
        return depthOffset >= layerMin * 10 && depthOffset < (layerMax+1) * 10;
    }

    private static Prop? GetPropAt(Vector2 point, int layer)
    {
        GetSelectionLayerFilter(layer, out int layerMin, out int layerMax);
        
        for (int i = RainEd.Instance.Level.Props.Count - 1; i >= 0; i--)
        {
            var prop = RainEd.Instance.Level.Props[i];
            if (!IsSublayerWithinFilter(prop.DepthOffset, layerMin, layerMax)) continue;

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
        GetSelectionLayerFilter(layer, out int layerMin, out int layerMax);
        var list = new List<Prop>();

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (!IsSublayerWithinFilter(prop.DepthOffset, layerMin, layerMax)) continue;

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

    private static Rectangle CalcPropExtents(IEnumerable<Prop> props, bool excludeNonmovable = false)
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
            (isGizmoHovered ? 6f : 3f) / window.ViewZoom * Boot.WindowScale,
            color
        );

        return isGizmoHovered;
    }

    private void BeginTransformMode(ITransformMode mode)
    {
        if (transformMode is not null)
        {
            changeRecorder.PushChanges();

            if (transformMode is WarpTransformMode)
            {
                selectedProps[0].TryConvertToAffine();
            }
        }

        changeRecorder.BeginTransform();
        isModeMouseDown = EditorWindow.IsMouseDown(ImGuiMouseButton.Left);
        transformMode = mode;
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;

        level.SortPropsByDepth();

        levelRender.RenderLevelComposite(mainFrame, layerFrames, new Rendering.LevelRenderConfig()
        {
            Scissor = false,
            DrawProps = true,
            DrawPropsInFront = true,
            ActiveLayer = window.WorkLayer,
            LayerOffset = 0
        });
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
                Raylib.DrawLineV(pts[0] * Level.TileSize, pts[1] * Level.TileSize, col);
                Raylib.DrawLineV(pts[1] * Level.TileSize, pts[2] * Level.TileSize, col);
                Raylib.DrawLineV(pts[2] * Level.TileSize, pts[3] * Level.TileSize, col);
                Raylib.DrawLineV(pts[3] * Level.TileSize, pts[0] * Level.TileSize, col);
            }
        }
        else
        {
            foreach (var prop in selectedProps)
            {
                if (prop == highlightedProp) continue;

                var pts = prop.QuadPoints;
                var col = prop.IsMovable ? OutlineColors[0] : OutlineColors[3];;
                Raylib.DrawLineV(pts[0] * Level.TileSize, pts[1] * Level.TileSize, col);
                Raylib.DrawLineV(pts[1] * Level.TileSize, pts[2] * Level.TileSize, col);
                Raylib.DrawLineV(pts[2] * Level.TileSize, pts[3] * Level.TileSize, col);
                Raylib.DrawLineV(pts[3] * Level.TileSize, pts[0] * Level.TileSize, col);
            }
        }

        if (highlightedProp is not null)
        {
            var pts = highlightedProp.QuadPoints;
            var col = OutlineGlowColors[0];
            Raylib.DrawLineV(pts[0] * Level.TileSize, pts[1] * Level.TileSize, col);
            Raylib.DrawLineV(pts[1] * Level.TileSize, pts[2] * Level.TileSize, col);
            Raylib.DrawLineV(pts[2] * Level.TileSize, pts[3] * Level.TileSize, col);
            Raylib.DrawLineV(pts[3] * Level.TileSize, pts[0] * Level.TileSize, col);
        }

        // prop transform gizmos
        if (selectedProps.Count > 0 && !isRopeSimulationActive)
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
                        BeginTransformMode(new ScaleTransformMode(
                            handleId: i,
                            props: selectedProps,
                            snap: GetSnapValue(snappingMode)
                        ));
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
                Raylib.DrawLineV(
                    startPos: handleCnPos * Level.TileSize,
                    endPos: rotDotPos * Level.TileSize,
                    OutlineColors[0]
                );

                // draw gizmo handle
                if (DrawGizmoHandle(rotDotPos, 0) && EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    BeginTransformMode(new RotateTransformMode(
                        rotCenter: aabb.Position + aabb.Size / 2f,
                        props: selectedProps
                    ));
                }
                
                if (KeyShortcuts.Activated(KeyShortcut.RotatePropCW) || KeyShortcuts.Activated(KeyShortcut.RotatePropCCW))
                {
                    BeginTransformMode(new MoveTransformMode(
                        props: selectedProps,
                        snap: GetSnapValue(snappingMode),
                        mouseDown: false
                    ));
                    isModeMouseDown = false;
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
                                BeginTransformMode(new WarpTransformMode(
                                    handleId: i,
                                    prop: selectedProps[0],
                                    snap: GetSnapValue(snappingMode)
                                ));
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
                                BeginTransformMode(new LongTransformMode(
                                    handleId: i,
                                    prop: prop,
                                    snap: GetSnapValue(snappingMode)
                                ));
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

            GetSelectionLayerFilter(window.WorkLayer, out int layerMin, out int layerMax);

            foreach (var prop in level.Props)
            {
                if (selectedProps.Contains(prop)) continue;
                if (!IsSublayerWithinFilter(prop.DepthOffset, layerMin, layerMax)) continue;
                
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
        }

        // in prop transform mode
        if (!isModeMouseDown && !isRopeSimulationActive)
        {
            // in default mode
            PropSelectUpdate();
        }

        // update transform mode
        if (transformMode is not null)
        {
            transformMode.Update(dragStartPos, window.MouseCellFloat);
            if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
            {
                transformMode.MouseReleased();
                isModeMouseDown = false;
            }

            if (transformMode.Deactivated())
            {
                changeRecorder.PushChanges();

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
        if (ImGui.BeginPopup("PropSelectionList"))
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

            if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        // update depth init for next prop placement based on currently selected props
        if (selectedProps.Count > 0)
        {
            // check that the depth offset of all selected props are the same
            int curDepthOffset = selectedProps[0].DepthOffset;
            foreach (var prop in selectedProps)
            {
                if (prop.DepthOffset != curDepthOffset)
                {
                    curDepthOffset = -1;
                    break;
                }
            }

            initDepth = curDepthOffset;
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
        var prefs = RainEd.Instance.Preferences;

        if (window.IsViewportHovered)
        {
            // write selected tile
            var propsAtCursor = GetPropsAt(EditorWindow.IsMouseDragging(ImGuiMouseButton.Left) ? dragStartPos : window.MouseCellFloat, window.WorkLayer);
            Prop? hoveredProp;
            {
                hoveredProp = propsAtCursor.Length == 0 ? null : propsAtCursor[0];
                foreach (var prop in propsAtCursor)
                {
                    if (selectedProps.Contains(prop))
                    {
                        hoveredProp = prop;
                        break;
                    }
                }
            }
            
            if (hoveredProp is not null)
            {
                window.WriteStatus(hoveredProp.PropInit.Name, 3);
            }

            if (EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (!isMouseDragging)
                {
                    // drag had begun
                    // if dragging over an empty space, begin rect select
                    if (hoveredProp is null)
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
                        if (!selectedProps.Contains(hoveredProp))
                        {
                            selectedProps.Clear();
                            selectedProps.Add(hoveredProp);
                        }

                        BeginTransformMode(new MoveTransformMode(
                            selectedProps,
                            GetSnapValue(snappingMode),
                            mouseDown: true
                        ));
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

            // left double-click opens menu to select one of multiple props under the cursor
            // useful for when props overlap (which i assume is common)
            if (EditorWindow.IsMouseDoubleClicked(ImGuiMouseButton.Left)) isDoubleClick = true;

            // account for the preference to double click to create prop
            bool showSelectionList, createProp;
            if (prefs.DoubleClickToCreateProp)
            {
                createProp = isDoubleClick && EditorWindow.IsMouseReleased(ImGuiMouseButton.Left) && !isMouseDragging;
                showSelectionList = EditorWindow.IsMouseClicked(ImGuiMouseButton.Right);
            }
            else
            {
                showSelectionList = isDoubleClick && EditorWindow.IsMouseReleased(ImGuiMouseButton.Left) && !isMouseDragging;
                createProp = EditorWindow.IsMouseClicked(ImGuiMouseButton.Right);
            }
            
            if (showSelectionList)
            {
                if (!prefs.DoubleClickToCreateProp) isDoubleClick = false;

                propSelectionList = propsAtCursor;
                
                if (propSelectionList.Length > 1)
                {
                    ImGui.OpenPopup("PropSelectionList");
                }
            }

            if (!EditorWindow.IsMouseDragging(ImGuiMouseButton.Left))
                isMouseDragging = false;

            // when C is pressed, create new selected prop
            // TODO: drag and drop from props list
            if (KeyShortcuts.Activated(KeyShortcut.NewObject) || createProp)
            {
                if (createProp && prefs.DoubleClickToCreateProp) isDoubleClick = false;

                var createPos = window.MouseCellFloat;
                
                var snap = GetSnapValue(snappingMode);
                if (snap > 0)
                {
                    createPos.X = MathF.Round(createPos.X / snap) * snap;
                    createPos.Y = MathF.Round(createPos.Y / snap) * snap;
                
                }
                if (selectedInit is not null)
                {
                    changeRecorder.BeginListChange();

                    int propDepth = window.WorkLayer * 10;

                    // if a prop is selected while adding a new one, the new prop will copy
                    // the depth offset value of the old prop. also make sure that it is on the same
                    // work layer.
                    if (initDepth != -1 && (int)Math.Floor(initDepth / 10f) == window.WorkLayer)
                    {
                        propDepth = initDepth;
                    }

                    var prop = new Prop(selectedInit, createPos, new Vector2(selectedInit.Width, selectedInit.Height))
                    {
                        DepthOffset = propDepth
                    };
                    prop.Randomize();

                    RainEd.Instance.Level.Props.Add(prop);
                    selectedProps.Clear();
                    selectedProps.Add(prop);

                    changeRecorder.PushListChange();
                }
            }

            // when E is pressed, sample prop
            if (KeyShortcuts.Activated(KeyShortcut.Eyedropper))
            {
                var prop = hoveredProp;
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
                Prop newProp = srcProp.Clone();
                RainEd.Instance.Level.Props.Add(newProp);
                selectedProps.Add(newProp);
            }

            changeRecorder.PushListChange();
        }

        if (KeyShortcuts.Activated(KeyShortcut.Copy))
        {
            if (selectedProps.Count > 0)
            {
                var data = PropSerialization.SerializeProps([..selectedProps]);
                if (data is not null)
                {
                    Platform.SetClipboard(Boot.Window, Platform.ClipboardDataType.Props, data);
                }
            }
        }

        if (KeyShortcuts.Activated(KeyShortcut.Paste))
        {
            if (Platform.GetClipboard(Boot.Window, Platform.ClipboardDataType.Props, out byte[]? data))
            {
                var props = PropSerialization.DeserializeProps(data);
                if (props is not null && props.Length > 0)
                {
                    ChangeRecorder.BeginListChange();

                    selectedProps.Clear();
                    foreach (var p in props)
                    {
                        selectedProps.Add(p);
                        RainEd.Instance.Level.Props.Add(p);
                    }

                    ChangeRecorder.PushListChange();
                }
            }
        }
    }
}