using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Drizzle.Lingo.Runtime;

[DebuggerDisplay("Image {Width}x{Height} depth {Depth}")]
public sealed partial class LingoImage
{
    // Image data is BGRA instead of RGBA because ImageSharp doesn't have an Rgba5551 type, only Bgra5551.

    public static readonly LingoImage Pxl = MakePxl();

    public ImageType Type { get; }
    public int Depth => (int)Type;

    // Note: image buffer may contain pad bytes at the end to make operations easier.
    public byte[] ImageBuffer { get; private set; }
    public bool ImageBufferShared { get; set; }

    public LingoRect rect => new(0, 0, width, height);

    public int Width { get; }
    public int Height { get; }
    public LingoNumber width => Width;
    public LingoNumber height => Height;
    public LingoNumber depth => Depth;

    // So the editor does a *ton* of tiny copypixels() operations from the "pxl" cast member,
    // which basically amounts to line/rect drawings in a silly way.
    // Guess we're fast pathing this.
    public bool IsPxl { get; set; }

    public LingoImage(int width, int height, int depth) : this(width, height, (ImageType)depth)
    {
    }

    public LingoImage(int width, int height, ImageType type)
    {
        Width = width;
        Height = height;
        Type = type;

        ImageBuffer = NewImgBufferForType(width, height, type);
    }

    public LingoImage(Image<Bgra32> image) : this(image.Width, image.Height, ImageType.B8G8R8A8)
    {
        var span = image.GetSinglePixelSpan();
        span.CopyTo(MemoryMarshal.Cast<byte, Bgra32>(ImageBuffer));
    }

    private LingoImage(byte[] buffer, int width, int height, ImageType type)
    {
        Width = width;
        Height = height;
        Type = type;

        ImageBuffer = buffer;
    }

    public Span<byte> ImageBufferNoPadding
    {
        get
        {
            if (Type == ImageType.Palette1)
                return ImageBuffer;

            var size = Width * Height * PixelTypeSize(Type);
            return ImageBuffer.AsSpan(0, size);
        }
    }

    public void SaveAsPng(Stream stream)
    {
        using var img = GetImgSharpImage();
        img.SaveAsPng(stream);
    }

