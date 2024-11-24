using System.Diagnostics.CodeAnalysis;
namespace Rained.Assets;

/// <summary>
/// Stores the database of Drizzle cast libraries, read from the reading the drizzle-cast directory.
/// </summary>
static class DrizzleCast
{
    private static readonly Dictionary<string, string> _filenameMap = [];
    private static bool _init = false;

    public static readonly string DirectoryPath = Path.Combine(Boot.AppDataPath, "assets", "drizzle-cast");

    public static void Initialize()
    {
        if (_init) return;
        _init = true;

        foreach (var filePath in Directory.GetFiles(DirectoryPath))
        {
            var filename = Path.GetFileName(filePath);
            var libIdSep = filename.IndexOf('_', 0);
            var idNameSep = filename.IndexOf('_', libIdSep + 1);

            _filenameMap.TryAdd(filename[(idNameSep+1)..], filePath);
        }
    }

    /// <summary>
    /// Scans all registered cast libraries to obtain the full file path of
    /// the member with the same name.
    /// </summary>
    /// <param name="key">The member name to query for.</param>
    /// <param name="value">The file path of the member, if it exists.</param>
    /// <returns>True if a member with the given name exists, false if not.</returns>
    public static bool GetFileName(string memberName, [NotNullWhen(true)] out string? filePath)
    {
        return _filenameMap.TryGetValue(memberName, out filePath);
    }
}