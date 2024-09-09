using System.Reflection;
using System.Text;

namespace RainEd;

/// <summary>
/// Handles saving/loading of properties in the editorConfig.txt file
/// </summary>
class DrizzleConfiguration
{
    [AttributeUsage(AttributeTargets.Property)]
    class StringName(string Name) : Attribute
    {
        public string Name = Name;
    }

    /*
    Rain World Community Editor; V.0.4.31; Editor configuration file
    More tile previews : TRUE
    Grime on gradients : FALSE
    Grime : TRUE
    Exit button : DROUGHT
    Exit render button : DROUGHT
    Material fixes : TRUE
    Camera editor border fix : TRUE
    Slime always affects editor decals : FALSE
    voxelStructRandomDisplace for tiles as props : TRUE
    notTrashProp fix : TRUE
    Show controls : TRUE
    Trash and Small pipes non solid : FALSE
    Gradients with BackgroundScenes fix : TRUE
    Invisible material fix : TRUE
    Tiles as props fixes : TRUE
    Large trash debug log : FALSE
    Rough Rock spreads more : FALSE
    Dark Slime fix : TRUE
    */

    public readonly string FilePath;

    // only a select options, because the other ones don't affect rendering
    [StringName("Grime on gradients")]
    public bool GrimeOnGradients { get; set; } = false;

    [StringName("Grime")]
    public bool Grime { get; set; } = true;

    [StringName("Material fixes")]
    public bool MaterialFixes { get; set; } = true;

    [StringName("Slime always affects editor decals")]
    public bool SlimeAlwaysAffectsEditorDecals { get; set; } = false;

    [StringName("notTrashProp fix")]
    public bool NotTrashPropFix { get; set; } = true;

    [StringName("Trash and Small pipes non solid")]
    public bool TrashAndSmallPipesNonSolid { get; set; } = false;

    [StringName("Gradients with BackgroundScenes fix")]
    public bool GradientsWithBackgroundScenesFix { get; set; } = true;

    [StringName("Invisible material fix")]
    public bool InvisibleMaterialFix { get; set; } = true;

    [StringName("Tiles as props fix")]
    public bool TilesAsPropsFixes { get; set; } = true;

    [StringName("Large trash debug log")]
    public bool LargeTrashDebugLog { get; set; } = false;

    [StringName("Rough Rock spreads more")]
    public bool RoughRockSpreadsMore { get; set; } = false;

    [StringName("Dark Slime fix")]
    public bool DarkSlimeFix { get; set; } = true;

    private static PropertyInfo? GetPropertyByKey(string key)
    {
        foreach (var prop in typeof(DrizzleConfiguration).GetProperties())
        {
            var attr = prop.GetCustomAttribute<StringName>();
            if (attr is not null && attr.Name.Equals(key, StringComparison.InvariantCultureIgnoreCase))
            {
                return prop;
            }
        }

        return null;
    }

    /// <summary>
    /// Try to set the value of an option by its name in the editorConfig.txt file.
    /// </summary>
    /// <param name="pref">The name of the option.</param>
    /// <param name="value">The new value of the option.</param>
    /// <returns>True if the option was recognized, false if not.</returns>
    public bool TrySetConfig(string pref, bool value)
    {
        var prop = GetPropertyByKey(pref);
        if (prop is null) return false;
        prop.SetValue(this, value);
        return true;
    }

    /// <summary>
    /// Try to get the value of an option by its name in the editorConfig.txt file
    /// </summary>
    /// <param name="pref">The name of the option.</param>
    /// <returns>Null if the option is not recognized.</returns>
    public bool? TryGetConfig(string pref)
    {
        var prop = GetPropertyByKey(pref);
        if (prop is null) return null;
        return (bool) prop.GetValue(this)!;
    }

    /// <summary>
    /// Get the value of an option by its name in the editorConfig.txt file
    /// </summary>
    /// <param name="pref">The name of the option.</param>
    /// <returns>The value of the option.</returns>
    public bool GetConfig(string pref)
    {
        var prop = GetPropertyByKey(pref) ?? throw new ArgumentException($"The given option {pref} was not recognized", nameof(pref));
        return (bool) prop.GetValue(this)!;
    }

    public DrizzleConfiguration(string filePath)
    {
        FilePath = filePath;
        if (!LoadPreferences())
        {
            throw new Exception("Invalid header!");
        }
    }

    /// <summary>
    /// Load a configuration file, creating a new one if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The file path for the configuration file.</param>
    public static DrizzleConfiguration LoadConfiguration(string filePath)
    {
        if (!File.Exists(filePath))
        {
            if (DrizzleCast.GetFileName("baseConfig.txt", out string? baseConfigPath))
            {
                var baseConfig = File.ReadAllBytes(baseConfigPath);
                File.WriteAllBytes(filePath, baseConfig);
            }
            else
            {
                throw new Exception("Could not find file path for cast member 'baseConfig.txt'");
            }
        }

        return new DrizzleConfiguration(filePath);
    }

    private bool LoadPreferences()
    {
        var fileLines = File.ReadAllLines(FilePath);

        // process first line, which should be the header
        // we are ignoring validating the version number
        // if the header doesn't match, we don't process the file
        var headerInfo = fileLines[0].Split(';');
        if (headerInfo.Length != 3 || headerInfo[0] != "Rain World Community Editor" || headerInfo[2] != " Editor configuration file")
            return false;
        
        // process the options
        for (int i = 1; i < fileLines.Length; i++)
        {
            var line = fileLines[i];
            var lineInfo = line.Split(" : ");
            if (lineInfo.Length != 2) continue;

            if (lineInfo[1] == "TRUE")
                TrySetConfig(lineInfo[0], true);
            else if (lineInfo[1] == "FALSE")
                TrySetConfig(lineInfo[0], false);
        }

        return true;
    }

    public void SavePreferences()
    {
        var fileLines = new List<string>(File.ReadAllLines(FilePath));

        // process first line, which should be the header
        // we are ignoring validating the version number
        // if the header doesn't match, we don't process the file
        var headerInfo = fileLines[0].Split(';');
        if (headerInfo.Length != 3 || headerInfo[0] != "Rain World Community Editor" || headerInfo[2] != " Editor configuration file")
            return;
        
        // set options in the position they already were in the file
        List<string> processedOptions = [];
        for (int i = 1; i < fileLines.Count; i++)
        {
            var line = fileLines[i];
            var lineInfo = line.Split(" : ");
            if (lineInfo.Length != 2) continue;

            bool? value = TryGetConfig(lineInfo[0]);
            if (value is not null)
            {
                processedOptions.Add(lineInfo[0]);
                fileLines[i] = lineInfo[0] + " : " + (value.Value ? "TRUE" : "FALSE");
            }
        }

        // add options if they were not already in the file
        foreach (var prop in typeof(DrizzleConfiguration).GetProperties())
        {
            var attr = prop.GetCustomAttribute<StringName>();
            if (attr is null || processedOptions.Contains(attr.Name)) continue;
            fileLines.Add(attr.Name + " : " + ((bool)prop.GetValue(this)! ? "TRUE" : "FALSE"));
        }

        // write to file
        File.WriteAllText(FilePath, string.Join("\r\n", fileLines));
    }
}