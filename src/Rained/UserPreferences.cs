using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;
namespace RainEd;

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
        int combined = (R << 16) | (G << 8) | B;
        return "#" + combined.ToString("X6");
    }

    public readonly Color ToRGBA(byte alpha)
    {
        return new Color(R, G, B, alpha);
    }
}

class UserPreferences
{
    public bool ViewGrid { get; set; }
    public bool ViewObscuredBeams { get; set; }
    public bool ViewKeyboardShortcuts { get; set; }
    public bool ViewTileHeads { get; set; }

    public string GeometryViewMode { get; set; }
    public string PropSnap { get; set; }

    public bool ResizeShowScreenSize { get; set; }

    public bool WindowMaximized { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    public string Theme { get; set; }

    public HexColor LayerColor1;
    public HexColor LayerColor2;
    public HexColor LayerColor3;
    public HexColor BackgroundColor; 

    [JsonPropertyName("layerColor1")]
    public string LayerColor1String { get => LayerColor1.ToString(); set => LayerColor1 = new HexColor(value); }
    [JsonPropertyName("layerColor2")]
    public string LayerColor2String { get => LayerColor2.ToString(); set => LayerColor2 = new HexColor(value); }
    [JsonPropertyName("layerColor3")]
    public string LayerColor3String { get => LayerColor3.ToString(); set => LayerColor3 = new HexColor(value); }
    [JsonPropertyName("bgColor")]
    public string BackgroundColorString { get => BackgroundColor.ToString(); set => BackgroundColor = new HexColor(value); }

    public Dictionary<string, string> Shortcuts { get; set; }

    // default user preferences
    public UserPreferences()
    {
        ViewGrid = true;
        ViewObscuredBeams = false;
        ViewKeyboardShortcuts = true;
        ViewTileHeads = false;

        GeometryViewMode = "overlay";
        PropSnap = "0.5x";
        ResizeShowScreenSize = false;

        WindowMaximized = false;
        WindowWidth = Boot.DefaultWindowWidth;
        WindowHeight = Boot.DefaultWindowHeight;

        Theme = "dark";
        LayerColor1 = new HexColor("#000000");
        LayerColor2 = new HexColor("#59ff59");
        LayerColor3 = new HexColor("#ff1e1e");
        BackgroundColor = new HexColor(127, 127, 127);

        // initialize shortcuts
        Shortcuts = null!;
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
}