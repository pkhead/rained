using ImGuiNET;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace RainEd;

enum KeyShortcut : int
{
    None = -1,

    RightMouse,
    
    // General
    NavUp, NavLeft, NavDown, NavRight,
    NewObject, RemoveObject, SwitchLayer, SwitchTab, Duplicate,
    ViewZoomIn, ViewZoomOut,
    IncreaseBrushSize, DecreaseBrushSize,

    New, Open, Save, SaveAs,
    Cut, Copy, Paste, Undo, Redo,

    Render, ExportGeometry,

    // Geometry
    ToggleLayer1, ToggleLayer2, ToggleLayer3,
    ToggleMirrorX, ToggleMirrorY,

    // Tile Editor
    Eyedropper, SetMaterial,
    TileForceGeometry, TileForcePlacement, TileIgnoreDifferent,

    // Light
    ResetBrushTransform,
    ZoomLightIn, ZoomLightOut,
    RotateLightCW, RotateLightCCW,
    ScaleLightBrush, RotateLightBrush,

    // Camera
    CameraSnapX, CameraSnapY,

    // Props
    ToggleVertexMode,

    // View settings shortcuts
    ToggleViewGrid, ToggleViewTiles, ToggleViewProps,
    ToggleViewCameras,

    /// <summary>
    /// Do not bind - this is just the number of shortcut IDs
    /// </summary>
    COUNT
}

static class KeyShortcuts
{
    private class KeyShortcutBinding
    {
        public readonly KeyShortcut ID;
        public readonly string Name;
        public string ShortcutString = null!;
        public ImGuiKey Key;
        public ImGuiModFlags Mods;
        public bool IsActivated = false;
        public bool IsDeactivated = false;
        public bool IsDown = false;
        public bool AllowRepeat = false;

        public readonly ImGuiKey OriginalKey;
        public readonly ImGuiModFlags OriginalMods; 

        public KeyShortcutBinding(string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
        {
            if (key == ImGuiKey.Backspace) key = ImGuiKey.Delete;

            Name = name;
            ID = id;
            Key = key;
            Mods = mods;
            AllowRepeat = allowRepeat;

            OriginalKey = Key;
            OriginalMods = Mods;

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
            if (ID == KeyShortcut.RightMouse && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                return true;
            }

            if (Key == ImGuiKey.None) return false;

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
            if (ID == KeyShortcut.RightMouse && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                return true;
            }

            if (Key == ImGuiKey.None) return false;

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

    private static void Register(string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods, bool allowRepeat = false)
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(name, id, key, mods, allowRepeat));
    }

    public static void Rebind(KeyShortcut id, ImGuiKey key, ImGuiModFlags mods)
    {
        if (key == ImGuiKey.Backspace) key = ImGuiKey.Delete;

        var data = keyShortcuts[id];
        data.Key = key;
        data.Mods = mods;
        data.GenerateShortcutString();
    }

    public static void Rebind(KeyShortcut id, string shortcut)
    {
        var keyStr = shortcut.Split('+');
        ImGuiModFlags mods = ImGuiModFlags.None;
        ImGuiKey tKey = ImGuiKey.None;

        for (int i = 0; i < keyStr.Length - 1; i++)
        {
            var modStr = keyStr[i];
            
            if (modStr == "Ctrl")
                mods |= ImGuiModFlags.Ctrl;
            else if (modStr == "Alt")
                mods |= ImGuiModFlags.Alt;
            else if (modStr == "Shift")
                mods |= ImGuiModFlags.Shift;
            else if (modStr == "Super")
                mods |= ImGuiModFlags.Super;
            else
                throw new Exception($"Unknown modifier key '{modStr}'");
        }

        if (keyStr[^1] == "None")
        {
            tKey = ImGuiKey.None;
        }
        else
        {
            for (int ki = (int)ImGuiKey.NamedKey_BEGIN; ki < (int)ImGuiKey.NamedKey_END; ki++)
            {
                ImGuiKey key = (ImGuiKey) ki;
                if (keyStr[^1] == ImGui.GetKeyName(key))
                {
                    tKey = key;
                    break;
                }
            }

            // throw an exception if the ImGuiKey was not found from the string
            if (tKey == ImGuiKey.None)
                throw new Exception($"Unknown key '{keyStr[^1]}'");
        }
        
        // assign to binding data
        KeyShortcutBinding data = keyShortcuts[id];
        data.Key = tKey;
        data.Mods = mods;
        data.GenerateShortcutString();
    }

    public static void Reset(KeyShortcut id)
    {
        var data = keyShortcuts[id];
        data.Key = data.OriginalKey;
        data.Mods = data.OriginalMods;
        data.GenerateShortcutString();
    }

    public static bool IsModifierKey(ImGuiKey key)
    {
        return key == ImGuiKey.LeftShift || key == ImGuiKey.RightShift
            || key == ImGuiKey.LeftCtrl || key == ImGuiKey.RightCtrl
            || key == ImGuiKey.LeftAlt || key == ImGuiKey.RightAlt
            || key == ImGuiKey.LeftSuper || key == ImGuiKey.RightSuper
            || key == ImGuiKey.ReservedForModAlt
            || key == ImGuiKey.ReservedForModCtrl
            || key == ImGuiKey.ReservedForModShift
            || key == ImGuiKey.ReservedForModSuper
            || key == ImGuiKey.ModAlt || key == ImGuiKey.ModShift || key == ImGuiKey.ModCtrl || key == ImGuiKey.ModSuper;
    }

