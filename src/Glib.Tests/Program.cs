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
            rctx.BackgroundColor = new Glib.Color(0.5f, 0.5f, 0.5f, 1f);

            ReadOnlySpan<Vector3> vertices = [
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 200f, 0f),
                new Vector3(200f, 200f, 0f),
                new Vector3(200f, 0f, 0f),
            ];

            ReadOnlySpan<Glib.Color> colors = [
                Glib.Color.White,
                Glib.Color.White,
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

            var shader = Glib.Shader.Create();
            var texture = Glib.Texture.LoadFromFile("assets/icon128.png");

            shader.SetUniform("glib_texture", texture);
            
            while (!window.IsClosing)
            {
                window.PollEvents();
                window.BeginRender();

                rctx.Shader = shader;
                rctx.PushTransform();
                rctx.Translate(window.MouseX, window.MouseY, 0f);
                rctx.Draw(mesh);
                rctx.PopTransform();

                window.EndRender();
                window.SwapBuffers();
            }

            window.Dispose();
        }
    }
}