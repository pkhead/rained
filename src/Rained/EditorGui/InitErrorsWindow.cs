using ImGuiNET;
namespace Rained.EditorGui;

static class InitErrorsWindow
{
    public const string WindowName = "Init.txt Error";
    public static bool IsWindowOpen = false;

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGui.TextWrapped("One or more errors were encountered when loading the asset Init.txt files.");

            ImGui.Separator();

            if (ImGui.Button("View Logs", StandardPopupButtons.ButtonSize))
            {
                LogsWindow.IsWindowOpen = true;
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Close", StandardPopupButtons.ButtonSize))
            {
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.PopTextWrapPos();
            ImGui.EndPopup();   
        }
    }
}