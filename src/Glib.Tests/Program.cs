using System.Numerics;
using Glib;
using Glib.ImGui;
using ImGuiNET;
using Key = Glib.Key;

// NEED ADD SCISSCOR!!

namespace GlibTests
{
    class Program
    {
        private static Glib.Window window = null!;
        private static ImGuiController imGuiController = null!;
        private static Glib.StandardMesh mesh = null!;
        private static Glib.Mesh dynamicMesh = null!;
        private static Glib.Texture texture = null!;
        private static Glib.Texture rainedLogo = null!;
        private static Glib.Shader testShader = null!;
        private static Glib.Shader invertColorShader = null!;
        private static Glib.Framebuffer framebuffer = null!;

        private static int mode = 0;
        private static float sqX = 0f;
        private static float sqY = 0f;
        private static float sqW = 100.0f;
        private static float sqH = 100.0f;

        private const string InvertFragmentSource = @"#version 300 es
        precision mediump float;

        in vec2 v_texcoord0;
        in vec4 v_color0;

        out vec4 fragColor;

        uniform sampler2D u_texture0;
        uniform vec4 u_color;
        
        void main() {
            vec4 texel = texture(u_texture0, v_texcoord0);
            fragColor = vec4(vec3(1.0) - texel.rgb, texel.a) * v_color0 * u_color;
        }
        ";

        private static void Main(string[] args)
        {
            // create a window
            var options = new Glib.WindowOptions()
            {
                Width = 800,
                Height = 600,
                Title = "Silk.NET test",
                IsEventDriven = false,
                VSync = true,
                GlDebugContext = true
            };
            
            window = new Glib.Window(options);

            // assign events
            window.Load += OnLoad;
            //window.Resize += OnResize;
            //window.Closing += OnClose;

            // run the window
            RenderContext.Log += (LogLevel level, string msg) =>
            {
                if (level == LogLevel.Debug)
                    Console.WriteLine("[DBG] " + msg);
                else if (level == LogLevel.Information)
                    Console.WriteLine("[INF] " + msg);
                else if (level == LogLevel.Error)
                    Console.WriteLine("[ERR] " + msg);
            };
            window.Initialize();

            var ctx = RenderContext.Instance!;
            imGuiController = new ImGuiController(window);

            while (!window.IsClosing)
            {
                window.PollEvents();
                ctx.Begin();

                OnUpdate((float)window.DeltaTime);
                OnRender((float)window.DeltaTime, ctx);

                ctx.End();
                window.SwapBuffers();
            }

            // dispose resources after run is done
            imGuiController.Dispose();
            //mesh.Dispose();
            window.Dispose();
        }

        private static void OnLoad()
        {
            Console.WriteLine("Load!");

            var ctx = RenderContext.Init(window);
            ctx.CullMode = CullMode.None;

            texture = Glib.Texture.Load("assets/icon48.png");
            rainedLogo = Glib.Texture.Load("assets/rained-logo.png");
            testShader = Glib.Shader.Create();
            invertColorShader = Glib.Shader.Create(null, InvertFragmentSource);

            mesh = Glib.StandardMesh.CreateIndexed([0, 1, 2, 3, 0, 2], 4);

            mesh.SetVertexData([
                new(0f, 0f, 0f),
                new(0, 50f, 0f),
                new(50f, 50f, 0f),
                new(50f, 0f, 0f)
            ]);

            mesh.SetColorData([
                Glib.Color.Red,
                Glib.Color.Green,
                Glib.Color.Blue,
                Glib.Color.White
            ]);

            mesh.SetTexCoordData([
                new(0f, 1f),
                new(0f, 0f),
                new(1f, 0f),
                new(1f, 1f)
            ]);

            mesh.Upload();

            // setup framebuffer
            framebuffer = Glib.FramebufferConfiguration.Standard(300, 300)
                .Create();
        }

