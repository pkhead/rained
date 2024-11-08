using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using SixLabors.ImageSharp;
using Rained.Assets;
namespace Rained.Drizzle;

enum RenderPreviewStage 
{
    Setup,
    Props,
    Effects,
    Lights,
}

[Serializable]
public class DrizzleRenderException : Exception
{
    public DrizzleRenderException() { }
    public DrizzleRenderException(string message) : base(message) { }
    public DrizzleRenderException(string message, System.Exception inner) : base(message, inner) { }
}

enum PixelFormat
{
    /// <summary>
    /// BGRA format, 32 bits per pixel
    /// </summary>
    Bgra32,

    /// <summary>
    /// Luminance, 1 bit per pixel
    /// </summary>
    L1
}

/// <summary>
/// An image for a render sublayer.
/// </summary> 
class RenderImage : IDisposable
{
    public readonly int Width;
    public readonly int Height;
    public byte[]? Pixels;
    public readonly PixelFormat Format;
    
    public RenderImage(int width, int height, PixelFormat format)
    {
        Width = width;
        Height = height;
        Format = format;
        Pixels = null;

        /*Pixels = format switch
        {
            PixelFormat.Bgra32 => new byte[width * height * 4],
            PixelFormat.L1 => new byte[(int)Math.Ceiling(width * height / 8.0)],
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        Array.Clear(Pixels);*/
    }

    // dispose does nothing, but use it anyway.
    public void Dispose()
    {}
}

/// <summary>
/// The collection of render preview images
/// </summary>
class RenderPreviewImages : IDisposable
{
    public RenderImage[] Layers;
    
    // this is used for the effects stage
    public RenderImage? BlackOut1 = null;
    public RenderImage? BlackOut2 = null;

    public bool RenderBlackOut1 = false;
    public bool RenderBlackOut2 = false;

    public RenderPreviewStage Stage;

    private int lastWidth = -1;
    private int lastHeight = -1;
    private PixelFormat oldPixelFormat;

    public RenderPreviewImages()
    {
        Stage = RenderPreviewStage.Setup;
        Layers = new RenderImage[30];
        oldPixelFormat = PixelFormat.Bgra32;
    }

    public void SetSize(int newWidth, int newHeight, PixelFormat format)
    {
        if (lastWidth == newWidth && lastHeight == newHeight && format == oldPixelFormat)
            return;

        lastWidth = newWidth;
        lastHeight = newHeight;
        oldPixelFormat = format;

        Log.Debug("Resize/reformat images");

        for (int i = 0; i < 30; i++)
        {
            Layers[i]?.Dispose();
            Layers[i] = new RenderImage(newWidth, newHeight, format);
        }
        
        if (BlackOut1 is not null)
        {
            BlackOut1.Dispose();
            BlackOut1 = new RenderImage(newWidth, newHeight, PixelFormat.Bgra32);
            //BlackOut1.Format(PixelFormat.UncompressedGrayscale);
        }

        if (BlackOut2 is not null)
        {
            BlackOut2.Dispose();
            BlackOut2 = new RenderImage(newWidth, newHeight, PixelFormat.Bgra32);
            //BlackOut2.Format(PixelFormat.UncompressedGrayscale);
        }
    }

    public void EnableBlackOut()
    {
        BlackOut1 ??= new RenderImage(lastWidth, lastHeight, PixelFormat.Bgra32);
        BlackOut2 ??= new RenderImage(lastWidth, lastHeight, PixelFormat.Bgra32);
    }

    public void DisableBlackOut()
    {
        BlackOut1?.Dispose();
        BlackOut2?.Dispose();
        BlackOut1 = null;
        BlackOut2 = null;
    }

    public void Dispose()
    {
        for (int i = 0; i < 30; i++)
        {
            Layers[i]?.Dispose();
        }

        BlackOut1?.Dispose();
        BlackOut2?.Dispose();
    }
}

/// <summary>
/// Rendering process for a single level.
/// </summary>
class DrizzleRender : IDisposable
{
    private abstract record ThreadMessage;

