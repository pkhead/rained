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
/*zygote thread */ record MassRenderBegin(int Total) : MassRenderNotification;
/*zygote thread */ record MassRenderBeginLevel(string LevelFile) : MassRenderNotification;
/*process thread*/ record MassRenderLevelProgress(string LevelFile, float Progress) : MassRenderNotification;
/*zygote thread */ record MassRenderLevelCompleted(string LevelFile, bool Success, int[] Cameras, Exception? Error) : MassRenderNotification;

/// <summary>
/// Rendering process for multiple levels.
/// </summary>
class DrizzleMassRender : IDisposable
{
    private readonly string[] levelPaths;
    // private readonly int maxDegreeOfParallelism;

    private readonly int _maxThreads;
    private readonly int _threadStackSize = DrizzleManager.UseCustomStackSize ? DrizzleManager.ThreadStackSize : 0;
    private int _levelsCompleted = 0;
    private int _levelIndex = 0;

    private readonly LinkedList<LingoRuntime> _runtimes = []; // available runtime clones
    private readonly LinkedList<Thread> _activeThreads = [];
    private readonly LinkedList<MassRenderLevelCompleted> _levelCompletions = [];
    private readonly Queue<(Thread thread, LevelRenderParameters @params)> _levelQueue;

    private readonly IProgress<MassRenderNotification>? _progress;
    private readonly CancellationToken? _cancel;

    private readonly AutoResetEvent _waitHandle = new(false);

