using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
namespace Rained.EditorGui;

static partial class ShortcutsWindow
{
    public static bool IsWindowOpen = false;
    
    private readonly static string[] NavTabs = new string[] { "General", "Environment Edit", "Geometry Edit", "Tile Edit", "Camera Edit", "Light Edit", "Effects Edit", "Prop Edit" };

    private readonly static (string, string)[][] TabData = new (string, string)[][]
    {
        // General
        [
            ("Scroll Wheel", "Zoom"),
            ("[ViewZoomIn]/[ViewZoomOut]", "Zoom In/Out"),
            ("Middle Mouse", "Pan"),
            ("<Alt>+Left Mouse", "Pan"),
            ("[Undo]", "Undo"),
            ("[Redo]", "Redo"),
            ("[Render]", "Render"),
            ("[ExportGeometry]", "Export geometry"),
            ("1", "Edit environment"),
            ("2", "Edit geometry"),
            ("3", "Edit tiles"),
            ("4", "Edit cameras"),
            ("5", "Edit light"),
            ("6", "Edit effects"),
            ("7", "Edit props"),
            ("[SelectEditor]", "Editor mode radial menu"),
        ],

        // Environment
        [
            ("Left Mouse", "Set water level")
        ],

        // Geometry
        [
            ("[NavUp][NavLeft][NavDown][NavRight]", "Browse tool selector"),
            ("Left Mouse", "Place/remove"),
            ("Right Mouse", "Remove object"),
            ("Shift+Left Mouse", "Rect fill"),
            ("[FloodFill]+Left Mouse", "Flood fill"),
            ("[SwitchLayer]", "Cycle layers"),
            ("[ToggleLayer1]", "Toggle layer 1"),
            ("[ToggleLayer2]", "Toggle layer 2"),
            ("[ToggleLayer3]", "Toggle layer 3"),
            ("[ToggleMirrorX]", "Toggle Mirror X"),
            ("[ToggleMirrorY]", "Toggle Mirror Y"),
            ("[Select]", "Begin selection"),
            ("[Copy]", "Copy"),
            ("[Paste]", "Paste"),
        ],

        // Tile
        [
            ("[SwitchLayer]", "Switch layer"),
            ("[SwitchTab]", "Switch selector tab"),
            ("[NavUp]/[NavDown]", "Browse selected category"),
            ("[NavLeft]/[NavRight]", "Browse tile categories"),
            ("Shift+Mouse Wheel", "Change material brush size"),
            ("[DecreaseBrushSize]/[IncreaseBrushSize]", "Change material brush size"),
            ("[Eyedropper]", "Sample tile from level"),
            ("[SetMaterial]", "Set selected to default material"),
            ("Left Mouse", "Place tile/material"),
            ("Right Mouse", "Remove tile/material"),
            ("Shift+Left Mouse", "Rect fill tile/material"),
            ("Shift+Right Mouse", "Rect remove tile/material"),
            ("[TileIgnoreDifferent]+Left Mouse", "Ignore differing materials"),
            ("[TileIgnoreDifferent]+Left Mouse", "Ignore materials or tiles"),
            ("[TileForcePlacement]+Left Mouse", "Force tile placement"),
            ("[TileForceGeometry]+Left Mouse", "Force tile geometry"),
            ("[TileForceGeometry]+Right Mouse", "Remove tile and geometry"),
            ("[Select]", "Begin selection"),
            ("[Copy]", "Copy"),
            ("[Paste]", "Paste"),
        ],

        // Camera
        [
            ("Double-click", "Create camera"),
            ("[NewObject]", "Create camera"),
            ("Left Mouse", "Select camera"),
            ("Right Mouse", "Reset camera corner"),
            ("[RemoveObject]", "Delete selected camera"),
            ("[Duplicate]", "Duplicate selected camera"),
            ("[CameraSnapX]/[NavUp]/[NavDown]", "Snap X to other cameras"),
            ("[CameraSnapY]/[NavLeft]/[NavRight]", "Snap Y to other cameras"),
            ("Hold Shift", "Lock/unlock camera angle"),
            ("Ctrl+A", "Select all cameras"),
        ],

        // Light
        [
            ("[NavUp][NavLeft][NavDown][NavRight]", "Browse brush catalog"),
            ("[ZoomLightIn]", "Move light inward"),
            ("[ZoomLightOut]", "Move light outward"),
            ("[RotateLightCW]", "Rotate light clockwise"),
            ("[RotateLightCCW]", "Rotate light counter-clockwise"),
            ("[ScaleLightBrush]+Mouse Move", "Scale brush"),
            ("[RotateLightBrush]+Mouse Move", "Rotate brush"),
            ("[ResetBrushTransform]", "Reset brush transform"),
            ("Left Mouse", "Paint shadow"),
            ("Right Mouse", "Paint light"),
        ],

        // Effects
        [
            ("Left Mouse", "Paint effect"),
            ("Shift+Left Mouse", "Paint effect stronger"),
            ("Right Mouse", "Erase effect"),
            ("Shift+Mouse Wheel", "Change brush size"),
            ("[DecreaseBrushSize]/[IncreaseBrushSize]", "Change brush size"),
            ("<Ctrl>+Mouse Wheel", "Change brush strength"),
            ("Shift+Add effect", "Add effect after selected")
        ],

        // Props
        [
            ("[SwitchLayer]", "Switch layer"),
            ("[SwitchTab]", "Switch selector tab"),
            ("[NavUp]/[NavDown]", "Browse selected category"),
            ("[NavLeft]/[NavRight]", "Browse prop categories"),
            ("[Eyedropper]", "Sample prop under mouse"),
            ("Right-click", "Create prop"),
            ("[NewObject]", "Create prop"),
            ("Left Mouse", "Select prop"),
            ("Shift+Left Mouse", "Add prop to selection"),
            ("Double-click", "Find prop(s) under the mouse"),
            ("[RemoveObject]", "Delete selected prop(s)"),
            ("[ToggleVertexMode]", "Toggle vertex mode"),
            ("[Duplicate]", "Duplicate selected prop(s)"),
            ("[ChangePropSnapping]", "Change prop snapping"),
            ("[RopeSimulation]", "Simulate selected rope prop(s)"),
            ("[RopeSimulationFast]", "Simulate ropes faster")
        ]
    };