        private static void OnRender(float dt, Glib.RenderContext renderContext)
        {
            renderContext.LineWidth = 4f;
            renderContext.UseGlLines = false;

            renderContext.DrawColor = Glib.Color.White;
            renderContext.DrawTexture(texture, new Glib.Rectangle(0f, 0f, 200f, 100f));

            bool all = mode == 9;

            // test rect
            if (all || mode == 0)
            {
                renderContext.DrawColor = Glib.Color.FromRGBA(255, 127, 51, 255);
                renderContext.DrawRectangle(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);

                renderContext.PushTransform();
                renderContext.Translate(sqX - 0, sqY - 0, 0f);
                renderContext.Rotate((float)window.Time);
                renderContext.DrawColor = Glib.Color.White;
                renderContext.Draw(mesh);
                renderContext.DrawColor = Glib.Color.Red;
                renderContext.DrawTexture(texture);
                renderContext.DrawColor = Glib.Color.Green;
                //renderContext.Draw(mesh);
                renderContext.DrawTexture(texture, texture.Width, 0f);
                renderContext.PopTransform();

                renderContext.DrawColor = Glib.Color.FromRGBA(255, 255, 255, 50);
                renderContext.DrawRectangleLines(sqX - sqW / 2.0f, sqY - sqH / 2.0f, sqW, sqH);

                renderContext.DrawColor = Glib.Color.Blue;
                renderContext.DrawTriangle(0f, 0f, 0, 10f, 10f, 10f);
            }
            
            // test line
            if (all || mode == 1)
            {
                renderContext.DrawColor = Glib.Color.FromRGBA(255, 255, 255);
                renderContext.DrawLine(window.Width / 2.0f, window.Height / 2.0f, sqX, sqY);
            }

            // test circle
            if (all || mode == 2)
            {
                renderContext.DrawColor = Glib.Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawCircle(window.MouseX, window.MouseY, sqH);
            }

            // test circle outline
            if (all || mode == 3)
            {
                renderContext.DrawColor = Glib.Color.FromRGBA(255, 255, 255, 100);
                renderContext.DrawRing(window.MouseX, window.MouseY, sqH);
            }

            // dynamic, non-indexed, textured mesh
            if (all || mode == 4)
            {
                renderContext.DrawColor = Glib.Color.White;
                
                dynamicMesh ??= new Glib.MeshConfiguration()
                    .AddBuffer(AttributeName.Position, DataType.Float, 3, MeshBufferUsage.Dynamic)
                    .AddBuffer(AttributeName.TexCoord0, DataType.Float, 2, MeshBufferUsage.Dynamic)
                    .AddBuffer(AttributeName.Color0, DataType.Float, 4, MeshBufferUsage.Dynamic)
                    .Create(6);

                var a = (float) window.Time;
                dynamicMesh.SetBufferData(0, [
                    new Vector3(MathF.Cos(a) * 20f, MathF.Sin(a) * 20f, 0),
                    new Vector3(0f, 100f, 0f),
                    new Vector3(MathF.Cos(a) * 5f, MathF.Sin(a) * 5f, 0f) + new Vector3(100f, 140f, 0f),

                    new Vector3(MathF.Cos(a) * 5f, MathF.Sin(a) * 5f, 0f) + new Vector3(100f, 100f, 0f),
                    new Vector3(MathF.Cos(a) * 20f, MathF.Sin(a) * 20f, 0),
                    new Vector3(120f, 0, 0f),
                ]);

                dynamicMesh.SetBufferData(2, [
                    Glib.Color.White, Glib.Color.White, Glib.Color.White,
                    Glib.Color.White, Glib.Color.White, Glib.Color.White,
                ]);

                dynamicMesh.SetBufferData(1, [
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),

                    new Vector2(1, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                ]);

                dynamicMesh.Upload();

                renderContext.Shader = testShader;
                renderContext.PushTransform();
                renderContext.Translate(window.MouseX, window.MouseY, 0f);
                renderContext.Draw(dynamicMesh, rainedLogo);
                renderContext.PopTransform();

                renderContext.Shader = null;
            }

            // framebuffer test
            if (all || mode == 5)
            {
                renderContext.DrawColor = Glib.Color.White;
                renderContext.PushFramebuffer(framebuffer);
                renderContext.Clear(ClearFlags.Color | ClearFlags.Depth, Color.Transparent);
                renderContext.DrawTexture(rainedLogo);
                renderContext.DrawRectangle(window.MouseX, window.MouseY, 40f, 40f);
                renderContext.PopFramebuffer();

                renderContext.Shader = invertColorShader;
                var tex = framebuffer.GetTexture(0);
                //invertColorShader.SetUniform("uColor", Color.White);
                //invertColorShader.SetUniform("uTexture", tex);
                renderContext.DrawTexture(
                    tex,
                    new Glib.Rectangle(0f, tex.Height, tex.Width, -tex.Height),
                    new Glib.Rectangle(0f, 0f, window.Width, window.Height)
                );
                renderContext.Shader = null;
            }

            ImGui.ShowDemoWindow();

            if (ImGui.Begin("Test"))
            {
                ImGui.Image(imGuiController.UseTexture(rainedLogo), new Vector2(rainedLogo.Width, rainedLogo.Height));
                ImGui.Image(imGuiController.UseTexture(texture), new Vector2(texture.Width, texture.Height));
            } ImGui.End();
            
            imGuiController.Render();
        }

        private static void OnUpdate(float dt)
        {
            imGuiController.Update(dt);
            
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