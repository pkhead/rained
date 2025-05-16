using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        LingoRuntime.MovieBasePath = AssetDataPath.GetPath() + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;

        var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
        runtime.Init();

        EditorRuntimeHelpers.RunStartup(runtime);

        return runtime;
    }

    static TaskScheduler? _customScheduler = null;

    public void Start(IProgress<MassRenderNotification>? progress, CancellationToken? cancel)
    {
        Directory.CreateDirectory(Path.Combine(AssetDataPath.GetPath(), "Levels"));
        
        var zygote = DrizzleRender.StaticRuntime;
        if (zygote is null)
        {
            Log.Information("Initializing zygote runtime...");
            zygote = MakeZygoteRuntime();
        }

        cancel?.ThrowIfCancellationRequested();

        Log.Information("Starting render of {LevelCount} levels", levelPaths.Length);
        Log.Information("Parallelism: {ThreadCount}", maxDegreeOfParallelism == 0 ? "unlimited" : maxDegreeOfParallelism);
        progress?.Report(new MassRenderBegan(levelPaths.Length));
        var sw = Stopwatch.StartNew();

        TaskScheduler? scheduler = null;
        if (OperatingSystem.IsMacOS())
        {
            // 1 MiB of stack space
            scheduler = _customScheduler ??= new StackSizeTaskScheduler(64, 1024 * 1024);
        }

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism == 0 ? -1 : maxDegreeOfParallelism,
            TaskScheduler = scheduler
        };

        Shuffle(levelPaths, new Random());

        var errors = 0;
        var successes = 0;
        var done = 0;

        Parallel.ForEach(levelPaths, parallelOptions, s =>
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

    record LevelStat(string name, int status, TimeSpan elapsed);

    public static int ConsoleRender(string[] paths, int maxConcurrency)
    {
        int exitCode = 0;

        var files = new List<string>(paths.Length);
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                foreach (var f in Directory.EnumerateFiles(path, "*.txt"))
                    files.Add(f);
            }
            else
            {
                files.Add(path);
            }
        }

        var massRender = new Drizzle.DrizzleMassRender([..files], maxConcurrency);

        // setup progress handler
        Dictionary<string, Stopwatch> levelStopwatches = [];
        var masterStopwatch = new Stopwatch();
        masterStopwatch.Start();
        
        List<LevelStat> levelStats = [];
        object lockDummy = new();

        var prog = new Progress<Drizzle.MassRenderNotification>();
        prog.ProgressChanged += (object? sender, Drizzle.MassRenderNotification prog) =>
        {
            switch (prog)
            {
                case MassRenderBegan began:
                    Console.WriteLine($"Starting render of {began.Total} levels...");
                    break;

                case MassRenderLevelCompleted level:
                {
                    // Console.Write(level.LevelName);
                    lock (lockDummy)
                    {
                        var stopwatch = levelStopwatches[level.LevelName];
                        stopwatch.Stop();
                        levelStopwatches.Remove(level.LevelName);

                        levelStats.Add(new LevelStat(level.LevelName, level.Success ? 0 : 1, stopwatch.Elapsed));
                    }

                    break;
                }

                case MassRenderLevelProgress level:
                    lock (lockDummy)
                    {
                        if (level.Progress == 0)
                        {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();
                            levelStopwatches[level.LevelName] = stopwatch;
                        }
                    }
                    break;
            }
        };

        // setup cancel handler
        var cancelSource = new CancellationTokenSource();

        Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
        {
            e.Cancel = true;
            cancelSource.Cancel();
        };

        // start!!
        Console.WriteLine("Initializing zygote runtime...");
        try
        {
            massRender.Start(prog, cancelSource.Token);
        }
        catch (OperationCanceledException)
        {
            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (!levelStats.Any(x => x.name == name))
                {
                    levelStats.Add(new LevelStat(name, 2, new TimeSpan(0)));
                }
            }
        }

        masterStopwatch.Stop();
        var elapsed = masterStopwatch.Elapsed;
        Console.Write($"Finished in {elapsed.Minutes}:");
        Console.Write(string.Format("{0:00.00}", elapsed.TotalSeconds));
        Console.WriteLine();

        levelStats.Sort(static (LevelStat a, LevelStat b) =>
        {
            return a.status.CompareTo(b.status);
        });

        var failed = 0;
        var succ = 0;
        var canceled = 0;
        foreach (var stat in levelStats)
        {
            int len;
            if (stat.name.Length > 16)
            {
                Console.Write(stat.name.Substring(0, 13));
                Console.Write("...");
                len = 16;
            }
            else
            {
                Console.Write(stat.name);
                len = stat.name.Length;
            }
            Console.Write(":");
            len++;

            // pad with spaces
            for (int i = len; i < 20; i++)
                Console.Write(" ");

            if (stat.status == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("success    ");
                Console.ResetColor();
                succ++;
            }
            else if (stat.status == 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("fail       ");
                Console.ResetColor();
                failed++;
                exitCode = 1;
            }
            else if (stat.status == 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("not started");
                Console.ResetColor();
                canceled++;
                exitCode = 1;
            }

            // print elapsed time
            var dt = stat.elapsed;
            
            Console.Write(" (");
            Console.Write(string.Format("{0:00}:{1:00.0000}", dt.Minutes, dt.TotalSeconds));
            Console.WriteLine(")");
        }
        Console.WriteLine();

        Console.Write($"{failed} failed, {succ} succeeded");
        if (canceled > 0)
        {
            Console.WriteLine($", {canceled} canceled.");
        }
        else
        {
            Console.WriteLine(".");
        }

        return exitCode;
    }
}