    private record MessageRenderStarted : ThreadMessage;
    private record MessageRenderGeometryStarted : ThreadMessage;
    private record MessageLevelLoading : ThreadMessage;
    private record MessageRenderFailed(Exception Exception) : ThreadMessage;
    private record MessageRenderCancelled : ThreadMessage;
    private record MessageRenderFinished : ThreadMessage;
    private record MessageRenderProgress(float Percentage) : ThreadMessage;
    private record MessageDoCancel : ThreadMessage;
    private record MessageReceievePreview(RenderPreview Preview) : ThreadMessage;

    private static LingoRuntime? staticRuntime = null; 
    public static LingoRuntime? StaticRuntime => staticRuntime;

    private class RenderThread
    {
        public ConcurrentQueue<ThreadMessage> Queue;
        public ConcurrentQueue<ThreadMessage> InQueue;

        public string filePath;
        public LevelRenderer? Renderer;
        public Action<RenderStatus>? StatusChanged = null;

        public readonly bool GeometryExport;
        public readonly int PrioritizedCameraIndex;

        public RenderThread(string filePath, bool geoExport, int prioCam)
        {
            GeometryExport = geoExport;
            PrioritizedCameraIndex = prioCam;

            Queue = new ConcurrentQueue<ThreadMessage>();
            InQueue = new ConcurrentQueue<ThreadMessage>();
            this.filePath = filePath;
        }

        public void ThreadProc()
        {
            try
            {
                LingoRuntime runtime;

                // create large trash log file, in case user decided to have it enabled
                // otherwise drizzle will not work
                var largeTrashLogFile = Path.Combine(RainEd.Instance.AssetDataPath, "largeTrashLog.txt");
                if (!File.Exists(largeTrashLogFile))
                    File.Create(largeTrashLogFile).Dispose();

                if (staticRuntime is not null)
                {
                    runtime = staticRuntime;
                }
                else
                {
                    Log.Information("Initializing Lingo runtime...");

                    LingoRuntime.MovieBasePath = RainEd.Instance.AssetDataPath + Path.DirectorySeparatorChar;
                    LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;
                    
                    runtime = new LingoRuntime(typeof(MovieScript).Assembly);
                    runtime.Init();
                    EditorRuntimeHelpers.RunStartup(runtime);
                }

                // process user cancel if cancelled while init
                // drizzle runtime
                if (InQueue.TryDequeue(out ThreadMessage? msg))
                {
                    if (msg is MessageDoCancel)
                        throw new RenderCancelledException();
                }

                Queue.Enqueue(new MessageLevelLoading());
                Log.Information("RENDER: Loading {LevelName}", Path.GetFileNameWithoutExtension(filePath));
                
                EditorRuntimeHelpers.RunLoadLevel(runtime, filePath);

                var movie = (MovieScript)runtime.MovieScriptInstance;
                movie.gPrioCam = PrioritizedCameraIndex + 1;

                if (GeometryExport)
                {
                    // process user cancel if cancelled while init
                    // drizzle runtime
                    if (InQueue.TryDequeue(out msg))
                    {
                        if (msg is MessageDoCancel)
                            throw new RenderCancelledException();
                    }
                    Queue.Enqueue(new MessageRenderGeometryStarted());

                    Log.Information("RENDER: Exporting Geometry...");
                    movie.newmakelevel(movie.gLoadedName);
                }
                else
                {
                    Renderer = new LevelRenderer(runtime, null);
                    Renderer.StatusChanged += StatusChanged;
                    Renderer.PreviewSnapshot += PreviewSnapshot;
                    
                    // process user cancel if cancelled while init
                    // drizzle runtime
                    if (InQueue.TryDequeue(out msg))
                    {
                        if (msg is MessageDoCancel)
                            throw new RenderCancelledException();
                    }
                    Queue.Enqueue(new MessageRenderStarted());

                    Log.Information("RENDER: Begin");
                    Renderer.DoRender();
                }

                Log.Information("Render successful!");
                Queue.Enqueue(new MessageRenderFinished());
            }
            catch (RenderCancelledException)
            {
                Log.Information("Render was cancelled");
                Queue.Enqueue(new MessageRenderCancelled());
            }
            catch (Exception e)
            {
                Queue.Enqueue(new MessageRenderFailed(e));
            }
        }

