using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;
using ImGuiNET;
using Rained.EditorGui;
namespace Rained;

struct HexColor(byte r = 0, byte g = 0, byte b = 0)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;

    public HexColor(string hexString) : this(0, 0, 0)
    {
        if (hexString[0] != '#')
            throw new Exception("Hex string does not begin with a #");
        
        int color = int.Parse(hexString[1..], System.Globalization.NumberStyles.HexNumber);

        R = (byte)((color >> 16) & 0xFF);
        G = (byte)((color >> 8) & 0xFF);
        B = (byte)(color & 0xFF);
    }

    public readonly override string ToString()
    {
        uint combined = ((uint)R << 16) | ((uint)G << 8) | (uint)B;
        return "#" + combined.ToString("X6");
    }

    public readonly Color ToRaylibColor(byte alpha = 255)
    {
        return new Color(R, G, B, alpha);
    }

    public readonly Glib.Color ToGlibColor(float alpha = 1f)
    {
        return new Glib.Color(R / 255f, G / 255f, B / 255f, alpha);
    }

    public readonly System.Numerics.Vector3 ToVector3()
    {
        return new System.Numerics.Vector3(R / 255f, G / 255f, B / 255f);
    }
}

record struct HexColorRGBA(byte R = 0, byte G = 0, byte B = 0, byte A = 0)
{
    public HexColorRGBA(string hexString) : this(0, 0, 0, 0)
    {
        if (hexString[0] != '#')
            throw new Exception("Hex string does not begin with a #");
        
        // compatibility with normal HexColor
        int color = int.Parse(hexString[1..], System.Globalization.NumberStyles.HexNumber);
        if (hexString.Length == 7)
        {
            R = (byte)((color >> 16) & 0xFF);
            G = (byte)((color >> 8) & 0xFF);
            B = (byte)(color & 0xFF);
            A = 255;
        }
        else
        {
            R = (byte)((color >> 24) & 0xFF);
            G = (byte)((color >> 16) & 0xFF);
            B = (byte)((color >> 8) & 0xFF);
            A = (byte)(color & 0xFF);
        }
        

    }

    public readonly override string ToString()
    {
        uint combined = ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | (uint)A;
        return "#" + combined.ToString("X6");
    }

    public readonly Color ToRaylibColor()
    {
        return new Color(R, G, B, A);
    }

    public readonly Glib.Color ToGlibColor()
    {
        return new Glib.Color(R / 255f, G / 255f, B / 255f, A / 255f);
    }

    public readonly System.Numerics.Vector4 ToVector4()
    {
        return new System.Numerics.Vector4(R / 255f, G / 255f, B / 255f, A / 255f);
    }
}

class NodeViewFilterData
{
    public bool[] Flags;

    public NodeViewFilterData()
    {
        Flags = new bool[8];
        
        RoomExit = true;
        CreatureDen = true;
        RegionTransport = false;
        SideExit = false;
        SkyExit = false;
        SeaExit = false;
        Hives = false;
        GarbageHole = true;
    }
    
    public bool RoomExit
    {
        get => Flags[0];
        set => Flags[0] = value;
    }

    public bool CreatureDen
    {
        get => Flags[1];
        set => Flags[1] = value;
    }

    public bool RegionTransport
    {
        get => Flags[2];
        set => Flags[2] = value;
    }

    public bool SideExit
    {
        get => Flags[3];
        set => Flags[3] = value;
    }

    public bool SkyExit
    {
        get => Flags[4];
        set => Flags[4] = value;
    }

    public bool SeaExit
    {
        get => Flags[5];
        set => Flags[5] = value;
    }

    public bool Hives
    {
        get => Flags[6];
        set => Flags[6] = value;
    }

    public bool GarbageHole
    {
        get => Flags[7];
        set => Flags[7] = value;
    }
}

class UserPreferences
{
    public string DataPath { get; set; }

