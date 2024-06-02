using System.Numerics;
using Glib;
using ImGuiNET;

// NEED ADD SCISSCOR!!

namespace GlibTests
{
    class Program
    {
        private static Window window = null!;
        private static StandardMesh mesh = null!;
        private static StandardMesh dynamicMesh = null!;
        private static Texture texture = null!;
        private static Texture rainedLogo = null!;
        private static Shader testShader = null!;
        private static Shader invertColorShader = null!;
        private static Framebuffer framebuffer = null!;

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
                RefreshRate = 10,
                IsEventDriven = false,
                VSync = true
            };
            
            window = new Window(options);

            // assign events
            window.Load += OnLoad;
            //window.Resize += OnResize;
            //window.Closing += OnClose;

            // run the window
            window.Initialize();
            while (!window.IsClosing)
            {
                window.PollEvents();
                window.BeginRender();

                OnUpdate((float)window.DeltaTime);
                OnRender((float)window.DeltaTime, window.RenderContext!);

                window.EndRender();
                window.SwapBuffers();
            }

            // dispose resources after run is done
            mesh.Dispose();
            window.Dispose();
        }

        private static void OnLoad()
        {
            Console.WriteLine("Load!");
            var ctx = window.RenderContext!;

            texture = ctx.LoadTexture("assets/icon48.png");
            rainedLogo = ctx.LoadTexture("assets/rained-logo.png");
            testShader = ctx.CreateShader();
            invertColorShader = ctx.CreateShader(
                vsSource: null,
                fsSource: @"
                #version 330 core

                in vec4 VertexColor;
                in vec2 TexCoord;
                out vec4 FragColor;

                uniform sampler2D uTexture; 
                
                void main() {
                    vec4 col = VertexColor * texture(uTexture, TexCoord);
                    FragColor = vec4(vec3(1.0 - col.rgb), col.a);
                }
                "
            );

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

            // setup framebuffer
            var bufferConfig = FramebufferConfiguration.Standard(300, 300);
            framebuffer = ctx.CreateFramebuffer(bufferConfig);
        }

        private static void OnRender(float dt, RenderContext renderContext)
        {
            renderContext.LineWidth = 4f;

            renderContext.DrawColor = Color.White;
            renderContext.Draw(texture, new Rectangle(0f, 0f, 200f, 100f));

            bool all = mode == 9;

            // test rect
            if (all || mode == 0)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 127, 51, 255);
                renderContext.DrawRectangle(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);

                renderContext.PushTransform();
                renderContext.Translate(sqX - 0, sqY - 0, 0f);
                renderContext.Rotate((float)window.Time);
                renderContext.DrawColor = Color.White;
                renderContext.Draw(mesh);
                renderContext.DrawColor = Color.Red;
                renderContext.Draw(texture);
                renderContext.DrawColor = Color.Green;
                //renderContext.Draw(mesh);
                renderContext.Draw(texture, texture.Width, 0f);
                renderContext.PopTransform();

                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 50);
                renderContext.DrawRectangleLines(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);

                renderContext.DrawColor = Color.Blue;
                renderContext.DrawTriangle(0f, 0f, 0, 10f, 10f, 10f);
            }
            
            // test line
            if (all || mode == 1)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255);
                renderContext.DrawLine(window.Width / 2.0f, window.Height / 2.0f, sqX, sqY);
            }

            // test circle
            if (all || mode == 2)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawCircle(window.MouseX, window.MouseY, sqH);
            }

            // test circle outline
            if (all || mode == 3)
            {
                renderContext.DrawColor = Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawRing(window.MouseX, window.MouseY, sqH);
            }

            // dynamic, non-indexed, textured mesh
            if (all || mode == 4)
            {
                renderContext.DrawColor = Color.White;

                dynamicMesh ??= renderContext.CreateMesh(false);

                var a = (float) window.Time;
                dynamicMesh.SetVertices([
                    new Vector3(MathF.Cos(a) * 20f, MathF.Sin(a) * 20f, 0),
                    new Vector3(0f, 100f, 0f),
                    new Vector3(MathF.Cos(a) * 5f, MathF.Sin(a) * 5f, 0f) + new Vector3(100f, 140f, 0f),

                    new Vector3(MathF.Cos(a) * 5f, MathF.Sin(a) * 5f, 0f) + new Vector3(100f, 100f, 0f),
                    new Vector3(MathF.Cos(a) * 20f, MathF.Sin(a) * 20f, 0),
                    new Vector3(120f, 0, 0f),
                ]);

                dynamicMesh.SetColors([
                    Color.White, Color.White, Color.White,
                    Color.White, Color.White, Color.White,
                ]);

                dynamicMesh.SetTexCoords([
                    new(0, 0),
                    new(0, 1),
                    new(1, 1),

                    new(1, 1),
                    new(0, 0),
                    new(1, 0),
                ]);

                dynamicMesh.Upload();

                renderContext.Shader = testShader;
                testShader.SetUniform("uTexture", rainedLogo);

                renderContext.PushTransform();
                renderContext.Translate(window.MouseX, window.MouseY, 0f);
                renderContext.Draw(dynamicMesh);
                renderContext.PopTransform();

                renderContext.Shader = null;
            }

            // framebuffer test
            if (all || mode == 5)
            {
                renderContext.DrawColor = Color.White;
                renderContext.PushFramebuffer(framebuffer);
                renderContext.Clear(Color.Transparent);
                renderContext.Draw(rainedLogo);
                renderContext.DrawRectangle(window.MouseX, window.MouseY, 40f, 40f);
                renderContext.PopFramebuffer();

                renderContext.Shader = invertColorShader;
                var tex = framebuffer.GetTexture(0);
                invertColorShader.SetUniform("uColor", Color.White);
                invertColorShader.SetUniform("uTexture", tex);
                renderContext.Draw(tex, 0f, 0f, window.Width, window.Height);
                renderContext.Shader = null;
            }

            ImGui.ShowDemoWindow();

            renderContext.DrawBatch();
            window.ImGuiController!.Render();
        }

        private static void OnUpdate(float dt)
        {
            window.ImGuiController!.Update(dt);

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
            if (window.IsKeyPressed(Key.Number7))
                mode = 6;
            if (window.IsKeyPressed(Key.Number8))
                mode = 7;
            if (window.IsKeyPressed(Key.Number9))
                mode = 8;
            if (window.IsKeyPressed(Key.Number0))
                mode = 9;

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