using System.Diagnostics;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using Rained.Assets;
using SixLabors.ImageSharp;

namespace Rained.Drizzle;

record MassRenderNotification;
record MassRenderBegan(int Total) : MassRenderNotification;
record MassRenderLevelProgress(string LevelName, float Progress) : MassRenderNotification;
record MassRenderLevelCompleted(string LevelName, bool Success) : MassRenderNotification;

/// <summary>
/// Rendering process for multiple levels.
/// </summary>
class DrizzleMassRender
{
    private readonly string[] levelPaths;
    private readonly int maxDegreeOfParallelism;

    public DrizzleMassRender(string[] levelPaths, int maxDegreeOfParallelism = 0)
    {
        this.levelPaths = levelPaths;
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    static void Shuffle<T>(T[] array, Random random)
    {
        var n = array.Length;
        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            (array[k], array[n]) =
                (array[n], array[k]);
        }
    }

    static LingoRuntime MakeZygoteRuntime()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = RainEd.Instance.AssetDataPath + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;

        var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
        runtime.Init();

        EditorRuntimeHelpers.RunStartup(runtime);

        return runtime;
    }

    public void Start(IProgress<MassRenderNotification>? progress, CancellationToken? cancel)
    {
        var zygote = DrizzleRender.StaticRuntime;
        if (zygote is null)
        {
            Log.Information("Initializing zygote runtime...");
            zygote = MakeZygoteRuntime();
        }

        cancel?.ThrowIfCancellationRequested();

        Log.Information("Starting render of {LevelCount} levels", levelPaths.Length);
        progress?.Report(new MassRenderBegan(levelPaths.Length));
        var sw = Stopwatch.StartNew();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism == 0 ? -1 : maxDegreeOfParallelism
        };

        Shuffle(levelPaths, new Random());

        var errors = 0;
        var successes = 0;
        var done = 0;

        Parallel.ForEach(levelPaths, s =>
        {
            cancel?.ThrowIfCancellationRequested();

            var runtime = zygote.Clone();
            var levelName = Path.GetFileNameWithoutExtension(s);
            var levelSw = Stopwatch.StartNew();
            var success = true;

            var movie = (MovieScript)runtime.MovieScriptInstance;

            try
            {
                progress?.Report(new MassRenderLevelProgress(levelName, 0f));
                EditorRuntimeHelpers.RunLoadLevel(runtime, s);

                var renderer = new LevelRenderer(runtime, null);

                if (cancel is not null || progress is not null)
                {
                    var cameraCount = (int)movie.gCameraProps.cameras.count;
                    renderer.StatusChanged += (status) =>
                    {
                        cancel?.ThrowIfCancellationRequested();
                        progress?.Report(new MassRenderLevelProgress(levelName, GetStatusProgress(status, cameraCount)));
                    };
                }

                renderer.DoRender();
            }
            catch (Exception e)
            {
                if (cancel is not null && cancel.Value.IsCancellationRequested)
                    return;
                
                Log.Error("{LevelName}: Exception while rendering!\n" + e, levelName);
                success = false;
            }

            Log.Information("{LevelName}: Render succeeded in {Elapsed}", levelName, levelSw.Elapsed);

            if (success)
            {
                Interlocked.Increment(ref successes);
            }
            else
            {
                Interlocked.Increment(ref errors);
            }

            var newDone = Interlocked.Increment(ref done);
            progress?.Report(new MassRenderLevelCompleted(levelName, success));
        });

        cancel?.ThrowIfCancellationRequested();

        Log.Information("Finished rendering in {Elapsed}. {Errors} errored, {Successes} succeeded", sw.Elapsed, errors, successes);
    }

    private static float GetStatusProgress(RenderStatus status, int cameraCount)
    {
        var stageEnum = status.Stage.Stage;

        // from 0 to 1
        float stageProgress = 0f;

        switch (status.Stage)
        {
            case RenderStageStatusLayers layers:
            {
                stageProgress = (3 - layers.CurrentLayer) / 3f;
                break;
            }

            case RenderStageStatusLight light:
            {
                stageProgress = light.CurrentLayer / 30f;
                break;
            }

            case RenderStageStatusEffects effects:
            {
                stageProgress = Math.Clamp((effects.CurrentEffect - 1f) / effects.EffectNames.Count, 0f, 1f);
                break;
            }
        }

        // send progress
        float renderProgress = status.CountCamerasDone * 10 + stageEnum switch
        {
            RenderStage.Start => 0f,
            RenderStage.CameraSetup => 0f,
            RenderStage.RenderLayers => 1f,
            RenderStage.RenderPropsPreEffects => 2f,
            RenderStage.RenderEffects => 3f,
            RenderStage.RenderPropsPostEffects => 4f,
            RenderStage.RenderLight => 5f,
            RenderStage.Finalize => 6f,
            RenderStage.RenderColors => 7f,
            RenderStage.Finished => 8f,
            RenderStage.SaveFile => 9f,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        return (renderProgress + Math.Clamp(stageProgress, 0f, 1f)) / (cameraCount * 10f);
    }
}