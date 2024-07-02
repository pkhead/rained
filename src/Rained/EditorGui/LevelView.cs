using System.Numerics;
using Raylib_cs;
using ImGuiNET;
namespace RainEd;

class LevelView
{
    public bool IsWindowOpen = true;

    private Vector2 viewOffset = new();
    private float viewZoom = 1f;
    private int zoomSteps = 0;
    private int workLayer = 0;

    public float ViewZoom { get => viewZoom; }
    public int WorkLayer { get => workLayer; set => workLayer = value; }
    public Vector2 ViewOffset { get => viewOffset; set => viewOffset = value; }

    private int mouseCx = 0;
    private int mouseCy = 0;
    private Vector2 mouseCellFloat = new();

    public int MouseCx { get => mouseCx; }
    public int MouseCy { get => mouseCy; }
    public Vector2 MouseCellFloat { get => mouseCellFloat; }
    public bool IsMouseInLevel() => RainEd.Instance.Level.IsInBounds(mouseCx, mouseCy);
    public bool OverrideMouseWheel = false;
    public string StatusText { get; private set; } = string.Empty;

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
    private readonly RlManaged.RenderTexture2D[] layerRenderTextures;
    public readonly LevelEditRender Renderer;

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

    public static Color BackgroundColor
    {
        get
        {
            var renderer = RainEd.Instance.LevelView.Renderer;
            if (renderer.Palette >= 0)
            {
                return renderer.GetPaletteColor(PaletteColor.Sky);
            }
            else
            {
                var col = RainEd.Instance.Preferences.BackgroundColor;
                return new Color(col.R, col.G, col.B, (byte)255);
            }
        }
    }

    public static Color GeoColor(int alpha)
    {
        var renderer = RainEd.Instance.LevelView.Renderer;
        if (renderer.Palette >= 0)
        {
            var col = renderer.GetPaletteColor(PaletteColor.Black);
            return new Color(col.R, col.G, col.B, (byte)alpha);
        }
        else
        {
            var col = RainEd.Instance.Preferences.LayerColor1;
            return new Color(col.R, col.G, col.B, (byte)alpha);  
        }
    }

    public static Color GeoColor(float fade, int alpha)
    {
        fade = Math.Clamp(fade, 0f, 1f);

        var renderer = RainEd.Instance.LevelView.Renderer;
        Color col;
        if (renderer.Palette >= 0)
        {
            col = renderer.GetPaletteColor(PaletteColor.Black);
        }
        else
        {
            col = RainEd.Instance.Preferences.LayerColor1.ToRGBA(255);
        }

        return new Color(
            (byte)(col.R * (1f - fade) + 255f * fade),
            (byte)(col.G * (1f - fade) + 255f * fade),
            (byte)(col.B * (1f - fade) + 255f * fade),
            (byte)alpha
        );  
    }

    public LevelView()
    {
        canvasWidget = new(1, 1);
        layerRenderTextures = new RlManaged.RenderTexture2D[3];

        for (int i = 0; i < 3; i++)
        {
            layerRenderTextures[i] = RlManaged.RenderTexture2D.Load(1, 1);
        }
        
        Renderer = new LevelEditRender();
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
        Renderer.ViewGrid = RainEd.Instance.Preferences.ViewGrid;
        Renderer.ViewObscuredBeams = RainEd.Instance.Preferences.ViewObscuredBeams;
        Renderer.ViewTileHeads = RainEd.Instance.Preferences.ViewTileHeads;
        Renderer.ViewCameras = RainEd.Instance.Preferences.ViewCameras;
        Renderer.Palette = RainEd.Instance.Preferences.UsePalette ? RainEd.Instance.Preferences.PaletteIndex : -1;
        Renderer.FadePalette = RainEd.Instance.Preferences.PaletteFadeIndex;
        Renderer.PaletteMix = RainEd.Instance.Preferences.PaletteFade;
    }

