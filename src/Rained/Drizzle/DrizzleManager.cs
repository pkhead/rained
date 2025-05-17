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

    private static LingoRuntime CreateRuntime()
    {
        SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = AssetDataPath.GetPath() + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;

        var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
        runtime.Init();
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