using System;
using System.IO;
using System.Reflection;

namespace Drizzle.Lingo.Runtime;

public partial class LingoRuntime
{
    public static readonly string MovieBasePath;

    static LingoRuntime()
    {
        /*
#if FULL_RELEASE
        var path = Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", "Data");
#else
        var path = Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", "..", "..", "..", "..", "Data");
#endif
        */
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        MovieBasePath = Path.GetFullPath(path) + Path.DirectorySeparatorChar;
        CastPath = Path.Combine(MovieBasePath, "Cast");
    }

    public string GetFilePath(string relPath)
    {
        var fullPath = Path.Combine(MovieBasePath, relPath);
        if (File.Exists(fullPath))
            return fullPath;

        // Try case sensitive compare.
        var dir = Path.GetDirectoryName(fullPath);
        if (dir == null || !Directory.Exists(dir))
            return fullPath;

        var origFileName = Path.GetFileName(fullPath.AsSpan());
        foreach (var dirFile in Directory.EnumerateFiles(dir))
        {
            var dirFileName = Path.GetFileName(dirFile.AsSpan());
            if (origFileName.Equals(dirFileName, StringComparison.InvariantCultureIgnoreCase))
                return dirFile;
        }

        return fullPath;
    }
}
