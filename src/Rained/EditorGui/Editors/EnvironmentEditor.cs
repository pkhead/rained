using ImGuiNET;
using Raylib_cs;
using Rained.LevelData;
namespace Rained.EditorGui.Editors;

class EnvironmentEditor : IEditorMode
{
    public string Name { get => "Environment"; }
    public bool SupportsCellSelection => false;
    
    private readonly LevelWindow window;

    private ChangeHistory.EnvironmentChangeRecorder changeRecorder;
    private bool isDragging = false;

    public EnvironmentEditor(LevelWindow window)
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
        var level = RainEd.Instance.Level;

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
        } ImGui.End();
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        bool wasDragging = isDragging;
        isDragging = false;

        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;

        // set water level
        if (window.IsViewportHovered && level.HasWater)
        {
            if (EditorWindow.IsMouseDown(ImGuiMouseButton.Left))
            {
                isDragging = true;

                var my = Math.Clamp(window.MouseCy, -10, level.Height - level.BufferTilesBot);
                level.WaterLevel = (int)(level.Height - level.BufferTilesBot - 0.5f - my);
            }
        }

        // render level
        levelRender.RenderLevel(new Rendering.LevelRenderConfig()
        {
            FillWater = true
        });
        if (RainEd.Instance.Preferences.ViewNodeIndices)
        {
            levelRender.RenderShortcuts(Color.White);
            levelRender.RenderNodes(Color.White);
        }
        levelRender.RenderGrid();
        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();

        if (wasDragging && !isDragging)
            changeRecorder.PushChange();
    }
}