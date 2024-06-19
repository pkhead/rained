using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using RawImage = SixLabors.ImageSharp.Image;

namespace Glib;

public enum PixelFormat
{
    Grayscale,
    GrayscaleAlpha,
    RGB,
    RGBA
};

public class Image : IDisposable
{
    public static uint GetBytesPerPixel(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Grayscale => 1,
            PixelFormat.GrayscaleAlpha => 2,
            PixelFormat.RGB => 3,
            PixelFormat.RGBA => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Invalid PixelFormat enum value")
        };
    }

    /*private static ColorComponents StbPixelFormat(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Grayscale => ColorComponents.Grey,
            PixelFormat.GrayscaleAlpha => ColorComponents.GreyAlpha,
            PixelFormat.RGB => ColorComponents.RedGreenBlue,
            PixelFormat.RGBA => ColorComponents.RedGreenBlueAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }*/

    private readonly RawImage rawImage;
    public readonly PixelFormat PixelFormat;
    public readonly uint BytesPerPixel;

    public int Width { get => rawImage.Width; }
    public int Height { get => rawImage.Height; }
    public RawImage ImageSharpImage => rawImage;

    // make a Configuration instance for specifically for glib images because
    // Drizzle also uses ImageSharp and assumes PreferContiguousImageBuffers to be true
    // for the image it creates
    private static Configuration _globalConfig = null!;

    public static Configuration ImageSharpConfiguration {
        get
        {
            if (_globalConfig is not null) return _globalConfig;
            return _globalConfig = new Configuration(
                new PngConfigurationModule(),
                new JpegConfigurationModule()
            )
            {
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 3)
            };
        }
    } 

    private Image(RawImage image)
    {
        rawImage = image;
        
        PixelFormat = rawImage.PixelType.BitsPerPixel switch
        {
            8 => PixelFormat.Grayscale,
            16 => PixelFormat.GrayscaleAlpha,
            24 => PixelFormat.RGB,
            32 => PixelFormat.RGBA,
            _ => throw new Exception("Unrecognized color depth of " + rawImage.PixelType.BitsPerPixel)
        };

        BytesPerPixel = GetBytesPerPixel(PixelFormat);
    }

    public Image(Stream stream, PixelFormat format = PixelFormat.RGBA)
    {
        var config = ImageSharpConfiguration;

        rawImage = format switch
        {
            PixelFormat.Grayscale => RawImage.Load<L8>(config, stream),
            PixelFormat.GrayscaleAlpha => RawImage.Load<La16>(config, stream),
            PixelFormat.RGB => RawImage.Load<Rgb24>(config, stream),
            PixelFormat.RGBA => RawImage.Load<Rgba32>(config, stream),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        PixelFormat = format;
        BytesPerPixel = GetBytesPerPixel(PixelFormat);
    }

    public Image(ReadOnlySpan<byte> rawPixels, int width, int height, PixelFormat format)
    {
        var config = ImageSharpConfiguration;

        rawImage = format switch
        {
            PixelFormat.Grayscale => RawImage.LoadPixelData<L8>(config, rawPixels, width, height),
            PixelFormat.GrayscaleAlpha => RawImage.LoadPixelData<La16>(config, rawPixels, width, height),
            PixelFormat.RGB => RawImage.LoadPixelData<Rgb24>(config, rawPixels, width, height),
            PixelFormat.RGBA => RawImage.LoadPixelData<Rgba32>(config, rawPixels, width, height),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        PixelFormat = format;
        BytesPerPixel = GetBytesPerPixel(PixelFormat);
    }

    public static Image FromFile(string filePath, PixelFormat format = PixelFormat.RGBA)
    {
        using var stream = File.OpenRead(filePath);
        return new Image(stream, format);
    }

    public static Image FromColor(int width, int height, Color color, PixelFormat format = PixelFormat.RGBA)
    {
        var r = (byte)Math.Clamp(color.R * 255f, 0f, 255f);
        var g = (byte)Math.Clamp(color.G * 255f, 0f, 255f);
        var b = (byte)Math.Clamp(color.B * 255f, 0f, 255f);
        var a = (byte)Math.Clamp(color.A * 255f, 0f, 255f);

        var rgba = new Rgba32(r, g, b, a);
        RawImage rawImage;
        
        switch (format)
        {
            case PixelFormat.Grayscale:
            {
                var pixel = new L8();
                pixel.FromRgba32(rgba);
                rawImage = new Image<L8>(width, height, pixel);
                break;
            }
            
            case PixelFormat.GrayscaleAlpha:
            {
                var pixel = new La16();
                pixel.FromRgba32(rgba);
                rawImage = new Image<La16>(width, height, pixel);
                break;
            }

            case PixelFormat.RGB:
            {
                var pixel = new Rgb24();
                pixel.FromRgba32(rgba);
                rawImage = new Image<Rgb24>(width, height, pixel);
                break;
            }

            case PixelFormat.RGBA:
            {
                var pixel = new Rgba32();
                pixel.FromRgba32(rgba);
                rawImage = new Image<Rgba32>(width, height, pixel);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
        
        return new Image(rawImage);
    }

    public Image ConvertToFormat(PixelFormat newFormat)
    {
        var config = ImageSharpConfiguration;

        return new Image(newFormat switch
        {
            PixelFormat.Grayscale => rawImage.CloneAs<L8>(config),
            PixelFormat.GrayscaleAlpha => rawImage.CloneAs<La16>(config),
            PixelFormat.RGB => rawImage.CloneAs<Rgb24>(config),
            PixelFormat.RGBA => rawImage.CloneAs<Rgba32>(config),
            _ => throw new ArgumentOutOfRangeException(nameof(newFormat))
        });
    }

    public Color GetPixel(int x, int y)
    {
        Rgba32 rgba = new();

        switch (PixelFormat)
        {
            case PixelFormat.Grayscale:
            {
                var pixel = ((Image<L8>)rawImage)[x, y];
                pixel.ToRgba32(ref rgba);
                break;
            }

            case PixelFormat.GrayscaleAlpha:
            {
                var pixel = ((Image<La16>)rawImage)[x, y];
                pixel.ToRgba32(ref rgba);
                break;
            }

            case PixelFormat.RGB:
            {
                var pixel = ((Image<Rgb24>)rawImage)[x, y];
                pixel.ToRgba32(ref rgba);
                break;
            }

            case PixelFormat.RGBA:
            {
                var pixel = ((Image<Rgba32>)rawImage)[x, y];
                pixel.ToRgba32(ref rgba);
                break;
            }
        }
        
        return new Color(rgba.R / 255f, rgba.G / 255f, rgba.B / 255f, rgba.A / 255f);
    }

    public void SetPixel(int x, int y, Color color)
    {
        Rgba32 rgba = new(
            (byte)Math.Clamp(color.R * 255f, 0f, 255f),
            (byte)Math.Clamp(color.G * 255f, 0f, 255f),
            (byte)Math.Clamp(color.B * 255f, 0f, 255f),
            (byte)Math.Clamp(color.A * 255f, 0f, 255f)
        );

        switch (PixelFormat)
        {
            case PixelFormat.Grayscale:
            {
                var lum = (rgba.R + rgba.G + rgba.B) / 3 * (rgba.A / 255f);
                ((Image<L8>)rawImage)[x, y] = new L8((byte)lum);
                break;
            }

            case PixelFormat.GrayscaleAlpha:
            {
                var lum = (rgba.R + rgba.G + rgba.B) / 3;
                ((Image<La16>)rawImage)[x, y] = new La16((byte)lum, rgba.A);
                break;
            }

            case PixelFormat.RGB:
            {
                ((Image<Rgb24>)rawImage)[x, y] = new Rgb24(rgba.R, rgba.G, rgba.B);
                break;
            }

            case PixelFormat.RGBA:
            {
                ((Image<Rgba32>)rawImage)[x, y] = rgba;
                break;
            }
        }
    }

    public void CopyPixelDataTo(Span<byte> bytes)
    {
        switch (PixelFormat)
        {
            case PixelFormat.Grayscale:
            {
                ((Image<L8>)rawImage).CopyPixelDataTo(bytes);
                break;
            }

            case PixelFormat.GrayscaleAlpha:
            {
                ((Image<La16>)rawImage).CopyPixelDataTo(bytes);
                break;
            }

            case PixelFormat.RGB:
            {
                ((Image<Rgb24>)rawImage).CopyPixelDataTo(bytes);
                break;
            }

            case PixelFormat.RGBA:
            {
                ((Image<Rgba32>)rawImage).CopyPixelDataTo(bytes);
                break;
            }

            default: throw new Exception("Unrecognized pixel format");
        }
    }

    public void DrawImage(Image srcImage, Rectangle srcRec, Rectangle dstRec, Color tintCol)
    {
        var opts = new GraphicsOptions();
        {
            opts.Antialias = false;
            //opts.AlphaCompositionMode = PixelAlphaCompositionMode.Src;
        };
        
        var imgSrcRect = new SixLabors.ImageSharp.Rectangle((int)srcRec.X, (int)srcRec.Y, (int)srcRec.Width, (int)srcRec.Height);
        var imgDstRect = new SixLabors.ImageSharp.Rectangle((int)dstRec.X, (int)dstRec.Y, (int)dstRec.Width, (int)dstRec.Height);

        var srcImg = srcImage.ImageSharpImage;

        if (imgSrcRect.X != 0 || imgSrcRect.Y != 0 || dstRec.Width != srcRec.Width || dstRec.Height != srcRec.Height || tintCol != Color.White)
        {
            var mat = ColorMatrix.Identity;
            mat.M11 = tintCol.R;
            mat.M22 = tintCol.G;
            mat.M33 = tintCol.G;
            mat.M44 = tintCol.A;

            var resampler = new NearestNeighborResampler();

            using var clone = srcImg.Clone(
                x => x.Crop(imgSrcRect)
                    .Resize(imgDstRect.Width, imgDstRect.Height, resampler)
                    .Filter(mat)
            );
            
            ImageSharpImage.Mutate(x => x.DrawImage(clone, new Point(imgDstRect.X, imgDstRect.Y), opts));
        }
        else
        {
            ImageSharpImage.Mutate(x => x.DrawImage(srcImage.ImageSharpImage, new Point(imgDstRect.X, imgDstRect.Y), opts));
        }
    }

    public void FlipVertical()
    {
        ImageSharpImage.Mutate(x => x.Flip(FlipMode.Vertical));
    }

    public void FlipHorizontal()
    {
        ImageSharpImage.Mutate(x => x.Flip(FlipMode.Horizontal));
    }

    public void ExportPng(string filePath)
    {
        ImageSharpImage.SaveAsPng(filePath);
    }

    public void ExprtPng(Stream stream)
    {
        ImageSharpImage.SaveAsPng(stream);
    }

    public Image Clone()
    {
        return PixelFormat switch
        {
            PixelFormat.Grayscale => new Image(((Image<L8>)rawImage).Clone()),
            PixelFormat.GrayscaleAlpha => new Image(((Image<La16>)rawImage).Clone()),
            PixelFormat.RGB => new Image(((Image<Rgb24>)rawImage).Clone()),
            PixelFormat.RGBA => new Image(((Image<Rgba32>)rawImage).Clone()),
            _ => throw new Exception("Unrecognized pixel format"),
        };
    }

    public void Dispose()
    {
        rawImage.Dispose();
        GC.SuppressFinalize(this);
    }
}