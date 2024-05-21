using System.Numerics;
using Glib;

namespace GlibTests
{
    class Program
    {
        private static Window window = null!;
        private static StandardMesh mesh = null!;

        private static int mode = 0;
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
                IsEventDriven = false,
                VSync = true
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
            var ctx = window.RenderContext!;

            mesh = ctx.CreateMesh(true);

            mesh.SetVertices([
                new(0f, 0f, 0f),
                new(0, 50f, 0f),
                new(50f, 50f, 0f),
                new(50f, 0f, 0f)
            ]);

            mesh.SetColors([
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.White
            ]);

            mesh.SetTexCoords([
                new(0f, 1f),
                new(0f, 0f),
                new(1f, 0f),
                new(1f, 1f)
            ]);

            mesh.SetIndexBufferData([
                0, 1, 2,
                3, 0, 2
            ]);

            mesh.Upload();
        }

        private static void OnRender(float dt, RenderContext renderContext)
        {
            renderContext.LineWidth = 4f;

            // test rect
            if (mode == 0)
            {
                renderContext.DrawColor = Color.Blue;
                renderContext.DrawTriangle(0f, 0f, 0, 10f, 10f, 10f);
                renderContext.DrawColor = Color.FromRGBA(255, 127, 51, 255);
                renderContext.DrawRectangle(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);

                renderContext.PushTransform();
                renderContext.Translate(sqX - 0, sqY - 0, 0f);
                renderContext.Rotate((float)window.Time);
                renderContext.Draw(mesh);
                renderContext.PopTransform();

                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 50);
                renderContext.DrawRectangleLines(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);
            }
            
            // test line
            else if (mode == 1)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255);
                renderContext.DrawLine(window.Width / 2.0f, window.Height / 2.0f, sqX, sqY);
            }

            // test circle
            else if (mode == 2)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawCircle(window.MouseX, window.MouseY, sqH);
            }

            // test circle outline
            else if (mode == 3)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawRing(window.MouseX, window.MouseY, sqH);
            }
        }

        private static void OnUpdate(float dt)
        {
            if (dt > 0.2f)
            {
                throw new Exception("Wut da sigma!?!");
            }

            sqX = window.MouseX;
            sqY = window.MouseY;

            if (window.IsKeyDown(Key.Escape))
            {
                window.Close();
            }

            if (window.IsKeyPressed(Key.Number1))
                mode = 0;
            if (window.IsKeyPressed(Key.Number2))
                mode = 1;
            if (window.IsKeyPressed(Key.Number3))
                mode = 2;
            if (window.IsKeyPressed(Key.Number4))
                mode = 3;
            if (window.IsKeyPressed(Key.Number5))
                mode = 4;
            if (window.IsKeyPressed(Key.Number6))
                mode = 5;

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