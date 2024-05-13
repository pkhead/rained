using System.Numerics;
using ImGuiNET;
using Raylib_cs;

// i probably should create an IGUIWindow interface for the various miscellaneous windows...
namespace RainEd;
static class PreferencesWindow
{
    private const string WindowName = "Preferences";
    private static bool isWindowOpen = false;
    public static bool IsWindowOpen { get => isWindowOpen; }

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

    private static bool openPopupCmd = false;
    public static void OpenWindow()
    {
        openPopupCmd = true;
    }

    public static void ShowWindow()
    {
        if (openPopupCmd)
        {
            openPopupCmd = false;
            isWindowOpen = true;
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.FirstUseEver);
        }

        // keep track of this, as i want to clear some data
        // when the following tabs are no longer shown
        bool showAssetsTab = false;

        if (ImGui.BeginPopupModal(WindowName, ref isWindowOpen))
        {
            var lastNavTab = selectedNavTab;

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
                    ShowThemeTab(lastNavTab != selectedNavTab);
                    break;
                
                case NavTabEnum.Assets:
                    AssetManagerGUI.Show();
                    showAssetsTab = true;
                    break;
            }

            ImGui.EndChild();
            ImGui.EndPopup();
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
                if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                {
                    KeyShortcuts.Rebind(activeShortcut, ImGuiKey.Tab, modFlags);
                    activeShortcut = KeyShortcut.None;
                }
                else
                {
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

        if (!showAssetsTab)
        {
            AssetManagerGUI.Unload();
        }
    }

