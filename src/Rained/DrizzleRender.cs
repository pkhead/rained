using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using SixLabors.ImageSharp;
namespace RainEd;

class DrizzleRender : IDisposable
{
    private abstract record ThreadMessage;

    private record MessageRenderStarted : ThreadMessage;
    private record MessageRenderFailed(Exception Exception) : ThreadMessage;
    private record MessageRenderCancelled : ThreadMessage;
    private record MessageRenderFinished : ThreadMessage;
    private record MessageRenderProgress(float Percentage) : ThreadMessage;
    private record MessageDoCancel : ThreadMessage;
    private record MessageReceievePreview(RenderPreview Preview) : ThreadMessage;

    private static LingoRuntime? staticRuntime = null; 

    private class RenderThread
    {
        public ConcurrentQueue<ThreadMessage> Queue;
        public ConcurrentQueue<ThreadMessage> InQueue;

        public string filePath;
        public LevelRenderer? Renderer;
        public Action<RenderStatus>? StatusChanged = null;

        public RenderThread(string filePath)
        {
            Queue = new ConcurrentQueue<ThreadMessage>();
            InQueue = new ConcurrentQueue<ThreadMessage>();
            this.filePath = filePath;
        }

        public void ThreadProc()
        {
            try
            {
                LingoRuntime runtime;

                if (staticRuntime is not null)
                {
                    runtime = staticRuntime;
                }
                else
                {
                    RainEd.Logger.Information("Initializing Zygote runtime...");

                    LingoRuntime.MovieBasePath = RainEd.Instance.AssetDataPath + Path.DirectorySeparatorChar;
                    LingoRuntime.CastPath = Path.Combine(LingoRuntime.MovieBasePath, "Cast");
                    
                    runtime = new LingoRuntime(typeof(MovieScript).Assembly);
                    runtime.Init();
                    EditorRuntimeHelpers.RunStartup(runtime);
                }

                // process user cancel if cancelled while init
                // zygote runtime
                if (InQueue.TryDequeue(out ThreadMessage? msg))
                {
                    if (msg is MessageDoCancel)
                        throw new RenderCancelledException();
                }
                Queue.Enqueue(new MessageRenderStarted());

                EditorRuntimeHelpers.RunLoadLevel(runtime, filePath);

                Renderer = new LevelRenderer(runtime, null);
                Renderer.StatusChanged += StatusChanged;
                Renderer.PreviewSnapshot += PreviewSnapshot;

                RainEd.Logger.Information("Begin render of {LevelName}", Path.GetFileNameWithoutExtension(filePath));
                Renderer.DoRender();
                RainEd.Logger.Information("Render successful!");
                Queue.Enqueue(new MessageRenderFinished());
            }
            catch (RenderCancelledException)
            {
                RainEd.Logger.Information("Render was cancelled");
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
        Rendering,
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

    public readonly RlManaged.Image[] RenderLayerPreviews;
    public Action? PreviewUpdated;

    public DrizzleRender()
    {
        cameraCount = RainEd.Instance.Level.Cameras.Count;

        state = RenderState.Initializing;
        var filePath = RainEd.Instance.CurrentFilePath;
        if (string.IsNullOrEmpty(filePath)) throw new Exception("Render called but level wasn't saved");

        // create render layer preview images
        var renderW = 2000;
        var renderH = 1200;
        RenderLayerPreviews = new RlManaged.Image[30];

        for (int i = 0; i < 30; i++)
        {
            RenderLayerPreviews[i] = RlManaged.Image.GenColor((int)renderW, (int)renderH, Raylib_cs.Color.Black);
        } 

        LevelSerialization.Save(filePath);

        threadState = new RenderThread(filePath);
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
        foreach (RlManaged.Image image in RenderLayerPreviews)
            image.Dispose();
    }

    public static void InitStaticRuntime()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = RainEd.Instance.AssetDataPath + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = Path.Combine(LingoRuntime.MovieBasePath, "Cast");

        staticRuntime = new LingoRuntime(typeof(MovieScript).Assembly);
        staticRuntime.Init();
        EditorRuntimeHelpers.RunStartup(staticRuntime);
    }

    private void StatusChanged(RenderStatus status)
    {
        var renderer = threadState.Renderer!;

        RainEd.Logger.Debug("Status changed");

        var camIndex = status.CameraIndex;
        var stageEnum = status.Stage.Stage;

        camsDone = status.CountCamerasDone;

        switch (status.Stage)
        {
            case RenderStageStatusLayers layers:
            {
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
                    if (i == effects.CurrentEffect)
                        builder.Append("> ");
                    else
                        builder.Append("  ");

                    builder.Append(effects.EffectNames[i]);
                    builder.Append('\n');
                }

                DisplayString = builder.ToString();
                break;
            }
        }

        // send progress
        currentStage = stageEnum;
        var renderProgress = status.CountCamerasDone * 10 + stageEnum switch
        {
            RenderStage.Start => 0,
            RenderStage.CameraSetup => 0,
            RenderStage.RenderLayers => 1,
            RenderStage.RenderPropsPreEffects => 2,
            RenderStage.RenderEffects => 3,
            RenderStage.RenderPropsPostEffects => 4,
            RenderStage.RenderLight => 5,
            RenderStage.Finalize => 6,
            RenderStage.RenderColors => 7,
            RenderStage.Finished => 8,
            RenderStage.SaveFile => 9,
            _ => throw new ArgumentOutOfRangeException()
        };

        progress = renderProgress / (cameraCount * 10f);
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
                    RainEd.Logger.Debug("Close render thread");
                    progress = 1f;
                    DisplayString = "";
                    thread.Join();
                    break;
                
                case MessageRenderStarted:
                    state = RenderState.Rendering;
                    break;
                
                case MessageRenderFailed msgFail:
                    RainEd.Logger.Error("Error occured when rendering level:\n{ErrorMessage}", msgFail.Exception);
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

    private void ProcessPreview(RenderPreview renderPreview)
    {
        RainEd.Logger.Verbose("Receive preview");
        
        switch (renderPreview)
        {
            case RenderPreviewEffects effects:
            {
                ProcessLingoImageLayers(effects.Layers);
                break;
            }

            case RenderPreviewLights lights:
            {
                // TODO: light stage uses a differently sized image buffer
                // ProcessLingoImageLayers(lights.Layers);
                break;
            }
        }

        PreviewUpdated?.Invoke();
    }

    private void ProcessLingoImageLayers(LingoImage[] layers)
    {
        var srcImage = layers[0];

        /*
        Console.WriteLine(srcImage.width);
        Console.WriteLine(PreviewImage.Width);

        Console.WriteLine(srcImage.height);
        Console.WriteLine(PreviewImage.Height);
        */

        // Lingo Image:
        // 2000, 1200
        // Output:
        // 1400, 800
        if (layers.Length != 30)
            throw new Exception("Count of layers is not 30");
        
        for (int i = 0; i < layers.Length; i++)
        {
            var img = layers[i];
            var dstImage = RenderLayerPreviews[i];

            unsafe
            {
                Marshal.Copy(img.ImageBuffer, 0, (nint) dstImage.Data, dstImage.Width * dstImage.Height * 4);
            }
        }
    }
}