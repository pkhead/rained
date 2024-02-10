using System.Collections.Concurrent;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using SixLabors.ImageSharp;
namespace RainEd;

class LevelDrizzleRender
{
    private class RenderThread
    {
        public ConcurrentQueue<string> Queue;
        public string filePath;

        public RenderThread(string filePath)
        {
            Queue = new ConcurrentQueue<string>();
            this.filePath = filePath;
        }

        public void ThreadProc()
        {
            try
            {
                var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
                runtime.Init();
                EditorRuntimeHelpers.RunStartup(runtime);
                EditorRuntimeHelpers.RunLoadLevel(runtime, filePath);

                var renderer = new LevelRenderer(runtime, null);

                Console.WriteLine("Begin render");
                renderer.DoRender();
                Console.WriteLine("Render successful!");
                Queue.Enqueue("done");
            }
            catch
            {
                Queue.Enqueue("error");
            }
        }
    }
    private RenderThread threadState;
    private Thread thread;

    private bool isDone = false;
    public bool IsDone { get => isDone; }

    public LevelDrizzleRender(RainEd editor)
    {
        var filePath = editor.CurrentFilePath;
        if (string.IsNullOrEmpty(filePath)) throw new Exception("Render called but level wasn't saved");

        LevelSerialization.Save(editor, filePath);
        Configuration.Default.PreferContiguousImageBuffers = true;

        threadState = new RenderThread(filePath);
        thread = new Thread(new ThreadStart(threadState.ThreadProc));
        thread.Start();
    }

    public void Update(RainEd editor)
    {
        while (threadState.Queue.TryDequeue(out string? message))
        {
            if (string.IsNullOrEmpty(message)) continue;

            if (message == "done")
            {
                isDone = true;
                Console.WriteLine("Thread is done!");
            }
            else if (message == "error")
            {
                editor.ShowError("Error occured while rendering level");
            }
        }
    }
}