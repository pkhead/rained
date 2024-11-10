using System.Globalization;
using ImGuiNET;
namespace Rained.EditorGui;

static class EmergencySaveWindow
{
    public const string WindowName = "Emergency save file detected!";
    public static bool IsWindowOpen = false;

    private static string[] savList = [];
    private static string[] savDisplays = [];
    private static string[] dateList = [];

    public static void UpdateList(string[] emSavList)
    {
        savList = new string[emSavList.Length];
        dateList = new string[emSavList.Length];
        savDisplays = new string[emSavList.Length];

        var culture = Boot.UserCulture;

        for (int i = 0; i < emSavList.Length; i++)
        {
            var writeTime = File.GetLastWriteTime(emSavList[i]);
            var levelName = Path.GetFileNameWithoutExtension(emSavList[i]);

            savList[i] = emSavList[i];
            savDisplays[i] = levelName[0..levelName.LastIndexOf('-')];
            dateList[i] = writeTime.ToString(culture.DateTimeFormat.ShortDatePattern, culture) + " at " + writeTime.ToString(culture.DateTimeFormat.ShortTimePattern, culture);
        }
    }

    public static void ShowWindow()
    {
        // prevent both windows from showing up simultaneously
        bool windowOpen = IsWindowOpen && !InitErrorsWindow.IsWindowOpen;

        if (!ImGui.IsPopupOpen(WindowName) && windowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGuiExt.BeginPopupModal(WindowName, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 32.0f);
            ImGui.TextWrapped("Rained has detected one or more emergency saves. You may choose what to do with them.");
            ImGui.TextWrapped("If you choose to open a file, it is recommended that you review the level and, if desired, use Save As to replace the original level file with the emergency save.");
            ImGui.PopTextWrapPos();
            
            var tableFlags = ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("Emergency Save List", 2, tableFlags))
            {
                ImGui.TableSetupColumn("File Name");
                ImGui.TableSetupColumn("Date");
                ImGui.TableHeadersRow();

                for (int i = 0; i < savList.Length; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(savDisplays[i]);
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(dateList[i]);
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            if (ImGui.Button("Open All", StandardPopupButtons.ButtonSize))
            {
                foreach (var save in savList)
                {
                    RainEd.Instance.LoadLevel(save);
                }

                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Ignore", StandardPopupButtons.ButtonSize))
            {
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.SameLine();
            if (ImGui.Button("Discard All", StandardPopupButtons.ButtonSize))
            {
                RainEd.DiscardEmergencySaves();
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.EndPopup();
        }
    }
}