    public static bool Activated(KeyShortcut id)
        => keyShortcuts[id].IsActivated;
    
    public static bool Active(KeyShortcut id)
        => keyShortcuts[id].IsDown;

    public static bool Deactivated(KeyShortcut id)
        => keyShortcuts[id].IsDeactivated;

    public static void ImGuiMenuItem(KeyShortcut id, string name, bool selected = false)
    {
        var shortcutData = keyShortcuts[id];
        if (ImGui.MenuItem(name, shortcutData.ShortcutString, selected))
            shortcutData.IsActivated = true;
    }

    public static string GetShortcutString(KeyShortcut id)
        => keyShortcuts[id].ShortcutString;
    
    public static string GetName(KeyShortcut id)
        => keyShortcuts[id].Name;

    public static void Update()
    {
        // activate shortcuts on key press
        bool inputDisabled = ImGui.GetIO().WantTextInput;
        
        foreach (var shortcut in keyShortcuts.Values)
        {
            shortcut.IsActivated = false;

            if (shortcut.IsKeyPressed() && (!inputDisabled || shortcut.ID == KeyShortcut.RightMouse))
            {
                shortcut.IsActivated = true;
                shortcut.IsDown = true;
            }

            shortcut.IsDeactivated = false;
            if (shortcut.IsDown && !shortcut.IsKeyDown())
            {
                shortcut.IsDown = false;
                shortcut.IsDeactivated = true;
            }
        }
    }

    public static void InitShortcuts()
    {
        Register("Right Mouse Substitute", KeyShortcut.RightMouse, ImGuiKey.None, ImGuiModFlags.None);

        Register("Navigate Up", KeyShortcut.NavUp, ImGuiKey.W, ImGuiModFlags.None, true);
        Register("Navigate Left", KeyShortcut.NavLeft, ImGuiKey.A, ImGuiModFlags.None, true);
        Register("Navigate Down", KeyShortcut.NavDown, ImGuiKey.S, ImGuiModFlags.None, true);
        Register("Navigate Right", KeyShortcut.NavRight, ImGuiKey.D, ImGuiModFlags.None, true);
        Register("Zoom View In", KeyShortcut.ViewZoomIn, ImGuiKey.Equal, ImGuiModFlags.None, true);
        Register("Zoom View Out", KeyShortcut.ViewZoomOut, ImGuiKey.Minus, ImGuiModFlags.None, true);

        Register("New Object", KeyShortcut.NewObject, ImGuiKey.C, ImGuiModFlags.None, true);
        Register("Remove", KeyShortcut.RemoveObject, ImGuiKey.X, ImGuiModFlags.None, true);
        Register("Duplicate", KeyShortcut.Duplicate, ImGuiKey.D, ImGuiModFlags.Ctrl, true);

        Register("New File", KeyShortcut.New, ImGuiKey.N, ImGuiModFlags.Ctrl);
        Register("Open File", KeyShortcut.Open, ImGuiKey.O, ImGuiModFlags.Ctrl);
        Register("Save File", KeyShortcut.Save, ImGuiKey.S, ImGuiModFlags.Ctrl);
        Register("Save File As", KeyShortcut.SaveAs, ImGuiKey.S, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);
        Register("Render", KeyShortcut.Render, ImGuiKey.R, ImGuiModFlags.Ctrl);
        Register("Export Geometry", KeyShortcut.ExportGeometry, ImGuiKey.R, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);

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
        Register("Increase Brush Size", KeyShortcut.IncreaseBrushSize, ImGuiKey.O, ImGuiModFlags.None, true);
        Register("Decrease Brush Size", KeyShortcut.DecreaseBrushSize, ImGuiKey.I, ImGuiModFlags.None, true);
        Register("Toggle Mirror X", KeyShortcut.ToggleMirrorX, ImGuiKey.F, ImGuiModFlags.None, false);
        Register("Toggle Mirror Y", KeyShortcut.ToggleMirrorY, ImGuiKey.G, ImGuiModFlags.None, false);

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
        Register("Scale Brush", KeyShortcut.ScaleLightBrush, ImGuiKey.Q, ImGuiModFlags.None);
        Register("Rotate Brush", KeyShortcut.RotateLightBrush, ImGuiKey.E, ImGuiModFlags.None);

        // Camera Editor
        Register("Camera Snap X", KeyShortcut.CameraSnapX, ImGuiKey.Q, ImGuiModFlags.None);
        Register("Camera Snap Y", KeyShortcut.CameraSnapY, ImGuiKey.E, ImGuiModFlags.None);

        // Prop Editor
        Register("Toggle Vertex Mode", KeyShortcut.ToggleVertexMode, ImGuiKey.F, ImGuiModFlags.None);

        // View options
        Register("View Grid", KeyShortcut.ToggleViewGrid, ImGuiKey.G, ImGuiModFlags.Ctrl);
        Register("View Tiles", KeyShortcut.ToggleViewTiles, ImGuiKey.T, ImGuiModFlags.Ctrl);
        Register("View Props", KeyShortcut.ToggleViewProps, ImGuiKey.P, ImGuiModFlags.Ctrl);
        Register("View Camera Borders", KeyShortcut.ToggleViewCameras, ImGuiKey.M, ImGuiModFlags.Ctrl);
    }
}