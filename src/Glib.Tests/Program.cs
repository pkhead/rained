using System.Numerics;
using Glib;
using Key = Glib.Key;

// SHADER PREPROCESSING!!

namespace Glib.Tests
{
    class Program
    {
        private static Glib.Window window = null!;
        //private static ImGuiController imGuiController = null!;
        //private static Glib.StandardMesh mesh = null!;
        //private static Glib.Mesh dynamicMesh = null!;
        private static Glib.Texture texture = null!;
        private static Glib.Texture rainedLogo = null!;
        private static Glib.Shader testShader = null!;
        private static Glib.Shader invertColorShader = null!;
        private static Glib.Framebuffer framebuffer = null!;

        private const string InvertShaderFsrc = @"#version 300 es
        precision mediump float;

        uniform vec4 glib_uColor;
        uniform sampler2D glib_uTexture;

        in vec2 glib_texCoord;
        in vec4 glib_color;

        out vec4 glib_fragColor;

        void main()
        {
            vec4 col = glib_color * glib_uColor * texture(glib_uTexture, glib_texCoord);
            glib_fragColor = vec4(vec3(1.0 - col.rgb), col.a);
        }
        ";

        private static int mode = 0;
        private static float sqX = 0f;
        private static float sqY = 0f;
        private static float sqW = 100.0f;
        private static float sqH = 100.0f;

        private static void Main(string[] args)
        {
            // create a window
            var options = new Glib.WindowOptions()
            {
                Width = 800,
                Height = 600,
                Title = "Silk.NET test",
                RefreshRate = 10,
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
            window.Initialize();
            var rctx = RenderContext.Instance!;

            while (!window.IsClosing)
            {
                window.PollEvents();
                rctx.Begin();

                OnUpdate((float)window.DeltaTime);
                OnRender((float)window.DeltaTime, rctx);

                rctx.End();
                window.SwapBuffers();
            }

            // dispose resources after run is done
            //imGuiController.Dispose();
            //mesh.Dispose();
            rctx.Dispose();
            window.Dispose();
        }

        private static void OnLoad()
        {
            Console.WriteLine("Load!");
            //imGuiController = new ImGuiController(window);

            var ctx = RenderContext.Init(window);

            texture = Glib.Texture.Load("assets/icon48.png");
            rainedLogo = Glib.Texture.Load("assets/rained-logo.png");
            testShader = Glib.Shader.Create();
            invertColorShader = Glib.Shader.Create(null, InvertShaderFsrc);

            /*mesh = Glib.StandardMesh.CreateIndexed([0, 1, 2, 3, 0, 2], 4);

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

            mesh.Upload();*/

            // setup framebuffer
            framebuffer = Glib.FramebufferConfiguration.Standard(300, 300)
                .Create();
        }

        private static void OnRender(float dt, Glib.RenderContext rctx)
        {
            rctx.LineWidth = 1f;
            rctx.UseGlLines = true;
            rctx.DrawRectangleLines(10.0f, 10.0f, 100.0f, 80.0f);

            rctx.LineWidth = 2f;
            rctx.UseGlLines = false;

            rctx.DrawColor = Glib.Color.Red;
            rctx.DrawRectangle(window.MouseX, window.MouseY, 50.0f, 50.0f);
            rctx.DrawColor = Glib.Color.Green;
            rctx.DrawRectangleLines(window.MouseX, window.MouseY, 50.0f, 50.0f);

            //renderContext.DrawColor = Glib.Color.White;
            //renderContext.DrawTexture(texture, new Glib.Rectangle(0f, 0f, 200f, 100f));
        }

        private static void OnUpdate(float dt)
        {
            //imGuiController.Update(dt);

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