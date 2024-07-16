using System.Numerics;

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
            var rctx = window.RenderContext!;

            if (!rctx.CanUseMultipleWindows()) throw new Exception("no swapchain support");
            if (!rctx.CanReadBackFramebufferAttachments()) throw new Exception("no readback support");
            
            rctx.BackgroundColor = new Glib.Color(0.5f, 0.5f, 0.5f, 1f);

            ReadOnlySpan<Vector3> vertices = [
                200f * new Vector3(-0.5f, -0.5f, 0f),
                200f * new Vector3(-0.5f, 0.5f, 0f),
                200f * new Vector3(0.5f, 0.5f, 0f),
                200f * new Vector3(0.5f, -0.5f, 0f),
            ];

            ReadOnlySpan<Glib.Color> colors = [
                Glib.Color.Red,
                Glib.Color.Red,
                Glib.Color.White,
                Glib.Color.White
            ];

            ReadOnlySpan<Vector2> uvs = [
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(1.0f, 0.0f)
            ];

            var mesh = Glib.StandardMesh.CreateIndexed([0, 1, 2, 2, 3, 0], 4);
            mesh.SetVertexData(vertices);
            mesh.SetColorData(colors);
            mesh.SetTexCoordData(uvs);
            mesh.Upload();

            Glib.Texture.DefaultFilterMode = Glib.TextureFilterMode.Nearest;

            var shader = Glib.Shader.Create();
            var texture = Glib.Texture.LoadFromFile("assets/icon128.png");

            rctx.UseGlLines = true;
            rctx.LineWidth = 3f;
            
            while (!window.IsClosing)
            {
                window.PollEvents();
                window.BeginRender();

                // draw rotating white square
                rctx.PushTransform();
                rctx.Translate(50f, 50f);
                rctx.Rotate((float)window.Time);
                rctx.DrawRectangle(-20f, -20f, 40f, 40f);
                rctx.PopTransform();

                // draw textured mesh
                rctx.Shader = shader;
                rctx.PushTransform();
                rctx.Translate(window.MouseX + 8, window.MouseY + 8);
                rctx.DrawColor = new Glib.Color(0f, 0f, 0f, 0.5f);
                rctx.Draw(mesh, texture);
                rctx.Translate(-8, -8);
                rctx.DrawColor = Glib.Color.White;
                rctx.Draw(mesh, texture);
                rctx.PopTransform();

                // draw another rotating white square
                rctx.PushTransform();
                rctx.Translate(90f, 90f);
                rctx.Rotate((float)window.Time);
                rctx.DrawColor = Glib.Color.Red;
                rctx.DrawRectangleLines(-20f, -20f, 40f, 40f);
                rctx.PopTransform();

                rctx.DrawColor = Glib.Color.Blue;
                rctx.DrawRing(window.MousePosition, 20f);

                window.EndRender();
                window.SwapBuffers();
            }

            window.Dispose();
        }
    }
}