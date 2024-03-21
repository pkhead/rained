using ImGuiNET;
using Raylib_cs;

namespace RainEd;

class EnvironmentEditor : IEditorMode
{
    public string Name { get => "Environment"; }
    private readonly EditorWindow window;

    private ChangeHistory.EnvironmentChangeRecorder changeRecorder;
    private bool isDragging = false;

    public EnvironmentEditor(EditorWindow window)
    {
        this.window = window;
        changeRecorder = new();
        changeRecorder.TakeSnapshot();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder = new();
            changeRecorder.TakeSnapshot();
        };

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            changeRecorder.TakeSnapshot();
        };
    }

    public void Unload()
    {
        changeRecorder.PushChange();
    }

    private void RecordItemChanges()
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            changeRecorder.PushChange();
        }
    }

    public void DrawToolbar()
    {
        var level = window.Editor.Level;

        if (ImGui.Begin("Environment", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text("Tile Random Seed");
            ImGui.SetNextItemWidth(-0.001f);

            ImGui.SliderInt("##seed", ref level.TileSeed, 0, 400, "%i", ImGuiSliderFlags.AlwaysClamp);
            RecordItemChanges();

            ImGui.Checkbox("Enclosed Room", ref level.DefaultMedium);
            RecordItemChanges();

            ImGui.Checkbox("Sunlight", ref level.HasSunlight);
            RecordItemChanges();

            ImGui.Checkbox("Water", ref level.HasWater);
            RecordItemChanges();

            ImGui.Checkbox("Is Water In Front", ref level.IsWaterInFront);
            RecordItemChanges();

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

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        bool wasDragging = isDragging;
        isDragging = false;

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // set water level
        if (window.IsViewportHovered && level.HasWater)
        {
            if (window.IsMouseDown(ImGuiMouseButton.Left))
            {
                isDragging = true;

                var my = Math.Clamp(window.MouseCy, -1, level.Height - level.BufferTilesBot);
                level.WaterLevel = (int)(level.Height - level.BufferTilesBot - 0.5f - my);
            }
        }

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, EditorWindow.BackgroundColor);
        
        // draw the layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == 0 ? 255 : 50;
            var color = EditorWindow.GeoColor(alpha);
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

        if (wasDragging && !isDragging)
            changeRecorder.PushChange();
    }
}