    public static void ShowWindow()
    {
        if (!IsWindowOpen) return;

        var editMode = RainEd.Instance.LevelView.EditMode;
        if (ImGui.Begin("Shortcuts", ref IsWindowOpen))
        {
            if (ImGui.BeginTabBar("ShortcutTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    ShowTab(0);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Current Edit Mode"))
                {
                    ShowTab(editMode + 1);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            /*var halfWidth = ImGui.GetTextLineHeight() * 30.0f;
            ImGui.BeginChild("General", new Vector2(halfWidth, ImGui.GetContentRegionAvail().Y));
            ShowTab(0);
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.BeginChild("Edit Mode", ImGui.GetContentRegionAvail());
            ShowTab(editMode + 1);
            ImGui.EndChild();*/
            /*ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
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
            ImGui.EndChild();*/
        } ImGui.End();
    }

    private static void ShowTab(int navTab)
    {
        var strBuilder = new StringBuilder();

        var tableFlags = ImGuiTableFlags.RowBg;
        if (ImGui.BeginTable("ControlTable", 2, tableFlags))
        {
            ImGui.TableSetupColumn("Shortcut", ImGuiTableColumnFlags.WidthFixed, ImGui.GetTextLineHeight() * 10.0f);
            ImGui.TableSetupColumn("Action");
            ImGui.TableHeadersRow();

            var tabData = TabData[navTab];

            for (int i = 0; i < tabData.Length; i++)
            {
                var tuple = tabData[i];

                string str;
                str = ShortcutRegex().Replace(tuple.Item1, ShortcutEvaluator);
                str = ModifierRegex().Replace(str, ModifierEvaluator);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(str);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(tuple.Item2);
            }
            
            ImGui.EndTable();
        }
    }

    private static string ShortcutEvaluator(Match match)
    {
        var shortcutId = Enum.Parse<KeyShortcut>(match.Value[1..^1]);
        return KeyShortcuts.GetShortcutString(shortcutId);
    }

    private static string ModifierEvaluator(Match match)
    {
        var modifierName = match.Value[1..^1];

        switch (modifierName)
        {
            case "Ctrl":
                return KeyShortcuts.CtrlName;
            
            case "Alt":
                return KeyShortcuts.AltName;

            case "Super":
                return KeyShortcuts.SuperName;

            default:
                Log.Error("ShortcutsWindow: unknown modifier name " + modifierName);
                return "<error>";
        }
    }

    [GeneratedRegex("\\[(\\w+?)\\]")]
    private static partial Regex ShortcutRegex();

    [GeneratedRegex("\\<(\\w+?)\\>")]
    private static partial Regex ModifierRegex();
}
