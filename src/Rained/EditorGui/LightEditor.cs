using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace RainEd;

class LightEditor : IEditorMode
{
    public string Name { get => "Light"; }
    private readonly EditorWindow window;

    private Vector2 brushSize = new(50f, 70f);
    private float brushRotation = 0f;
    private int selectedBrush = 0;

    private bool isCursorEnabled = true;
    private bool isDrawing = false;
    private bool isChangingParameters = false; // true if the user is using keyboard shortcuts to change parameters
    private Vector2 savedMouseGp = new();
    private Vector2 savedMousePos = new();

    private ChangeHistory.LightChangeRecorder changeRecorder = null!;

    public LightEditor(EditorWindow window)
    {
        this.window = window;
        ReloadLevel();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder?.Dispose();
            changeRecorder = new ChangeHistory.LightChangeRecorder();
            changeRecorder.UpdateParametersSnapshot();
        };

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            changeRecorder.UpdateParametersSnapshot();
        };
    }

    public void ReloadLevel()
    {   
        changeRecorder?.Dispose();
        changeRecorder = new ChangeHistory.LightChangeRecorder();
        changeRecorder.UpdateParametersSnapshot();
    }

    public void Load()
    {
        changeRecorder.ClearStrokeData();
    }

    public void Unload()
    {
        if (isChangingParameters)
        {
            changeRecorder.PushParameterChanges();
            isChangingParameters = false;
        }

        if (!isCursorEnabled)
        {
            Raylib.ShowCursor();
            Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            isCursorEnabled = true;
        }
    }

    public void DrawToolbar()
    {
        bool wasParamChanging = isChangingParameters;
        isChangingParameters = false;

        var level = window.Editor.Level;
        var brushDb = RainEd.Instance.LightBrushDatabase;

        if (ImGui.Begin("Light###Light Catalog", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            float lightDeg = level.LightAngle / MathF.PI * 180f;

            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

            ImGui.SliderFloat("Light Angle", ref lightDeg, 0f, 360f, "%.1f deg");
            if (ImGui.IsItemDeactivatedAfterEdit())
                changeRecorder.PushParameterChanges();
            
            ImGui.SliderFloat("Light Distance", ref level.LightDistance, 1f, Level.MaxLightDistance, "%.3f", ImGuiSliderFlags.AlwaysClamp);
            if (ImGui.IsItemDeactivatedAfterEdit())
                changeRecorder.PushParameterChanges();
            
            ImGui.PopItemWidth();

            level.LightAngle = lightDeg / 180f * MathF.PI;

            if (ImGui.Button("Reset Brush") || (!ImGui.GetIO().WantTextInput && EditorWindow.IsKeyPressed(ImGuiKey.R)))
            {
                brushSize = new(50f, 70f);
                brushRotation = 0f;
            }

            // paint brushes
            ImGui.Text("Brushes");

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
                if (rlImGui.ImageButtonRect("##Texture", texture, 64, 64, new Rectangle(0, 0, texture.Width, texture.Height)))
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

        // keyboard brush navigation
        if (!EditorWindow.IsKeyDown(ImGuiKey.ModShift))
        {
            // S to move down one row
            if (EditorWindow.IsKeyPressed(ImGuiKey.S))
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
            if (EditorWindow.IsKeyPressed(ImGuiKey.W))
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
            if (EditorWindow.IsKeyPressed(ImGuiKey.D))
                selectedBrush = (selectedBrush + 1) % brushDb.Brushes.Count;
            
            // A to move left
            if (EditorWindow.IsKeyPressed(ImGuiKey.A))
            {
                if (selectedBrush == 0)
                    selectedBrush = brushDb.Brushes.Count - 1;
                else
                    selectedBrush--;
            }
        }

        // when shift is held, WASD changes light parameters
        else
        {
            var lightAngleChange = 0f;
            var lightDistChange = 0f;

            if (EditorWindow.IsKeyDown(ImGuiKey.D))
            {
                isChangingParameters = true;
                lightAngleChange = 1f;
            }

            if (EditorWindow.IsKeyDown(ImGuiKey.A))
            {
                isChangingParameters = true;
                lightAngleChange = -1f;
            }

            if (EditorWindow.IsKeyDown(ImGuiKey.W))
            {
                isChangingParameters = true;
                lightDistChange = -1f;
            }

            if (EditorWindow.IsKeyDown(ImGuiKey.S))
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
            changeRecorder.PushParameterChanges();
        }
    }

    private void DrawOcclusionPlane()
    {
        var level = window.Editor.Level;
        
        // render light plane
        var levelBoundsW = level.Width * 20;
        var levelBoundsH = level.Height * 20;
        Raylib.DrawTextureRec(
            level.LightMap.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(levelBoundsW - level.LightMap.Width, levelBoundsH - level.LightMap.Height),
            new Color(255, 0, 0, 100)
        );
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

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
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize - 30, level.Height * Level.TileSize - 30, new Color(127, 127, 127, 255));
        
        // draw the layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == 0 ? 255 : 50;
            var color = new Color(30, 30, 30, alpha);
            int offset = l * 2;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            levelRender.RenderGeometry(l, color);
            Rlgl.PopMatrix();
        }

        levelRender.RenderBorder();

        Raylib.BeginShaderMode(RainEd.Instance.LightBrushDatabase.Shader);

        // render cast
        Vector2 castOffset = new(
            MathF.Sin(level.LightAngle) * level.LightDistance * Level.TileSize,
            -MathF.Cos(level.LightAngle) * level.LightDistance * Level.TileSize
        );

        Raylib.DrawTextureRec(
            level.LightMap.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            lightMapOffset + castOffset,
            new Color(0, 0, 0, 80)
        );

        // Render mouse cursor
        if (window.IsViewportHovered)
        {
            var tex = RainEd.Instance.LightBrushDatabase.Brushes[selectedBrush].Texture;
            var mpos = window.MouseCellFloat;
            if (!wasCursorEnabled) mpos = savedMouseGp;

            var screenSize = brushSize / window.ViewZoom;

            // render brush preview
            // if drawing, draw on light texture instead of screen
            var lmb = Raylib.IsMouseButtonDown(MouseButton.Left);
            var rmb = Raylib.IsMouseButtonDown(MouseButton.Right);
            if (lmb || rmb)
            {
                isDrawing = true;

                DrawOcclusionPlane();

                Rlgl.LoadIdentity(); // why the hell do i have to call this
                level.LightMap.RaylibBeginTextureMode();

                // draw on brush plane
                Light.BrushAtom atom = new()
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

                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    atom.rect,
                    screenSize / 2f,
                    atom.rotation,
                    lmb ? Color.Black : Color.White
                );
                
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

            var doScale = EditorWindow.IsKeyDown(ImGuiKey.Q);
            var doRotate = EditorWindow.IsKeyDown(ImGuiKey.E);

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
            changeRecorder.EndStroke();
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