    public bool ViewGrid { get; set; }
    public bool ViewObscuredBeams { get; set; }
    public bool ViewKeyboardShortcuts { get; set; }
    public bool ViewTileHeads { get; set; }
    public bool ViewCameras { get; set; }
    public bool ViewTiles { get; set; }
    public bool ViewProps { get; set; }
    public bool ViewPreviews { get; set; }
    public bool ViewTileGraphicPreview { get; set; } = true;
    public bool ViewTileSpecPreview { get; set; } = true;
    public bool ViewTileSpecsOnTooltip { get; set; } = true;
    public bool ViewNodeIndices { get; set; } = false;
    public NodeViewFilterData NodeViewFilter { get; set; } = new();

    public string GeometryViewMode { get; set; }
    public bool GeometryMaskMouseDecor { get; set; } = true;
    public string PropSnap { get; set; }
    public bool ShowCameraNumbers { get; set; } = false;

    //public bool ResizeShowScreenSize { get; set; } // whoops, i set this to false - but now i want it true by default.
    public bool HideScreenSize { get; set; }
    public bool MinimalStatusBar { get; set; } = false;
    public bool HideEditorSwitch { get; set; } = false;

    public enum CameraBorderModeOption : int
    {
        Standard,
        Widescreen,
        Both
    };
    public CameraBorderModeOption CameraBorderMode;

    [JsonPropertyName("cameraBorderMode")]
    public string CameraBorderModeString {
        get => CameraBorderMode switch
        {
            CameraBorderModeOption.Standard => "standardBorder",
            CameraBorderModeOption.Widescreen => "widescreenBorder",
            CameraBorderModeOption.Both => "both",
            _ => throw new Exception("Invalid CameraBorderModeOption")
        };

        set
        {
            switch(value)
            {
                case "standardBorder":
                    CameraBorderMode = CameraBorderModeOption.Standard;
                    break;

                case "widescreenBorder":
                    CameraBorderMode = CameraBorderModeOption.Widescreen;
                    break;

                case "both":
                    CameraBorderMode = CameraBorderModeOption.Both;
                    break;

                default:
                    Log.Error("Invalid CameraBorderMode '{Value}'", value);
                    
                    CameraBorderMode = CameraBorderModeOption.Both;
                    break;
            }
        }
    }

