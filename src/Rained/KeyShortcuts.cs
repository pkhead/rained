using ImGuiNET;
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

    SelectEditor,

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
        public ImGuiModFlags Mods;
        public ImGuiModFlags AllowedMods;
        public bool IsActivated = false;
        public bool IsDeactivated = false;
        public bool IsDown = false;
        public bool AllowRepeat = false;

        public readonly ImGuiKey OriginalKey;
        public readonly ImGuiModFlags OriginalMods;

        public KeyShortcutBinding(
            string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods,
            bool allowRepeat = false,
            ImGuiModFlags allowedMods = ImGuiModFlags.None
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

            if (Mods.HasFlag(ImGuiModFlags.Ctrl))
                str.Add(CtrlName);
            
            if (Mods.HasFlag(ImGuiModFlags.Shift))
                str.Add(ShiftName);
            
            if (Mods.HasFlag(ImGuiModFlags.Alt))
                str.Add(AltName);
            
            if (Mods.HasFlag(ImGuiModFlags.Super))
                str.Add(SuperName);
            
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
            CheckModKey(ImGuiModFlags.Ctrl, ImGuiKey.ModCtrl) &&
            CheckModKey(ImGuiModFlags.Shift, ImGuiKey.ModShift) &&
            CheckModKey(ImGuiModFlags.Alt, ImGuiKey.ModAlt) &&
            CheckModKey(ImGuiModFlags.Super, ImGuiKey.ModSuper);
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
            CheckModKey(ImGuiModFlags.Ctrl, ImGuiKey.ModCtrl) &&
            CheckModKey(ImGuiModFlags.Shift, ImGuiKey.ModShift) &&
            CheckModKey(ImGuiModFlags.Alt, ImGuiKey.ModAlt) &&
            CheckModKey(ImGuiModFlags.Super, ImGuiKey.ModSuper);
        }

        private bool CheckModKey(ImGuiModFlags mod, ImGuiKey key) {
            var down = ImGui.IsKeyDown(key);
            return (Mods.HasFlag(mod) == down) || (down && AllowedMods.HasFlag(mod));
        }
    }
    private static readonly Dictionary<KeyShortcut, KeyShortcutBinding> keyShortcuts = [];


    private static void Register(
        string name, KeyShortcut id, ImGuiKey key, ImGuiModFlags mods,
        bool allowRepeat = false,
        ImGuiModFlags allowedMods = ImGuiModFlags.None
    )
    {
        keyShortcuts.Add(id, new KeyShortcutBinding(name, id, key, mods, allowRepeat, allowedMods));
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
            
            if (modStr == CtrlName)
                mods |= ImGuiModFlags.Ctrl;
            else if (modStr == AltName)
                mods |= ImGuiModFlags.Alt;
            else if (modStr == ShiftName)
                mods |= ImGuiModFlags.Shift;
            else if (modStr == SuperName)
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
        Register("Right Mouse Substitute", KeyShortcut.RightMouse, ImGuiKey.None, ImGuiModFlags.None,
            allowedMods: ImGuiModFlags.Ctrl | ImGuiModFlags.Shift | ImGuiModFlags.Alt | ImGuiModFlags.Super
        );

        Register("Environment Editor", KeyShortcut.EnvironmentEditor, ImGuiKey._1, ImGuiModFlags.None);
        Register("Geometry Editor", KeyShortcut.GeometryEditor, ImGuiKey._2, ImGuiModFlags.None);
        Register("Tile Editor", KeyShortcut.TileEditor, ImGuiKey._3, ImGuiModFlags.None);
        Register("Camera Editor", KeyShortcut.CameraEditor, ImGuiKey._4, ImGuiModFlags.None);
        Register("Light Editor", KeyShortcut.LightEditor, ImGuiKey._5, ImGuiModFlags.None);
        Register("Effects Editor", KeyShortcut.EffectsEditor, ImGuiKey._6, ImGuiModFlags.None);
        Register("Prop Editor", KeyShortcut.PropEditor, ImGuiKey._7, ImGuiModFlags.None);

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
        Register("Close", KeyShortcut.CloseFile, ImGuiKey.W, ImGuiModFlags.Ctrl);
        Register("Close All", KeyShortcut.CloseAllFiles, ImGuiKey.W, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);

        Register("Render", KeyShortcut.Render, ImGuiKey.R, ImGuiModFlags.Ctrl);
        Register("Export Geometry", KeyShortcut.ExportGeometry, ImGuiKey.R, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);

        Register("Cut", KeyShortcut.Cut, ImGuiKey.X, ImGuiModFlags.Ctrl);
        Register("Copy", KeyShortcut.Copy, ImGuiKey.C, ImGuiModFlags.Ctrl);
        Register("Paste", KeyShortcut.Paste, ImGuiKey.V, ImGuiModFlags.Ctrl);
        Register("Select", KeyShortcut.Select, ImGuiKey.E, ImGuiModFlags.Ctrl);
        Register("Undo", KeyShortcut.Undo, ImGuiKey.Z, ImGuiModFlags.Ctrl, true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Y, ImGuiModFlags.Ctrl, true);
        else
            Register("Redo", KeyShortcut.Redo, ImGuiKey.Z, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift, true);

        Register("Mode Select", KeyShortcut.SelectEditor, ImGuiKey.GraveAccent, ImGuiModFlags.None);
        
        Register("Cycle Layer", KeyShortcut.SwitchLayer, ImGuiKey.Tab, ImGuiModFlags.None);
        Register("Switch Tab", KeyShortcut.SwitchTab, ImGuiKey.Tab, ImGuiModFlags.Shift);
        Register("Increase Brush Size", KeyShortcut.IncreaseBrushSize, ImGuiKey.O, ImGuiModFlags.None, true);
        Register("Decrease Brush Size", KeyShortcut.DecreaseBrushSize, ImGuiKey.I, ImGuiModFlags.None, true);

        // Geometry
        Register("Toggle Layer 1", KeyShortcut.ToggleLayer1, ImGuiKey.E, ImGuiModFlags.None);
        Register("Toggle Layer 2", KeyShortcut.ToggleLayer2, ImGuiKey.R, ImGuiModFlags.None);
        Register("Toggle Layer 3", KeyShortcut.ToggleLayer3, ImGuiKey.T, ImGuiModFlags.None);
        Register("Toggle Mirror X", KeyShortcut.ToggleMirrorX, ImGuiKey.F, ImGuiModFlags.None, false);
        Register("Toggle Mirror Y", KeyShortcut.ToggleMirrorY, ImGuiKey.G, ImGuiModFlags.None, false);
        Register("Flood Fill Modifier", KeyShortcut.FloodFill, ImGuiKey.Q, ImGuiModFlags.None, false);
        Register("Wall Tool", KeyShortcut.ToolWall, ImGuiKey.Z, ImGuiModFlags.None, false);
        Register("Shortcut Entrance Tool", KeyShortcut.ToolShortcutEntrance, ImGuiKey.X, ImGuiModFlags.None);
        Register("Shortcut Dot Tool", KeyShortcut.ToolShortcutDot, ImGuiKey.C, ImGuiModFlags.None);

        // Tile Editor
        Register("Eyedropper", KeyShortcut.Eyedropper, ImGuiKey.Q, ImGuiModFlags.None, true);
        Register("Set Material to Default", KeyShortcut.SetMaterial, ImGuiKey.E, ImGuiModFlags.None, true);
        Register("Force Geometry Modifier", KeyShortcut.TileForceGeometry, ImGuiKey.G, ImGuiModFlags.None,
            allowedMods: ImGuiModFlags.Shift
        );
        Register("Force Placement Modifier", KeyShortcut.TileForcePlacement, ImGuiKey.F, ImGuiModFlags.None,
            allowedMods: ImGuiModFlags.Shift
        );
        Register("Disallow Overwrite Modifier", KeyShortcut.TileIgnoreDifferent, ImGuiKey.R, ImGuiModFlags.None,
            allowedMods: ImGuiModFlags.Shift
        );

        // Light Editor
        Register("Reset Brush Transform", KeyShortcut.ResetBrushTransform, ImGuiKey.R, ImGuiModFlags.None);
        Register("Move Light Inward", KeyShortcut.ZoomLightIn, ImGuiKey.W, ImGuiModFlags.Shift);
        Register("Move Light Outward", KeyShortcut.ZoomLightOut, ImGuiKey.S, ImGuiModFlags.Shift);
        Register("Rotate Light CW", KeyShortcut.RotateLightCW, ImGuiKey.D, ImGuiModFlags.Shift);
        Register("Rotate Light CCW", KeyShortcut.RotateLightCCW, ImGuiKey.A, ImGuiModFlags.Shift);
        Register("Mouse Scale Brush", KeyShortcut.ScaleLightBrush, ImGuiKey.Q, ImGuiModFlags.None);
        Register("Mouse Rotate Brush", KeyShortcut.RotateLightBrush, ImGuiKey.E, ImGuiModFlags.None);

        Register("Rotate Brush CW", KeyShortcut.RotateBrushCW, ImGuiKey.E, ImGuiModFlags.None);
        Register("Rotate Brush CCW", KeyShortcut.RotateBrushCCW, ImGuiKey.Q, ImGuiModFlags.None);
        Register("Previous Brush", KeyShortcut.PreviousBrush, ImGuiKey.Z, ImGuiModFlags.None,
            allowRepeat: true
        );
        Register("Next Brush", KeyShortcut.NextBrush, ImGuiKey.X, ImGuiModFlags.None,
            allowRepeat: true
        );

        Register("Lightmap Warp", KeyShortcut.LightmapStretch, ImGuiKey.None, ImGuiModFlags.None);

        // Camera Editor
        Register("Camera Snap X", KeyShortcut.CameraSnapX, ImGuiKey.Q, ImGuiModFlags.None);
        Register("Camera Snap Y", KeyShortcut.CameraSnapY, ImGuiKey.E, ImGuiModFlags.None);

        // Prop Editor
        Register("Toggle Vertex Mode", KeyShortcut.ToggleVertexMode, ImGuiKey.F, ImGuiModFlags.None);
        Register("Rope Simulation", KeyShortcut.RopeSimulation, ImGuiKey.Space, ImGuiModFlags.None);
        Register("Rope Simulation Fast", KeyShortcut.RopeSimulationFast, ImGuiKey.Space, ImGuiModFlags.Shift);
        Register("Reset Rope Simulation", KeyShortcut.ResetSimulation, ImGuiKey.None, ImGuiModFlags.None);
        
        Register("Rotate Prop CW", KeyShortcut.RotatePropCW, ImGuiKey.E, ImGuiModFlags.Shift);
        Register("Rotate Prop CCW", KeyShortcut.RotatePropCCW, ImGuiKey.Q, ImGuiModFlags.Shift);

        Register("Change Prop Snapping", KeyShortcut.ChangePropSnapping, ImGuiKey.R, ImGuiModFlags.None);

        // View options
        Register("View Grid", KeyShortcut.ToggleViewGrid, ImGuiKey.G, ImGuiModFlags.Ctrl);
        Register("View Tiles", KeyShortcut.ToggleViewTiles, ImGuiKey.T, ImGuiModFlags.Ctrl);
        Register("View Props", KeyShortcut.ToggleViewProps, ImGuiKey.P, ImGuiModFlags.Ctrl);
        Register("View Camera Borders", KeyShortcut.ToggleViewCameras, ImGuiKey.M, ImGuiModFlags.Ctrl);
        Register("View Tile Graphics", KeyShortcut.ToggleViewGraphics, ImGuiKey.T, ImGuiModFlags.Ctrl | ImGuiModFlags.Shift);
        Register("View Node Indices", KeyShortcut.ToggleViewNodeIndices, ImGuiKey.None, ImGuiModFlags.None);
    }
}