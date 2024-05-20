using System.Drawing;
using Glib;

namespace GlibTests
{
    class Program
    {
        private static Window window;

        private static void Main(string[] args)
        {
            // create a window
            var options = new WindowOptions()
            {
                Width = 800,
                Height = 600,
                Title = "Silk.NET test"
            };
            
            window = new Window(options);

            // assign events
            window.Load += OnLoad;
            //window.Update += OnUpdate;
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
            renderContext.FillRect(0f, 0f, 100f, 100f);
        }

        private static void OnUpdate(double obj)
        {
        }
    }
}