using System.Numerics;
using ImGuiNET;
namespace RainEd;

static class ShortcutsWindow
{
    public static bool IsWindowOpen = false;
    
    private readonly static string[] NavTabs = new string[] { "General", "Environment Edit", "Geometry Edit", "Tile Edit", "Camera Edit", "Light Edit", "Effects Edit", "Prop Edit" };
    private static int selectedNavTab = 0;

    private readonly static (string, string)[][] TabData = new (string, string)[][]
    {
        // General
        [
            ("Scroll Wheel", "Zoom"),
            ("Middle Mouse", "Pan"),
            ("Alt+Left Mouse", "Pan"),
            ("Ctrl+Z", "Undo"),
            ("Ctrl+Shift+Z / Ctrl+Y", "Redo"),
            ("1", "Edit environment"),
            ("2", "Edit geometry"),
            ("3", "Edit tiles"),
            ("4", "Edit cameras"),
            ("5", "Edit light"),
            ("6", "Edit effects"),
            ("7", "Edit props"),
        ],

        // Environment
        [
            ("Left Mouse", "Set water level")
        ],

        // Geometry
        [
            ("WASD", "Browse tool selector"),
            ("Left Mouse", "Place/remove"),
            ("Right Mouse", "Remove"),
            ("Shift+Left Mouse", "Rect fill"),
            ("E", "Toggle layer 1"),
            ("R", "Toggle layer 2"),
            ("T", "Toggle layer 3"),
        ],

        // Tile
        [
            ("Tab", "Switch layer"),
            ("Shift+Tab", "Switch selector tab"),
            //("W/S", "Browse selected category"),
            //("A/D", "Browse tile categories"),
            ("Shift+Mouse Wheel", "Change material brush size"),
            ("E", "Sample tile from level"),
            ("Q", "Set selected to default material"),
            ("Left Mouse", "Place tile/material"),
            ("Right Mouse", "Remove tile/material"),
            ("F+Left Mouse", "Force tile placement"),
            ("G+Left Mouse", "Force tile geometry"),
            ("G+Right Mouse", "Remove tile and geometry"),
        ],

        // Camera
        [
            ("Double-click", "Create camera"),
            ("Left Mouse", "Select camera"),
            ("Right Mouse", "Reset camera corner"),
            ("Backspace/Delete", "Delete selected camera"),
            ("Ctrl+D", "Duplicate selected camera")
        ],

        // Light
        [
            ("WASD", "Browse brush catalog"),
            ("Shift+W/D", "Change light distance"),
            ("Shift+A/D", "Change light angle"),
            ("Q+Mouse Move", "Scale brush"),
            ("E+Mouse Move", "Rotate brush"),
            ("R", "Reset brush transform"),
            ("Left Mouse", "Paint shadow"),
            ("Right Mouse", "Paint light"),
        ],

        // Effects
        [
            ("Left Mouse", "Paint effect"),
            ("Shift+Left Mouse", "Paint effect stronger"),
            ("Right Mouse", "Erase effect"),
            ("Shift+Mouse Wheel", "Change brush size")
        ],

        // Props
        [
            ("Tab", "Switch layer"),
            ("Shift+Tab", "Switch selector tab"),
            //("W/S", "Browse selected category"),
            //("A/D", "Browse prop categories"),
            ("E", "Sample prop under mouse"),
            ("Q", "Toggle tile/prop tabs"),
            ("Double-click", "Create prop"),
            ("Left Mouse", "Select prop"),
            ("Shift+Left Mouse", "Add prop to selection"),
            ("Right Mouse", "Find props under the mouse"),
            ("Backspace/Delete", "Delete selected prop(s)"),
            ("F", "Toggle freeform warp mode"),
            ("Ctrl+D", "Duplicate selected prop"),
        ]
    };

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;

        if (ImGui.Begin("Shortcuts", ref IsWindowOpen))
        {
            ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
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
            ImGui.BeginChild("Controls", ImGui.GetContentRegionAvail());
            ShowTab();
            ImGui.EndChild();
        } ImGui.End();
    }

    private static void ShowTab()
    {
        var tableFlags = ImGuiTableFlags.RowBg;
        if (ImGui.BeginTable("ControlTable", 2, tableFlags))
        {
            ImGui.TableSetupColumn("Shortcut");
            ImGui.TableSetupColumn("Action");
            ImGui.TableHeadersRow();

            var tabData = TabData[selectedNavTab];

            for (int i = 0; i < tabData.Length; i++)
            {
                var tuple = tabData[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(tuple.Item1);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(tuple.Item2);
            }
            
            ImGui.EndTable();
        }
    }
}