        private void PreviewSnapshot(RenderPreview renderPreview)
        {
            Queue.Enqueue(new MessageReceievePreview(renderPreview));
        }
    }

    private readonly RenderThread threadState;
    private readonly Thread thread;

    public enum RenderState
    {
        Initializing,
        Loading,
        Rendering,
        GeometryExport,
        Finished,
        Cancelling,
        Canceled,
        Errored
    };

    private RenderState state;
    private RenderStage currentStage;
    private float progress = 0.0f;
    public RenderState State { get => state; }
    public RenderStage Stage { get => currentStage; }
    public float RenderProgress { get => progress; }
    public bool IsDone { get => state == RenderState.Canceled || state == RenderState.Finished; }

    private readonly int cameraCount;
    private int camsDone = 0;

    public string DisplayString = string.Empty;
    public int CameraCount { get => cameraCount; }
    public int CamerasDone { get => camsDone; }

    public RenderPreviewImages? PreviewImages;
    public Action? PreviewUpdated;

    public readonly bool OnlyGeometry;

    public DrizzleRender(bool geoExport, int prioCam = -1)
    {
        OnlyGeometry = geoExport;

        cameraCount = RainEd.Instance.Level.Cameras.Count;

        state = RenderState.Initializing;
        var filePath = RainEd.Instance.CurrentFilePath;
        if (string.IsNullOrEmpty(filePath)) throw new Exception("Render called but level wasn't saved");

        // create render layer preview images
        if (RainEd.Instance.Preferences.ShowRenderPreview)
        {
            PreviewImages = new RenderPreviewImages();
        }
        
        threadState = new RenderThread(filePath, OnlyGeometry, prioCam);
        threadState.StatusChanged += StatusChanged;
        Configuration.Default.PreferContiguousImageBuffers = true;
        thread = new Thread(new ThreadStart(threadState.ThreadProc))
        {
            CurrentCulture = Thread.CurrentThread.CurrentCulture
        };
        thread.Start();
    }

    public void Dispose()
    {
        PreviewImages?.Dispose();
    }

    public static void InitStaticRuntime()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = RainEd.Instance.AssetDataPath + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;

