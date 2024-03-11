using Raylib_cs;
using ImGuiNET;
using System.Numerics;

namespace RainEd;

interface IEditorMode
{
    string Name { get; }

    void Load() {}
    void Unload() {}
    void SavePreferences(UserPreferences prefs) {}

    // write dirty changes to the Level object
    // this is used by the light editor, since most everything is done in the GPU
    // since doing the processing on the CPU would prove too slow
    void FlushDirty() {}
    void ReloadLevel() {}

    void DrawToolbar();
    void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames);
}

enum EditModeEnum
{
    None = -1,
    Environment = 0,
    Geometry,
    Tile,
    Camera,
    Light,
    Effect,
    Prop
};

class EditorWindow
{
    public bool IsWindowOpen = true;

    public readonly RainEd Editor;

    private Vector2 viewOffset = new();
    private float viewZoom = 1f;
    private int zoomSteps = 0;
    private int workLayer = 0;

    public float ViewZoom { get => viewZoom; }
    public int WorkLayer { get => workLayer; set => workLayer = value; }

    public Vector2 ViewOffset { get => viewOffset; }

    private int mouseCx = 0;
    private int mouseCy = 0;
    private Vector2 mouseCellFloat = new();
    
    // input overrides for lmb because of alt + left click drag
    private bool isLmbPanning = false;
    private bool isLmbDown = false;
    private bool isLmbClicked = false;
    private bool isLmbReleased = false;
    private bool isLmbDragging = false;

    public int MouseCx { get => mouseCx; }
    public int MouseCy { get => mouseCy; }
    public Vector2 MouseCellFloat { get => mouseCellFloat; }
    public bool IsMouseInLevel() => Editor.Level.IsInBounds(mouseCx, mouseCy);
    public bool OverrideMouseWheel = false;
    public string StatusText = string.Empty;

    private readonly UICanvasWidget canvasWidget;
    public bool IsViewportHovered { get => canvasWidget.IsHovered; }

    private readonly List<IEditorMode> editorModes = new();
    private int selectedMode = (int) EditModeEnum.Environment;
    private int queuedEditMode = (int) EditModeEnum.None;

    public int EditMode {
        get => selectedMode;
        set => queuedEditMode = value;
    }

    // render texture given to each editor mode class
    private RlManaged.RenderTexture2D[] layerRenderTextures;
    public readonly LevelEditRender LevelRenderer;

    private ChangeHistory.CellChangeRecorder cellChangeRecorder;
    public ChangeHistory.CellChangeRecorder CellChangeRecorder { get => cellChangeRecorder; }

    public IEditorMode GetEditor(int editMode)
    {
        return editorModes[editMode];
    }

    public IEditorMode GetEditor(EditModeEnum editMode) => GetEditor((int) editMode);

    public T GetEditor<T>()
    {
        foreach (var editor in editorModes)
        {
            if (editor is T subclass) return subclass;
        }
        
        throw new Exception("Could not find editor mode");
    }
    
    public EditorWindow()
    {
        Editor = RainEd.Instance;
        canvasWidget = new(1, 1);
        layerRenderTextures = new RlManaged.RenderTexture2D[3];

        for (int i = 0; i < 3; i++)
        {
            layerRenderTextures[i] = RlManaged.RenderTexture2D.Load(1, 1);
        }
        
        LevelRenderer = new LevelEditRender();
        cellChangeRecorder = new ChangeHistory.CellChangeRecorder();
        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            cellChangeRecorder = new ChangeHistory.CellChangeRecorder();
        };

        editorModes.Add(new EnvironmentEditor(this));
        editorModes.Add(new GeometryEditor(this));
        editorModes.Add(new TileEditor(this));
        editorModes.Add(new CameraEditor(this));
        editorModes.Add(new LightEditor(this));
        editorModes.Add(new EffectsEditor(this));
        editorModes.Add(new PropEditor(this));

