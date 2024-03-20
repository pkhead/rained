using System.Numerics;
using ImGuiNET;
using RainEd;

static class PreferencesWindow
{
    private const string WindowName = "Preferences";
    public static bool IsWindowOpen = false;

    enum NavTabEnum : int
    {
        General = 0,
        Shortcuts = 1
    }

    private readonly static string[] NavTabs = ["General", "Shortcuts"];
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
            
            if (selectedNavTab == NavTabEnum.General)
            {
                ShowGeneralTab();
            }
            else if (selectedNavTab == NavTabEnum.Shortcuts)
            {
                ShowShortcutsTab();
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
                    if (key == ImGuiKey.LeftShift || key == ImGuiKey.RightShift
                        || key == ImGuiKey.LeftCtrl || key == ImGuiKey.RightCtrl
                        || key == ImGuiKey.LeftAlt || key == ImGuiKey.RightAlt
                        || key == ImGuiKey.LeftSuper || key == ImGuiKey.RightSuper
                        || key == ImGuiKey.ReservedForModAlt
                        || key == ImGuiKey.ReservedForModCtrl
                        || key == ImGuiKey.ReservedForModShift
                        || key == ImGuiKey.ReservedForModSuper
                    )
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
        ShortcutButton(KeyShortcut.NavUp);
    }

    private static void ShortcutButton(KeyShortcut id)
    {
        ImGui.PushID((int) id);

        var btnSize = new Vector2(ImGui.GetTextLineHeight() * 8f, 0f);
        if (ImGui.Button(activeShortcut == id ? "..." : KeyShortcuts.GetShortcutString(id), btnSize))
        {
            activeShortcut = id;
        }

        ImGui.SetItemTooltip(KeyShortcuts.GetShortcutString(id));

        ImGui.SameLine();
        ImGui.Text(KeyShortcuts.GetName(id));

        ImGui.PopID();
    }
}