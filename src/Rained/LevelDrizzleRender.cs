using System.Collections;
using System.Collections.Concurrent;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using SixLabors.ImageSharp;
namespace RainEd;

class LevelDrizzleRender
{
    private record ThreadMessage;

    private record MessageRenderStarted : ThreadMessage;
    private record MessageRenderFailed(Exception Exception) : ThreadMessage;
    private record MessageRenderFinished : ThreadMessage;
    private record MessageRenderProgress(float Percentage) : ThreadMessage;

    private class RenderThread
    {
        public ConcurrentQueue<ThreadMessage> Queue;
        public string filePath;
        public LevelRenderer? Renderer;
        private int cameraCount;

        public RenderThread(string filePath, int cameraCount)
        {
            this.cameraCount = cameraCount;
            Queue = new ConcurrentQueue<ThreadMessage>();
            this.filePath = filePath;
        }

        private void StatusChanged(RenderStatus status)
        {
            Console.WriteLine("Status changed");

            var camIndex = status.CameraIndex;
            var stageEnum = status.Stage.Stage;

            // send progress
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

            Queue.Enqueue(new MessageRenderProgress(
                (float)renderProgress / (cameraCount * 10f))
            );
        }

        public void ThreadProc()
        {
            try
            {
                var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
                runtime.Init();
                EditorRuntimeHelpers.RunStartup(runtime);
                EditorRuntimeHelpers.RunLoadLevel(runtime, filePath);

                Renderer = new LevelRenderer(runtime, null);
                Renderer.StatusChanged += StatusChanged;

                Queue.Enqueue(new MessageRenderStarted());

                Console.Write("Begin Render");
                Renderer.DoRender();
                Console.WriteLine("Render successful!");
                Queue.Enqueue(new MessageRenderFinished());
            }
            catch (RenderCancelledException)
            {}
            catch (Exception e)
            {
                Queue.Enqueue(new MessageRenderFailed(e));
            }
        }
    }
    private readonly RenderThread threadState;
    private readonly Thread thread;

    public enum RenderState
    {
        Initializing,
        Rendering,
        Finished
    };

    private RenderState state;
    private float progress = 0.0f;
    public RenderState State { get => state; }
    public float RenderProgress { get => progress; }

    public LevelDrizzleRender(RainEd editor)
    {
        state = RenderState.Initializing;
        var filePath = editor.CurrentFilePath;
        if (string.IsNullOrEmpty(filePath)) throw new Exception("Render called but level wasn't saved");

        LevelSerialization.Save(editor, filePath);
        Configuration.Default.PreferContiguousImageBuffers = true;

        threadState = new RenderThread(filePath, editor.Level.Cameras.Count);
        thread = new Thread(new ThreadStart(threadState.ThreadProc));
        thread.Start();
    }

    public void Update(RainEd editor)
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
                    Console.WriteLine("Thread is done!");
                    progress = 1f;
                    break;
                
                case MessageRenderStarted:
                    state = RenderState.Rendering;
                    break;
                
                case MessageRenderFailed msgFail:
                    editor.ShowError("Error occured while rendering level");
                    Console.WriteLine("Error occured when rendering level");
                    Console.WriteLine(msgFail.Exception);
                    break;
            }

            /*if (messageGeneral is MessageRenderFinished)
            {
                state = RenderState.Finished;
                Console.WriteLine("Thread is done!");
            }
            else if (messageGeneral is MessageRenderStarted)
            {
                state = RenderState.Rendering;
            }
            else if (messageGeneral is MessageRenderFailed msgFail)
            {
            }*/
        }
    }
}