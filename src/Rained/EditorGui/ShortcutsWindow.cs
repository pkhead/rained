using System.Numerics;
using ImGuiNET;
namespace RainEd;

static class ShortcutsWindow
{
    public static bool IsWindowOpen = false;
    
    private static string[] NavTabs = new string[] { "General", "Geometry Edit", "Tile Edit", "Camera Edit", "Light Edit", "Effects Edit", "Prop Edit" };
    private static int selectedNavTab = 0;

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;

        if (ImGui.Begin("Shortcuts", ref IsWindowOpen))
        {
            ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 8.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
            {
                for (int i = 0; i < NavTabs.Length; i++)
                {
                    if (ImGui.Selectable(NavTabs[i], i == selectedNavTab))
                    {
                        selectedNavTab = i;
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ShowTab();
            ImGui.EndGroup();
        } ImGui.End();
    }

    private static void ShowTab()
    {

    }
}