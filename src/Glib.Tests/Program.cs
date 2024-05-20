using System.Drawing;
using Glib;

namespace GlibTests
{
    class Program
    {
        private static Window window = null!;
        private static float sqX = 0f;
        private static float sqY = 0f;
        private static float sqW = 100.0f;
        private static float sqH = 100.0f;

        private static void Main(string[] args)
        {
            // create a window
            var options = new WindowOptions()
            {
                Width = 800,
                Height = 600,
                Title = "Silk.NET test",
            };
            
            window = new Window(options);

            // assign events
            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Draw += OnRender;
            //window.Resize += OnResize;
            //window.Closing += OnClose;

            // run the window
            window.Run();

            // dispose window after run is done
            window.Dispose();
        }

        private static void OnLoad()
        {
            Console.WriteLine("Load!");
        }

        private static void OnRender(float dt, RenderContext renderContext)
        {
            renderContext.DrawColor = Color.FromArgb(255, 127, 51, 255);
            renderContext.FillRect(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);
        }

        private static void OnUpdate(float dt)
        {
            sqX = window.MouseX;
            sqY = window.MouseY;

            if (window.IsKeyDown(Key.Escape))
            {
                window.Close();
            }

            if (window.IsKeyPressed(Key.Q))
            {
                sqW += 10f;
            }

            if (window.IsKeyDown(Key.W))
            {
                sqH += 60f * dt;
            }
        }
    }
}