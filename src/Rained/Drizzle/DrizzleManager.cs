using System.Diagnostics;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Ported;
using Rained.Assets;
namespace Rained.Drizzle;

/// <summary>
/// Class used to handle the creation of Drizzle instances.
/// </summary>
static class DrizzleManager
{
    /// <summary>
    /// Stack size that should be used when UseCustomStackSize is true.
    /// </summary>
    public static readonly int ThreadStackSize = 1024 * 1024 * 2; // 2 MiB

    /// <summary>
    /// If this is true, threads that run Drizzle code should use a max stack size of ThreadStackSize.
    /// </summary>
    public static bool UseCustomStackSize => OperatingSystem.IsMacOS();

    private static LingoRuntime? _staticRuntime;
    public static LingoRuntime? StaticRuntime => _staticRuntime;

    // fix editorConfig.txt, because if invalid the startUp function
    // will unintentionally abort early, since the movie score is not
    // implemented in Drizzle.
    private static void FixEditorConfig(string dataPath)
    {
        if (!DrizzleCast.GetFileName("baseConfig.txt", out string? baseConfigPath))
            throw new Exception("baseConfig.txt not found in cast");

        // create file it does not exist
        var editorConfigPath = Path.Combine(dataPath, "editorConfig.txt");
        if (!File.Exists(editorConfigPath))
        {
            File.WriteAllText(editorConfigPath, File.ReadAllText(baseConfigPath));
            return;
        }

        // version check
        var editorConfigLines = File.ReadAllLines(editorConfigPath);
        var baseConfigLines = File.ReadAllLines(baseConfigPath);

        // if version check failed, correct the file
        if (editorConfigLines[0] != baseConfigLines[0])
        {
            // I also want to keep whatever options the user had
            // so first, I need to read what options existed in the old file
            Dictionary<string, string> userOptions = [];
            for (int i = 1; i < editorConfigLines.Length; i++)
            {
                var line = editorConfigLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var idx = line.IndexOf(" : ");
                if (idx == -1)
                {
                    Log.UserLogger?.Warning("Line {LineNumber} of editorConfig.txt was invalid: {Line}", i+1, line);
                    continue;
                }

                var optionName = line[..idx];
                userOptions[optionName] = line;
            }

            string[] newLines = new string[baseConfigLines.Length];
            newLines[0] = baseConfigLines[0];

            for (int i = 1; i < baseConfigLines.Length; i++)
            {
                var baseLine = baseConfigLines[i];
                var idx = baseLine.IndexOf(" : ");
                if (idx == -1)
                {
                    Log.UserLogger?.Warning("Line {LineNumber} of cast baseConfig.txt was invalid: {Line}", i+1, baseLine);
                    newLines[i] = baseLine;
                    continue;
                }

                var optionName = baseLine[..idx];

                if (userOptions.TryGetValue(optionName, out var origLine))
                {
                    newLines[i] = origLine;
                }
                else
                {
                    newLines[i] = baseLine;
                }
            }

            // write output to file
            File.WriteAllLines(editorConfigPath, newLines);
        }
    }

    private static LingoRuntime CreateRuntime()
    {
        var dataPath = AssetDataPath.GetPath();

        // create large trash log file, in case user decided to have it enabled
        // otherwise drizzle will not work
        var largeTrashLogFile = Path.Combine(dataPath, "largeTrashLog.txt");
        if (!File.Exists(largeTrashLogFile))
            File.Create(largeTrashLogFile).Dispose();

        // ensure output directory exists
        Directory.CreateDirectory(Path.Combine(dataPath, "Levels"));

        SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = dataPath + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;

        var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
        runtime.Init();

        FixEditorConfig(LingoRuntime.MovieBasePath);
        EditorRuntimeHelpers.RunStartup(runtime);

        return runtime;
    }

    public static bool NeedsCreateRuntime(bool useStatic)
    {
        if (useStatic)
            return _staticRuntime is null;
        else
            return true;
    }

    /// <summary>
    /// Obtain either the static runtime or a newly created runtime.
    /// If useStatic is true and this is the first ever call to GetRuntime, this function will
    /// need to create the runtime.
    /// <param name="useStatic">True if it should use the static runtime. False if it should use a newly created one.</param>
    /// </summary>
    public static LingoRuntime GetRuntime(bool useStatic)
    {
        if (useStatic)
            return _staticRuntime ??= CreateRuntime();
        else
            return CreateRuntime();
    }

    /// <summary>
    /// Dispose the static runtime, if it was created.
    /// </summary>
    public static void DisposeStaticRuntime()
    {
        _staticRuntime = null;
    }
}