using Glib;

namespace GlibTests
{
    class Program
    {
        public static void Main()
        {
            // create a window
            var options = new Glib.WindowOptions()
            {
                Width = 800,
                Height = 600,
                Title = "Silk.NET test",
                RefreshRate = 60,
                IsEventDriven = false,
                VSync = true
            };

            var window = new Glib.Window(options);
            window.Initialize();
            
            window.RenderContext!.BackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            while (!window.IsClosing)
            {
                window.PollEvents();
                window.BeginRender();

                window.EndRender();
                window.SwapBuffers();
            }

            window.Dispose();
        }
    }
}