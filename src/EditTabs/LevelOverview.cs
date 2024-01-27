using ImGuiNET;

namespace RainEd;

public class LevelOverview
{
    public bool IsWindowOpen = true;

    public LevelOverview(RainEd editor) {
    }

    public void Render() {
        if (IsWindowOpen && ImGui.Begin("Overview", ref IsWindowOpen))
        {
            // TODO
        }

        ImGui.End();
    }
}