    public bool WindowMaximized { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    public bool StaticDrizzleLingoRuntime { get; set; }
    public bool ShowRenderPreview { get; set; }
    public bool CheckForUpdates { get; set; }
    public bool OptimizedTilePreviews { get; set; }
    public bool MaterialSelectorPreview { get; set; } = true;

    public enum AutotileMouseModeOptions
    {
        Click, Hold
    }
    public AutotileMouseModeOptions AutotileMouseMode;

    [JsonPropertyName("autotileMouseMode")]
    public string AutotileMouseModeString {
        get => AutotileMouseMode switch
            {
                AutotileMouseModeOptions.Click => "click",
                AutotileMouseModeOptions.Hold => "hold",
                _ => throw new Exception("Invalid AutotileMouseMode option")
            };
        set
        {
            switch (value)
            {
                case "click":
                    AutotileMouseMode = AutotileMouseModeOptions.Click;
                    break;
                
                case "hold":
                    AutotileMouseMode = AutotileMouseModeOptions.Hold;
                    break;
                
                default:
                    Log.Error("Invalid CameraBorderMode '{value}'", value);
                    
                    AutotileMouseMode = AutotileMouseModeOptions.Hold;
                    break;
            }
        }
    }

    public enum PropSelectionLayerFilterOption
    {
        All, Current, InFront
    }

    public PropSelectionLayerFilterOption PropSelectionLayerFilter = PropSelectionLayerFilterOption.Current;

    [JsonPropertyName("propSelectionLayerFilter")]
    public string PropSelectionLayerFilterString {
        get => PropSelectionLayerFilter switch
        {
            PropSelectionLayerFilterOption.All => "all",
            PropSelectionLayerFilterOption.Current => "current",
            PropSelectionLayerFilterOption.InFront => "inFront",
            _ => throw new Exception("Invalid PropSelectionLayerFilterOption")
        };
        set
        {
            switch (value)
            {
                case "all":
                    PropSelectionLayerFilter = PropSelectionLayerFilterOption.All;
                    break;
                
                case "current":
                    PropSelectionLayerFilter = PropSelectionLayerFilterOption.Current;
                    break;

                case "inFront":
                    PropSelectionLayerFilter = PropSelectionLayerFilterOption.InFront;
                    break;
                
                default:
                    Log.Error("Invalid 'propSelecitonLayerFilter' option");
                    break;
            }
        }
    }

    public bool DoubleClickToCreateProp { get; set; } = false;
    public bool TilePlacementModeToggle { get; set; } = false;
    public bool RemoveCameraAngleLimit { get; set; } = false;

    public enum LightEditorControlSchemeOption
    {
        Mouse, Keyboard
    };
    public LightEditorControlSchemeOption LightEditorControlScheme = LightEditorControlSchemeOption.Mouse;

    [JsonPropertyName("lightEditorControlScheme")]
    public string LightEditorControlSchemeString
    {
        get => LightEditorControlScheme switch
        {
            LightEditorControlSchemeOption.Mouse => "mouse",
            LightEditorControlSchemeOption.Keyboard => "keyboard",
            _ => throw new Exception("Invalid LightEditorControlScheme")
        };
        set
        {
            switch (value)
            {
                case "mouse":
                    LightEditorControlScheme = LightEditorControlSchemeOption.Mouse;
                    break;

                case "keyboard":
                    LightEditorControlScheme = LightEditorControlSchemeOption.Keyboard;
                    break;

                default:
                    Log.Error("Invalid 'lightEditorControlScheme' option");
                    break;
            }
        }
    }

    public bool ShowPaletteWindow { get; set; }
    public bool UsePalette { get; set; }
    public int PaletteIndex { get; set; }
    public int PaletteFadeIndex { get; set; } = 0;
    public float PaletteFade { get; set; } = 0f;

    public HexColor LayerColor1;
    public HexColor LayerColor2;
    public HexColor LayerColor3;
    public HexColor BackgroundColor;
    public HexColorRGBA TileSpec1;
    public HexColorRGBA TileSpec2;
    
    [JsonPropertyName("layerColor1")]
    public string LayerColor1String { get => LayerColor1.ToString(); set => LayerColor1 = new HexColor(value); }
    [JsonPropertyName("layerColor2")]
    public string LayerColor2String { get => LayerColor2.ToString(); set => LayerColor2 = new HexColor(value); }
    [JsonPropertyName("layerColor3")]
    public string LayerColor3String { get => LayerColor3.ToString(); set => LayerColor3 = new HexColor(value); }
    [JsonPropertyName("bgColor")]
    public string BackgroundColorString { get => BackgroundColor.ToString(); set => BackgroundColor = new HexColor(value); }
    [JsonPropertyName("tileSpec1")]
    public string TileSpec1String { get => TileSpec1.ToString(); set => TileSpec1 = new HexColorRGBA(value); }
    [JsonPropertyName("tileSpec2")]
    public string TileSpec2String { get => TileSpec2.ToString(); set => TileSpec2 = new HexColorRGBA(value); }

    public string Theme { get; set; }
    public string Font { get; set; }
    public int FontSize { get; set; } = 13;
    public float ContentScale { get; set; }
    public bool ImGuiMultiViewport { get; set; }
    public bool Vsync { get; set; } = false;
    public int RefreshRate { get; set; } = 0;

    public Dictionary<string, string> Shortcuts { get; set; }
    public uint MaxRecentFiles { get; set; } = 20;
    public List<string> RecentFiles { get; set; }

    // default user preferences
    public UserPreferences()
    {
        DataPath = Path.Combine(Boot.AppDataPath, "Data");
        
        ViewGrid = true;
        ViewObscuredBeams = false;
        ViewKeyboardShortcuts = true;
        ViewTileHeads = false;
        ViewCameras = false;
        ViewTiles = false;
        ViewProps = false;
        ViewPreviews = false;

        GeometryViewMode = "overlay";
        PropSnap = "0.5x";
        HideScreenSize = false;
        CameraBorderMode = CameraBorderModeOption.Both;

        WindowMaximized = false;
        WindowWidth = Boot.DefaultWindowWidth;
        WindowHeight = Boot.DefaultWindowHeight;

        StaticDrizzleLingoRuntime = false;
        ShowRenderPreview = true;
        CheckForUpdates = true;
        AutotileMouseMode = AutotileMouseModeOptions.Hold;
        OptimizedTilePreviews = true;

        ContentScale = Boot.Window is null ? 1.0f : Boot.WindowScale;
        Font = (ContentScale == 1.0f) ? "ProggyClean" : "ProggyVector-Regular";
        Theme = "Dark";
        if (Boot.Window is not null)
        {
            if (Boot.Window.Theme == Glib.WindowTheme.Light)
                Theme = "Light";
            else if (Boot.Window.Theme == Glib.WindowTheme.Dark)
                Theme = "Dark";
        }

        ImGuiMultiViewport = false;
        ShowPaletteWindow = false;
        UsePalette = false;
        PaletteIndex = 0;
        LayerColor1 = new HexColor("#000000");
        LayerColor2 = new HexColor("#59ff59");
        LayerColor3 = new HexColor("#ff1e1e");
        BackgroundColor = new HexColor(127, 127, 127);
        TileSpec1 = new HexColorRGBA("#99FF5B");
        TileSpec2 = new HexColorRGBA("#61A338");

        RecentFiles = [];

        // initialize shortcuts
        Shortcuts = null!;

        if (RainEd.Instance is not null)
            SaveKeyboardShortcuts();
    }

    public static void SaveToFile(UserPreferences prefs, string filePath)
    {
        // pascal case is a superstition
        prefs.SaveKeyboardShortcuts();
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // why does it escape '+'
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(prefs, serializeOptions);
        File.WriteAllText(filePath, jsonString);
    }

    public static UserPreferences LoadFromFile(string filePath)
    {
        // PASCAL CASE IS A SOCIAL CONSTRUCT
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // why does it escape '+'?
            WriteIndented = true
        };

        var jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<UserPreferences>(jsonString, serializeOptions)!;
    }

