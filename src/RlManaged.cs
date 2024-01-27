using Raylib_cs;

namespace RlManaged
{
    public class RenderTexture2D : IDisposable
    {
        private Raylib_cs.RenderTexture2D raw;
        private bool _disposed = false;

        public RenderTexture2D(int width, int height)
        {
            raw = Raylib.LoadRenderTexture(width, height);
        }

        ~RenderTexture2D() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Raylib.UnloadRenderTexture(raw);
                _disposed = true;
            }
        }

        // OpenGL Framebuffer object ID
        public uint Id { get => raw.Id; }

        // Color buffer attachment texture
        public Raylib_cs.Texture2D Texture { get => raw.Texture; }

        // Depth buffer attachment texture
        public Raylib_cs.Texture2D Depth { get => raw.Depth; }

        public static implicit operator Raylib_cs.RenderTexture2D(RenderTexture2D tex) => tex.raw;
    }

    public class Texture2D : IDisposable
    {
        private Raylib_cs.Texture2D raw;
        private bool _disposed = false;

        public Texture2D(Image image)
        {
            raw = Raylib.LoadTextureFromImage(image);
        }

        public Texture2D(string fileName)
        {
            raw = Raylib.LoadTexture(fileName);
        }

        ~Texture2D() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                Raylib.UnloadTexture(raw);
            }
        }

        public static implicit operator Raylib_cs.Texture2D(Texture2D tex) => tex.raw;
    }
}