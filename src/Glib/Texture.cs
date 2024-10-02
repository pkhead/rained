using System.Diagnostics;
using System.Runtime.InteropServices;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace Glib;

public enum TextureFilterMode
{
    Nearest,
    Linear,
}

public enum TextureWrapMode
{
    Clamp,
    Repeat,
    Mirror,
}

public class Texture : Resource
{
    private uint _handle;

    public readonly int Width;
    public readonly int Height;
    public readonly Glib.PixelFormat? PixelFormat;
    protected GLEnum GlTextureFormat { get; private set; }

    /// <summary>
    /// Size of the texture in video memory, used for diagnostics.
    /// </summary>
    private uint textureSize;

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

    private static int? _maxSize = null;

    /// <summary>
    /// The maxmimum size in any given axis of a texture that the graphics backend can support.
    /// </summary>
    public static int MaxSize => _maxSize ??= RenderContext.Gl.GetInteger(GetPName.MaxTextureSize);

    private unsafe void CreateTexture(PixelFormat format, void* data)
    {
        if (Width <= 0 || Height <= 0)
            throw new InvalidOperationException("Width and height must be integers greater than 0");
        
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
        var filterMode = (int)GLFilterMode(DefaultFilterMode);

        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, ref wrapMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, ref wrapMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref filterMode);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref filterMode);
        GlUtil.CheckError(gl, "Could not create texture");
        
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)GlTextureFormat, (uint)Width, (uint)Height, 0, GlTextureFormat, GLEnum.UnsignedByte, data);
        GlUtil.CheckError(gl, "Could not create texture");

        uint pixelByteSize = format switch
        {
            Glib.PixelFormat.Grayscale => 1,
            Glib.PixelFormat.GrayscaleAlpha => 2,
            Glib.PixelFormat.RGB => 3,
            Glib.PixelFormat.RGBA => 4,
            _ => throw new UnreachableException()
        };
        
        var resList = RenderContext.Instance!.resourceList;
        textureSize = pixelByteSize * (uint)Width * (uint)Height;

        lock (resList.syncRoot)
            resList.totalTextureMemory += textureSize;
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
        var gl = RenderContext.Gl;

        Width = width;
        Height = height;
        PixelFormat = GetPixelFormatFromTexture(format);
        _handle = handle;
        GlTextureFormat = format;
        
        // calculate size of texture for diagnostics
        textureSize = 0;
        /*
        var oldTex = (uint) gl.GetInteger(GLEnum.TextureBinding2D);
        gl.BindTexture(GLEnum.Texture2D, _handle);
        GlUtil.CheckError(gl, "Could not query texture internal format");

        gl.GetTexLevelParameter(GLEnum.Texture2D, 0, GLEnum.TextureInternalFormat, out int internalFormat);
        GlUtil.CheckError(gl, "Could not query texture internal format");

        uint pixelByteSize = (InternalFormat)internalFormat switch
        {
            InternalFormat.Red => 1,
            InternalFormat.RG => 2,
            InternalFormat.Rgb => 3,
            InternalFormat.Rgba => 4,
            InternalFormat.DepthComponent16 => 2,
            InternalFormat.DepthComponent24 => 3,
            InternalFormat.DepthComponent32 => 4,
            _ => 0
        };

        if (pixelByteSize == 0)
            RenderContext.LogInfo("Unknown internal format " + internalFormat);
        
        textureSize = (uint)Width * (uint)Height * pixelByteSize;
        var resList = RenderContext.Instance!.resourceList;
        lock (resList.syncRoot)
            resList.totalTextureMemory += textureSize;

        gl.BindTexture(GLEnum.Texture2D, oldTex);
        GlUtil.CheckError(gl, "Could not query texture internal format");*/
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

    protected override void FreeResources(RenderContext rctx)
    {
        var resList = rctx.resourceList;
        lock (resList.syncRoot)
            resList.totalTextureMemory -= textureSize;

        rctx.gl.DeleteTexture(_handle);
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
        GlUtil.CheckError(gl, "Could not update texture data");
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
        GlUtil.CheckError(gl, "Could not update texture data");
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

        GlUtil.CheckError(gl, "Could not update texture data");
    }

    static GLEnum GLWrapMode(TextureWrapMode mode) => mode switch
    {
        TextureWrapMode.Clamp => GLEnum.ClampToEdge,
        TextureWrapMode.Mirror => GLEnum.MirroredRepeat,
        TextureWrapMode.Repeat => GLEnum.Repeat,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    static GLEnum GLFilterMode(TextureFilterMode mode) => mode switch
    {
        TextureFilterMode.Linear => GLEnum.Linear,
        TextureFilterMode.Nearest => GLEnum.Nearest,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>
    /// Binds the texture and uploads sampler parameters.
    /// </summary>
    /// <param name="gl"></param>
    internal void Bind(GL gl)
    {
        var wrapModeU = (int)GLWrapMode(WrapModeU);
        var wrapModeV = (int)GLWrapMode(WrapModeV);
        var filterMin = (int)GLFilterMode(MinFilterMode);
        var filterMag = (int)GLFilterMode(MagFilterMode);

        gl.BindTexture(GLEnum.Texture2D, _handle);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, ref wrapModeU);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, ref wrapModeV);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, ref filterMin);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, ref filterMag);
        GlUtil.CheckError(gl, "Could not bind texture");
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
            GLEnum.Rgba => Glib.PixelFormat.RGBA,
            GLEnum.Rgb => Glib.PixelFormat.RGB,
            GLEnum.RG => Glib.PixelFormat.GrayscaleAlpha,
            GLEnum.Red => Glib.PixelFormat.Grayscale,
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
        GlUtil.CheckError(gl, "Could not read framebuffer attachment");

        byte* mem = null;

        try
        {
            var storageSize = Width * Height * Image.GetBytesPerPixel(PixelFormat.Value);
            mem = (byte*) NativeMemory.Alloc((nuint)storageSize);
            gl.ReadPixels(0, 0, (uint)Width, (uint)Height, GlTextureFormat, GLEnum.UnsignedByte, mem);
            GlUtil.CheckError(gl, "Could not read framebuffer attachment");

            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, oldFb);

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