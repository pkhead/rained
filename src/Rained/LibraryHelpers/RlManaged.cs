/**
* Rained no longer uses Raylib. I have replaced all Raylib calls
* with an implementation that uses my Glib library.
*
* As such, this file only continues to exist so that I don't
* have to adapt all of my pre-existing code to Glib.
*
* If you are looking for the original RlManaged code that
* works with Raylib, look at the source code of this file
* from this commit:
*
* 9a0df9ee9b3b2e8f9165734f23f9da8732a32e15
* (Git commit SHA)
*/
using System.Numerics;
using Raylib_cs;

namespace RlManaged
{
    abstract class RlObject : IDisposable
    {
        protected abstract Glib.GLResource? GetGLResource();

        public void Dispose()
        {
            GetGLResource()?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    class RenderTexture2D : RlObject
    {
        private Raylib_cs.RenderTexture2D raw;
        protected override Glib.GLResource? GetGLResource() => raw.ID;

        public RenderTexture2D(Raylib_cs.RenderTexture2D raw) : base()
        {
            this.raw = raw;
        }

        public static RenderTexture2D Load(int width, int height)
            => LoadWithAlpha(width, height);

        private static RenderTexture2D LoadWithAlpha(int width, int height)
            => new(Raylib.LoadRenderTexture(width, height));

        // OpenGL Framebuffer object ID

        // Color buffer attachment texture
        public Raylib_cs.Texture2D Texture { get => new() { ID = raw.ID!.GetTexture(0) }; }

        public static implicit operator Raylib_cs.RenderTexture2D(RenderTexture2D tex) => tex.raw;
    }

    class Image : IDisposable
    {
        private Raylib_cs.Image raw;

        public int Width { get => raw.image!.Width; }
        public int Height { get => raw.image!.Height; }
        //public byte[] Data { get => throw new NotImplementedException(); }
        public Raylib_cs.PixelFormat PixelFormat => raw.image!.PixelFormat switch
        {
            Glib.PixelFormat.Grayscale => Raylib_cs.PixelFormat.UncompressedGrayscale,
            Glib.PixelFormat.RGBA => Raylib_cs.PixelFormat.UncompressedR8G8B8A8,
            _ => throw new NotImplementedException(raw.image!.PixelFormat.ToString())
        };

        private static int GetBitsPerPixel(Raylib_cs.PixelFormat format)
        {
            return format switch
            {
                Raylib_cs.PixelFormat.UncompressedGrayscale => 8,
                //Raylib_cs.PixelFormat.UncompressedGrayAlpha => 16,
                //Raylib_cs.PixelFormat.UncompressedR5G6B5 => 16,
                //Raylib_cs.PixelFormat.UncompressedR8G8B8 => 24,
                //Raylib_cs.PixelFormat.UncompressedR5G5B5A1 => 16,
                //Raylib_cs.PixelFormat.UncompressedR4G4B4A4 => 16,
                Raylib_cs.PixelFormat.UncompressedR8G8B8A8 => 32,
                //Raylib_cs.PixelFormat.UncompressedR32 => 32,
                //Raylib_cs.PixelFormat.UncompressedR32G32B32 => 32*3,
                //Raylib_cs.PixelFormat.UncompressedR32G32B32A32 => 32*4,
                //Raylib_cs.PixelFormat.CompressedDxt1Rgb => 4,
                //Raylib_cs.PixelFormat.CompressedDxt1Rgba => 4,
                //Raylib_cs.PixelFormat.CompressedDxt3Rgba => 8,
                //Raylib_cs.PixelFormat.CompressedDxt5Rgba => 8,
                //Raylib_cs.PixelFormat.CompressedEtc1Rgb => 4,
                //Raylib_cs.PixelFormat.CompressedEtc2Rgb => 4,
                //Raylib_cs.PixelFormat.CompressedEtc2EacRgba => 8,
                //Raylib_cs.PixelFormat.CompressedPvrtRgb => 4,
                //Raylib_cs.PixelFormat.CompressedPvrtRgba => 4,
                //Raylib_cs.PixelFormat.CompressedAstc4X4Rgba => 8,
                //Raylib_cs.PixelFormat.CompressedAstc8X8Rgba => 2,
                _ => throw new Exception("Invalid pixel format")
            };
        }
        
        public Image(Raylib_cs.Image raw) : base()
        {
            this.raw = raw;
        }

        public static Image Load(string fileName)
            => new(Raylib.LoadImage(fileName));

        public static Image LoadFromTexture(Raylib_cs.Texture2D texture)
            => new (Raylib.LoadImageFromTexture(texture));
        
        public static Image GenColor(int width, int height, Raylib_cs.Color color)
            => new(Raylib.GenImageColor(width, height, color));
        
        //public static Image FromImage(Raylib_cs.Image image, Raylib_cs.Rectangle rec)
        //    => new(Raylib.ImageFromImage(image, rec));
        
        public static Image Copy(Raylib_cs.Image image)
            => new(Raylib.ImageCopy(image));

        public void DrawPixel(int x, int y, Color color)
        {
            Raylib.ImageDrawPixel(raw, x, y, color);
        }
        
        public void UpdateTexture(Raylib_cs.Texture2D texture)
        {
            Raylib.UpdateTexture(texture, raw);
        }
        
        public void Format(PixelFormat newFormat)
        {
            Raylib.ImageFormat(ref raw, newFormat);
        }

        public void Dispose()
        {
            Raylib.UnloadImage(raw);
        }

        public static implicit operator Raylib_cs.Image(Image tex) => tex.raw;

        public ref Raylib_cs.Image Ref() => ref raw;
    }

    class Texture2D : RlObject
    {
        private Raylib_cs.Texture2D raw;
        public Glib.Texture GlibTexture => raw.ID!;
        protected override Glib.GLResource? GetGLResource() => raw.ID;
        
        public int Width { get => raw.ID!.Width; }
        public int Height { get => raw.ID!.Height; }

        public Texture2D(Raylib_cs.Texture2D raw)
        {
            this.raw = raw;
        }

        public static Texture2D LoadFromImage(Raylib_cs.Image image)
            => new(Raylib.LoadTextureFromImage(image));

        public static Texture2D Load(string fileName)
            => new(Raylib.LoadTexture(fileName));
        
        public static implicit operator Raylib_cs.Texture2D(Texture2D tex) => tex.raw;
    }

    class Shader : RlObject
    {
        private Raylib_cs.Shader raw;
        public Glib.Shader GlibShader => raw.ID!;
        protected override Glib.GLResource? GetGLResource() => raw.ID;
        
        private Shader(Raylib_cs.Shader src)
        {
            raw = src;
        }

        public static Shader LoadFromMemory(string? vsCode, string? fsCode)
            => new(Raylib.LoadShaderFromMemory(vsCode, fsCode));
        
        public static Shader Load(string? vsFileName, string? fsFileName)
            => new(Raylib.LoadShader(vsFileName, fsFileName));

        public static implicit operator Raylib_cs.Shader(Shader tex) => tex.raw;
    }
}