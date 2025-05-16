namespace Rained.Assets;

/// <summary>
/// Handler for the config directory, with a fallback to the assets directory
/// if a given path does not exist.
/// </summary>
static class ConfigDirectory
{
    /// <summary>
    /// Correct the path to a file in the config folder, or fall-back to the assets
    /// directory if not found.
    /// </summary>
    /// <param name="relativePath">The path, relative to the config folder.</param>
    /// <returns>The file in the config folder if present, otherwise the file in the assets folder.</returns>
    public static string GetFilePath(string relativePath)
    {
        string configPath = Path.Combine(Boot.ConfigPath, relativePath);
        if (File.Exists(configPath))
        {
            return configPath;
        }

        return Path.Combine(Boot.AppDataPath, "assets", relativePath);
    }

    private static (string configPath, string assetsPath) ParseRelativePath(string? relativePath = null)
    {
        string configPath, assetsPath;
        if (relativePath is not null)
        {
            configPath = Path.Combine(Boot.ConfigPath, relativePath);
            assetsPath = Path.Combine(Boot.AppDataPath, "assets", relativePath);
        }
        else
        {
            configPath = Boot.ConfigPath;
            assetsPath = Boot.AppDataPath;
        }

        return (configPath, assetsPath);
    }

    private static bool FileFilter(string configPath, string assetsPath, string x) => !File.Exists(Path.Combine(configPath, Path.GetRelativePath(assetsPath, x)));

    public static IEnumerable<string> EnumerateFiles(string? relativePath = null)
    {
        var (configPath, assetsPath) = ParseRelativePath(relativePath);
        if (!Directory.Exists(configPath)) return Directory.EnumerateFiles(assetsPath);
        return Directory.EnumerateFiles(configPath).Concat(
            Directory.EnumerateFiles(assetsPath).Where(x => FileFilter(configPath, assetsPath, x))
        );
    }

    public static IEnumerable<string> EnumerateFiles(string? relativePath, string searchPattern)
    {
        var (configPath, assetsPath) = ParseRelativePath(relativePath);
        if (!Directory.Exists(configPath)) return Directory.EnumerateFiles(assetsPath, searchPattern);
        return Directory.EnumerateFiles(configPath, searchPattern).Concat(
            Directory.EnumerateFiles(assetsPath, searchPattern).Where(
                x => FileFilter(configPath, assetsPath, x)
            )
        );
    }

    public static string[] GetFiles(string? relativePath = null)
    {
        return [..EnumerateFiles(relativePath)];
    }
    
    public static string[] GetFiles(string? relativePath, string searchPattern)
    {
        return [..EnumerateFiles(relativePath, searchPattern)];
    }

    // public static IEnumerable<string> EnumerateDirectories(string? relativePath = null)
    // {
    //     var (configPath, assetsPath) = ParseRelativePath(relativePath);
    //     return Directory.EnumerateDirectories(configPath).Concat(
    //         Directory.EnumerateDirectories(assetsPath).Where(
    //             x => !File.Exists(Path.Combine(configPath, Path.GetRelativePath(assetsPath, x))
    //         ))
    //     );
    // }

    // public static IEnumerable<string> EnumerateFileSystemEntries(string? relativePath = null)
    // {
    //     var (configPath, assetsPath) = ParseRelativePath(relativePath);
    //     return Directory.EnumerateFileSystemEntries(configPath).Concat(
    //         Directory.EnumerateFileSystemEntries(assetsPath).Where(
    //             x => !File.Exists(Path.Combine(configPath, Path.GetRelativePath(assetsPath, x))
    //         ))
    //     );
    // }
}