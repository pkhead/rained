using ImGuiNET;
namespace RainEd;

enum KeyShortcut
{
    NavUp, NavLeft, NavDown, NavRight,
    NewObject, SwitchLayer,

    New, Open, Save, SaveAs,
    Cut, Copy, Paste, Undo, Redo,

    Render,

    // Geometry
    ToggleLayer1, ToggleLayer2, ToggleLayer3,

    // Light
    ResetBrushTransform
}

static class KeyShortcuts
{
    private class KeyShortcutBinding
    {
        public KeyShortcut ID;
        public string ShortcutString;
        public ImGuiKey Key;
        public ImGuiModFlags Mods;
        public bool IsActivated = false;
        public bool AllowRepeat = false;

        public KeyShortcutBinding(KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
        {
            // build shortcut string
            var str = new List<string>();

            if (mods.HasFlag(ImGuiModFlags.Ctrl))
                str.Add("Ctrl");
            
            if (mods.HasFlag(ImGuiModFlags.Shift))
                str.Add("Shift");
            
            if (mods.HasFlag(ImGuiModFlags.Alt))
                str.Add("Alt");
            
            if (mods.HasFlag(ImGuiModFlags.Super))
                str.Add("Super");
            
            str.Add(ImGui.GetKeyName(key));

            ShortcutString = string.Join('+', str);

            // initialize other properties
            ID = id;
            Key = key;
            Mods = mods;
            AllowRepeat = allowRepeat;
        }

        public bool IsKeyPressed()
            =>
                ImGui.IsKeyPressed(Key, AllowRepeat) &&
                (Mods.HasFlag(ImGuiModFlags.Ctrl) == ImGui.IsKeyDown(ImGuiKey.ModCtrl)) &&
                (Mods.HasFlag(ImGuiModFlags.Shift) == ImGui.IsKeyDown(ImGuiKey.ModShift)) &&
                (Mods.HasFlag(ImGuiModFlags.Alt) == ImGui.IsKeyDown(ImGuiKey.ModAlt)) &&
                (Mods.HasFlag(ImGuiModFlags.Super) == ImGui.IsKeyDown(ImGuiKey.ModSuper));
    }
    private static readonly Dictionary<KeyShortcut, KeyShortcutBinding> keyShortcuts = [];

    public static void Register(KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(id, key, mods, allowRepeat));
    }

    public static bool Activated(KeyShortcut id)
        => keyShortcuts[id].IsActivated;

    public static void ImGuiMenuItem(KeyShortcut id, string name)
    {
        var shortcutData = keyShortcuts[id];
        if (ImGui.MenuItem(name, shortcutData.ShortcutString))
            shortcutData.IsActivated = true;
    }

    public static void Update()
    {
        // activate shortcuts on key press
        if (!ImGui.GetIO().WantTextInput)
        {
            foreach (var shortcut in keyShortcuts.Values)
            {
                if (shortcut.IsKeyPressed())
                    shortcut.IsActivated = true;
                else
                    shortcut.IsActivated = false;
            }
        }
    }
}