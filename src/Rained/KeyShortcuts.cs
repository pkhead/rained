using ImGuiNET;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace RainEd;

enum KeyShortcut
{
    // General
    NavUp, NavLeft, NavDown, NavRight,
    NewObject, RemoveObject, SwitchLayer, SwitchTab,

    New, Open, Save, SaveAs,
    Cut, Copy, Paste, Undo, Redo,

    Render,

    // Geometry
    ToggleLayer1, ToggleLayer2, ToggleLayer3,

    // Light
    ResetBrushTransform,
    ZoomLightIn, ZoomLightOut,
    RotateLightCW, RotateLightCCW
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
        {
            bool kp;

            // i disable imgui from receiving tab inputs
            if (Key == ImGuiKey.Tab)
                kp = (bool)Raylib.IsKeyPressed(KeyboardKey.Tab);
            
            // delete/backspace will do the same thing
            else if (Key == ImGuiKey.Delete)
                kp = ImGui.IsKeyPressed(ImGuiKey.Delete, AllowRepeat) || ImGui.IsKeyPressed(ImGuiKey.Backspace, AllowRepeat);

            else
                kp = ImGui.IsKeyPressed(Key, AllowRepeat);
            
            return kp &&
            (Mods.HasFlag(ImGuiModFlags.Ctrl) == ImGui.IsKeyDown(ImGuiKey.ModCtrl)) &&
            (Mods.HasFlag(ImGuiModFlags.Shift) == ImGui.IsKeyDown(ImGuiKey.ModShift)) &&
            (Mods.HasFlag(ImGuiModFlags.Alt) == ImGui.IsKeyDown(ImGuiKey.ModAlt)) &&
            (Mods.HasFlag(ImGuiModFlags.Super) == ImGui.IsKeyDown(ImGuiKey.ModSuper));
        }

        public bool IsKeyDown()
        {
            bool kp;

            // i disable imgui from receiving tab inputs
            if (Key == ImGuiKey.Tab)
                kp = (bool)Raylib.IsKeyDown(KeyboardKey.Tab);
            
            // delete/backspace will do the same thing
            else if (Key == ImGuiKey.Delete)
                kp = ImGui.IsKeyDown(ImGuiKey.Delete) || ImGui.IsKeyDown(ImGuiKey.Backspace);

            else
                kp = ImGui.IsKeyDown(Key);
            
            return kp &&
            (Mods.HasFlag(ImGuiModFlags.Ctrl) == ImGui.IsKeyDown(ImGuiKey.ModCtrl)) &&
            (Mods.HasFlag(ImGuiModFlags.Shift) == ImGui.IsKeyDown(ImGuiKey.ModShift)) &&
            (Mods.HasFlag(ImGuiModFlags.Alt) == ImGui.IsKeyDown(ImGuiKey.ModAlt)) &&
            (Mods.HasFlag(ImGuiModFlags.Super) == ImGui.IsKeyDown(ImGuiKey.ModSuper));
        }
    }
    private static readonly Dictionary<KeyShortcut, KeyShortcutBinding> keyShortcuts = [];

    public static void Register(KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(id, key, mods, allowRepeat));
    }

    public static bool Activated(KeyShortcut id)
        => keyShortcuts[id].IsActivated;
    
    public static bool Active(KeyShortcut id)
        => !ImGui.GetIO().WantTextInput && keyShortcuts[id].IsKeyDown();

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

    public static void InitShortcuts()
    {
        Register(KeyShortcut.NavUp, ImGuiKey.W, ImGuiModFlags.None, true);
        Register(KeyShortcut.NavLeft, ImGuiKey.A, ImGuiModFlags.None, true);
        Register(KeyShortcut.NavDown, ImGuiKey.S, ImGuiModFlags.None, true);
        Register(KeyShortcut.NavRight, ImGuiKey.D, ImGuiModFlags.None, true);
        Register(KeyShortcut.NewObject, ImGuiKey.N, ImGuiModFlags.None, true);
        Register(KeyShortcut.RemoveObject, ImGuiKey.Delete, ImGuiModFlags.None, true);

        Register(KeyShortcut.New, ImGuiKey.N, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.Open, ImGuiKey.O, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.Save, ImGuiKey.S, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.SaveAs, ImGuiKey.S, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);
        Register(KeyShortcut.Render, ImGuiKey.R, ImGuiModFlags.Ctrl);

        Register(KeyShortcut.Cut, ImGuiKey.X, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.Copy, ImGuiKey.C, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.Paste, ImGuiKey.V, ImGuiModFlags.Ctrl);
        Register(KeyShortcut.Undo, ImGuiKey.Z, ImGuiModFlags.Ctrl, true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Register(KeyShortcut.Redo, ImGuiKey.Y, ImGuiModFlags.Ctrl, true);
        else
            Register(KeyShortcut.Redo, ImGuiKey.Z, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift, true);
        
        Register(KeyShortcut.SwitchLayer, ImGuiKey.Tab, ImGuiModFlags.None);
        Register(KeyShortcut.SwitchTab, ImGuiKey.Tab, ImGuiModFlags.Shift);
        Register(KeyShortcut.ToggleLayer1, ImGuiKey.E, ImGuiModFlags.None);
        Register(KeyShortcut.ToggleLayer2, ImGuiKey.R, ImGuiModFlags.None);
        Register(KeyShortcut.ToggleLayer3, ImGuiKey.T, ImGuiModFlags.None);

        Register(KeyShortcut.ResetBrushTransform, ImGuiKey.R, ImGuiModFlags.None);
        Register(KeyShortcut.ZoomLightIn, ImGuiKey.W, ImGuiModFlags.Shift);
        Register(KeyShortcut.ZoomLightOut, ImGuiKey.S, ImGuiModFlags.Shift);
        Register(KeyShortcut.RotateLightCW, ImGuiKey.D, ImGuiModFlags.Shift);
        Register(KeyShortcut.RotateLightCCW, ImGuiKey.A, ImGuiModFlags.Shift);
    }
}