using System.Globalization;
using ImGuiNET;

namespace RainEd;

static class EmergencySaveWindow
{
    public const string WindowName = "Emergency save file detected!";
    public static bool IsWindowOpen = false;

    private static string[] savList = [];
    private static string[] savDisplays = [];
    private static string[] dateList = [];

    private static int radio = -1;

    public static void UpdateList(string[] emSavList)
    {
        radio = -1;
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

        if (emSavList.Length == 1) radio = 0;
    }

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
                    ImGui.RadioButton(savDisplays[i] + "##" + savList[i], ref radio, i);
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(dateList[i]);
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            if (ImGui.Button("Open", StandardPopupButtons.ButtonSize) && radio >= 0)
            {
                RainEd.Instance.LoadLevel(savList[radio]);
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
            if (ImGui.Button("Discard", StandardPopupButtons.ButtonSize))
            {
                RainEd.DiscardEmergencySaves();
                ImGui.CloseCurrentPopup();
                IsWindowOpen = false;
            }

            ImGui.EndPopup();
        }
    }
}