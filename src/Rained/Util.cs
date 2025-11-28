namespace Rained;

/// <summary>
/// Provides generalized functions where I don't know a good class to put it in.
/// </summary>
static class Util
{
    public static int Mod(int a, int b)
        => (a%b + b)%b;

    public static float Mod(float a, float b)
        => (a%b + b)%b;
    
    public static float Rad2Deg(float rad)
        => rad / MathF.PI * 180f;
    
    public static float Deg2Rad(float deg)
        => deg / 180f * MathF.PI;
    private static bool? _cachedFsCaseSensitive;

    /// <summary>
    /// <para>
    /// Check if the file system is case sensitive. On the first query, it performs
    /// a test with the filesystem, then caches the result for later calls of this function.
    /// </para>
    /// 
    /// <para>
    /// This doesn't simply check the OS that the app is running on because it is possible
    /// that the user configured the OS to treat files in a case-sensitive manner.
    /// Although on Windows the user can set case sensitivity on a per-partition basis,
    /// meaning that I have to re-check for each drive... Dangit.
    /// </para>
    /// </summary>
    public static bool IsFileSystemCaseSensitive {
        get
        {
            if (_cachedFsCaseSensitive is null)
            {
                var tmpDir = Path.GetTempPath();
                var createdFileName = Path.Combine(tmpDir, "rained_casesensitivitytest.txt");
                File.Create(createdFileName).Dispose();
                _cachedFsCaseSensitive = !File.Exists(Path.Combine(tmpDir, "rainED_CaseSensitivityTest.TXT"));
                File.Delete(createdFileName);
            }

            return _cachedFsCaseSensitive.Value;
        }
    }

    public static bool ArePathsEquivalent(string? pathA, string? pathB)
    {
        if (pathA is null && pathB is null) return true;
        if (pathA is null || pathB is null) return false;

        pathA = Path.GetFullPath(pathA);
        pathB = Path.GetFullPath(pathB);

        if (OperatingSystem.IsWindows()) // windows is a little quirky...
        {
            pathA = pathA.Replace('/', '\\');
            pathB = pathB.Replace('/', '\\');
        }

        if (pathA[^1] == Path.DirectorySeparatorChar)
            pathA = pathA[..^1];

        if (pathB[^1] == Path.DirectorySeparatorChar)
            pathB = pathB[..^1];

        if (IsFileSystemCaseSensitive)
            return pathA.Equals(pathB, StringComparison.Ordinal);
        else
            return pathA.Equals(pathB, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The user agent to use for HTTP requests.
    /// </summary>
    public static string HttpUserAgent;

    static Util()
    {
        var os = Environment.OSVersion.ToString();
        var clr = Environment.Version.ToString();
        HttpUserAgent = $"Mozilla/4.0 (compatible; MSIE 6.0; {os}; .NET CLR {clr};)";
    }
}