    public DrizzleMassRender(string[] levelPaths, int maxDegreeOfParallelism = 0, IProgress<MassRenderNotification>? progress = null, CancellationToken? cancel = null)
    {
        this.levelPaths = levelPaths;
        _progress = progress;
        _cancel = cancel;

        _maxThreads = levelPaths.Length;
        if (maxDegreeOfParallelism > 0 && _maxThreads > maxDegreeOfParallelism)
            _maxThreads = maxDegreeOfParallelism;

        _levelQueue = new(_maxThreads);
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

    // public void AppStart(IProgress<MassRenderNotification>? progress, CancellationToken? cancel)
    // {
    //     var useStatic = RainEd.Instance.Preferences.StaticDrizzleLingoRuntime;
    //     if (DrizzleManager.NeedsCreateRuntime(useStatic))
    //         Log.UserLogger.Information("Initializing Drizzle...");
    //     var zygote = DrizzleManager.GetRuntime(useStatic);

    //     ProcessStart(zygote, progress, cancel);
    // }

    class LevelRenderParameters
    {
        public required LingoRuntime runtime;
        public required string levelPath;
    }

    public void Dispose()
    {
        _waitHandle.Dispose();
    }

    public void StartUp(LingoRuntime zygote)
    {
        Shuffle(levelPaths, new Random());

        Parallel.For(0, _maxThreads, (i, s) =>
        {
            var clone = zygote.Clone();
            lock (_runtimes) _runtimes.AddLast(clone);
            _cancel?.ThrowIfCancellationRequested();
        });
        _cancel?.ThrowIfCancellationRequested();

        _progress?.Report(new MassRenderBegin(levelPaths.Length));
    }

    private void ThreadProc(object? paramObj)
    {
        var param = (LevelRenderParameters) paramObj!;
        var runtime = param.runtime;
        List<int> finishedScreens = [];

        try
        {   
            _cancel?.ThrowIfCancellationRequested();

            var levelSw = Stopwatch.StartNew();

            var movie = (MovieScript)runtime.MovieScriptInstance;

            _progress?.Report(new MassRenderLevelProgress(param.levelPath, 0f));

            using var tmpDir = DrizzleManager.ConvertToDrizzle(param.levelPath, out var levelTxt);
            EditorRuntimeHelpers.RunLoadLevel(runtime, levelTxt);

            var renderer = new LevelRenderer(runtime, null);

            renderer.OnScreenRenderCompleted += (int index, LingoImage img) =>
            {
                finishedScreens.Add(index);
            };

            if (_cancel is not null || _progress is not null)
            {
                var cameraCount = (int)movie.gCameraProps.cameras.count;
                renderer.StatusChanged += (status) =>
                {
                    _cancel?.ThrowIfCancellationRequested();
                    _progress?.Report(new MassRenderLevelProgress(param.levelPath, DrizzleUtil.GetStatusProgress(status, cameraCount)));
                };
            }

            renderer.DoRender();

            Log.Information("{LevelName}: Render successfully in {Elapsed}", Path.GetFileNameWithoutExtension(param.levelPath), levelSw.Elapsed);
            _cancel?.ThrowIfCancellationRequested();

            lock (_levelCompletions) _levelCompletions.AddLast(new MassRenderLevelCompleted(param.levelPath, true, [..finishedScreens], null));
        }
        catch (OperationCanceledException)
        {
            HandleCancel();
        }
        catch (Exception e)
        {
            // when cancelling inside StatusChanged, it gets wrapped in a Drizzle.RenderCameraException camera
            if (e.InnerException is OperationCanceledException)
                HandleCancel();
            else
                lock (_levelCompletions) _levelCompletions.AddLast(new MassRenderLevelCompleted(param.levelPath, false, [..finishedScreens], e));
        }
        finally
        {
            lock (runtime) _runtimes.AddLast(runtime);
            lock (_activeThreads) _activeThreads.Remove(Thread.CurrentThread);

            _waitHandle.Set();
        }

        void HandleCancel()
        {
            lock (_levelCompletions) _levelCompletions.AddLast(new MassRenderLevelCompleted(param.levelPath, false, [..finishedScreens], null));
        }
    }

    /// <summary>
    /// Blocks thread until action needs to be taken.
    /// </summary>
    public void Wait()
    {
        _waitHandle.WaitOne();
    }

    private bool ProcessCancel()
    {
        MassRenderLevelCompleted[] statuses;
        lock (_levelCompletions)
        {
            statuses = [.._levelCompletions];
            _levelCompletions.Clear();
        }

        foreach (var status in statuses)
        {
            _levelsCompleted++;
            _progress?.Report(status);
        }

        int runtimeCount;
        lock (_runtimes) runtimeCount = _runtimes.Count;

        if (runtimeCount == _maxThreads) return false;
        return true;
    }

    /// <summary>
    /// If true, continue processing. If false, it is done.
    /// </summary>
    public bool ProcessUpdate()
    {
        if (_cancel is not null && _cancel.Value.IsCancellationRequested)
        {
            return ProcessCancel();
        }

        while (true)
        {
            if (_levelsCompleted == levelPaths.Length)
            {
                return false;
            }

            // ran out of threads, wait until one has finished
            if (_runtimes.Count == 0) return true;

            // process completed threads
            MassRenderLevelCompleted[] statuses;
            lock (_levelCompletions)
            {
                statuses = [.._levelCompletions];
                _levelCompletions.Clear();
            }

            foreach (var status in statuses)
            {
                _levelsCompleted++;
                _progress?.Report(status);
            }

            // queue new threads
            if (_levelIndex < levelPaths.Length)
            {
                _levelQueue.Clear();
                lock (_runtimes)
                {
                    for (; _levelIndex < levelPaths.Length; _levelIndex++)
                    {
                        if (_runtimes.First is null) break;
                        var r = _runtimes.First.Value;
                        _runtimes.RemoveFirst();

                        var thread = new Thread(new ParameterizedThreadStart(ThreadProc), _threadStackSize);
                        _levelQueue.Enqueue((thread, new LevelRenderParameters
                        {
                            runtime = r,
                            levelPath = levelPaths[_levelIndex]
                        }));
                    }
                }

                foreach (var (thread, @params) in _levelQueue)
                {
                    _progress?.Report(new MassRenderBeginLevel(@params.levelPath));
                    lock (_activeThreads) _activeThreads.AddLast(thread);
                    thread.Start(@params);
                }
            }
        }
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

        // setup progress handler
        Dictionary<string, Stopwatch> levelStopwatches = [];
        var masterStopwatch = new Stopwatch();
        masterStopwatch.Start();
        
        List<LevelStat> levelStats = [];
        object lockDummy = new();

        var prog = new ProgressNoSync<MassRenderNotification>();
        prog.ProgressChanged += (object? sender, MassRenderNotification prog) =>
        {
            switch (prog)
            {
                case MassRenderBeginLevel level:
                {
                    LuaScripting.Modules.RainedModule.PreRenderCallback(level.LevelFile);
                    levelStopwatches[level.LevelFile] = Stopwatch.StartNew();
                    break;
                }

                case MassRenderLevelCompleted level:
                {
                    var levelsPath = Path.Combine(AssetDataPath.GetPath(), "Levels") + Path.DirectorySeparatorChar;
                    var levelName = Path.GetFileNameWithoutExtension(level.LevelFile);

                    LuaScripting.Modules.RainedModule.PostRenderCallback(
                        sourceTxt: level.LevelFile,
                        dstTxt: Path.Combine(AssetDataPath.GetPath(), "Levels", levelName + ".txt"),
                        dstPngs: level.Cameras.Select(x => levelsPath + $"{levelName}_{x}.png").ToArray()
                    );

                    lock (lockDummy)
                    {
                        var stopwatch = levelStopwatches[level.LevelFile];
                        stopwatch.Stop();
                        levelStopwatches.Remove(level.LevelFile);

                        levelStats.Add(new LevelStat(levelName, level.Success ? 0 : (level.Error is not null ? 1 : 2), stopwatch.Elapsed));
                    }

                    break;
                }
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
        try
        {
            Directory.CreateDirectory(Path.Combine(AssetDataPath.GetPath(), "Levels"));

            using var massRender = new Drizzle.DrizzleMassRender([..files], maxConcurrency, prog, cancelSource.Token);

            Console.WriteLine("Initializing zygote runtime...");
            var zygote = DrizzleManager.GetRuntime(false);
            cancelSource.Token.ThrowIfCancellationRequested();
            massRender.StartUp(zygote);

            Console.WriteLine($"Starting render of {files.Count} levels...");
            
            while (massRender.ProcessUpdate())
                massRender.Wait();

            cancelSource.Token.ThrowIfCancellationRequested();
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
                Console.Write("canceled   ");
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

    private readonly LinkedList<Task> _tasks = [];
    // private readonly LinkedList<Thread> _freeThreads = new LinkedList<Thread>();
    private readonly int _maxThreads;
    private readonly int _stackSize;

    // private readonly LinkedList<Thread> _threads = [];
    private int _activeThreads = 0;

    private readonly object lockDummy = new();

    public StackSizeTaskScheduler(int maxThreads, int stackSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxThreads, 1);
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