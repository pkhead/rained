using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;

namespace RainEd;

public class GeometryEditor
{
    public bool IsWindowOpen = true;

    private readonly Level level;

    public int Width;
    public int Height;
    private int workLayer = 0;

    public int WorkLayer { get => workLayer; set => workLayer = value; }

    public enum LayerViewMode : int
    {
        Overlay = 0,
        Stack = 1
    }
    public LayerViewMode layerViewMode = LayerViewMode.Overlay;

    private static readonly Color[] LAYER_COLORS = new Color[3] {
        new(0, 0, 0, 255),
        new(0, 255, 0, 127),
        new(255, 0, 0, 127)
    };

    private readonly string[] _viewModes = new string[2] {
        "Overlay", "Stack"
    };

    private const int TILE_SIZE = 20;

    private readonly UICanvasWidget canvasWidget;

    public GeometryEditor(Level level)
    {
        this.level = level;
        canvasWidget = new(1, 1);
        Width = 72;
        Height = 42;
    }

    public void Render()
    {
        // work layer
        {
            var workLayerV = workLayer + 1;
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4);
            ImGui.InputInt("Work Layer", ref workLayerV);
            workLayerV = Math.Clamp(workLayerV, 1, 3);    
            workLayer = workLayerV - 1;
        }

        // view mode
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 8);
            if (ImGui.BeginCombo("View Mode", _viewModes[(int)layerViewMode]))
            {
                for (int i = 0; i < _viewModes.Count(); i++)
                {
                    bool isSelected = i == (int)layerViewMode;
                    if (ImGui.Selectable(_viewModes[i], isSelected))
                    {
                        layerViewMode = (GeometryEditor.LayerViewMode) i;
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }
        }

        // canvas widget
        {
            var regionMax = ImGui.GetWindowContentRegionMax();
            var regionMin = ImGui.GetCursorPos();

            canvasWidget.Resize((int)(regionMax.X - regionMin.X), (int)(regionMax.Y - regionMin.Y));
            
            if (canvasWidget.RenderTexture is not null)
            {
                Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                DrawCanvas();
                Raylib.EndTextureMode();
            }

            canvasWidget.Draw();
        }
    }

    private void DrawCanvas()
    {
        Raylib.ClearBackground(new Color(0, 0, 0, 0)); // make canvas bg transparent
        Raylib.DrawRectangle(0, 0, level.Width * TILE_SIZE, level.Height * TILE_SIZE, Color.White); // draw white over level area

        switch (layerViewMode)
        {
            case LayerViewMode.Overlay:
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    var color = LAYER_COLORS[l];

                    for (int x = 0; x < level.Width; x++)
                    {
                        for (int y = 0; y < level.Height; y++)
                        {
                            LevelCell c = level.Layers[l,x,y];

                            if (c.Cell != CellType.Air)
                            {
                                Raylib.DrawRectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE, color);
                            }
                        }
                    }
                }

                break;
            
            case LayerViewMode.Stack:
                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = l == WorkLayer ? 255 : 50;
                    var color = new Color(LAYER_COLORS[l].R, LAYER_COLORS[l].G, LAYER_COLORS[l].B, alpha);
                    int offset = l * 2;

                    for (int x = 0; x < level.Width; x++)
                    {
                        for (int y = 0; y < level.Height; y++)
                        {
                            LevelCell c = level.Layers[l,x,y];

                            if (c.Cell != CellType.Air)
                            {
                                Raylib.DrawRectangle(x * TILE_SIZE + offset, y * TILE_SIZE + offset, TILE_SIZE, TILE_SIZE, color);
                            }
                        }
                    }
                }

                break;
        }

        var mouseCx = (int)(canvasWidget.MouseX / TILE_SIZE);
        var mouseCy = (int)(canvasWidget.MouseY / TILE_SIZE);

        if (canvasWidget.IsHovered && mouseCx >= 0 && mouseCy >= 0 && mouseCx < level.Width && mouseCy < level.Height)
        {
            Raylib.DrawRectangleLines(mouseCx * TILE_SIZE, mouseCy * TILE_SIZE, TILE_SIZE, TILE_SIZE, Color.White);

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
                level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Solid;

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
                level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Air;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }
    }
}