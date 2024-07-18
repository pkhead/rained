using System.Diagnostics;
using System.Runtime.InteropServices;
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
    Mirror,
}

public class Texture : BgfxResource
{
    private Bgfx.TextureHandle _handle;

    public readonly int Width;
    public readonly int Height;
    public readonly Glib.PixelFormat? PixelFormat;
    protected Bgfx.TextureFormat BgfxTextureFormat { get; private set; }

    internal Bgfx.TextureHandle Handle => _handle;

    public static TextureFilterMode DefaultFilterMode { get; set; } = TextureFilterMode.Linear;

    public TextureFilterMode MinFilterMode = DefaultFilterMode;
    public TextureFilterMode MagFilterMode = DefaultFilterMode;
    public TextureFilterMode FilterMode {
        set
        {
            MinFilterMode = value;
            MagFilterMode = value;
        }
    }
    public TextureWrapMode WrapModeU = TextureWrapMode.Clamp;
    public TextureWrapMode WrapModeV = TextureWrapMode.Clamp;
    public TextureWrapMode WrapModeUV
    {
        set
        {
            WrapModeU = value;
            WrapModeV = value;
        }
    }

    private unsafe void CreateTexture(PixelFormat format)
    {
        BgfxTextureFormat = format switch
        {
            Glib.PixelFormat.Grayscale => Bgfx.TextureFormat.R8,
            Glib.PixelFormat.GrayscaleAlpha => Bgfx.TextureFormat.RG8,
            Glib.PixelFormat.RGB => Bgfx.TextureFormat.RGB8,
            Glib.PixelFormat.RGBA => Bgfx.TextureFormat.RGBA8,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var flags = Bgfx.SamplerFlags.UClamp |
                    Bgfx.SamplerFlags.VClamp |
                    Bgfx.SamplerFlags.MinAnisotropic |
                    Bgfx.SamplerFlags.MagAnisotropic;

        if (Width < 0 || Height < 0 || Width > ushort.MaxValue || Height > ushort.MaxValue || Bgfx.is_texture_valid(0, false, 1, BgfxTextureFormat, (ulong) flags))
        {
            _handle = Bgfx.create_texture_2d((ushort)Width, (ushort)Height, false, 1, BgfxTextureFormat, (ulong)flags, null);
        }
        else
        {
            throw new UnsupportedRendererOperationException($"Could not create texture of size ({Width}, {Height}) and format {format}");
        }
    }
    
    internal unsafe Texture(int width, int height, PixelFormat format)
    {
        Width = width;
        Height = height;
        PixelFormat = format;
        CreateTexture(format);
    }

    internal unsafe Texture(int width, int height, PixelFormat? format, Bgfx.TextureHandle handle)
    {
        Width = width;
        Height = height;
        PixelFormat = format;
        _handle = handle;
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

    /// <summary>
    /// Create an uninitialized texture.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <param name="format">The pixel format of the texture.</param>
    /// <returns>A new, uninitialized texture.</returns>
    public static Texture Create(int width, int height, PixelFormat format) => new(width, height, format);

    /// <summary>
    /// Create a texture from an Image.
    /// </summary>
    /// <param name="image">The image to source from.</param>
    /// <returns>A new texture.</returns>
    public static Texture Load(Image image) => new(image);

    /// <summary>
    /// Load an image from a file path and use it to a create a texture.
    /// </summary>
    /// <param name="filePath">The path to the image.</param>
    /// <returns>A new texture.</returns>
    public static Texture Load(string filePath, PixelFormat format = Glib.PixelFormat.RGBA)
    {
        using var img = Image.FromFile(filePath, format);
        return new Texture(img);
    }

    protected override void FreeResources(bool disposing)
    {
        Bgfx.destroy_texture(_handle);
        _handle.idx = ushort.MaxValue;
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
        if (PixelFormat is null) throw new InvalidOperationException("Texture cannot be modified");
        
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
        if (PixelFormat is null) throw new InvalidOperationException("Texture cannot be modified");

        var bytesPerPixel = PixelFormat switch
        {
            Glib.PixelFormat.Grayscale => 1,
            Glib.PixelFormat.GrayscaleAlpha => 2,
            Glib.PixelFormat.RGB => 3,
            Glib.PixelFormat.RGBA => 4,
            _ => throw new Exception("Texture.PixelFormat is somehow invalid")
        };

        var size = Width * Height * bytesPerPixel;
        if (pixels.Length != size)
            throw new ArgumentException("Mismatched pixel buffer sizes");

        var alloc = Bgfx.alloc((uint)size);
        pixels.CopyTo(new Span<byte>(alloc->data, (int)alloc->size));
        Bgfx.update_texture_2d(_handle, 0, 0, 0, 0, (ushort)Width, (ushort)Height, alloc, ushort.MaxValue);
    }

    internal static Glib.PixelFormat? GetPixelFormatFromTexture(Bgfx.TextureFormat fmt)
    {
        return fmt switch
        {
            Bgfx.TextureFormat.RGBA8 => Glib.PixelFormat.RGBA,
            Bgfx.TextureFormat.RGB8 => Glib.PixelFormat.RGB,
            Bgfx.TextureFormat.RG8 => Glib.PixelFormat.GrayscaleAlpha,
            Bgfx.TextureFormat.R8 => Glib.PixelFormat.Grayscale,
            _ => null
        };
    }
}

/// <summary>
/// A texture whose data can be read back from the GPU.
/// This is only created by framebuffers.
/// </summary>
public class ReadableTexture : Texture
{
    private Bgfx.TextureHandle _blitDest;

    internal unsafe ReadableTexture(int width, int height, Bgfx.TextureFormat fmt, Bgfx.TextureHandle handle)
        : base(width, height, Texture.GetPixelFormatFromTexture(fmt), handle)
    {
        var flags = Bgfx.TextureFlags.BlitDst | Bgfx.TextureFlags.ReadBack;
        if (!Bgfx.is_texture_valid(0, false, 1, fmt, (ulong)flags))
        {
            throw new UnsupportedRendererOperationException("Could not create ReadableFramebufferTexture");
        }

        _blitDest = Bgfx.create_texture_2d(
            _width: (ushort)Width,
            _height: (ushort)Height,
            _hasMips: false,
            _numLayers: 1,
            _format: fmt,
            _flags: (ulong)flags,
            _mem: null
        );
        Debug.Assert(_blitDest.Valid);
    }

    protected override void FreeResources(bool disposing)
    {
        base.FreeResources(disposing);
        Bgfx.destroy_texture(_blitDest);
    }

    /*public unsafe Image GetImage()
    {
        if (PixelFormat is null) throw new InvalidOperationException("The texture's pixel format is not readable from the CPU");

        byte* mem = null;

        try
        {
            Bgfx.TextureInfo texInfo = new();
            Bgfx.calc_texture_size(&texInfo, (ushort)Width, (ushort)Height, 0, false, false, 1, BgfxTextureFormat);
            mem = (byte*) NativeMemory.Alloc((nuint)texInfo.storageSize);
            Bgfx.read_texture(Handle, mem, 0);

            if (BgfxTextureFormat == Bgfx.TextureFormat.RGBA8)
            {
            }
            else
            {
                throw new NotImplementedException($"Readback from {BgfxTextureFormat} format is not implemented");
            }
        }
        finally
        {
            if (mem != null)
                NativeMemory.Free(mem);
        }
    }*/
}