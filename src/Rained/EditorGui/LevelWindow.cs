namespace Rained.EditorGui;
using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using Rained.EditorGui.Editors;
using Rained.Rendering;
using Rained.LevelData;
using CellSelection = Editors.CellEditing.CellSelection;

enum CellDirtyFlags
{
    Geometry = 1,
    Objects = 2,
    Material = 4,
    TileHead = 8
};

class LevelWindow
{
    public bool IsWindowOpen = true;

    private const float ZoomFactorPerStep = 1.5f;
    private const int MaxZoomSteps = 11;
    private const int MinZoomSteps = -10;

    public float ViewZoom { get => RainEd.Instance.CurrentTab!.ViewZoom; set => RainEd.Instance.CurrentTab!.ViewZoom = value; }
    public ref int WorkLayer => ref RainEd.Instance.CurrentTab!.WorkLayer;
    public ref Vector2 ViewOffset => ref RainEd.Instance.CurrentTab!.ViewOffset;

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

    private readonly IEditorMode[] editorModes = new IEditorMode[7];
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
            if (renderer.UsePalette)
            {
                return renderer.Palette.GetPaletteColor(PaletteColor.Sky);
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
        if (renderer.UsePalette)
        {
            var col = renderer.Palette.GetPaletteColor(PaletteColor.Black);
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
        if (renderer.UsePalette)
        {
            col = renderer.Palette.GetPaletteColor(PaletteColor.Black);
        }
        else
        {
            col = RainEd.Instance.Preferences.LayerColor1.ToRaylibColor(255);
        }

        return new Color(
            (byte)(col.R * (1f - fade) + 255f * fade),
            (byte)(col.G * (1f - fade) + 255f * fade),
            (byte)(col.B * (1f - fade) + 255f * fade),
            (byte)alpha
        );  
    }

    public LevelWindow()
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

        editorModes[(int)EditModeEnum.Environment] = new EnvironmentEditor(this);
        editorModes[(int)EditModeEnum.Geometry] = new GeometryEditor(this);
        editorModes[(int)EditModeEnum.Tile] = new TileEditor(this);
        editorModes[(int)EditModeEnum.Camera] = new CameraEditor(this);
        editorModes[(int)EditModeEnum.Light] = new LightEditor(this);
        editorModes[(int)EditModeEnum.Effect] = new EffectsEditor(this);
        editorModes[(int)EditModeEnum.Prop] = new PropEditor(this);

        // load user preferences
        var prefs = RainEd.Instance.Preferences;
        Renderer.UsePalette = prefs.UsePalette;
        Renderer.Palette.Index = prefs.PaletteIndex;
        Renderer.Palette.FadeIndex = prefs.PaletteFadeIndex;
        Renderer.Palette.Mix = prefs.PaletteFade;
    }

    public void SavePreferences(UserPreferences prefs)
    {
        // i suppose this is redundant, as the PaletteWindow automatically
        // updates the values in the prefs json
        prefs.UsePalette = Renderer.UsePalette;
        prefs.PaletteFadeIndex = Renderer.Palette.FadeIndex;
        prefs.PaletteFade = Renderer.Palette.Mix;
        
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
        
        RainEd.Instance.CurrentTab!.NodeData.Reset();
    }

    public void ChangeLevel(Level newLevel)
    {
        foreach (var mode in editorModes)
            mode.ChangeLevel(newLevel);
        
        RainEd.Instance.CurrentTab!.NodeData.Reset();
    }

    public void LevelCreated(Level level)
    {
        foreach (var mode in editorModes)
            mode.LevelCreated(level);
    }

    public void LevelClosed(Level level)
    {
        foreach (var mode in editorModes)
            mode.LevelClosed(level);
    }