    public void copypixels(LingoImage source, LingoList destQuad, LingoRect sourceRect)
    {
        copypixels(source, destQuad, sourceRect, new LingoPropertyList());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public LingoColor getpixel(LingoPoint point)
    {
        return getpixel(point.loch, point.locv);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public LingoColor getpixel(LingoNumber x, LingoNumber y)
    {
        return getpixel((int)x, (int)y);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public LingoColor getpixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return LingoColor.White;

        var idx = y * Width + x;
        switch (Type)
        {
            case ImageType.B8G8R8A8:
            {
                var buf = MemoryMarshal.Cast<byte, Bgra32>(ImageBuffer);
                ref var val = ref buf[idx];
                return new LingoColor(val.R, val.G, val.B);
            }
            case ImageType.B5G5R5A1:
            {
                var buf = MemoryMarshal.Cast<byte, Bgra5551>(ImageBuffer);
                ref var val = ref buf[idx];
                var rgba = new Rgba32();
                val.ToRgba32(ref rgba);
                return new LingoColor(rgba.R, rgba.G, rgba.B);
            }
            case ImageType.Palette1:
            {
                var buf = MemoryMarshal.Cast<byte, int>(ImageBuffer);
                return DoBitRead(buf, idx) ? new LingoColor(255, 255, 255) : default;
            }
            case ImageType.L8:
            {
                var val = ImageBuffer[idx];
                return new LingoColor(val, val, val);
            }
            default:
                Log.Warning("getpixel(): Unimplemented image type: {Type}", Type);
                return default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void setpixel(LingoPoint point, LingoColor color)
    {
        setpixel(point.loch, point.locv, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void setpixel(LingoNumber x, LingoNumber y, LingoColor color)
    {
        setpixel((int)x, (int)y, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void setpixel(int x, int y, LingoColor color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        CopyIfShared();

        var idx = y * Width + x;
        switch (Type)
        {
            case ImageType.B8G8R8A8:
            {
                var buf = MemoryMarshal.Cast<byte, Bgra32>(ImageBuffer);
                ref var val = ref buf[idx];
                val = new Bgra32((byte)color.red, (byte)color.green, (byte)color.blue, 255);
                return;
            }
            case ImageType.B5G5R5A1:
            {
                var buf = MemoryMarshal.Cast<byte, Bgra5551>(ImageBuffer);
                ref var val = ref buf[idx];
                val.FromRgba32(new Rgba32((byte)color.red, (byte)color.green, (byte)color.blue, 255));
                return;
            }
            case ImageType.Palette1:
            {
                var buf = MemoryMarshal.Cast<byte, int>(ImageBuffer);
                var white = color.red != 0;

                DoBitWrite(buf, idx, white);

                return;
            }
            case ImageType.L8:
            {
                ImageBuffer[idx] = color.RedByte;

                return;
            }
            default:
                Log.Warning("setpixel(): Unimplemented image type: {Type}", Type);
                break;
        }
    }

    public LingoImage duplicate()
    {
        var newBuf = GC.AllocateUninitializedArray<byte>(ImageBuffer.Length);
        ImageBuffer.AsSpan().CopyTo(newBuf);
        return new LingoImage(newBuf, Width, Height, Type);
    }

    public LingoImage DuplicateShared()
    {
        ImageBufferShared = true;
        return new LingoImage(ImageBuffer, Width, Height, Type) { IsPxl = IsPxl, ImageBufferShared = true };
    }

    public LingoMask createmask()
    {
        if (Depth == 1)
            return new LingoMask(Width, Height, ImageBuffer);

        var copy = makesilhouette(0);
        return new LingoMask(Width, Height, copy.ImageBuffer);
    }

    private void CopyIfShared()
    {
        if (!ImageBufferShared)
            return;

        ImageBufferShared = false;
        var prevBuf = ImageBuffer;
        ImageBuffer = GC.AllocateUninitializedArray<byte>(prevBuf.Length);
        Array.Copy(prevBuf, ImageBuffer, prevBuf.Length);
    }

    public void ShowImage()
    {
        using var img = GetImgSharpImage();
        img.ShowImage();
    }

    public static LingoImage LoadFromPath(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            return LoadFromStream(fs);
        }
        catch (FileNotFoundException)
        {
            return LoadFromStream(null);
        }
    }

    public static LingoImage LoadFromStream(Stream? stream)
    {
        // Empty (0 byte file) images get imported into cast members by *clearing the cast member entirely*.
        // Some levels (e.g. SS_I03) have such images, and this previously broke loading.
        // Trying to actually clear the cast member directly however just results in insane cast type mixing,
        // which Director *does* support, but I really do not want to get into because that's awful.
        // Simply loading an empty image instead satisfies the editor's loading code so good enough for me.
        if (stream is not { Length: > 0 })
            return new LingoImage(1, 1, 32);

        var img = Image.Load<Bgra32>(stream);
        // TODO: loading of low-bit images, maybe.
        return new LingoImage(img);
    }

    public Image GetImgSharpImage()
    {
        var img = this;
        if (img.Type is not (ImageType.B5G5R5A1 or ImageType.B8G8R8A8))
        {
            var copy = new LingoImage(Width, Height, ImageType.B8G8R8A8);
            copy.copypixels(this, rect, rect);
            img = copy;
        }

        Image imgSharp;
        Span<byte> imgSharpSpan;
        if (img.Depth == 16)
        {
            var bgra5551 = new Image<Bgra5551>(img.Width, img.Height);
            imgSharp = bgra5551;
            imgSharpSpan = MemoryMarshal.Cast<Bgra5551, byte>(bgra5551.GetSinglePixelSpan());
        }
        else
        {
            Debug.Assert(img.Depth == 32);
            var bgra32 = new Image<Bgra32>(img.Width, img.Height);
            imgSharp = bgra32;
            imgSharpSpan = MemoryMarshal.Cast<Bgra32, byte>(bgra32.GetSinglePixelSpan());
        }

        img.ImageBufferNoPadding.CopyTo(imgSharpSpan);

        return imgSharp;
    }

    private static byte[] NewImgBufferForType(int width, int height, ImageType type)
    {
        if (type is ImageType.Palette2 or ImageType.Palette4)
            throw new NotSupportedException();

        int size;
        var pixels = width * height;
        if (type == ImageType.Palette1)
        {
            size = (pixels + 7) >> 3;
            // Round up to 4 bytes so we can do 32-bit wide operations safely.
            var rem = size & 3;
            if (rem != 0)
                size += 4 - rem;
        }
        else
        {
            // Add 8 elements to the end cuz I'm a hack fraud
            // and it makes me not have to care about the end of the data in the pixel ops impls.
            pixels += 8;

            size = pixels * PixelTypeSize(type);
        }

        // 8-bit palettized needs to be initialized to 0 (0 == white).
        if (type == ImageType.Palette8)
            return new byte[size];

        // all other formats initialize to all 1s (white).
        // Use AllocateUninitializedArray
        var buf = GC.AllocateUninitializedArray<byte>(size);
        buf.AsSpan().Fill(255);
        return buf;
    }

    private static int PixelTypeSize(ImageType type)
    {
        return type switch
        {
            ImageType.Palette8 => 1,
            ImageType.B5G5R5A1 => 2,
            ImageType.B8G8R8A8 => 4,
            ImageType.L8 => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>
    /// Trim this image by removing all white border around it possible.
    /// </summary>
    /// <returns>May return the current instance if no trimming is necessary.</returns>
    public LingoImage Trimmed()
    {
        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        var any = false;

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var px = getpixel(x, y);
            if (px != LingoColor.White)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                // +1 because copy bounds are on the bottom-right of pixels.
                maxX = Math.Max(maxX, x + 1);
                maxY = Math.Max(maxY, y + 1);
                any = true;
            }
        }

        if (!any)
            return new LingoImage(1, 1, Depth);

        if (minX == 0 && minY == 0 && maxX == Width - 1 && maxY == Height - 1)
            return this;

        var image = new LingoImage(maxX - minX, maxY - minY, Depth);
        image.copypixels(this, new LingoRect(0, 0, image.Width, image.Height), new LingoRect(minX, minY, maxX, maxY));
        return image;
    }

    private static LingoImage MakePxl()
    {
        var img = new LingoImage(1, 1, 32) { IsPxl = true };
        img.setpixel(0, 0, LingoColor.Black);
        return img;
    }
}

public enum ImageType
{
    // Base Lingo image types.

    Palette1 = 1,
    Palette2 = 2, // Not supported by Drizzle.
    Palette4 = 4, // Not supported by Drizzle.
    Palette8 = 8,
    B5G5R5A1 = 16,
    B8G8R8A8 = 32,

    // New image types to make rendering go NYOOOM.
    // These depth values don't make sense, but they're to avoid conflicts with built-in Director stuff.
    L8 = 33,
}