    public void SavePreferences(UserPreferences prefs)
    {
        prefs.ViewGrid = Renderer.ViewGrid;
        prefs.ViewObscuredBeams = Renderer.ViewObscuredBeams;
        prefs.ViewTileHeads = Renderer.ViewTileHeads;
        prefs.ViewCameras = Renderer.ViewCameras;

        // i suppose this is redundant, as the PaletteWindow automatically
        // updates the values in the prefs json
        prefs.UsePalette = Renderer.Palette != -1;
        prefs.PaletteFadeIndex = Renderer.FadePalette;
        prefs.PaletteFade = Renderer.PaletteMix;
        
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

    public void ResetView()
    {
        viewOffset = Vector2.Zero;
        viewZoom = 1f;
        zoomSteps = 0;
    }

    public void ShowEditMenu()
    {
        var mode = editorModes[selectedMode];
        ImGui.TextDisabled(mode.Name);
        mode.ShowEditMenu();
    }

    /// <summary>
    /// Append the given text to the status text.
    /// </summary>
    /// <param name="text">The text to append</param>
    /// <param name="extraSpace">Extra text space to reserve for the text</param>
    public void WriteStatus(string text, int extraSpace = 0)
    {
        int spaces = 6 * (extraSpace+1);
        StatusText += text.PadRight(((int)MathF.Floor((text.Length+1) / spaces) + 1) * spaces, ' ');
    }

    public void Render()
    {
        var dt = Raylib.GetFrameTime();

        if (queuedEditMode >= 0)
        {
            if (queuedEditMode != selectedMode)
            {
                RainEd.Logger.Information("Switch to {Editor} editor", editorModes[queuedEditMode].Name);
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
            ImGui.TextUnformatted(StatusText);
            StatusText = string.Empty;
            WriteStatus($"Zoom: {Math.Floor(viewZoom * 100f)}%");
            WriteStatus($"Mouse: ({MouseCx}, {MouseCy})");
            //ImGui.TextUnformatted($"Zoom: {Math.Floor(viewZoom * 100f)}%      {StatusText}");


            if (!ImGui.GetIO().WantTextInput)
            {
                // scroll keybinds
                var moveX = (EditorWindow.IsKeyDown(ImGuiKey.RightArrow)?1:0) - (EditorWindow.IsKeyDown(ImGuiKey.LeftArrow)?1:0);
                var moveY = (EditorWindow.IsKeyDown(ImGuiKey.DownArrow)?1:0) - (EditorWindow.IsKeyDown(ImGuiKey.UpArrow)?1:0);
                var moveSpeed = EditorWindow.IsKeyDown(ImGuiKey.ModShift) ? 60f : 30f;
                viewOffset.X += moveX * Level.TileSize * moveSpeed * dt;
                viewOffset.Y += moveY * Level.TileSize * moveSpeed * dt;

                // edit mode keybinds
                if (EditorWindow.IsKeyPressed(ImGuiKey._1))
                    newEditMode = (int) EditModeEnum.Environment;
                
                if (EditorWindow.IsKeyPressed(ImGuiKey._2))
                    newEditMode = (int) EditModeEnum.Geometry;
                
                if (EditorWindow.IsKeyPressed(ImGuiKey._3))
                    newEditMode = (int) EditModeEnum.Tile;
                
                if (EditorWindow.IsKeyPressed(ImGuiKey._4))
                    newEditMode = (int) EditModeEnum.Camera;
                
                if (EditorWindow.IsKeyPressed(ImGuiKey._5))
                    newEditMode = (int) EditModeEnum.Light;
                
                if (EditorWindow.IsKeyPressed(ImGuiKey._6))
                    newEditMode = (int) EditModeEnum.Effect;

                if (EditorWindow.IsKeyPressed(ImGuiKey._7))
                    newEditMode = (int) EditModeEnum.Prop;
            }

            // change edit mode if requested
            if (newEditMode != selectedMode)
            {
                RainEd.Logger.Information("Switch to {Editor} editor", editorModes[newEditMode].Name);
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
        Renderer.ViewTopLeft = viewOffset / Level.TileSize;
        Renderer.ViewBottomRight =
            (viewOffset + new Vector2(viewportW, viewportH) / viewZoom)
            / Level.TileSize;
        Renderer.ViewZoom = viewZoom;

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
        RainEd.RenderContext.SetBlendFactorsSeparate(0x0302, 0x0303, 1, 0x0303, 0x8006, 0x8006);
        editorModes[selectedMode].DrawViewport(canvasWidget.RenderTexture, layerRenderTextures);

        // drwa resize preview
        if (EditorWindow.LevelResizeWindow is LevelResizeWindow resizeData)
        {
            var level = RainEd.Instance.Level;

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

        RainEd.RenderContext.SetBlendMode(Glib.BlendMode.Normal);

        // view controls
        if (canvasWidget.IsHovered)
        {
            // middle click pan
            if (EditorWindow.IsPanning || ImGui.IsMouseDown(ImGuiMouseButton.Middle))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                viewOffset -= mouseDelta / viewZoom;
            }

            // begin alt+left panning
            if (ImGui.IsKeyDown(ImGuiKey.ModAlt) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                EditorWindow.IsPanning = true;
            }
        }

        // scroll wheel zooming
        if (!OverrideMouseWheel)
        {
            var wheelMove = Raylib.GetMouseWheelMove();
            if (!canvasWidget.IsHovered)
                wheelMove = 0f;
            
            if (KeyShortcuts.Activated(KeyShortcut.ViewZoomIn))
            {
                wheelMove = 1f;
            }
            else if (KeyShortcuts.Activated(KeyShortcut.ViewZoomOut))
            {
                wheelMove = -1f;
            }

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

        if (EditorWindow.IsPanning && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            EditorWindow.IsPanning = false;
        }

        Rlgl.PopMatrix();
    }

    public void BeginLevelScissorMode()
    {
        Raylib.BeginScissorMode(
            (int) (-viewOffset.X * viewZoom),
            (int) (-viewOffset.Y * viewZoom),
            (int) (RainEd.Instance.Level.Width * Level.TileSize * viewZoom),
            (int) (RainEd.Instance.Level.Height * Level.TileSize * viewZoom)
        );
    }
}