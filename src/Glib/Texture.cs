using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

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

public class Texture : Resource
{
    private uint _handle;

    public readonly int Width;
    public readonly int Height;
    public readonly Glib.PixelFormat? PixelFormat;
    protected GLEnum GlTextureFormat { get; private set; }

    internal uint Handle => _handle;

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

    private unsafe void CreateTexture(PixelFormat format, void* data)
    {
        GlTextureFormat = format switch
        {
            Glib.PixelFormat.Grayscale => GLEnum.Red,
            Glib.PixelFormat.GrayscaleAlpha => GLEnum.RG,
            Glib.PixelFormat.RGB => GLEnum.Rgb,
            Glib.PixelFormat.RGBA => GLEnum.Rgba,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var gl = RenderContext.Gl;
        _handle = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2D, _handle);

        var wrapMode = (int)GLEnum.ClampToEdge;
        var filterMode = DefaultFilterMode switch
        {
            TextureFilterMode.Linear => (int)GLEnum.Linear,
            TextureFilterMode.Nearest => (int)GLEnum.Nearest,
            _ => throw new Exception("Unknown default texture filter mode")
        };

        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, ref wrapMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, ref wrapMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref filterMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref filterMode);
        
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)GlTextureFormat, (uint)Width, (uint)Height, 0, GlTextureFormat, GLEnum.UnsignedByte, data);

        var err = gl.GetError();
        if (err != 0)
        {
            throw new UnsupportedOperationException($"Could not create texture: {err}");
        }
    }
    
    internal unsafe Texture(int width, int height, PixelFormat format)
    {
        Width = width;
        Height = height;
        PixelFormat = format;
        CreateTexture(format, null);
    }

    internal unsafe Texture(int width, int height, GLEnum format, uint handle)
    {
        Width = width;
        Height = height;
        PixelFormat = GetPixelFormatFromTexture(format);
        _handle = handle;
        GlTextureFormat = format;
    }

    private unsafe byte* GetImageData(Image image)
    {
        if (image.PixelFormat != PixelFormat)
            throw new ArgumentException($"Mismatched pixel formats: expected {PixelFormat}, got {image.PixelFormat}");
        
        //Bgfx.Memory* alloc = Bgfx.alloc((uint)(image.Width * image.Height * image.BytesPerPixel));
        var allocSize = (nuint)(image.Width * image.Height * image.BytesPerPixel);
        var alloc = NativeMemory.Alloc(allocSize);
        image.CopyPixelDataTo(new Span<byte>(alloc, (int)allocSize));

        return (byte*) alloc;
    }

    internal unsafe Texture(Image image)
    {
        Width = image.Width;
        Height = image.Height;
        PixelFormat = image.PixelFormat;

        var alloc = GetImageData(image);
        CreateTexture(image.PixelFormat, alloc);
        NativeMemory.Free(alloc);
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
        RenderContext.Gl.DeleteTexture(_handle);
        _handle = 0;
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
        var gl = RenderContext.Gl;

        if (PixelFormat is null) throw new InvalidOperationException("Texture cannot be modified");
        
        if (image.Width != Width || image.Height != Height)
            throw new ArgumentException("Image dimensions must match texture dimensions", nameof(image));

        if (image.PixelFormat != PixelFormat)
            throw new ArgumentException("Mismatched pixel formats", nameof(image));

        var alloc = GetImageData(image);
        gl.BindTexture(GLEnum.Texture2D, _handle);
        gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)Width, (uint)Height, GlTextureFormat, GLEnum.UnsignedByte, alloc);
        NativeMemory.Free(alloc);
    }

    /// <summary>
    /// Update a part of a texture with an image.
    /// </summary>
    /// <param name="image">The image to update the texture with.</param>
    /// <exception cref="ArgumentException">Thrown if the image does not fit in the texture or does not have the same pixel format.</exception>
    public unsafe void UpdateFromImage(Image image, uint dstX, uint dstY)
    {
        var gl = RenderContext.Gl;

        if (PixelFormat is null) throw new InvalidOperationException("Texture cannot be modified");
        
        if (dstX > ushort.MaxValue || dstY > ushort.MaxValue || dstX + image.Width >= Width || dstY + image.Height >= Height)
            throw new ArgumentException("Image does not fit", nameof(image));

        if (image.PixelFormat != PixelFormat)
            throw new ArgumentException("Mismatched pixel formats", nameof(image));

        var alloc = GetImageData(image);
        gl.BindTexture(GLEnum.Texture2D, _handle);
        gl.TexSubImage2D(GLEnum.Texture2D, 0, (int)dstX, (int)dstY, (uint)image.Width, (uint)image.Height, GlTextureFormat, GLEnum.UnsignedByte, alloc);
        NativeMemory.Free(alloc);
    }

    /// <summary>
    /// Update the whole texture with an image given by a byte array. <br /><br />
    /// The dimensions and pixel format of the data will be interpreted as those of the texture.
    /// </summary>
    /// <param name="pixels">The image pixels to update the texture with.</param>
    /// <exception cref="ArgumentException">Thrown if the pixel data length is not the same as the texture's buffer size.</exception> 
    public unsafe void UpdateFromImage(ReadOnlySpan<byte> pixels)
    {
        var gl = RenderContext.Gl;

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

        //var alloc = Bgfx.alloc((uint)size);
        fixed (byte* data = pixels)
        {
            gl.BindTexture(GLEnum.Texture2D, _handle);
            gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)Width, (uint)Height, GlTextureFormat, GLEnum.UnsignedByte, data);
        }
    }

    /// <summary>
    /// Update the whole texture with a pre-allocated Bgfx memory handle.<br /><br />
    /// The dimensions and pixel format of the data will be interpreted as those of the texture.
    /// </summary>
    /// <param name="mem"></param>
    /// <exception cref="ArgumentException">Thrown if the memory allocation byte size does not match the texture byte size.</exception>
    /*public unsafe void UpdateFromMemory(Bgfx.Memory* mem)
    {
        var bytesPerPixel = PixelFormat switch
        {
            Glib.PixelFormat.Grayscale => 1,
            Glib.PixelFormat.GrayscaleAlpha => 2,
            Glib.PixelFormat.RGB => 3,
            Glib.PixelFormat.RGBA => 4,
            _ => 0
        };

        if (bytesPerPixel > 0)
        {
            var size = Width * Height * bytesPerPixel;
            if (mem->size != size)
                throw new ArgumentException("Mismatched pixel buffer sizes");
        }
        
        Bgfx.update_texture_2d(_handle, 0, 0, 0, 0, (ushort)Width, (ushort)Height, mem, ushort.MaxValue);
    }*/

    internal static Glib.PixelFormat? GetPixelFormatFromTexture(GLEnum fmt)
    {
        return fmt switch
        {
            GLEnum.Rgba8 => Glib.PixelFormat.RGBA,
            GLEnum.Rgb8 => Glib.PixelFormat.RGB,
            GLEnum.RG8 => Glib.PixelFormat.GrayscaleAlpha,
            GLEnum.R8 => Glib.PixelFormat.Grayscale,
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
    private readonly Framebuffer _fb;
    private readonly uint _index;

    internal unsafe ReadableTexture(int width, int height, GLEnum fmt, uint handle, Framebuffer fb, uint idx)
        : base(width, height, fmt, handle)
    {
        _fb = fb;
        _index = idx;
    }

    public unsafe Image GetImage()
    {
        if (PixelFormat is null) throw new InvalidOperationException("The texture's pixel format is not readable from the CPU");
        var gl = RenderContext.Gl;

        var oldFb = (uint)gl.GetInteger(GetPName.ReadFramebufferBinding);
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fb.Handle);
        gl.ReadBuffer((GLEnum)((int)GLEnum.ColorAttachment0 + _index));
        if (gl.GetError() != 0)
        {
            throw new UnsupportedOperationException($"Could not read framebuffer attachment");
        }

        byte* mem = null;

        try
        {
            var storageSize = Width * Height * Image.GetBytesPerPixel(PixelFormat.Value);
            mem = (byte*) NativeMemory.Alloc((nuint)storageSize);
            gl.ReadPixels(0, 0, (uint)Width, (uint)Height, GlTextureFormat, GLEnum.UnsignedByte, mem);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, oldFb);

            if (gl.GetError() != 0)
            {
                throw new UnsupportedOperationException($"Could not read framebuffer attachment");
            }

            var pixelSpan = new ReadOnlySpan<byte>(mem, (int)storageSize);
            if (GlTextureFormat is GLEnum.Rgba)
            {
                Debug.Assert(storageSize == Width * Height * 4);
                return new Image(pixelSpan, Width, Height, Glib.PixelFormat.RGBA);
            }
            else if (GlTextureFormat is GLEnum.Red)
            {
                Debug.Assert(storageSize == Width * Height);
                return new Image(pixelSpan, Width, Height, Glib.PixelFormat.Grayscale);
            }
            {
                throw new NotImplementedException($"Readback from {GlTextureFormat} format is not implemented");
            }
        }
        finally
        {
            if (mem != null)
                NativeMemory.Free(mem);
        }
    }
}