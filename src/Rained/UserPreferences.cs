using System.Text.Json;
namespace RainEd;

class UserPreferences
{
    public bool ViewGrid { get; set; }
    public bool ViewObscuredBeams { get; set; }
    public bool ViewKeyboardShortcuts { get; set; }
    public bool ViewTileHeads { get; set; }
    public bool ViewTiles { get; set; }

    public string GeometryViewMode { get; set; }
    public string PropSnap { get; set; }

    public bool ResizeShowScreenSize { get; set; }

    public bool WindowMaximized { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

    public string Theme { get; set; }

    // default user preferences
    public UserPreferences()
    {
        ViewGrid = true;
        ViewObscuredBeams = false;
        ViewKeyboardShortcuts = true;
        ViewTileHeads = false;
        ViewTiles = false;

        GeometryViewMode = "overlay";
        PropSnap = "0.5x";
        ResizeShowScreenSize = false;

        WindowMaximized = false;
        WindowWidth = Boot.DefaultWindowWidth;
        WindowHeight = Boot.DefaultWindowHeight;

        Theme = "dark";
    }

    public static void SaveToFile(UserPreferences prefs, string filePath)
    {
        // pascal case is a superstition
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
            WriteIndented = true
        };

        var jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<UserPreferences>(jsonString, serializeOptions)!;
    }
}