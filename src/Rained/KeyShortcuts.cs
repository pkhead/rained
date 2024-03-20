using ImGuiNET;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace RainEd;

enum KeyShortcut
{
    None,
    
    // General
    NavUp, NavLeft, NavDown, NavRight,
    NewObject, RemoveObject, SwitchLayer, SwitchTab,
    ToggleTiles, ToggleGrid,

    New, Open, Save, SaveAs,
    Cut, Copy, Paste, Undo, Redo,

    Render,

    // Geometry
    ToggleLayer1, ToggleLayer2, ToggleLayer3,

    // Tile Editor
    Eyedropper, SetMaterial,
    TileForceGeometry, TileForcePlacement, TileIgnoreDifferent,

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
        public string Name;
        public string ShortcutString = null!;
        public ImGuiKey Key;
        public ImGuiModFlags Mods;
        public bool IsActivated = false;
        public bool AllowRepeat = false;

        public KeyShortcutBinding(string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
        {
            Name = name;
            ID = id;
            Key = key;
            Mods = mods;
            AllowRepeat = allowRepeat;

            GenerateShortcutString();
        }

        public void GenerateShortcutString()
        {
            // build shortcut string
            var str = new List<string>();

            if (Mods.HasFlag(ImGuiModFlags.Ctrl))
                str.Add("Ctrl");
            
            if (Mods.HasFlag(ImGuiModFlags.Shift))
                str.Add("Shift");
            
            if (Mods.HasFlag(ImGuiModFlags.Alt))
                str.Add("Alt");
            
            if (Mods.HasFlag(ImGuiModFlags.Super))
                str.Add("Super");
            
            str.Add(ImGui.GetKeyName(Key));

            ShortcutString = string.Join('+', str);
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

    public static void Register(string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(name, id, key, mods, allowRepeat));
    }

    public static void Rebind(KeyShortcut id, ImGuiKey key, ImGuiModFlags mods)
    {
        var data = keyShortcuts[id];
        data.Key = key;
        data.Mods = mods;
        data.GenerateShortcutString();
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

    public static string GetShortcutString(KeyShortcut id)
        => keyShortcuts[id].ShortcutString;
    
    public static string GetName(KeyShortcut id)
        => keyShortcuts[id].Name;

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
        Register("Navigate Up", KeyShortcut.NavUp, ImGuiKey.W, ImGuiModFlags.None, true);
        Register("Navigate Left", KeyShortcut.NavLeft, ImGuiKey.A, ImGuiModFlags.None, true);
        Register("Navigate Down", KeyShortcut.NavDown, ImGuiKey.S, ImGuiModFlags.None, true);
        Register("Navigate Right", KeyShortcut.NavRight, ImGuiKey.D, ImGuiModFlags.None, true);
        Register("New Object", KeyShortcut.NewObject, ImGuiKey.N, ImGuiModFlags.None, true);
        Register("Remove Object", KeyShortcut.RemoveObject, ImGuiKey.Delete, ImGuiModFlags.None, true);
        Register("Toggle Tile Rendering", KeyShortcut.ToggleTiles, ImGuiKey.T, ImGuiModFlags.Ctrl, true);
        Register("Toggle Grid", KeyShortcut.ToggleGrid, ImGuiKey.G, ImGuiModFlags.Ctrl, true);

        Register("New File", KeyShortcut.New, ImGuiKey.N, ImGuiModFlags.Ctrl);
        Register("Open File", KeyShortcut.Open, ImGuiKey.O, ImGuiModFlags.Ctrl);
        Register("Save File", KeyShortcut.Save, ImGuiKey.S, ImGuiModFlags.Ctrl);
        Register("Save File As", KeyShortcut.SaveAs, ImGuiKey.S, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);
        Register("Render", KeyShortcut.Render, ImGuiKey.R, ImGuiModFlags.Ctrl);

        Register("Cut", KeyShortcut.Cut, ImGuiKey.X, ImGuiModFlags.Ctrl);
        Register("Copy", KeyShortcut.Copy, ImGuiKey.C, ImGuiModFlags.Ctrl);
        Register("Paste", KeyShortcut.Paste, ImGuiKey.V, ImGuiModFlags.Ctrl);
        Register("Undo", KeyShortcut.Undo, ImGuiKey.Z, ImGuiModFlags.Ctrl, true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Y, ImGuiModFlags.Ctrl, true);
        else
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Z, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift, true);
        
        Register("Cycle Layer", KeyShortcut.SwitchLayer, ImGuiKey.Tab, ImGuiModFlags.None);
        Register("Switch Tab", KeyShortcut.SwitchTab, ImGuiKey.Tab, ImGuiModFlags.Shift);
        Register("Toggle Layer 1", KeyShortcut.ToggleLayer1, ImGuiKey.E, ImGuiModFlags.None);
        Register("Toggle Layer 2", KeyShortcut.ToggleLayer2, ImGuiKey.R, ImGuiModFlags.None);
        Register("Toggle Layer 3", KeyShortcut.ToggleLayer3, ImGuiKey.T, ImGuiModFlags.None);

        // Tile Editor
        Register("Eyedropper", KeyShortcut.Eyedropper, ImGuiKey.Q, ImGuiModFlags.None, true);
        Register("Set Material to Default", KeyShortcut.SetMaterial, ImGuiKey.E, ImGuiModFlags.None, true);
        Register("Force Geometry", KeyShortcut.TileForceGeometry, ImGuiKey.G, ImGuiModFlags.None);
        Register("Force Placement", KeyShortcut.TileForcePlacement, ImGuiKey.F, ImGuiModFlags.None);
        Register("Disallow Overwrite", KeyShortcut.TileIgnoreDifferent, ImGuiKey.R, ImGuiModFlags.None);

        // Light Editor
        Register("Reset Brush Transform", KeyShortcut.ResetBrushTransform, ImGuiKey.R, ImGuiModFlags.None);
        Register("Move Light Inward", KeyShortcut.ZoomLightIn, ImGuiKey.W, ImGuiModFlags.Shift);
        Register("Move Light Outward", KeyShortcut.ZoomLightOut, ImGuiKey.S, ImGuiModFlags.Shift);
        Register("Rotate Light CW", KeyShortcut.RotateLightCW, ImGuiKey.D, ImGuiModFlags.Shift);
        Register("Rotate Light CCW", KeyShortcut.RotateLightCCW, ImGuiKey.A, ImGuiModFlags.Shift);
    }
}