    private static void ShowGeneralTab()
    {
        static Vector3 HexColorToVec3(HexColor color) => new(color.R / 255f, color.G / 255f, color.B / 255f);
        static HexColor Vec3ToHexColor(Vector3 vec) => new(
            (byte)(Math.Clamp(vec.X, 0f, 1f) * 255f),
            (byte)(Math.Clamp(vec.Y, 0f, 1f) * 255f),
            (byte)(Math.Clamp(vec.Z, 0f, 1f) * 255f)
        );

        var prefs = RainEd.Instance.Preferences;
        bool boolRef;

        ImGui.SeparatorText("Rendering");
        
        // static lingo runtime
        {
            boolRef = prefs.StaticDrizzleLingoRuntime;
            if (ImGui.Checkbox("Initialize the Zygote runtime on app startup", ref boolRef))
                prefs.StaticDrizzleLingoRuntime = boolRef;
            
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            ImGui.SetItemTooltip(
                """
                This will run the Zygote runtime initialization
                process once, when the app starts. This results
                in a longer startup time and more idle RAM
                usage, but will decrease the time it takes to
                start a render.

                This option requires a restart in order to
                take effect.    
                """);
        }

        // show render preview
        {
            boolRef = prefs.ShowRenderPreview;
            if (ImGui.Checkbox("Show render preview", ref boolRef))
                prefs.ShowRenderPreview = boolRef;
        }
        
        ImGui.SeparatorText("Level Colors");
        {
            Vector3 layerColor1 = HexColorToVec3(prefs.LayerColor1);
            Vector3 layerColor2 = HexColorToVec3(prefs.LayerColor2);
            Vector3 layerColor3 = HexColorToVec3(prefs.LayerColor3);
            Vector3 bgColor = HexColorToVec3(prefs.BackgroundColor);

            if (ImGui.ColorEdit3("##Layer Color 1", ref layerColor1))
                prefs.LayerColor1 = Vec3ToHexColor(layerColor1);

            ImGui.SameLine();
            if (ImGui.Button("X##ResetLC1"))
            {
                prefs.LayerColor1 = new HexColor("#000000");
            }
            ImGui.SetItemTooltip("Reset");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Layer Color 1");

            if (ImGui.ColorEdit3("##Layer Color 2", ref layerColor2))
                prefs.LayerColor2 = Vec3ToHexColor(layerColor2);

            ImGui.SameLine();
            if (ImGui.Button("X##ResetLC2"))
            {
                prefs.LayerColor2 = new HexColor("#59ff59");
            }
            ImGui.SetItemTooltip("Reset");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Layer Color 2");

            if (ImGui.ColorEdit3("##Layer Color 3", ref layerColor3))
                prefs.LayerColor3 = Vec3ToHexColor(layerColor3);

            ImGui.SameLine();
            if (ImGui.Button("X##ResetLC3"))
            {
                prefs.LayerColor3 = new HexColor("#ff1e1e");
            }
            ImGui.SetItemTooltip("Reset");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Layer Color 3");

            if (ImGui.ColorEdit3("##Background Color", ref bgColor))
                prefs.BackgroundColor = Vec3ToHexColor(bgColor);

            ImGui.SameLine();
            if (ImGui.Button("X##ResetBGC"))
            {
                prefs.BackgroundColor = new HexColor(127, 127, 127);
            }
            ImGui.SetItemTooltip("Reset");
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Background Color");

            // TODO: font scale
        }

        ImGui.SeparatorText("Miscellaneous");
        {
            // they've brainwashed me to not add this
            //bool showHiddenEffects = prefs.ShowDeprecatedEffects;
            //if (ImGui.Checkbox("Show deprecated effects", ref showHiddenEffects))
            //    prefs.ShowDeprecatedEffects = showHiddenEffects;

            bool versionCheck = prefs.CheckForUpdates;
            if (ImGui.Checkbox("Check for updates", ref versionCheck))
                prefs.CheckForUpdates = versionCheck;
            
            bool hideScreenSize = prefs.HideScreenSize;
            if (ImGui.Checkbox("Hide screen size parameters in the resize window", ref hideScreenSize))
                prefs.HideScreenSize = hideScreenSize;

            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 10f);
            
            // camera border view mode
            var camBorderMode = (int) prefs.CameraBorderMode;
            if (ImGui.Combo("Camera border view mode", ref camBorderMode, "Inner Border\0Outer Border\0Both Borders"))
                prefs.CameraBorderMode = (UserPreferences.CameraBorderModeOption) camBorderMode;
            
            // autotile mouse mode
            var autotileMouseMode = (int) prefs.AutotileMouseMode;
            if (ImGui.Combo("Autotile mouse mode", ref autotileMouseMode, "Click\0Hold"))
                prefs.AutotileMouseMode = (UserPreferences.AutotileMouseModeOptions) autotileMouseMode;
            
            ImGui.PopItemWidth();
        }
    }

    private static void ShowShortcutsTab()
    {
        ImGui.SeparatorText("Accessibility");
        ShortcutButton(KeyShortcut.RightMouse);
        
        ImGui.SeparatorText("General");
        ShortcutButton(KeyShortcut.ViewZoomIn);
        ShortcutButton(KeyShortcut.ViewZoomOut);
        ImGui.Separator();
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
        ShortcutButton(KeyShortcut.ExportGeometry);

        ImGui.SeparatorText("Editing");
        ShortcutButton(KeyShortcut.NavUp);
        ShortcutButton(KeyShortcut.NavDown);
        ShortcutButton(KeyShortcut.NavLeft);
        ShortcutButton(KeyShortcut.NavRight);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.NewObject);
        ShortcutButton(KeyShortcut.RemoveObject);
        ShortcutButton(KeyShortcut.Duplicate);
        ShortcutButton(KeyShortcut.Eyedropper);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.SwitchLayer);
        ShortcutButton(KeyShortcut.SwitchTab);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.IncreaseBrushSize);
        ShortcutButton(KeyShortcut.DecreaseBrushSize);

        ImGui.SeparatorText("Geometry");
        ShortcutButton(KeyShortcut.ToggleLayer1);
        ShortcutButton(KeyShortcut.ToggleLayer2);
        ShortcutButton(KeyShortcut.ToggleLayer3);

        ImGui.SeparatorText("Tiles");
        ShortcutButton(KeyShortcut.SetMaterial);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.TileForceGeometry);
        ShortcutButton(KeyShortcut.TileForcePlacement);
        ShortcutButton(KeyShortcut.TileIgnoreDifferent);

        ImGui.SeparatorText("Cameras");
        ShortcutButton(KeyShortcut.CameraSnapX);
        ShortcutButton(KeyShortcut.CameraSnapY);

        ImGui.SeparatorText("Light");
        ShortcutButton(KeyShortcut.ResetBrushTransform);
        ShortcutButton(KeyShortcut.ScaleLightBrush);
        ShortcutButton(KeyShortcut.RotateLightBrush);
        ImGui.Separator();
        ShortcutButton(KeyShortcut.ZoomLightIn);
        ShortcutButton(KeyShortcut.ZoomLightOut);
        ShortcutButton(KeyShortcut.RotateLightCW);
        ShortcutButton(KeyShortcut.RotateLightCCW);

        ImGui.SeparatorText("Props");
        ShortcutButton(KeyShortcut.ToggleVertexMode);
    }

    private static readonly List<string> availableThemes = [];
    private static bool initTheme = true;

    private static void ReloadThemeList()
    {
        availableThemes.Clear();
        foreach (var fileName in Directory.EnumerateFiles(Path.Combine(Boot.AppDataPath, "config", "themes")))
        {
            if (Path.GetExtension(fileName) != ".json") continue;
            availableThemes.Add(Path.GetFileNameWithoutExtension(fileName));    
        }
        availableThemes.Sort();
    }

    private static void ShowThemeTab(bool entered)
    {
        if (initTheme)
        {
            initTheme = false;
            ThemeEditor.ThemeSaved += ReloadThemeList;
        }

        // compile available themes when the tab is clicked
        if (entered)
        {
            ReloadThemeList();        
        }

        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 12.0f);
        if (ImGui.BeginCombo("Theme", RainEd.Instance.Preferences.Theme))
        {
            foreach (var themeName in availableThemes)
            {
                if (ImGui.Selectable(themeName, themeName == RainEd.Instance.Preferences.Theme))
                {
                    RainEd.Instance.Preferences.Theme = themeName;
                    RainEd.Instance.Preferences.ApplyTheme();
                    ThemeEditor.SaveRef();
                }
            }

            ImGui.EndCombo();
        }

        if (ImGui.TreeNode("Theme Editor"))
        {
            ThemeEditor.Show();
            ImGui.TreePop();
        }
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