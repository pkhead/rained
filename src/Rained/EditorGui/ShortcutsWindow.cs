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
        new (string, string)[]
        {
            ("Scroll Wheel", "Zoom"),
            ("Middle Mouse Button", "Pan"),
            ("Arrow Keys", "Pan"),
            ("Ctrl+Z", "Undo"),
            ("Ctrl+Shift+Z / Ctrl+Y", "Redo"),
            ("1", "Edit environment"),
            ("2", "Edit geometry"),
            ("3", "Edit tiles"),
            ("4", "Edit cameras"),
            ("5", "Edit light"),
            ("6", "Edit effects"),
            ("7", "Edit props"),
        },

        // Environment
        new (string, string)[]
        {
            ("Mouse Left", "Set water level")
        },

        // Geometry
        new (string, string)[]
        {
            ("WASD", "Browse tool selector"),
            ("Mouse Left", "Place/remove"),
            ("Mouse Right", "Remove"),
            ("Shift+Mouse Left", "Rect fill"),
            ("E", "Toggle layer 1"),
            ("R", "Toggle layer 2"),
            ("T", "Toggle layer 3"),
        },

        // Tile
        new (string, string)[]
        {
            ("Tab", "Switch layer"),
            ("W/S", "Browse selected category"),
            ("A/D", "Browse tile categories"),
            ("Shift+Mouse Wheel", "Change material brush size"),
            ("E", "Sample tile from level"),
            ("Q", "Set selected to default material"),
            ("Mouse Left", "Place tile/material"),
            ("Mouse Right", "Remove tile/material"),
            ("F+Mouse Left", "Force tile placement"),
            ("G+Mouse Left", "Force tile geometry"),
            ("G+Mouse Right", "Remove tile and geometry"),
        },

        // Camera
        new (string, string)[]
        {
            ("N", "Create camera"),
            ("Mouse Left", "Select camera"),
            ("Mouse Right", "Reset camera corner"),
            ("Backspace/Delete", "Delete selected camera")
        },

        // Light
        new (string, string)[]
        {
            ("WASD", "Browse brush catalog"),
            ("Shift+W/D", "Change light distance"),
            ("Shift+A/D", "Change light angle"),
            ("Q+Mouse Move", "Scale brush"),
            ("E+Mouse Move", "Rotate brush"),
            ("R", "Reset brush transform"),
            ("Mouse Left", "Paint shadow"),
            ("Mouse Right", "Paint light"),
        },

        // Effects
        new (string, string)[]
        {
            ("Mouse Left", "Paint effect"),
            ("Shift+Mouse Left", "Paint effect stronger"),
            ("Mouse Right", "Erase effect"),
            ("Shift+Mouse Wheel", "Change brush size")
        },

        // Props
        new (string, string)[]
        {
            ("W/S", "Browse selected category"),
            ("A/D", "Browse prop categories"),
            ("E", "Sample prop under mouse"),
            ("Q", "Toggle tile/prop tabs"),
            ("Double-click", "Create prop"),
            ("Mouse Left", "Select prop"),
            ("Shift+Mouse Left", "Add prop to selection"),
            ("Mouse Right", "Find props under the mouse"),
            ("Backspace/Delete", "Delete selected prop(s)"),
            ("F", "Toggle freeform warp mode"),
            ("Ctrl+D", "Duplicate selected prop"),
        }
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