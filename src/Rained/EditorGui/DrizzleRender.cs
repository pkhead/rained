using System.Numerics;
using ImGuiNET;
using RenderState = RainEd.LevelDrizzleRender.RenderState;
namespace RainEd;

class DrizzleRenderWindow
{
    public readonly LevelDrizzleRender drizzleRenderer;
    private RainEd editor;
    private bool isOpen = false;
    public DrizzleRenderWindow(RainEd editor)
    {
        this.editor = editor;
        drizzleRenderer = new LevelDrizzleRender(editor);
    }

    public bool DrawWindow()
    {
        if (!isOpen)
        {
            ImGui.OpenPopup("Render");
            isOpen = false;
        }

        var doClose = false;

        drizzleRenderer.Update(editor);

        if (ImGui.BeginPopupModal("Render"))
        {
            var cancel = drizzleRenderer.State == RenderState.Cancelling || drizzleRenderer.IsDone;

            // cancel button (disabled if cancelling/canceled)
            if (cancel)
                ImGui.BeginDisabled();
            
            if (ImGui.Button("Cancel"))
                drizzleRenderer.Cancel();
            
            if (cancel)
                ImGui.EndDisabled();

            // close button (disabled if render process is not done)
            if (!drizzleRenderer.IsDone)
                ImGui.BeginDisabled();
            
            ImGui.SameLine();
            if (ImGui.Button("Close"))
            {
                doClose = true;
                ImGui.CloseCurrentPopup();
            }
            
            if (!drizzleRenderer.IsDone)
                ImGui.EndDisabled();
            
            ImGui.SameLine();
            ImGui.ProgressBar(drizzleRenderer.RenderProgress, new Vector2(-1.0f, 0.0f));

            if (drizzleRenderer.State == RenderState.Cancelling)
            {
                ImGui.Text("Cancelling...");
            }
            else if (drizzleRenderer.State == RenderState.Canceled)
            {
                ImGui.Text("Canceled");
            }
            else
            {
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
                    ImGui.Text($"Rendering {drizzleRenderer.CamerasDone+1} of {drizzleRenderer.CameraCount} cameras...");
                }

                ImGui.TextUnformatted(drizzleRenderer.DisplayString);
            }

            ImGui.EndPopup();
        }

        return doClose;
    }
}