    public void LoadKeyboardShortcuts()
    {
        foreach ((string key, string shortcut) in Shortcuts)
        {
            // PASCAL CASE IS A NAZI INVENTION
            var enumName = char.ToUpperInvariant(key[0]) + key[1..];
            KeyShortcut enumShortcut = Enum.Parse<KeyShortcut>(enumName);

            KeyShortcuts.Rebind(enumShortcut, shortcut);
        }
    }

    public void SaveKeyboardShortcuts()
    {
        Shortcuts = [];
        for (int i = 0; i < (int) KeyShortcut.COUNT; i++)
        {
            var shortcut = (KeyShortcut)i;

            // PASCAL CASE WAS CREATED BY COMMUNISTS
            var srcString = shortcut.ToString();
            var key = char.ToLowerInvariant(srcString[0]) + srcString[1..];

            Shortcuts[key] = KeyShortcuts.GetShortcutString(shortcut);
        }
    }

    public void ApplyTheme()
    {
        try
        {
            var dir = Path.Combine(Boot.AppDataPath, "config", "themes");
            var filePath = Path.Combine(dir, Theme + ".jsonc");
            if (!File.Exists(filePath)) filePath = Path.Combine(dir, Theme + ".json");

            var style = SerializableStyle.FromFile(filePath);
            style!.Apply(ImGuiExt.Style);
        }
        catch (Exception e)
        {
            Log.Error("Could not apply theme!\n{Error}", e);
            EditorWindow.ShowNotification("Could not apply theme");
        }
    }
}