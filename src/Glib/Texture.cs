using Bgfx_cs;

namespace Glib;

public enum TextureFilterMode
{
    Nearest,
    Linear,
}

public enum TextureWrapMode
{
    Clamp,
    Repeat
}

public class Texture : BgfxResource
{
    private Bgfx.TextureHandle _handle;

    public readonly int Width;
    public readonly int Height;
    public readonly Glib.PixelFormat PixelFormat;

    internal Bgfx.TextureHandle Handle => _handle;

    public TextureFilterMode MinFilterMode = TextureFilterMode.Linear;
    public TextureFilterMode MagFilterMode = TextureFilterMode.Linear;
    public TextureFilterMode FilterMode {
        set
        {
            MinFilterMode = value;
            MagFilterMode = value;
        }
    }
    public TextureWrapMode WrapMode = TextureWrapMode.Clamp;

    private unsafe void CreateTexture(PixelFormat format)
    {
        var bgfxFormat = format switch
        {
            PixelFormat.Grayscale => Bgfx.TextureFormat.R8U,
            PixelFormat.GrayscaleAlpha => Bgfx.TextureFormat.RG8,
            PixelFormat.RGB => Bgfx.TextureFormat.RGB8,
            PixelFormat.RGBA => Bgfx.TextureFormat.RGBA8,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var flags = Bgfx.SamplerFlags.UClamp |
                    Bgfx.SamplerFlags.VClamp |
                    Bgfx.SamplerFlags.MinAnisotropic |
                    Bgfx.SamplerFlags.MagAnisotropic;

        if (Width < 0 || Height < 0 || Width > ushort.MaxValue || Height > ushort.MaxValue || Bgfx.is_texture_valid(0, false, 1, bgfxFormat, (ulong) flags))
        {
            _handle = Bgfx.create_texture_2d((ushort)Width, (ushort)Height, false, 1, bgfxFormat, (ulong)flags, null);
        }
        else
        {
            throw new UnsupportedOperationException($"Could not create texture of size ({Width}, {Height}) and format {format}");
        }
    }
    
    internal unsafe Texture(int width, int height, PixelFormat format)
    {
        Width = width;
        Height = height;
        PixelFormat = format;
        CreateTexture(format);
    }

    private unsafe Bgfx.Memory* GetImageData(Image image)
    {
        if (image.PixelFormat != PixelFormat)
            throw new ArgumentException($"Mismatched pixel formats: expected {PixelFormat}, got {image.PixelFormat}");
        
        Bgfx.Memory* alloc = Bgfx.alloc((uint)(image.Width * image.Height * image.BytesPerPixel));
        image.CopyPixelDataTo(new Span<byte>(alloc->data, (int)alloc->size));

        return alloc;
    }

    internal unsafe Texture(Image image)
    {
        Width = image.Width;
        Height = image.Height;
        PixelFormat = image.PixelFormat;

        CreateTexture(image.PixelFormat);
        var alloc = GetImageData(image);
        Bgfx.update_texture_2d(_handle, 0, 0, 0, 0, (ushort)Width, (ushort)Height, alloc, ushort.MaxValue);
    }

    protected override void FreeResources(bool disposing)
    {
        Bgfx.destroy_texture(_handle);
    }

    /*public unsafe Image ToImage(PixelFormat pixelFormat = PixelFormat.RGBA)
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
    }*/

    /// <summary>
    /// Update the whole texture with an image.
    /// The given image must have the same dimensions as the texture.
    /// </summary>
    /// <param name="image">The image to update the texture with.</param>
    /// <exception cref="ArgumentException">Thrown if the image does not have the same dimensions and pixel format as the texture.</exception>
    public unsafe void UpdateFromImage(Image image)
    {
        if (image.Width != Width || image.Height != Height)
        {
            throw new ArgumentException("Image dimensions must match texture dimensions", nameof(image));
        }

        var alloc = GetImageData(image);
        Bgfx.update_texture_2d(_handle, 0, 0, 0, 0, (ushort)Width, (ushort)Height, alloc, ushort.MaxValue);
    }

    /// <summary>
    /// Update the whole texture with an image given by a byte array. <br /><br />
    /// The dimensions and pixel format of the data will be interpreted as those of the texture.
    /// </summary>
    /// <param name="pixels">The image pixels to update the texture with.</param>
    /// <exception cref="ArgumentException">Thrown if the pixel data length is not the same as the texture's buffer size.</exception> 
    public unsafe void UpdateFromImage(ReadOnlySpan<byte> pixels)
    {
        var bytesPerPixel = PixelFormat switch
        {
            PixelFormat.Grayscale => 1,
            PixelFormat.GrayscaleAlpha => 2,
            PixelFormat.RGB => 3,
            PixelFormat.RGBA => 4,
            _ => throw new Exception("Texture.PixelFormat is somehow invalid")
        };

        var size = Width * Height * bytesPerPixel;
        if (pixels.Length != size)
            throw new ArgumentException("Mismatched pixel buffer sizes");

        var alloc = Bgfx.alloc((uint)size);
        pixels.CopyTo(new Span<byte>(alloc->data, (int)alloc->size));
        Bgfx.update_texture_2d(_handle, 0, 0, 0, 0, (ushort)Width, (ushort)Height, alloc, ushort.MaxValue);
    }
}