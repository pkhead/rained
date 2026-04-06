using Hexa.NET.ImGui;
using Raylib_cs;
using System.Runtime.InteropServices;

namespace Rained;

enum KeyShortcut : int
{
    None = -1,

    RightMouse,

    // Edit modes
    EnvironmentEditor, GeometryEditor, TileEditor,
    CameraEditor, LightEditor, EffectsEditor, PropEditor, 
    
    // General
    NavUp, NavLeft, NavDown, NavRight,
    NewObject, RemoveObject, SwitchLayer, SwitchTab, Duplicate,
    ViewZoomIn, ViewZoomOut,
    IncreaseBrushSize, DecreaseBrushSize,

    New, Open, Save, SaveAs, CloseFile, CloseAllFiles,
    Cut, Copy, Paste, Undo, Redo,
    Select,

    Render, ExportGeometry,

    SelectEditor, AdjustView,

    // Geometry
    ToggleLayer1, ToggleLayer2, ToggleLayer3,
    ToggleMirrorX, ToggleMirrorY,
    FloodFill,
    ToolWall, ToolShortcutEntrance, ToolShortcutDot,

    // Tile Editor
    Eyedropper, SetMaterial,
    TileForceGeometry, TileForcePlacement, TileIgnoreDifferent,

    // Light
    ResetBrushTransform,
    ZoomLightIn, ZoomLightOut,
    RotateLightCW, RotateLightCCW,
    ScaleLightBrush, RotateLightBrush,
    LightmapStretch,

    RotateBrushCW, RotateBrushCCW,
    PreviousBrush, NextBrush,

    // Camera
    CameraSnapX, CameraSnapY,

    // Props
    ToggleVertexMode, RopeSimulation, RopeSimulationFast, ResetSimulation,

    // View settings shortcuts
    ToggleViewGrid, ToggleViewTiles, ToggleViewProps,
    ToggleViewCameras, ToggleViewGraphics, ToggleViewNodeIndices,
    RotatePropCW, RotatePropCCW,
    ChangePropSnapping,

    /// <summary>
    /// Do not bind - this is just the number of shortcut IDs
    /// </summary>
    COUNT
}

static class KeyShortcuts
{
    public static readonly string CtrlName;
    public static readonly string ShiftName;
    public static readonly string AltName;
    public static readonly string SuperName;

    static KeyShortcuts()
    {
        ShiftName = "Shift";

        if (OperatingSystem.IsMacOS())
        {
            CtrlName = "Cmd";
            AltName = "Option";
            SuperName = "Ctrl";
        }
        else
        {
            CtrlName = "Ctrl";
            AltName = "Alt";

            if (OperatingSystem.IsWindows())
            {
                SuperName = "Win";
            }
            else
            {
                SuperName = "Super";
            }
        }
    }

    private class KeyShortcutBinding
    {
        public readonly KeyShortcut ID;
        public readonly string Name;
        public string ShortcutString = null!;
        public ImGuiKey Key;
        public ImGuiKey Mods;
        public ImGuiKey AllowedMods;
        public bool IsActivated = false;
        public bool IsDeactivated = false;
        public bool IsDown = false;
        public bool AllowRepeat = false;

        public readonly ImGuiKey OriginalKey;
        public readonly ImGuiKey OriginalMods;

        public KeyShortcutBinding(
            string name, KeyShortcut id, ImGuiKey key, ImGuiKey mods,
            bool allowRepeat = false,
            ImGuiKey allowedMods = ImGuiKey.None
        )
        {
            if (key == ImGuiKey.Backspace) key = ImGuiKey.Delete;

            Name = name;
            ID = id;
            Key = key;
            Mods = mods;
            AllowRepeat = allowRepeat;
            AllowedMods = allowedMods;

            OriginalKey = Key;
            OriginalMods = Mods;

            GenerateShortcutString();
        }

        public void GenerateShortcutString()
        {
            // build shortcut string
            var str = new List<string>();

            if (Mods.HasFlag(ImGuiKey.ModCtrl))
                str.Add(CtrlName);
            
            if (Mods.HasFlag(ImGuiKey.ModShift))
                str.Add(ShiftName);
            
            if (Mods.HasFlag(ImGuiKey.ModAlt))
                str.Add(AltName);
            
            if (Mods.HasFlag(ImGuiKey.ModSuper))
                str.Add(SuperName);
            
            str.Add(ImGui.GetKeyNameS(Key));

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
            CheckModKey(ImGuiKey.ModCtrl, ImGuiKey.ModCtrl) &&
            CheckModKey(ImGuiKey.ModShift, ImGuiKey.ModShift) &&
            CheckModKey(ImGuiKey.ModAlt, ImGuiKey.ModAlt) &&
            CheckModKey(ImGuiKey.ModSuper, ImGuiKey.ModSuper);
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
            CheckModKey(ImGuiKey.ModCtrl) &&
            CheckModKey(ImGuiKey.ModShift) &&
            CheckModKey(ImGuiKey.ModAlt) &&
            CheckModKey(ImGuiKey.ModSuper);
        }

