using System.Runtime.InteropServices;
using System.Text;
using Raylib_cs;

namespace RlManaged
{
    class RenderTexture2D : IDisposable
    {
        private Raylib_cs.RenderTexture2D raw;
        private bool _disposed = false;

        private RenderTexture2D(Raylib_cs.RenderTexture2D raw)
        {
            this.raw = raw;
        }

        public static RenderTexture2D Load(int width, int height)
            => new(Raylib.LoadRenderTexture(width, height));

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

    class Image : IDisposable
    {
        private Raylib_cs.Image raw;
        private bool _disposed = false;

        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }
        public int Mipmaps { get => raw.Mipmaps; }
        public PixelFormat PixelFormat { get => raw.Format; }

        private Image(Raylib_cs.Image raw)
        {
            this.raw = raw;
        }

        public static Image Load(string fileName)
            => new(Raylib.LoadImage(fileName));

        public static Image LoadFromTexture(Raylib_cs.Texture2D texture)
            => new (Raylib.LoadImageFromTexture(texture));
        
        public static Image GenColor(int width, int height, Color color)
            => new(Raylib.GenImageColor(width, height, color));

        public unsafe void DrawPixel(int x, int y, Color color)
        {
            fixed (Raylib_cs.Image* rawPtr = &raw)
                Raylib.ImageDrawPixel(rawPtr, x, y, color);
        }

        public unsafe byte[] ExportToMemory(string format)
        {
            int fileSize = 0;
            byte[] formatBytes = Encoding.ASCII.GetBytes(format);
            char* memBuf = null;

            fixed (byte* formatBytesPtr = formatBytes)
            {
                memBuf = Raylib.ExportImageToMemory(raw, (sbyte*) formatBytesPtr, &fileSize);
            }

            if (memBuf == null)
                throw new Exception();
            
            byte[] result = new byte[fileSize];
            Marshal.Copy((nint)memBuf, result, 0, fileSize);
            Raylib.MemFree(memBuf);

            return result;
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

    class Texture2D : IDisposable
    {
        private Raylib_cs.Texture2D raw;
        private bool _disposed = false;

        public uint Id { get => raw.Id; }
        public int Width { get => raw.Width; }
        public int Height { get => raw.Height; }

        private Texture2D(Raylib_cs.Texture2D raw)
        {
            this.raw = raw;
        }

        public static Texture2D LoadFromImage(Raylib_cs.Image image)
            => new(Raylib.LoadTextureFromImage(image));

        public static Texture2D Load(string fileName)
            => new(Raylib.LoadTexture(fileName));

        ~Texture2D() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                throw new Exception("Must manually call Dispose");
            }
            
            if (!_disposed)
            {
                _disposed = true;
                Raylib.UnloadTexture(raw);
            }
        }

        public static implicit operator Raylib_cs.Texture2D(Texture2D tex) => tex.raw;
    }

    class Shader : IDisposable
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