        // load user preferences
        LevelRenderer.ViewGrid = RainEd.Instance.Preferences.ViewGrid;
        LevelRenderer.ViewObscuredBeams = RainEd.Instance.Preferences.ViewObscuredBeams;
        LevelRenderer.ViewTileHeads = RainEd.Instance.Preferences.ViewTileHeads;
    }

    public void SavePreferences(UserPreferences prefs)
    {
        prefs.ViewGrid = LevelRenderer.ViewGrid;
        prefs.ViewObscuredBeams = LevelRenderer.ViewObscuredBeams;
        prefs.ViewTileHeads = LevelRenderer.ViewTileHeads;
        
        foreach (var mode in editorModes)
        {
            mode.SavePreferences(prefs);
        }
    }

    public void UnloadView()
    {
        editorModes[selectedMode].Unload();
    }

    public void LoadView()
    {
        editorModes[selectedMode].Load();
    }

    public void FlushDirty()
    {
        editorModes[selectedMode].FlushDirty();
    }

    public void ReloadLevel()
    {
        foreach (var mode in editorModes)
            mode.ReloadLevel();
    }

    public static bool IsKeyDown(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyDown(key);
    }

    public static bool IsKeyPressed(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyPressed(key);
    }

    // need to use Raylib.IsKeyPressed instead of EditorWindow.IsKeyPressed
    // because i specifically disabled the Tab key in ImGui input handling
    public static bool IsTabPressed()
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return Raylib.IsKeyPressed(KeyboardKey.Tab);
    }

    public bool IsMouseClicked(ImGuiMouseButton button, bool repeat = false)
    {
        if (button == ImGuiMouseButton.Left) return isLmbClicked;
        return ImGui.IsMouseClicked(button, repeat);
    }

    public bool IsMouseDown(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDown;
        return ImGui.IsMouseDown(button);
    }

    public bool IsMouseDoubleClicked(ImGuiMouseButton button)
    {
        if (isLmbPanning) return false;
        return ImGui.IsMouseDoubleClicked(button);
    }

    public bool IsMouseReleased(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbReleased;
        return ImGui.IsMouseReleased(button);
    }

    public bool IsMouseDragging(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDragging;
        return ImGui.IsMouseDragging(button);
    }

    public void Render(float dt)
    {
        if (queuedEditMode >= 0)
        {
            if (queuedEditMode != selectedMode)
            {
                editorModes[selectedMode].Unload();
                selectedMode = queuedEditMode;
                editorModes[selectedMode].Load();
            }

            queuedEditMode = -1;
        }
        
        if (IsWindowOpen && ImGui.Begin("Level"))
        {
            var newEditMode = selectedMode;

            // edit mode
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Edit Mode");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 8f);
            if (ImGui.BeginCombo("##EditMode", editorModes[selectedMode].Name))
            {
                for (int i = 0; i < editorModes.Count; i++)
                {
                    var isSelected = i == selectedMode;
                    if (ImGui.Selectable(editorModes[i].Name, isSelected))
                    {
                        newEditMode = i;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Reset View"))
            {
                viewOffset = Vector2.Zero;
                viewZoom = 1f;
                zoomSteps = 0;
            }

            ImGui.SameLine();
            ImGui.TextUnformatted($"Zoom: {Math.Floor(viewZoom * 100f)}%  Mouse: ({MouseCx}, {MouseCy})    {StatusText}");
            StatusText = string.Empty;

            if (!ImGui.GetIO().WantTextInput)
            {
                // scroll keybinds
                var moveX = (IsKeyDown(ImGuiKey.RightArrow)?1:0) - (IsKeyDown(ImGuiKey.LeftArrow)?1:0);
                var moveY = (IsKeyDown(ImGuiKey.DownArrow)?1:0) - (IsKeyDown(ImGuiKey.UpArrow)?1:0);
                var moveSpeed = IsKeyDown(ImGuiKey.ModShift) ? 60f : 30f;
                viewOffset.X += moveX * Level.TileSize * moveSpeed * dt;
                viewOffset.Y += moveY * Level.TileSize * moveSpeed * dt;

                // edit mode keybinds
                if (IsKeyPressed(ImGuiKey._1))
                    newEditMode = (int) EditModeEnum.Environment;
                
                if (IsKeyPressed(ImGuiKey._2))
                    newEditMode = (int) EditModeEnum.Geometry;
                
                if (IsKeyPressed(ImGuiKey._3))
                    newEditMode = (int) EditModeEnum.Tile;
                
                if (IsKeyPressed(ImGuiKey._4))
                    newEditMode = (int) EditModeEnum.Camera;
                
                if (IsKeyPressed(ImGuiKey._5))
                    newEditMode = (int) EditModeEnum.Light;
                
                if (IsKeyPressed(ImGuiKey._6))
                    newEditMode = (int) EditModeEnum.Effect;

                if (IsKeyPressed(ImGuiKey._7))
                    newEditMode = (int) EditModeEnum.Prop;
            }

            // change edit mode if requested
            if (newEditMode != selectedMode)
            {
                editorModes[selectedMode].Unload();
                selectedMode = newEditMode;
                editorModes[selectedMode].Load();
            }

            // canvas widget
            {
                var regionMax = ImGui.GetWindowContentRegionMax();
                var regionMin = ImGui.GetCursorPos();

                int canvasW = (int)(regionMax.X - regionMin.X);
                int canvasH = (int)(regionMax.Y - regionMin.Y);

                canvasWidget.Resize(canvasW, canvasH);

                for (int i = 0; i < 3; i++)
                {
                    ref var renderTex = ref layerRenderTextures[i];

                    if (renderTex.Texture.Width != canvasW || renderTex.Texture.Height != canvasH)
                    {
                        renderTex.Dispose();
                        renderTex = RlManaged.RenderTexture2D.Load(canvasW, canvasH);
                    }
                }
                
                Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                DrawCanvas();
                Raylib.EndTextureMode();

                canvasWidget.Draw();
            }
        }
        
        editorModes[selectedMode].DrawToolbar();
    }

    private void Zoom(float factor, Vector2 mpos)
    {
        viewZoom *= factor;
        viewOffset = -(mpos - viewOffset) / factor + mpos;
    }

    private void DrawCanvas()
    {
        OverrideMouseWheel = false;
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        Rlgl.PushMatrix();
        Rlgl.Scalef(viewZoom, viewZoom, 1f);
        Rlgl.Translatef(-viewOffset.X, -viewOffset.Y, 0);

        // send view info to the level renderer
        var viewportW = canvasWidget.RenderTexture.Texture.Width;
        var viewportH = canvasWidget.RenderTexture.Texture.Height;
        LevelRenderer.ViewTopLeft = viewOffset / Level.TileSize;
        LevelRenderer.ViewBottomRight =
            (viewOffset + new Vector2(viewportW, viewportH) / viewZoom)
            / Level.TileSize;
        LevelRenderer.ViewZoom = viewZoom;

        // obtain mouse coordinates
        mouseCellFloat.X = (canvasWidget.MouseX / viewZoom + viewOffset.X) / Level.TileSize;
        mouseCellFloat.Y = (canvasWidget.MouseY / viewZoom + viewOffset.Y) / Level.TileSize;
        mouseCx = (int) Math.Floor(mouseCellFloat.X);
        mouseCy = (int) Math.Floor(mouseCellFloat.Y);

        // draw viewport
        // the blending functions here are some stupid hack
        // to fix transparent object writing to the fbo's alpha value
        // when being drawn, thus making the render texture at those areas
        // blend into the imgui background when rendered.
        // Good thing I downloaded renderdoc, otherwise there was no way
        // I would've figured that was the problem!
        Rlgl.SetBlendFactorsSeparate(0x0302, 0x0303, 1, 0x0303, 0x8006, 0x8006);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
        editorModes[selectedMode].DrawViewport(canvasWidget.RenderTexture, layerRenderTextures);

        // drwa resize preview
        if (Editor.LevelResizeWindow is LevelResizeWindow resizeData)
        {
            var level = Editor.Level;

            var lineWidth = 1f / ViewZoom;

            // calculate new level origin based on input anchor
            Vector2 newOrigin = new Vector2(resizeData.InputWidth - level.Width, resizeData.InputHeight - level.Height) * -resizeData.InputAnchor;

            // draw preview level dimensions
            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    (int)newOrigin.X * Level.TileSize, (int)newOrigin.Y * Level.TileSize,
                    resizeData.InputWidth * Level.TileSize, resizeData.InputHeight * Level.TileSize
                ),
                lineWidth,
                Color.White
            );

            // draw preview level buffer tiles
            int borderRight = resizeData.InputWidth - resizeData.InputBufferRight;
            int borderBottom = resizeData.InputHeight - resizeData.InputBufferBottom;
            int borderW = borderRight - resizeData.InputBufferLeft;
            int borderH = borderBottom - resizeData.InputBufferTop;
            int borderLeft = resizeData.InputBufferLeft + (int)newOrigin.X;
            int borderTop = resizeData.InputBufferTop + (int)newOrigin.Y;
            
            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    borderLeft * Level.TileSize, borderTop * Level.TileSize,
                    borderW * Level.TileSize, borderH * Level.TileSize
                ),
                lineWidth,
                new Color(255, 0, 255, 255)
            );
        }

        Raylib.EndBlendMode();

        // view controls
        isLmbClicked = false;
        isLmbDown = false;
        isLmbReleased = false;
        isLmbDragging = false;

        if (canvasWidget.IsHovered)
        {
            // middle click pan
            if (isLmbPanning || ImGui.IsMouseDown(ImGuiMouseButton.Middle))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                viewOffset -= mouseDelta / viewZoom;
            }

            // begin alt+left panning
            if (ImGui.IsKeyDown(ImGuiKey.ModAlt) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                isLmbPanning = true;
            }

            // scroll wheel zooming
            if (!OverrideMouseWheel)
            {
                var wheelMove = Raylib.GetMouseWheelMove();
                var zoomFactor = 1.5;
                if (wheelMove > 0f && zoomSteps < 5)
                {
                    var newZoom = Math.Round(viewZoom * zoomFactor * 1000.0) / 1000.0;
                    Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                    zoomSteps++;
                }
                else if (wheelMove < 0f && zoomSteps > -5)
                {
                    var newZoom = Math.Round(viewZoom / zoomFactor * 1000.0) / 1000.0;
                    Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                    zoomSteps--;
                }
            }
        }

        if (isLmbPanning)
        {
            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                isLmbPanning = false;
            }
        }
        else
        {
            isLmbClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
            isLmbDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            isLmbReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
            isLmbDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        }

        Rlgl.PopMatrix();
    }

    public void BeginLevelScissorMode()
    {
        Raylib.BeginScissorMode(
            (int) (-viewOffset.X * viewZoom),
            (int) (-viewOffset.Y * viewZoom),
            (int) (Editor.Level.Width * Level.TileSize * viewZoom),
            (int) (Editor.Level.Height * Level.TileSize * viewZoom)
        );
    }

    public bool IsShortcutActivated(RainEd.ShortcutID id)
        => Editor.IsShortcutActivated(id);
}