using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using Rained.LevelData;

namespace Rained.EditorGui.Editors;

class LightEditor : IEditorMode
{
    public string Name { get => "Light"; }
    private readonly LevelWindow window;

    private Vector2 brushSize = new(50f, 70f);
    private float brushRotation = 0f;
    private int selectedBrush = 0;

    private bool isCursorEnabled = true;
    private bool isDrawing = false;
    private bool isChangingParameters = false; // true if the user is using keyboard shortcuts to change parameters
    private Vector2 savedMouseGp = new();
    private Vector2 savedMousePos = new();

    private ChangeHistory.LightChangeRecorder? changeRecorder = null;

    public LightEditor(LevelWindow window)
    {
        this.window = window;
        ReloadLevel();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder?.Dispose();
            changeRecorder = new ChangeHistory.LightChangeRecorder(RainEd.Instance.Level.LightMap.GetImage());
            changeRecorder.UpdateParametersSnapshot();
        };

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            changeRecorder?.UpdateParametersSnapshot();
        };
    }

    public void ReloadLevel()
    {   
        changeRecorder?.Dispose();
        changeRecorder = new ChangeHistory.LightChangeRecorder(RainEd.Instance.Level.LightMap.GetImage());
        changeRecorder.UpdateParametersSnapshot();
    }

    public void Load()
    {
        changeRecorder?.ClearStrokeData();
    }

    public void Unload()
    {
        if (isChangingParameters)
        {
            changeRecorder?.PushParameterChanges();
            isChangingParameters = false;
        }

        if (!isCursorEnabled)
        {
            Raylib.ShowCursor();
            Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            isCursorEnabled = true;
        }
    }

    public void ShowEditMenu()
    {
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.ResetBrushTransform, "Reset Brush Transform");
    }

    public void DrawToolbar()
    {
        bool wasParamChanging = isChangingParameters;
        isChangingParameters = false;

        var level = RainEd.Instance.Level;
        var brushDb = RainEd.Instance.LightBrushDatabase;
        var prefs = RainEd.Instance.Preferences;

        if (ImGui.Begin("Light###Light Catalog", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (changeRecorder is null) ImGui.BeginDisabled();

            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

            ImGui.SliderAngle("Light Angle", ref level.LightAngle, 0f, 360f, "%.1f deg");
            if (ImGui.IsItemDeactivatedAfterEdit())
                changeRecorder?.PushParameterChanges();
            
            ImGui.SliderFloat("Light Distance", ref level.LightDistance, 1f, Level.MaxLightDistance, "%.3f", ImGuiSliderFlags.AlwaysClamp);
            if (ImGui.IsItemDeactivatedAfterEdit())
                changeRecorder?.PushParameterChanges();
            
            ImGui.PopItemWidth();

            // draw light angle ring
            var avail = ImGui.GetContentRegionAvail();
            if (avail.X != 0) // this happens when the window first appears
            {
                ImGui.NewLine();

                var drawList = ImGui.GetWindowDrawList();
                var screenCursor = ImGui.GetCursorScreenPos();
                
                var minRadius = 8f;
                var maxRadius = 70f;
                var radius = (level.LightDistance - 1f) / (Level.MaxLightDistance - 1f) * (maxRadius - minRadius) + minRadius;
                var centerRadius = (5f - 1f) / (Level.MaxLightDistance - 1f) * (maxRadius - minRadius) + minRadius;

                centerRadius *= Boot.WindowScale;
                radius *= Boot.WindowScale;

                var color = ImGui.ColorConvertFloat4ToU32( ImGui.GetStyle().Colors[(int) ImGuiCol.Text] );

                var circleCenter = screenCursor + new Vector2(avail.X / 2f, maxRadius * Boot.WindowScale);
                drawList.AddCircle(circleCenter, centerRadius, color); // draw center circle
                drawList.AddCircle(circleCenter, radius, color); // draw distance circle
                
                // draw angle
                var correctedAngle = MathF.PI / 2f + level.LightAngle;

                drawList.AddCircleFilled(
                    new Vector2(MathF.Cos(correctedAngle), MathF.Sin(correctedAngle)) * radius + circleCenter,
                    6f * Boot.WindowScale,
                    color
                );

                ImGui.InvisibleButton("LightRing", new Vector2(avail.X, maxRadius * 2f * Boot.WindowScale));
                if (ImGui.IsItemActive())
                {
                    isChangingParameters = true;

                    var vecDiff = (ImGui.GetMousePos() - circleCenter) / Boot.WindowScale;

                    level.LightAngle = MathF.Atan2(vecDiff.Y, vecDiff.X) - MathF.PI / 2f;
                    if (level.LightAngle < 0)
                    {
                        level.LightAngle += 2f * MathF.PI;
                    }

                    level.LightDistance = (vecDiff.Length() - minRadius) / (maxRadius - minRadius) * (Level.MaxLightDistance - 1f) + 1f;
                    level.LightDistance = Math.Clamp(level.LightDistance, 1f, Level.MaxLightDistance);
                }
                ImGui.NewLine();
            }

            if (changeRecorder is null) ImGui.EndDisabled();
        } ImGui.End();

        if (ImGui.Begin("Brush", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (ImGui.Button("Reset Brush") || KeyShortcuts.Activated(KeyShortcut.ResetBrushTransform))
            {
                brushSize = new(50f, 70f);
                brushRotation = 0f;
            }

            ImGui.BeginChild("BrushCatalog", ImGui.GetContentRegionAvail());
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

                int i = 0;
                foreach (var brush in RainEd.Instance.LightBrushDatabase.Brushes)
                {
                    var texture = brush.Texture;
                    
                    // highlight selected brush
                    if (i == selectedBrush)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered]);
                    
                    // buttons will have a more transparent hover color
                    } else {
                        Vector4 col = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered];
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                            new Vector4(col.X, col.Y, col.Z, col.W / 4f));
                    }

                    ImGui.PushID(i);
                    if (ImGuiExt.ImageButtonRect("##Texture", texture, 64 * Boot.WindowScale, 64 * Boot.WindowScale, new Rectangle(0, 0, texture.Width, texture.Height)))
                    {
                        selectedBrush = i;
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(brush.Name);

                    ImGui.PopStyleColor();

                    ImGui.PopID();

                    i++;

                    if (!(i % 3 == 0)) ImGui.SameLine();
                }

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();
        } ImGui.End();

        // keyboard catalog navigation
        if (prefs.LightEditorControlScheme == UserPreferences.LightEditorControlSchemeOption.Mouse)
        {
            // S to move down one row
            if (KeyShortcuts.Activated(KeyShortcut.NavDown))
            {
                int maxRow = brushDb.Brushes.Count / 3;
                int curRow = selectedBrush / 3;
                int col = selectedBrush % 3;

                // selected will only wrap around if it was on
                // the last row
                if (curRow == maxRow)
                {
                    curRow = 0;
                }
                else
                {
                    curRow++;
                }

                selectedBrush = Math.Clamp(curRow * 3 + col, 0, brushDb.Brushes.Count - 1);
            }

            // W to move up one row
            if (KeyShortcuts.Activated(KeyShortcut.NavUp))
            {
                int maxRow = brushDb.Brushes.Count / 3;
                int curRow = selectedBrush / 3;
                int col = selectedBrush % 3;

                // selected will only wrap around if it was on
                // the first row
                if (curRow == 0)
                {
                    curRow = maxRow;
                }
                else
                {
                    curRow--;
                }

                selectedBrush = Math.Clamp(curRow * 3 + col, 0, brushDb.Brushes.Count - 1);
            }

            // D to move right
            if (KeyShortcuts.Activated(KeyShortcut.NavRight))
                selectedBrush = (selectedBrush + 1) % brushDb.Brushes.Count;
            
            // A to move left
            if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
            {
                if (selectedBrush == 0)
                    selectedBrush = brushDb.Brushes.Count - 1;
                else
                    selectedBrush--;
            }
        }
        else
        {
            if (KeyShortcuts.Activated(KeyShortcut.NextBrush))
                selectedBrush = (selectedBrush + 1) % brushDb.Brushes.Count;
            
            if (KeyShortcuts.Activated(KeyShortcut.PreviousBrush))
            {
                if (selectedBrush == 0)
                    selectedBrush = brushDb.Brushes.Count - 1;
                else
                    selectedBrush--;
            }
        }

        // when shift is held, WASD changes light parameters
        if (changeRecorder is not null)
        {
            var lightAngleChange = 0f;
            var lightDistChange = 0f;

            if (KeyShortcuts.Active(KeyShortcut.RotateLightCW))
            {
                isChangingParameters = true;
                lightAngleChange = 1f;
            }

            if (KeyShortcuts.Active(KeyShortcut.RotateLightCCW))
            {
                isChangingParameters = true;
                lightAngleChange = -1f;
            }

            if (KeyShortcuts.Active(KeyShortcut.ZoomLightIn))
            {
                isChangingParameters = true;
                lightDistChange = -1f;
            }

            if (KeyShortcuts.Active(KeyShortcut.ZoomLightOut))
            {
                isChangingParameters = true;
                lightDistChange = 1f;
            }

            if (isChangingParameters)
            {
                level.LightAngle += lightAngleChange * (100f / 180f * MathF.PI) * Raylib.GetFrameTime();
                level.LightDistance += lightDistChange * 20f * Raylib.GetFrameTime();

                // wrap around light angle
                if (level.LightAngle > 2f * MathF.PI)
                    level.LightAngle -= 2f * MathF.PI;
                if (level.LightAngle < 0)
                    level.LightAngle += 2f * MathF.PI;

                // clamp light distance
                level.LightDistance = Math.Clamp(level.LightDistance, 1f, Level.MaxLightDistance);
            }
        }

        if (wasParamChanging && !isChangingParameters)
        {
            changeRecorder!.PushParameterChanges();
        }
    }

    private void DrawOcclusionPlane()
    {
        var level = RainEd.Instance.Level;
        
        // render light plane
        var levelBoundsW = level.Width * 20;
        var levelBoundsH = level.Height * 20;
        RlExt.DrawRenderTextureV(
            level.LightMap.RenderTexture,
            new Vector2(levelBoundsW - level.LightMap.Width, levelBoundsH - level.LightMap.Height),
            new Color(255, 0, 0, 100)
        );
        /*Raylib.DrawTextureRec(
            level.LightMap.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(levelBoundsW - level.LightMap.Width, levelBoundsH - level.LightMap.Height),
            new Color(255, 0, 0, 100)
        );*/
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;
        var prefs = RainEd.Instance.Preferences;

        var levelBoundsW = level.Width * 20;
        var levelBoundsH = level.Height * 20;
        var lightMapOffset = new Vector2(
            levelBoundsW - level.LightMap.Width,
            levelBoundsH - level.LightMap.Height
        );

        var wasCursorEnabled = isCursorEnabled;
        var wasDrawing = isDrawing;
        isCursorEnabled = true;
        isDrawing = false;

        // draw light background
        Raylib.DrawRectangle(
            (int)lightMapOffset.X,
            (int)lightMapOffset.Y,
            level.Width * Level.TileSize - (int)lightMapOffset.X,
            level.Height * Level.TileSize - (int)lightMapOffset.Y,
            Color.White
        );
        
        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, LevelWindow.BackgroundColor);
        
        // draw the layers
        var drawTiles = RainEd.Instance.Preferences.ViewTiles;
        var drawProps = RainEd.Instance.Preferences.ViewProps;
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == 0 ? 255 : 50;
            var color = LevelWindow.GeoColor(30f / 255f, alpha);
            int offset = l * 2;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            levelRender.RenderGeometry(l, color);

            if (drawTiles)
                levelRender.RenderTiles(l, (int)(alpha * (100.0f / 255.0f)));
            
            if (drawProps)
                levelRender.RenderProps(l, (int)(alpha * (100.0f / 255.0f)));
            
            Rlgl.PopMatrix();
        }

        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();

        var shader = Shaders.LevelLightShader;
        Raylib.BeginShaderMode(shader);
        //shader.GlibShader.SetUniform("uColor", Glib.Color.FromRGBA(0, 0, 0, 80));
        //shader.GlibShader.SetUniform("uTexture", RainEd.RenderContext.WhiteTexture);

        // render cast
        var correctedAngle = level.LightAngle + MathF.PI / 2f;
        Vector2 castOffset = new(
            -MathF.Cos(correctedAngle) * level.LightDistance * Level.TileSize,
            -MathF.Sin(correctedAngle) * level.LightDistance * Level.TileSize
        );

        RlExt.DrawRenderTextureV(level.LightMap.RenderTexture, lightMapOffset + castOffset, new Color(0, 0, 0, 80));
        /*Raylib.DrawTextureRec(
            level.LightMap.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            lightMapOffset + castOffset,
            new Color(0, 0, 0, 80)
        );*/

        // Render mouse cursor
        if (window.IsViewportHovered && changeRecorder is not null)
        {
            var tex = RainEd.Instance.LightBrushDatabase.Brushes[selectedBrush].Texture;
            var mpos = window.MouseCellFloat;
            if (!wasCursorEnabled) mpos = savedMouseGp;

            var screenSize = brushSize / window.ViewZoom;

            // render brush preview
            // if drawing, draw on light texture instead of screen
            var lmb = EditorWindow.IsMouseDown(ImGuiMouseButton.Left);
            var rmb = EditorWindow.IsMouseDown(ImGuiMouseButton.Right);
            if (lmb || rmb)
            {
                isDrawing = true;

                DrawOcclusionPlane();

                Rlgl.LoadIdentity(); // why the hell do i have to call this
                level.LightMap.RaylibBeginTextureMode();

                // draw on brush plane
                BrushAtom atom = new()
                {
                    rect = new Rectangle(
                        mpos.X * Level.TileSize - lightMapOffset.X,
                        mpos.Y * Level.TileSize - lightMapOffset.Y,
                        screenSize.X, screenSize.Y
                    ),
                    rotation = brushRotation,
                    mode = lmb,
                    brush = selectedBrush
                };

                changeRecorder.RecordAtom(atom);
                LightMap.DrawAtom(atom);
                
                Raylib.BeginTextureMode(mainFrame);
            }
            else
            {
                // cast of brush preview
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    new Rectangle(
                        mpos.X * Level.TileSize + castOffset.X,
                        mpos.Y * Level.TileSize + castOffset.Y,
                        screenSize.X, screenSize.Y
                    ),
                    screenSize / 2f,
                    brushRotation,
                    new Color(0, 0, 0, 80)
                );
                
                DrawOcclusionPlane();

                // draw preview on on occlusion plane
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    new Rectangle(
                        mpos.X * Level.TileSize,
                        mpos.Y * Level.TileSize,
                        screenSize.X, screenSize.Y
                    ),
                    screenSize / 2f,
                    brushRotation,
                    new Color(255, 0, 0, 100)
                );
            }

            switch (prefs.LightEditorControlScheme)
            {
                case UserPreferences.LightEditorControlSchemeOption.Mouse:
                {
                    var doScale = KeyShortcuts.Active(KeyShortcut.ScaleLightBrush);
                    var doRotate = KeyShortcuts.Active(KeyShortcut.RotateLightBrush);

                    if (doScale || doRotate)
                    {
                        if (wasCursorEnabled)
                        {
                            savedMouseGp = mpos;
                            savedMousePos = Raylib.GetMousePosition();
                        }
                        isCursorEnabled = false;

                        if (doScale)
                            brushSize += Raylib.GetMouseDelta();
                        if (doRotate)
                            brushRotation -= Raylib.GetMouseDelta().Y / 2f;
                    }

                    break;
                }

                case UserPreferences.LightEditorControlSchemeOption.Keyboard:
                {
                    var rotSpeed = Raylib.GetFrameTime() * 60f;

                    if (KeyShortcuts.Active(KeyShortcut.RotateBrushCW))
                        brushRotation += rotSpeed;
                    if (KeyShortcuts.Active(KeyShortcut.RotateBrushCCW))
                        brushRotation -= rotSpeed;
                    
                    var scaleSpeed = Raylib.GetFrameTime() * 60f;

                    if (KeyShortcuts.Active(KeyShortcut.NavRight))
                        brushSize.X += scaleSpeed;
                    if (KeyShortcuts.Active(KeyShortcut.NavLeft))
                        brushSize.X -= scaleSpeed;
                    if (KeyShortcuts.Active(KeyShortcut.NavUp))
                        brushSize.Y += scaleSpeed;
                    if (KeyShortcuts.Active(KeyShortcut.NavDown))
                        brushSize.Y -= scaleSpeed;
                    
                    break;
                }
            }

            brushSize.X = MathF.Max(0f, brushSize.X);
            brushSize.Y  = MathF.Max(0f, brushSize.Y);
        }
        else
        {
            DrawOcclusionPlane();
        }

        Raylib.EndShaderMode();
        
        // record stroke data at the end of the stroke
        if (wasDrawing && !isDrawing)
        {
            changeRecorder!.EndStroke();
        }

        // handle cursor lock when transforming brush
        if (!isCursorEnabled)
        {
            Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);    
        }
        
        if (wasCursorEnabled != isCursorEnabled)
        {
            if (isCursorEnabled)
            {
                Raylib.ShowCursor();
                Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            }
            else
            {
                Raylib.HideCursor();
            }
        }
    }
}