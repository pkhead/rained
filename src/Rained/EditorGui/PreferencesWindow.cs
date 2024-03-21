using System.Numerics;
using ImGuiNET;

namespace RainEd;
static class PreferencesWindow
{
    private const string WindowName = "Preferences";
    public static bool IsWindowOpen = false;

    enum NavTabEnum : int
    {
        General = 0,
        Shortcuts = 1,
        Theme = 2,
        Assets = 3,
    }

    private readonly static string[] NavTabs = ["General", "Shortcuts", "Theme", "Assets"];
    private static NavTabEnum selectedNavTab = NavTabEnum.General;

    private static KeyShortcut activeShortcut = KeyShortcut.None;

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.FirstUseEver);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen))
        {
            // show navigation sidebar
            ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
            {
                for (int i = 0; i < NavTabs.Length; i++)
                {
                    if (ImGui.Selectable(NavTabs[i], i == (int)selectedNavTab))
                    {
                        selectedNavTab = (NavTabEnum)i;
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.BeginChild("Controls", ImGui.GetContentRegionAvail());
            
            switch (selectedNavTab)
            {
                case NavTabEnum.General:
                    ShowGeneralTab();
                    break;

                case NavTabEnum.Shortcuts:
                    ShowShortcutsTab();
                    break;

                case NavTabEnum.Theme:
                    ShowThemeTab();
                    break;
                
                case NavTabEnum.Assets:
                    ShowAssetsTab();
                    break;
            }

            ImGui.EndChild();
        }
        else
        {
            activeShortcut = KeyShortcut.None;
        }

        // handle shortcut binding
        if (activeShortcut != KeyShortcut.None)
        {
            // abort binding if not on the shortcut tabs, or on mouse input or if escape is pressed
            if (selectedNavTab != NavTabEnum.Shortcuts ||
                EditorWindow.IsKeyPressed(ImGuiKey.Escape) ||
                ImGui.IsMouseClicked(ImGuiMouseButton.Left) ||
                ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                activeShortcut = KeyShortcut.None;
            }
            else
            {
                // get mod flags
                ImGuiModFlags modFlags = ImGuiModFlags.None;

                if (ImGui.IsKeyDown(ImGuiKey.ModCtrl)) modFlags |= ImGuiModFlags.Ctrl;
                if (ImGui.IsKeyDown(ImGuiKey.ModAlt)) modFlags |= ImGuiModFlags.Alt;
                if (ImGui.IsKeyDown(ImGuiKey.ModShift)) modFlags |= ImGuiModFlags.Shift;
                if (ImGui.IsKeyDown(ImGuiKey.ModSuper)) modFlags |= ImGuiModFlags.Super;

                // find the key that is currently pressed
                for (int ki = (int)ImGuiKey.NamedKey_BEGIN; ki < (int)ImGuiKey.NamedKey_END; ki++)
                {
                    ImGuiKey key = (ImGuiKey) ki;
                    
                    // don't process if this is a modifier key
                    if (KeyShortcuts.IsModifierKey(key))
                        continue;
                    
                    if (ImGui.IsKeyPressed(key))
                    {
                        // rebind the shortcut to this key
                        KeyShortcuts.Rebind(activeShortcut, key, modFlags);
                        activeShortcut = KeyShortcut.None;
                        break;
                    }
                }
            }
        }
    }

    private static void ShowGeneralTab()
    {
        ImGui.Text("Lorem ipsum dolor sit amet");
    }

    private static void ShowShortcutsTab()
    {
        ImGui.SeparatorText("General");
        ShortcutButton(KeyShortcut.Undo);
        ShortcutButton(KeyShortcut.Redo);
        ShortcutButton(KeyShortcut.Cut);
        ShortcutButton(KeyShortcut.Copy);
        ShortcutButton(KeyShortcut.Paste);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.New);
        ShortcutButton(KeyShortcut.Open);
        ShortcutButton(KeyShortcut.Save);
        ShortcutButton(KeyShortcut.SaveAs);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.Render);

        ImGui.SeparatorText("General Editing");
        ShortcutButton(KeyShortcut.NavUp);
        ShortcutButton(KeyShortcut.NavDown);
        ShortcutButton(KeyShortcut.NavLeft);
        ShortcutButton(KeyShortcut.NavRight);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.NewObject);
        ShortcutButton(KeyShortcut.RemoveObject);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.SwitchLayer);
        ShortcutButton(KeyShortcut.SwitchTab);

        ImGui.SeparatorText("Geometry Edit");
        ShortcutButton(KeyShortcut.ToggleLayer1);
        ShortcutButton(KeyShortcut.ToggleLayer2);
        ShortcutButton(KeyShortcut.ToggleLayer3);

        ImGui.SeparatorText("Tile Edit");
        ShortcutButton(KeyShortcut.Eyedropper);
        ShortcutButton(KeyShortcut.SetMaterial);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.TileForceGeometry);
        ShortcutButton(KeyShortcut.TileForcePlacement);
        ShortcutButton(KeyShortcut.TileIgnoreDifferent);

        ImGui.SeparatorText("Light Edit");
        ShortcutButton(KeyShortcut.ResetBrushTransform);
        ShortcutButton(KeyShortcut.ZoomLightIn);
        ShortcutButton(KeyShortcut.ZoomLightOut);
        ShortcutButton(KeyShortcut.RotateLightCW);
        ShortcutButton(KeyShortcut.RotateLightCCW);
    }

    private static void ShowThemeTab()
    {
        ImGui.ShowStyleSelector("Theme");

        if (ImGui.TreeNode("Style Editor"))
        {
            ImGui.ShowStyleEditor();
            ImGui.TreePop();
        }
    }

    private static void ShowAssetsTab()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        ImGui.Text("Categories");
        ImGui.BeginListBox("##Categories");
        {
            foreach (var category in tileDb.Categories)
            {
                ImGui.Selectable(category.Name);
            }
        }
        ImGui.EndListBox();
    }

    private static void ShortcutButton(KeyShortcut id, string? nameOverride = null)
    {
        ImGui.PushID((int) id);

        var btnSize = new Vector2(ImGui.GetTextLineHeight() * 8f, 0f);
        if (ImGui.Button(activeShortcut == id ? "..." : KeyShortcuts.GetShortcutString(id), btnSize))
        {
            activeShortcut = id;
        }

        ImGui.SetItemTooltip(KeyShortcuts.GetShortcutString(id));
        
        // reset button
        ImGui.SameLine();
        if (ImGui.Button("X"))
        {
            KeyShortcuts.Reset(id);
        }
        ImGui.SetItemTooltip("Reset");

        ImGui.SameLine();
        ImGui.Text(nameOverride ?? KeyShortcuts.GetName(id));

        ImGui.PopID();
    }
}