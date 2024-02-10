using ImGuiNET;

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
            if (drizzleRenderer.IsDone)
            {
                ImGui.Text("done");
            }
            else
            {
                ImGui.Text("not done");
            }
        }
    }
}