        staticRuntime = new LingoRuntime(typeof(MovieScript).Assembly);
        staticRuntime.Init();
        EditorRuntimeHelpers.RunStartup(staticRuntime);
    }

    private void StatusChanged(RenderStatus status)
    {
        var stageEnum = status.Stage.Stage;

        camsDone = status.CountCamerasDone;

        // from 0 to 1
        float stageProgress = 0f;

        switch (status.Stage)
        {
            case RenderStageStatusLayers layers:
            {
                stageProgress = (3 - layers.CurrentLayer) / 3f;
                DisplayString = $"Rendering tiles...\nLayer: {layers.CurrentLayer}";
                break;
            }

            case RenderStageStatusProps:
            {
                DisplayString = "Rendering props...";
                break;
            }

            case RenderStageStatusLight light:
            {
                stageProgress = light.CurrentLayer / 30f;
                DisplayString = $"Rendering light...\nLayer: {light.CurrentLayer}";
                break;
            }

            case RenderStageStatusRenderColors:
            {
                DisplayString = "Rendering colors...";
                break;
            }

            case RenderStageStatusFinalize:
            {
                DisplayString = "Finalizing...";
                break;
            }

            case RenderStageStatusEffects effects:
            {
                var builder = new StringBuilder();
                builder.Append("Rendering effects...\n");
                
                for (int i = 0; i < effects.EffectNames.Count; i++)
                {
                    if (i == effects.CurrentEffect - 1)
                        builder.Append("> ");
                    else
                        builder.Append("  ");

                    builder.Append(effects.EffectNames[i]);
                    builder.Append('\n');
                }

                stageProgress = Math.Clamp((effects.CurrentEffect - 1f) / effects.EffectNames.Count, 0f, 1f);

                DisplayString = builder.ToString();
                break;
            }
        }

        // send progress
        currentStage = stageEnum;
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
            _ => throw new ArgumentOutOfRangeException()
        };

        progress = (renderProgress + Math.Clamp(stageProgress, 0f, 1f)) / (cameraCount * 10f);

        if (stageEnum == RenderStage.Start && PreviewImages is not null)
        {
            PreviewImages.Stage = RenderPreviewStage.Setup;
        }
    }

    public void Cancel()
    {
        state = RenderState.Cancelling;

        if (threadState.Renderer is not null)
        {
            threadState.Renderer.CancelRender();
        }
        else
        {
            threadState.InQueue.Enqueue(new MessageDoCancel());
        }
    }

    public void Update()
    {
        while (threadState.Queue.TryDequeue(out ThreadMessage? messageGeneral))
        {
            if (messageGeneral is null) continue;

            switch (messageGeneral)
            {
                case MessageRenderProgress msgProgress:
                    progress = msgProgress.Percentage;
                    break;
                
                case MessageRenderFinished:
                    state = RenderState.Finished;
                    Log.Debug("Close render thread");
                    progress = 1f;
                    DisplayString = "";
                    thread.Join();
                    break;

                case MessageLevelLoading:
                    state = RenderState.Loading;
                    break;
                
                case MessageRenderStarted:
                    state = RenderState.Rendering;
                    break;
                
                case MessageRenderGeometryStarted:
                    state = RenderState.GeometryExport;
                    break;
                
                case MessageRenderFailed msgFail:
                    Log.Error("Error occured when rendering level:\n{ErrorMessage}", msgFail.Exception);
                    thread.Join();
                    state = RenderState.Errored;
                    break;
                
                case MessageRenderCancelled:
                    state = RenderState.Canceled;
                    thread.Join();
                    break;
                
                case MessageReceievePreview preview:
                    ProcessPreview(preview.Preview);
                    break;
            }
            
            threadState.Renderer?.RequestPreview();
        }
    }

    private RenderPreview? lastRenderPreview;

    private void ProcessPreview(RenderPreview renderPreview)
    {
        if (PreviewImages is null) return;

        Log.Verbose("Receive preview");
        
        lastRenderPreview = renderPreview;
        PreviewUpdated?.Invoke();
    }

    public void UpdatePreviewImages()
    {
        if (PreviewImages is null) return;

        switch (lastRenderPreview)
        {
            case RenderPreviewProps props:
                PreviewImages.Stage = RenderPreviewStage.Props;
                PreviewImages.SetSize(2000, 1200, PixelFormat.Bgra32);
                PreviewImages.DisableBlackOut();

                for (int i = 0; i < 30; i++)
                {
                    CopyLingoImage(props.Layers[i], PreviewImages.Layers[i]);
                }

                break;
            
            case RenderPreviewEffects effects:
            {
                PreviewImages.Stage = RenderPreviewStage.Effects;
                PreviewImages.SetSize(2000, 1200, PixelFormat.Bgra32);
                PreviewImages.EnableBlackOut();

                for (int i = 0; i < 30; i++)
                {
                    CopyLingoImage(effects.Layers[i], PreviewImages.Layers[i]);
                }

                CopyLingoImage(effects.BlackOut1, PreviewImages.BlackOut1!);
                CopyLingoImage(effects.BlackOut2, PreviewImages.BlackOut2!);

                PreviewImages.RenderBlackOut1 = effects.BlackOut1.Width != 1;
                PreviewImages.RenderBlackOut2 = effects.BlackOut2.Width != 1;
                
                break;
            }

            case RenderPreviewLights lights:
            {
                PreviewImages.Stage = RenderPreviewStage.Lights;
                PreviewImages.SetSize(2300, 1500, PixelFormat.L1);
                PreviewImages.DisableBlackOut();

                for (int i = 0; i < 30; i++)
                {
                    CopyLingoImage(lights.Layers[i], PreviewImages.Layers[i]);
                }
                
                break;
            }
        }
    }

    private unsafe static void CopyLingoImage(LingoImage srcImg, RenderImage dstImg)
    {
        if (srcImg.Width != dstImg.Width || srcImg.Height != dstImg.Height)
        {
            Log.Debug("Mismatched image sizes");
            return;
        }
        
        // depth validation
        if (srcImg.Depth == 1)
        {
            if (dstImg.Format != PixelFormat.L1)
                throw new Exception("Mismatched image formats");
        }
        else if (srcImg.Depth == 32)
        {
            if (dstImg.Format != PixelFormat.Bgra32)
                throw new Exception("Mismatched image formats");
        }
        else
        {
            throw new Exception("Unknown image color depth " + srcImg.Depth);
        }

        dstImg.Pixels = srcImg.ImageBuffer;

        //Debug.Assert(srcImg.ImageBufferNoPadding.Length >= dstImg.Pixels.Length);

        // copy the memory
        //Buffer.BlockCopy(srcImg.ImageBuffer, 0, dstImg.Pixels, 0, dstImg.Pixels.Length);
    }

    /// <summary>
    /// Begin a render of a level, blocking the process until it is finished.
    /// <br/><br/>
    /// Intended to be called without the presence of a Rained window.
    /// (i.e. when --render is passed as a command-line argument)
    /// </summary>
    /// <param name="levelPath">The path of the level to render</param>
    /// <returns></returns>
    public static void Render(string levelPath)
    {
        var levelName = Path.GetFileNameWithoutExtension(levelPath);
        var pathWithoutExt = Path.Combine(Path.GetDirectoryName(levelPath)!, levelName);

        if (!File.Exists(pathWithoutExt + ".txt"))
        {
            throw new DrizzleRenderException($"The file '{pathWithoutExt + ".txt"}' does not exist!");
        }

        if (!File.Exists(pathWithoutExt + ".png"))
        {
            throw new DrizzleRenderException($"The file '{pathWithoutExt + ".png"}' does not exist!");
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
                throw new Exception("preferences.json was not found");
            }
        }

        if (!Directory.Exists(dataPath))
        {
            throw new DrizzleRenderException($"The data directory {dataPath} does not exist.");
        }

        // create large trash log file, in case user decided to have it enabled
        // otherwise drizzle will not work
        var largeTrashLogFile = Path.Combine(dataPath, "largeTrashLog.txt");
        if (!File.Exists(largeTrashLogFile))
            File.Create(largeTrashLogFile).Dispose();

        LingoRuntime runtime;

        if (staticRuntime is not null)
        {
            runtime = staticRuntime;
        }
        else
        {
            Console.WriteLine("Initializing Lingo runtime...");

            Configuration.Default.PreferContiguousImageBuffers = true;
            LingoRuntime.MovieBasePath = dataPath + Path.DirectorySeparatorChar;
            LingoRuntime.CastPath = DrizzleCast.DirectoryPath + Path.DirectorySeparatorChar;
            
            runtime = new LingoRuntime(typeof(MovieScript).Assembly);
            runtime.Init();
            EditorRuntimeHelpers.RunStartup(runtime);
        }

        EditorRuntimeHelpers.RunLoadLevel(runtime, levelPath);

        var movie = (MovieScript)runtime.MovieScriptInstance;
        var camCount = (int) movie.gCameraProps.cameras.count;

        void StatusChanged(RenderStatus status)
        {
            if (status.Stage.Stage == RenderStage.CameraSetup)
                Console.WriteLine($"Rendering {status.CountCamerasDone + 1} of {camCount} cameras...");
        }

        void RenderComplete(int index, LingoImage image)
        {
            Console.WriteLine($"Finished {levelName}_{index}.png");
        }
        
        var renderer = new LevelRenderer(runtime, null);
        renderer.StatusChanged += StatusChanged;
        renderer.OnScreenRenderCompleted += RenderComplete;

        var stopwatch = Stopwatch.StartNew();
        renderer.DoRender();

        Console.WriteLine($"Render finished in {stopwatch.Elapsed}");
    }
}