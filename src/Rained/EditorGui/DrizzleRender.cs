using System.Numerics;
using ImGuiNET;
using RenderState = RainEd.LevelDrizzleRender.RenderState;
namespace RainEd;

class DrizzleRenderWindow
{
    public readonly LevelDrizzleRender drizzleRenderer;
    private RainEd editor;
    public DrizzleRenderWindow(RainEd editor)
    {
        this.editor = editor;
        drizzleRenderer = new LevelDrizzleRender(editor);
    }

    public void DrawWindow()
    {
        drizzleRenderer.Update(editor);

        if (ImGui.Begin("Drizzle Render"))
        {
            ImGui.Button("Cancel");
            ImGui.SameLine();
            ImGui.ProgressBar(drizzleRenderer.RenderProgress, new Vector2(-1.0f, 0.0f));
            if (drizzleRenderer.State == RenderState.Finished)
            {
                ImGui.Text("Done!");
            }
            else if (drizzleRenderer.State == RenderState.Initializing)
            {
                ImGui.Text("Initializing Zygote runtime...");
            }
            else
            {
                ImGui.Text("Rendering...");
            }

        } ImGui.End();
    }
}