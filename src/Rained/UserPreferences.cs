using System.Text.Encodings.Web;
using System.Text.Json;
namespace RainEd;

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