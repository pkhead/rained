namespace Rained.Assets;

/// <summary>
/// Class used for obtaining the asset data path without the need for loading the editor.
/// </summary>
class AssetDataPath
{
    private static string? _cachedDataPath = null;

    public static string GetPath()
    {
        if (RainEd.Instance is not null)
        {
            return RainEd.Instance.AssetDataPath;
        }

        if (_cachedDataPath is not null)
        {
            return _cachedDataPath;
        }

        var prefFilePath = Path.Combine(Boot.AppDataPath, "config", "preferences.json");
        string dataPath;

        if (Boot.Options.DrizzleDataPath is not null)
        {
            dataPath = Boot.Options.DrizzleDataPath;
        }
        else
        {
            // read preferences in order to get the data directory
            if (File.Exists(prefFilePath))
            {
                var prefs = UserPreferences.LoadFromFile(prefFilePath);
                dataPath = prefs.DataPath;
            }
            else
            {
                throw new FileNotFoundException("preferences.json was not found");
            }
        }

        if (!Directory.Exists(dataPath))
        {
            throw new DirectoryNotFoundException($"The data directory {dataPath} does not exist.");
        }

        _cachedDataPath = dataPath;
        return dataPath;
    }
}