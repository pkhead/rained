using Silk.NET.OpenGL;

namespace Glib;

public enum TextureFilterMode
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear
}

public enum TextureWrapMode
{
    ClampToEdge,
    ClampToBorder,
    MirroredRepeat,
    Repeat
}

public class Texture : GLResource
{
    private uint texture;
    private readonly GL gl;

    public readonly int Width;
    public readonly int Height;

    public uint TextureHandle { get => texture; }

    private static GLEnum GLWrapMode(TextureWrapMode v)
        => v switch
        {
            TextureWrapMode.ClampToEdge => GLEnum.ClampToEdge,
            TextureWrapMode.ClampToBorder => GLEnum.ClampToBorder,
            TextureWrapMode.MirroredRepeat => GLEnum.MirroredRepeat,
            TextureWrapMode.Repeat => GLEnum.Repeat,
            _ => throw new ArgumentOutOfRangeException(nameof(v))
        };

    private static GLEnum GLFilterMode(TextureFilterMode v)
        => v switch
        {
            TextureFilterMode.Nearest => GLEnum.Nearest,
            TextureFilterMode.Linear => GLEnum.Linear,
            TextureFilterMode.NearestMipmapNearest => GLEnum.NearestMipmapNearest,
            TextureFilterMode.LinearMipmapNearest => GLEnum.LinearMipmapNearest,
            TextureFilterMode.NearestMipmapLinear => GLEnum.NearestMipmapLinear,
            TextureFilterMode.LinearMipmapLinear => GLEnum.LinearMipmapLinear,
            _ => throw new ArgumentOutOfRangeException(nameof(v))
        };
    
    internal Texture(GL gl, int width, int height, GLEnum format, bool mipmaps = false)
    {
        this.gl = gl;

        Width = width;
        Height = height;

        texture = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, texture);

        unsafe
        {
            gl.TexImage2D(
                target: GLEnum.Texture2D,
                level: 0,
                internalformat: (int)format,
                width: (uint)width,
                height: (uint)height,
                border: 0,
                format: format,
                type: GLEnum.UnsignedByte,
                pixels: null
            );
        }

        if (mipmaps) gl.GenerateMipmap(GLEnum.Texture2D);

        int defaultFilter = (int)GLEnum.Linear;
        int defaultWrapMode = (int)GLEnum.ClampToEdge;
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref defaultFilter);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref defaultFilter);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, ref defaultWrapMode);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, ref defaultWrapMode);
    }

    internal Texture(GL gl, Image image, bool mipmaps = false)
    {
        this.gl = gl;

        Width = image.Width;
        Height = image.Height;

        var fmt = image.PixelFormat switch
        {
            PixelFormat.Grayscale => GLEnum.Red,
            PixelFormat.GrayscaleAlpha => GLEnum.RG,
            PixelFormat.RGB => GLEnum.Rgb,
            PixelFormat.RGBA => GLEnum.Rgba,
            _ => throw new ArgumentOutOfRangeException(nameof(image))
        };

        texture = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, texture);

        unsafe
        {
            fixed (byte* data = image.Pixels)
            {
                gl.TexImage2D(
                    target: GLEnum.Texture2D,
                    level: 0,
                    internalformat: (int)InternalFormat.Rgba,
                    width: (uint)image.Width,
                    height: (uint)image.Height,
                    border: 0,
                    format: fmt,
                    type: GLEnum.UnsignedByte,
                    pixels: data
                );

            }
        }

        if (mipmaps) gl.GenerateMipmap(GLEnum.Texture2D);

        int defaultFilter = (int)GLEnum.Linear;
        int defaultWrapMode = (int)GLEnum.ClampToEdge;
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref defaultFilter);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref defaultFilter);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, ref defaultWrapMode);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, ref defaultWrapMode);
    }

    protected override void FreeResources(bool disposing)
    {
        if (disposing)
        {
            gl.DeleteTexture(texture);
        }
        else
        {
            QueueFreeHandle(gl.DeleteTexture, texture);
        }

        texture = 0;
    }

    public unsafe void SetWrapMode(TextureWrapMode s, TextureWrapMode t)
    {
        gl.BindTexture(GLEnum.Texture2D, texture);
        int _s = (int)GLWrapMode(s);
        int _t = (int)GLWrapMode(t);

        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, &_s);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, &_t);
    }

    public unsafe void SetFilterMode(TextureFilterMode minFilter, TextureFilterMode magFilter)
    {
        gl.BindTexture(GLEnum.Texture2D, texture);
        int _min = (int)GLFilterMode(minFilter);
        int _mag = (int)GLFilterMode(magFilter);

        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, &_min);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, &_mag);
    }

    public void SetFilterMode(TextureFilterMode filter)
        => SetFilterMode(filter, filter);

    public unsafe Image ToImage(PixelFormat pixelFormat = PixelFormat.RGBA)
    {
        var format = pixelFormat switch
        {
            PixelFormat.Grayscale => GLEnum.Red,
            PixelFormat.GrayscaleAlpha => GLEnum.RG,
            PixelFormat.RGB => GLEnum.Rgb,
            PixelFormat.RGBA => GLEnum.Rgba,
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat))
        };

        byte[] pixels = new byte[Width * Height * Image.GetBytesPerPixel(pixelFormat)];

        gl.BindTexture(GLEnum.Texture2D, texture);

        fixed (byte* ptr = pixels)
        {
            gl.GetTexImage(GLEnum.Texture2D, 0, format, GLEnum.UnsignedByte, ptr);
        }

        return new Image(pixels, Width, Height, pixelFormat);
    }

    public unsafe void UpdateFromImage(Image image)
    {
        if (image.Width != Width || image.Height != Height)
        {
            throw new Exception("Image dimensions must match texture dimensions");
        }

        var fmt = image.PixelFormat switch
        {
            PixelFormat.Grayscale => GLEnum.Red,
            PixelFormat.GrayscaleAlpha => GLEnum.RG,
            PixelFormat.RGB => GLEnum.Rgb,
            PixelFormat.RGBA => GLEnum.Rgba,
            _ => throw new ArgumentOutOfRangeException(nameof(image))
        };

        gl.BindTexture(GLEnum.Texture2D, texture);

        fixed (byte* ptr = image.Pixels)
        {
            gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height, fmt, GLEnum.UnsignedByte, ptr);
        }
    }

    internal void Activate(uint unit)
    {
        if (unit >= 16)
            throw new ArgumentOutOfRangeException(nameof(unit), "The given unit index is greater than 15");
        
        gl.ActiveTexture((GLEnum)((int)GLEnum.Texture0 + unit));
        gl.BindTexture(GLEnum.Texture2D, texture);
    }
}