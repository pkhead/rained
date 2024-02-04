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

    public class Image : IDisposable
    {
        private Raylib_cs.Image raw;
        private bool _disposed = false;

        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }
        public int Mipmaps { get => raw.Mipmaps; }
        public PixelFormat PixelFormat { get => raw.Format; }

        public Image(Raylib_cs.Image raw)
        {
            this.raw = raw;
        }

        public Image(string fileName)
        {
            raw = Raylib.LoadImage(fileName);
        }

        public Image(int width, int height, Color color)
        {
            raw = Raylib.GenImageColor(width, height, color);
        }

        public unsafe void DrawPixel(int x, int y, Color color)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageDrawPixel(rawPtr, x, y, color);
        }

        /*public unsafe void Draw(Raylib_cs.Image src, Rectangle srcRec, Rectangle dstRec, Color tint)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageDraw()
        }
        */

        public unsafe void Format(PixelFormat newFormat)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageFormat(rawPtr, newFormat);
        }

        public Image(Raylib_cs.Image image, Rectangle rec)
        {
            raw = Raylib.ImageFromImage(image, rec);
        }

        ~Image() => Dispose(false);

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
                Raylib.UnloadImage(raw);
            }
        }

        public static implicit operator Raylib_cs.Image(Image tex) => tex.raw;

        public ref Raylib_cs.Image Ref() => ref raw;
    }

    public class Texture2D : IDisposable
    {
        private Raylib_cs.Texture2D raw;
        private bool _disposed = false;

        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }

        public Texture2D()
        {}

        public Texture2D(Raylib_cs.Image image)
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

    public class Shader : IDisposable
    {
        private Raylib_cs.Shader raw;
        private bool _disposed = false;

        private Shader(Raylib_cs.Shader src)
        {
            raw = src;
        }

        public static Shader LoadFromMemory(string? vsCode, string? fsCode)
            => new(Raylib.LoadShaderFromMemory(vsCode, fsCode));
        
        public static Shader Load(string? vsFileName, string? fsFileName)
            => new(Raylib.LoadShader(vsFileName, fsFileName));

        ~Shader() => Dispose(false);

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
                Raylib.UnloadShader(raw);
            }
        }

        public static implicit operator Raylib_cs.Shader(Shader tex) => tex.raw;
    }
}