        private bool CheckModKey(ImGuiKey modKey) {
            var down = ImGui.IsKeyDown(modKey);
            return (Mods.HasFlag(modKey) == down) || (down && AllowedMods.HasFlag(modKey));
        }
    }
    private static readonly Dictionary<KeyShortcut, KeyShortcutBinding> keyShortcuts = [];


    private static void Register(
        string name, KeyShortcut id, ImGuiKey key, ImGuiKey mods,
        bool allowRepeat = false,
        ImGuiKey allowedMods = ImGuiKey.None
    )
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(name, id, key, mods, allowRepeat, allowedMods));
    }

    public static void Rebind(KeyShortcut id, ImGuiKey key, ImGuiKey mods)
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
        ImGuiKey mods = ImGuiKey.None;
        ImGuiKey tKey = ImGuiKey.None;

        for (int i = 0; i < keyStr.Length - 1; i++)
        {
            var modStr = keyStr[i];
            
            if (modStr == CtrlName)
                mods |= ImGuiKey.ModCtrl;
            else if (modStr == AltName)
                mods |= ImGuiKey.ModAlt;
            else if (modStr == ShiftName)
                mods |= ImGuiKey.ModShift;
            else if (modStr == SuperName)
                mods |= ImGuiKey.ModSuper;
            else
                throw new Exception($"Unknown modifier key '{modStr}'");
        }

        if (keyStr[^1] == "None")
        {
            tKey = ImGuiKey.None;
        }
        else
        {
            for (int ki = (int)ImGuiKey.NamedKeyBegin; ki < (int)ImGuiKey.NamedKeyEnd; ki++)
            {
                ImGuiKey key = (ImGuiKey) ki;
                if (keyStr[^1] == ImGui.GetKeyNameS(key))
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

    public static void ImGuiMenuItem(KeyShortcut id, string name, bool selected = false, bool enabled = true)
    {
        var shortcutData = keyShortcuts[id];
        var shortcutStr = shortcutData.Key == ImGuiKey.None ? null : shortcutData.ShortcutString;

        if (ImGui.MenuItem(name, shortcutStr, selected, enabled))
            shortcutData.IsActivated = true;
    }

    public static string GetShortcutString(KeyShortcut id)
        => keyShortcuts[id].ShortcutString;
    
    public static string GetName(KeyShortcut id)
        => keyShortcuts[id].Name;

    public static void Update()
    {
        // activate shortcuts on key press
        bool inputDisabled = ImGui.GetIO().WantCaptureKeyboard;
        
        foreach (var shortcut in keyShortcuts.Values)
        {
            shortcut.IsActivated = false;
            if (inputDisabled)
            {
                shortcut.IsDown = false;
            }

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
        Register("Right Mouse Substitute", KeyShortcut.RightMouse, ImGuiKey.None, ImGuiKey.ModNone,
            allowedMods: ImGuiKey.ModCtrl | ImGuiKey.ModShift | ImGuiKey.ModAlt | ImGuiKey.ModSuper
        );

        Register("Environment Editor", KeyShortcut.EnvironmentEditor, ImGuiKey.Key1, ImGuiKey.ModNone);
        Register("Geometry Editor", KeyShortcut.GeometryEditor, ImGuiKey.Key2, ImGuiKey.ModNone);
        Register("Tile Editor", KeyShortcut.TileEditor, ImGuiKey.Key3, ImGuiKey.ModNone);
        Register("Camera Editor", KeyShortcut.CameraEditor, ImGuiKey.Key4, ImGuiKey.ModNone);
        Register("Light Editor", KeyShortcut.LightEditor, ImGuiKey.Key5, ImGuiKey.ModNone);
        Register("Effects Editor", KeyShortcut.EffectsEditor, ImGuiKey.Key6, ImGuiKey.ModNone);
        Register("Prop Editor", KeyShortcut.PropEditor, ImGuiKey.Key7, ImGuiKey.ModNone);

        Register("Navigate Up", KeyShortcut.NavUp, ImGuiKey.W, ImGuiKey.ModNone, true);
        Register("Navigate Left", KeyShortcut.NavLeft, ImGuiKey.A, ImGuiKey.ModNone, true);
        Register("Navigate Down", KeyShortcut.NavDown, ImGuiKey.S, ImGuiKey.ModNone, true);
        Register("Navigate Right", KeyShortcut.NavRight, ImGuiKey.D, ImGuiKey.ModNone, true);
        Register("Zoom View In", KeyShortcut.ViewZoomIn, ImGuiKey.Equal, ImGuiKey.ModNone, true);
        Register("Zoom View Out", KeyShortcut.ViewZoomOut, ImGuiKey.Minus, ImGuiKey.ModNone, true);

        Register("New Object", KeyShortcut.NewObject, ImGuiKey.C, ImGuiKey.ModNone, true);
        Register("Remove", KeyShortcut.RemoveObject, ImGuiKey.X, ImGuiKey.ModNone, true);
        Register("Duplicate", KeyShortcut.Duplicate, ImGuiKey.D, ImGuiKey.ModCtrl, true);

        Register("New File", KeyShortcut.New, ImGuiKey.N, ImGuiKey.ModCtrl);
        Register("Open File", KeyShortcut.Open, ImGuiKey.O, ImGuiKey.ModCtrl);
        Register("Save File", KeyShortcut.Save, ImGuiKey.S, ImGuiKey.ModCtrl);
        Register("Save File As", KeyShortcut.SaveAs, ImGuiKey.S, ImGuiKey.ModCtrl | ImGuiKey.ModShift);
        Register("Close", KeyShortcut.CloseFile, ImGuiKey.W, ImGuiKey.ModCtrl);
        Register("Close All", KeyShortcut.CloseAllFiles, ImGuiKey.W, ImGuiKey.ModCtrl | ImGuiKey.ModShift);

        Register("Render", KeyShortcut.Render, ImGuiKey.R, ImGuiKey.ModCtrl);
        Register("Export Geometry", KeyShortcut.ExportGeometry, ImGuiKey.R, ImGuiKey.ModCtrl | ImGuiKey.ModShift);

        Register("Cut", KeyShortcut.Cut, ImGuiKey.X, ImGuiKey.ModCtrl);
        Register("Copy", KeyShortcut.Copy, ImGuiKey.C, ImGuiKey.ModCtrl);
        Register("Paste", KeyShortcut.Paste, ImGuiKey.V, ImGuiKey.ModCtrl);
        Register("Select", KeyShortcut.Select, ImGuiKey.E, ImGuiKey.ModCtrl);
        Register("Undo", KeyShortcut.Undo, ImGuiKey.Z, ImGuiKey.ModCtrl, true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Y, ImGuiKey.ModCtrl, true);
        else
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Z, ImGuiKey.ModCtrl | ImGuiKey.ModShift, true);

        Register("Mode Select", KeyShortcut.SelectEditor, ImGuiKey.GraveAccent, ImGuiKey.ModNone);
        Register("Radial View Menu", KeyShortcut.AdjustView, ImGuiKey.GraveAccent, ImGuiKey.ModShift);
        
        Register("Cycle Layer", KeyShortcut.SwitchLayer, ImGuiKey.Tab, ImGuiKey.ModNone);
        Register("Switch Tab", KeyShortcut.SwitchTab, ImGuiKey.Tab, ImGuiKey.ModShift);
        Register("Increase Brush Size", KeyShortcut.IncreaseBrushSize, ImGuiKey.O, ImGuiKey.ModNone, true);
        Register("Decrease Brush Size", KeyShortcut.DecreaseBrushSize, ImGuiKey.I, ImGuiKey.ModNone, true);

        // Geometry
        Register("Toggle Layer 1", KeyShortcut.ToggleLayer1, ImGuiKey.E, ImGuiKey.ModNone);
        Register("Toggle Layer 2", KeyShortcut.ToggleLayer2, ImGuiKey.R, ImGuiKey.ModNone);
        Register("Toggle Layer 3", KeyShortcut.ToggleLayer3, ImGuiKey.T, ImGuiKey.ModNone);
        Register("Toggle Mirror X", KeyShortcut.ToggleMirrorX, ImGuiKey.F, ImGuiKey.ModNone, false);
        Register("Toggle Mirror Y", KeyShortcut.ToggleMirrorY, ImGuiKey.G, ImGuiKey.ModNone, false);
        Register("Flood Fill Modifier", KeyShortcut.FloodFill, ImGuiKey.Q, ImGuiKey.ModNone, false);
        Register("Wall Tool", KeyShortcut.ToolWall, ImGuiKey.Z, ImGuiKey.ModNone, false);
        Register("Shortcut Entrance Tool", KeyShortcut.ToolShortcutEntrance, ImGuiKey.X, ImGuiKey.ModNone);
        Register("Shortcut Dot Tool", KeyShortcut.ToolShortcutDot, ImGuiKey.C, ImGuiKey.ModNone);

        // Tile Editor
        Register("Eyedropper", KeyShortcut.Eyedropper, ImGuiKey.Q, ImGuiKey.ModNone, true);
        Register("Set Material to Default", KeyShortcut.SetMaterial, ImGuiKey.E, ImGuiKey.ModNone, true);
        Register("Force Geometry Modifier", KeyShortcut.TileForceGeometry, ImGuiKey.G, ImGuiKey.ModNone,
            allowedMods: ImGuiKey.ModShift
        );
        Register("Force Placement Modifier", KeyShortcut.TileForcePlacement, ImGuiKey.F, ImGuiKey.ModNone,
            allowedMods: ImGuiKey.ModShift
        );
        Register("Disallow Overwrite Modifier", KeyShortcut.TileIgnoreDifferent, ImGuiKey.R, ImGuiKey.ModNone,
            allowedMods: ImGuiKey.ModShift
        );

        // Light Editor
        Register("Reset Brush Transform", KeyShortcut.ResetBrushTransform, ImGuiKey.R, ImGuiKey.ModNone);
        Register("Move Light Inward", KeyShortcut.ZoomLightIn, ImGuiKey.W, ImGuiKey.ModShift);
        Register("Move Light Outward", KeyShortcut.ZoomLightOut, ImGuiKey.S, ImGuiKey.ModShift);
        Register("Rotate Light CW", KeyShortcut.RotateLightCW, ImGuiKey.D, ImGuiKey.ModShift);
        Register("Rotate Light CCW", KeyShortcut.RotateLightCCW, ImGuiKey.A, ImGuiKey.ModShift);
        Register("Mouse Scale Brush", KeyShortcut.ScaleLightBrush, ImGuiKey.Q, ImGuiKey.ModNone);
        Register("Mouse Rotate Brush", KeyShortcut.RotateLightBrush, ImGuiKey.E, ImGuiKey.ModNone);

        Register("Rotate Brush CW", KeyShortcut.RotateBrushCW, ImGuiKey.E, ImGuiKey.ModNone);
        Register("Rotate Brush CCW", KeyShortcut.RotateBrushCCW, ImGuiKey.Q, ImGuiKey.ModNone);
        Register("Previous Brush", KeyShortcut.PreviousBrush, ImGuiKey.Z, ImGuiKey.ModNone,
            allowRepeat: true
        );
        Register("Next Brush", KeyShortcut.NextBrush, ImGuiKey.X, ImGuiKey.ModNone,
            allowRepeat: true
        );

        Register("Lightmap Warp", KeyShortcut.LightmapStretch, ImGuiKey.None, ImGuiKey.ModNone);

        // Camera Editor
        Register("Camera Snap X", KeyShortcut.CameraSnapX, ImGuiKey.Q, ImGuiKey.ModNone);
        Register("Camera Snap Y", KeyShortcut.CameraSnapY, ImGuiKey.E, ImGuiKey.ModNone);

        // Prop Editor
        Register("Toggle Vertex Mode", KeyShortcut.ToggleVertexMode, ImGuiKey.F, ImGuiKey.ModNone);
        Register("Rope Simulation", KeyShortcut.RopeSimulation, ImGuiKey.Space, ImGuiKey.ModNone);
        Register("Rope Simulation Fast", KeyShortcut.RopeSimulationFast, ImGuiKey.Space, ImGuiKey.ModShift);
        Register("Reset Rope Simulation", KeyShortcut.ResetSimulation, ImGuiKey.None, ImGuiKey.ModNone);
        
        Register("Rotate Prop CW", KeyShortcut.RotatePropCW, ImGuiKey.E, ImGuiKey.ModShift);
        Register("Rotate Prop CCW", KeyShortcut.RotatePropCCW, ImGuiKey.Q, ImGuiKey.ModShift);

        Register("Change Prop Snapping", KeyShortcut.ChangePropSnapping, ImGuiKey.R, ImGuiKey.ModNone);

        // View options
        Register("View Grid", KeyShortcut.ToggleViewGrid, ImGuiKey.G, ImGuiKey.ModCtrl);
        Register("View Tiles", KeyShortcut.ToggleViewTiles, ImGuiKey.T, ImGuiKey.ModCtrl);
        Register("View Props", KeyShortcut.ToggleViewProps, ImGuiKey.P, ImGuiKey.ModCtrl);
        Register("View Camera Borders", KeyShortcut.ToggleViewCameras, ImGuiKey.M, ImGuiKey.ModCtrl);
        Register("View Tile Graphics", KeyShortcut.ToggleViewGraphics, ImGuiKey.T, ImGuiKey.ModCtrl | ImGuiKey.ModShift);
        Register("View Node Indices", KeyShortcut.ToggleViewNodeIndices, ImGuiKey.None, ImGuiKey.ModNone);
    }
}