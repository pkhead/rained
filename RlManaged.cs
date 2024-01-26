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
            GC.SuppressFinalize(true);
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
        public Texture2D Texture { get => raw.Texture; }

        // Depth buffer attachment texture
        public Texture2D Depth { get => raw.Depth; }

        public static implicit operator Raylib_cs.RenderTexture2D(RenderTexture2D tex) => tex.raw;
    }
}