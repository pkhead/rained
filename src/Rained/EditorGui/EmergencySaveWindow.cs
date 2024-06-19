using ImGuiNET;

namespace RainEd;

static class EmergencySaveWindow
{
    public const string WindowName = "Emergency save file detected!";
    public static bool IsWindowOpen = false;

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGuiExt.BeginPopupModal(WindowName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text("rainED had made an emergency save of your level at the time of the crash. Would you like to load it?");

            ImGui.Separator();
            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
            {
                if (btn == 0)
                {
                    RainEd.Instance.LoadLevel(RainEd.EmergencySaveFilePath);
                }

                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.EndPopup();
        }
    }
}