    public void ResetView()
    {
        ViewOffset = Vector2.Zero;
        RainEd.Instance.CurrentTab!.ViewZoom = 1f;
        RainEd.Instance.CurrentTab!.ZoomSteps = 0;
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

    public void SwitchMode(int newMode)
    {
        if (newMode != selectedMode)
        {
            Log.Information("Switch to {Editor} editor", editorModes[newMode].Name);
            
            if (!editorModes[newMode].SupportsCellSelection && CellSelection.Instance is not null)
            {
                CellSelection.Instance.Deactivate();
                CellSelection.Instance = null;
            }

            editorModes[selectedMode].Unload();
            selectedMode = newMode;
            editorModes[selectedMode].Load();
        }
    }

    public void Render()
    {
        var dt = Raylib.GetFrameTime();
        var prefs = RainEd.Instance.Preferences;

        if (queuedEditMode >= 0)
        {
            SwitchMode(queuedEditMode);
            queuedEditMode = -1;
        }
        
        if (IsWindowOpen)
        {
            var newEditMode = selectedMode;

            // edit mode switch radial menu
            {
                Span<string> options = ["Env", "Geo", "Tiles", "Cameras", "Light", "Effects", "Props"];
                var sel = RadialMenu.PopupRadialMenu("Mode Switch", KeyShortcut.SelectEditor, options, selectedMode);
                if (sel != -1)
                {
                    newEditMode = sel;
                }
            }

            if (ImGui.Begin("Level"))
            {
                // edit mode
                if (!prefs.HideEditorSwitch)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Edit Mode");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 8f);
                    if (ImGui.BeginCombo("##EditMode", editorModes[selectedMode].Name))
                    {
                        for (int i = 0; i < editorModes.Length; i++)
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
                }

                editorModes[selectedMode].DrawStatusBar();
                ImGui.SameLine();
                ImGui.TextUnformatted(StatusText);
                StatusText = string.Empty;

                if (!prefs.MinimalStatusBar)
                {
                    var zoomText = $"Zoom: {Math.Floor(ViewZoom * 100f)}%";
                    var mouseText = $"Mouse: ({MouseCx}, {MouseCy})";
                    WriteStatus(zoomText.PadRight(12, ' ') + mouseText);
                }

                //ImGui.TextUnformatted($"Zoom: {Math.Floor(viewZoom * 100f)}%      {StatusText}");


                if (!ImGui.GetIO().WantTextInput)
                {
                    // scroll keybinds
                    var moveX = (EditorWindow.IsKeyDown(ImGuiKey.RightArrow)?1:0) - (EditorWindow.IsKeyDown(ImGuiKey.LeftArrow)?1:0);
                    var moveY = (EditorWindow.IsKeyDown(ImGuiKey.DownArrow)?1:0) - (EditorWindow.IsKeyDown(ImGuiKey.UpArrow)?1:0);
                    var speedMult = Math.Max(0.75f, 1.0f / ViewZoom);
                    var moveSpeed = (EditorWindow.IsKeyDown(ImGuiKey.ModShift) ? 90f : 30f) * speedMult;
                    ViewOffset.X += moveX * Level.TileSize * moveSpeed * dt;
                    ViewOffset.Y += moveY * Level.TileSize * moveSpeed * dt;

                    // edit mode keybinds
                    if (KeyShortcuts.Activated(KeyShortcut.EnvironmentEditor))
                        newEditMode = (int) EditModeEnum.Environment;
                    
                    if (KeyShortcuts.Activated(KeyShortcut.GeometryEditor))
                        newEditMode = (int) EditModeEnum.Geometry;
                    
                    if (KeyShortcuts.Activated(KeyShortcut.TileEditor))
                        newEditMode = (int) EditModeEnum.Tile;
                    
                    if (KeyShortcuts.Activated(KeyShortcut.CameraEditor))
                        newEditMode = (int) EditModeEnum.Camera;
                    
                    if (KeyShortcuts.Activated(KeyShortcut.LightEditor))
                        newEditMode = (int) EditModeEnum.Light;
                    
                    if (KeyShortcuts.Activated(KeyShortcut.EffectsEditor))
                        newEditMode = (int) EditModeEnum.Effect;

                    if (KeyShortcuts.Activated(KeyShortcut.PropEditor))
                        newEditMode = (int) EditModeEnum.Prop;
                }

                // change edit mode if requested
                if (newEditMode != selectedMode)
                    SwitchMode(newEditMode);

                // canvas widget
                {
                    var regionMax = ImGui.GetWindowContentRegionMax();
                    var regionMin = ImGui.GetCursorPos();

                    int canvasW = (int)(regionMax.X - regionMin.X);
                    int canvasH = (int)(regionMax.Y - regionMin.Y);
                    canvasW = Math.Max(canvasW, 1);
                    canvasH = Math.Max(canvasH, 1);

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
                    
                    if (!RainEd.Instance.IsLevelLocked)
                    {
                        Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                        DrawCanvas();
                        Raylib.EndTextureMode();
                    }

                    canvasWidget.Draw();
                }
            }
            ImGui.End();
        }
        
        editorModes[selectedMode].DrawToolbar();
    }

    private void Zoom(float factor, Vector2 mpos)
    {
        ViewZoom *= factor;
        ViewOffset = -(mpos - ViewOffset) / factor + mpos;
    }

    private void DrawCanvas()
    {
        var doc = RainEd.Instance.CurrentTab!;

        OverrideMouseWheel = false;
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        Rlgl.PushMatrix();
        Rlgl.Scalef(doc.ViewZoom, doc.ViewZoom, 1f);
        Rlgl.Translatef(-doc.ViewOffset.X, -doc.ViewOffset.Y, 0);

        // send view info to the level renderer
        var viewportW = canvasWidget.RenderTexture.Texture.Width;
        var viewportH = canvasWidget.RenderTexture.Texture.Height;
        Renderer.ViewTopLeft = doc.ViewOffset / Level.TileSize;
        Renderer.ViewBottomRight =
            (ViewOffset + new Vector2(viewportW, viewportH) / doc.ViewZoom)
            / Level.TileSize;
        Renderer.ViewZoom = doc.ViewZoom;

        // obtain mouse coordinates
        mouseCellFloat.X = (canvasWidget.MouseX / doc.ViewZoom + doc.ViewOffset.X) / Level.TileSize;
        mouseCellFloat.Y = (canvasWidget.MouseY / doc.ViewZoom + doc.ViewOffset.Y) / Level.TileSize;
        mouseCx = (int) Math.Floor(mouseCellFloat.X);
        mouseCy = (int) Math.Floor(mouseCellFloat.Y);
        
        // view controls
        if (canvasWidget.IsHovered)
        {
            // middle click pan
            if (EditorWindow.IsPanning || ImGui.IsMouseDown(ImGuiMouseButton.Middle))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                ViewOffset -= mouseDelta / ViewZoom;
            }

            // begin alt+left panning
            if (ImGui.IsKeyDown(ImGuiKey.ModAlt) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                EditorWindow.IsPanning = true;
            }
        }

        // draw viewport
        // the blending functions here are some stupid hack
        // to fix transparent object writing to the fbo's alpha value
        // when being drawn, thus making the render texture at those areas
        // blend into the imgui background when rendered.
        // Good thing I downloaded renderdoc, otherwise there was no way
        // I would've figured that was the problem!

        // glSrcRGB: Silk.NET.OpenGL.BlendingFactor.SrcAlpha
        // glDstRGB: Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha
        // glSrcAlpha: 1
        // glDstAlpha: Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha
        // glEqRGB: Silk.NET.OpenGL.BlendEquationModeEXT.FuncAdd
        // glEqAlpha: Silk.NET.OpenGL.BlendEquationModeEXT.FuncAdd
        // TODO: blending
        RainEd.RenderContext!.BlendMode = Glib.BlendMode.CorrectedFramebufferNormal;
        //RainEd.RenderContext.SetBlendFactorsSeparate(0x0302, 0x0303, 1, 0x0303, 0x8006, 0x8006);
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

        RainEd.RenderContext.BlendMode = Glib.BlendMode.Normal;

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

            doc.ZoomSteps = Math.Clamp(doc.ZoomSteps + wheelMove, MinZoomSteps, MaxZoomSteps);

            if (wheelMove != 0f)
            {
                var newZoom = MathF.Pow(ZoomFactorPerStep, doc.ZoomSteps);
                Zoom((float)(newZoom / doc.ViewZoom), mouseCellFloat * Level.TileSize);
                doc.ViewZoom = newZoom; // to mitigate potential floating point error accumulation
            }

            /*if (wheelMove > 0f && zoomSteps < 7)
            {
                var newZoom = MathF.Pow(ZoomFactorPerStep, zoomSteps);
                //var newZoom = Math.Round(viewZoom * zoomFactor * 1000.0) / 1000.0;
                Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                zoomSteps++;
            }
            else if (wheelMove < 0f && zoomSteps > -5)
            {
                var newZoom = Math.Round(viewZoom / zoomFactor * 1000.0) / 1000.0;
                Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                zoomSteps--;
            }*/
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
            (int) (-ViewOffset.X * ViewZoom),
            (int) (-ViewOffset.Y * ViewZoom),
            (int) (RainEd.Instance.Level.Width * Level.TileSize * ViewZoom),
            (int) (RainEd.Instance.Level.Height * Level.TileSize * ViewZoom)
        );
    }

    /// <summary>
    /// Invalidate geometry for both the renderer and node list.
    /// </summary>
    /// <param name="x">The X position of the dirty cell.</param>
    /// <param name="y">The Y position of the dirty cell.</param>
    /// <param name="layer">The work layer of the dirty cell.</param>
    public void InvalidateGeo(int x, int y, int layer)
    {
        Renderer.InvalidateGeo(x, y, layer);
        if (layer == 0)
            RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(x, y);
    }

    /// <summary>
    /// Invalidate data of a cell.
    /// </summary>
    /// <param name="x">The X position of the dirty cell.</param>
    /// <param name="y">The Y position of the dirty cell.</param>
    /// <param name="layer">The work layer of the dirty cell.</param>
    /// <param name="flags">The data to invalidate.</param>
    public void InvalidateCell(int x, int y, int layer, CellDirtyFlags flags = CellDirtyFlags.Geometry | CellDirtyFlags.Objects)
    {
        if (flags.HasFlag(CellDirtyFlags.Geometry))
            Renderer.InvalidateGeo(x, y, layer);
        
        if (layer == 0 && flags.HasFlag(CellDirtyFlags.Objects))
            RainEd.Instance.CurrentTab!.NodeData.InvalidateCell(x, y);

        if (flags.HasFlag(CellDirtyFlags.TileHead))
            Renderer.InvalidateTileHead(x, y, layer);
    }

    /// <summary>
    /// Invalidate all data of a cell.
    /// </summary>
    /// <param name="x">The X position of the dirty cell.</param>
    /// <param name="y">The Y position of the dirty cell.</param>
    /// <param name="layer">The work layer of the dirty cell.</param>
    public void InvalidateCell(int x, int y, int layer)
    {
        InvalidateCell(x, y, layer, CellDirtyFlags.Geometry | CellDirtyFlags.Objects | CellDirtyFlags.Material | CellDirtyFlags.TileHead);
    }
}