// task scheduler to use for custom stack size
// (fucking MacOS)
class StackSizeTaskScheduler : TaskScheduler
{
    [ThreadStatic]
    private static bool _currentThreadIsProcessingItems;

    private readonly LinkedList<Task> _tasks = new LinkedList<Task>();
    // private readonly LinkedList<Thread> _freeThreads = new LinkedList<Thread>();
    private readonly int _maxThreads;
    private readonly int _stackSize;

    // private readonly LinkedList<Thread> _threads = [];
    private int _activeThreads = 0;

    private object lockDummy = new();

    public StackSizeTaskScheduler(int maxThreads, int stackSize)
    {
        if (maxThreads < 1) throw new ArgumentOutOfRangeException(nameof(maxThreads));
        _maxThreads = maxThreads;
        _stackSize = stackSize;
    }

    protected sealed override void QueueTask(Task task)
    {
        Thread? newThread = null;

        lock (_tasks)
        {
            _tasks.AddLast(task);

            if (_activeThreads < _maxThreads)
            {
                newThread = new Thread(ThreadStart, _stackSize);
                Interlocked.Increment(ref _activeThreads);
                // _threads.AddLast(thread);
            }
        }

        newThread?.Start();
    }

    private void ThreadStart()
    {
        _currentThreadIsProcessingItems = true;

        try
        {
            while (true)
            {
                Task? task = null;
                lock (_tasks)
                {
                    if (_tasks.First is not null)
                    {
                        task = _tasks.First.Value;
                        _tasks.RemoveFirst();
                    }
                }

                if (task is not null)
                {
                    TryExecuteTask(task);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeThreads);
            _currentThreadIsProcessingItems = false;
        }
    }

    protected override bool TryDequeue(Task task)
    {
        lock (_tasks) return _tasks.Remove(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // If this thread isn't already processing a task, we don't support inlining
       if (!_currentThreadIsProcessingItems) return false;

       // If the task was previously queued, remove it from the queue
       if (taskWasPreviouslyQueued)
          // Try to run the task.
          if (TryDequeue(task))
            return base.TryExecuteTask(task);
          else
             return false;
       else
          return base.TryExecuteTask(task);
        // throw new NotImplementedException();
    }

    public sealed override int MaximumConcurrencyLevel => _maxThreads;

    // Gets an enumerable of the tasks currently scheduled on this scheduler.
   protected sealed override IEnumerable<Task> GetScheduledTasks()
   {
       bool lockTaken = false;
       try
       {
           Monitor.TryEnter(_tasks, ref lockTaken);
           if (lockTaken) return _tasks;
           else throw new NotSupportedException();
       }
       finally
       {
           if (lockTaken) Monitor.Exit(_tasks);
       }
   }
}