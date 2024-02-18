using ImGuiNET;
using Raylib_cs;

namespace RainEd;

class EnvironmentEditor : IEditorMode
{
    public string Name { get => "Environment"; }
    private readonly EditorWindow window;

    public EnvironmentEditor(EditorWindow window)
    {
        this.window = window;
    }

    public void DrawToolbar()
    {
        var level = window.Editor.Level;

        if (ImGui.Begin("Environment", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text("Graphics Random Seed");
            ImGui.SetNextItemWidth(-0.001f);
            ImGui.SliderInt("##seed", ref level.TileSeed, 0, 400, "%i", ImGuiSliderFlags.AlwaysClamp);
            ImGui.Checkbox("Enclosed Room", ref level.DefaultMedium);
            ImGui.Checkbox("Sunlight", ref level.HasSunlight);
            ImGui.Checkbox("Water", ref level.HasWater);
            ImGui.Checkbox("Is Water In Front", ref level.IsWaterInFront);

            ImGui.End();
        }
    }

    private void DrawWater()
    {
        var level = window.Editor.Level;
        float waterHeight = level.WaterLevel + level.BufferTilesBot + 0.5f;
        Raylib.DrawRectangle(
            0,
            (int)((level.Height - waterHeight) * Level.TileSize),
            level.Width * Level.TileSize,
            (int)(waterHeight * Level.TileSize),
            new Color(0, 0, 255, 100)
        );
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // set water level
        if (window.IsViewportHovered && level.HasWater)
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                var my = Math.Clamp(window.MouseCy, -1, level.Height - level.BufferTilesBot);
                level.WaterLevel = (int)(level.Height - level.BufferTilesBot - 0.5f - my);
            }
        }

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));
        
        // draw the layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == 0 ? 255 : 50;
            var color = new Color(0, 0, 0, alpha);
            int offset = l * 2;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            levelRender.RenderGeometry(l, color);
            Rlgl.PopMatrix();

            // draw water behind first layer if set
            if (l == 1 && level.HasWater && !level.IsWaterInFront)
                DrawWater();
        }

        // draw water
        if (level.HasWater && level.IsWaterInFront)
            DrawWater();

        levelRender.RenderGrid();
        levelRender.RenderBorder();
    }
}