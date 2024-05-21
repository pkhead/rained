using StbImageSharp;

namespace Glib;

public enum PixelFormat
{
    Grayscale,
    GrayscaleAlpha,
    RGB,
    RGBA
};

public class Image
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

    private static ColorComponents StbPixelFormat(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Grayscale => ColorComponents.Grey,
            PixelFormat.GrayscaleAlpha => ColorComponents.GreyAlpha,
            PixelFormat.RGB => ColorComponents.RedGreenBlue,
            PixelFormat.RGBA => ColorComponents.RedGreenBlueAlpha,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private readonly ImageResult stbImage;
    public readonly PixelFormat PixelFormat;
    public readonly uint BytesPerPixel;

    public int Width { get => stbImage.Width; }
    public int Height { get => stbImage.Height; }
    public byte[] Pixels { get => stbImage.Data; set => stbImage.Data = value; }

    private Image(ImageResult srcImage, byte[] pixels, PixelFormat format)
    {
        stbImage = new ImageResult()
        {
            Comp = StbPixelFormat(format),
            Width = srcImage.Width,
            Height = srcImage.Height,
            SourceComp = srcImage.SourceComp,
            Data = pixels
        };

        PixelFormat = format;
        BytesPerPixel = GetBytesPerPixel(format);
    }

    public Image(Stream stream, PixelFormat format = PixelFormat.RGBA)
    {
        PixelFormat = format;
        stbImage = ImageResult.FromStream(stream, StbPixelFormat(format));
        BytesPerPixel = GetBytesPerPixel(PixelFormat);
    }

    private Image(byte[] pixels, PixelFormat format)
    {
        PixelFormat = format;
        stbImage = ImageResult.FromMemory(pixels, StbPixelFormat(format));
        BytesPerPixel = GetBytesPerPixel(PixelFormat);
    }

    public static Image FromFile(string filePath, PixelFormat format = PixelFormat.RGBA)
    {
        using var stream = File.OpenRead(filePath);
        return new Image(stream, format);
    }

    public static Image FromMemory(byte[] pixels, PixelFormat format = PixelFormat.RGBA)
    {
        return new Image(pixels, format);
    }

    public Image ConvertToFormat(PixelFormat newFormat)
    {
        var newData = new byte[stbImage.Width * stbImage.Height * GetBytesPerPixel(newFormat)];
        var newImg = new Image(stbImage, newData, newFormat);

        uint j = 0;
        for (uint i = 0; i < stbImage.Data.Length; i += BytesPerPixel)
        {
            newImg.SetPixel(j, GetPixel(i));
            j += newImg.BytesPerPixel;
        }

        return newImg;
    }

    private Color GetPixel(uint idx)
    {
        return PixelFormat switch
        {
            PixelFormat.Grayscale => Color.FromRGBA(stbImage.Data[idx], stbImage.Data[idx], stbImage.Data[idx]),
            PixelFormat.GrayscaleAlpha => Color.FromRGBA(stbImage.Data[idx], stbImage.Data[idx], stbImage.Data[idx], stbImage.Data[idx+1]),
            PixelFormat.RGB => Color.FromRGBA(stbImage.Data[idx], stbImage.Data[idx+1], stbImage.Data[idx+2], stbImage.Data[idx+3]),
            PixelFormat.RGBA => Color.FromRGBA(stbImage.Data[idx], stbImage.Data[idx+1], stbImage.Data[idx+2], stbImage.Data[idx+3]),
            _ => throw new Exception()
        };
    }

    private void SetPixel(uint idx, Color color)
    {
        switch (PixelFormat)
        {
            case PixelFormat.Grayscale:
                stbImage.Data[idx] = (byte)(Math.Clamp((color.R + color.G + color.B) / 3f, 0f, 1f) * 255f);
                break;

            case PixelFormat.GrayscaleAlpha:
                stbImage.Data[idx] = (byte)(Math.Clamp((color.R + color.G + color.B) / 3f, 0f, 1f) * 255f);
                stbImage.Data[idx+1] = (byte)Math.Clamp(color.A * 255f, 0f, 255f);
                break;

            case PixelFormat.RGB:
                stbImage.Data[idx] = (byte)Math.Clamp(color.R * 255f, 0f, 255f);
                stbImage.Data[idx] = (byte)Math.Clamp(color.G * 255f, 0f, 255f);
                stbImage.Data[idx] = (byte)Math.Clamp(color.B * 255f, 0f, 255f);
                break;

            case PixelFormat.RGBA:
                stbImage.Data[idx] = (byte)Math.Clamp(color.R * 255f, 0f, 255f);
                stbImage.Data[idx] = (byte)Math.Clamp(color.G * 255f, 0f, 255f);
                stbImage.Data[idx] = (byte)Math.Clamp(color.B * 255f, 0f, 255f);
                stbImage.Data[idx] = (byte)Math.Clamp(color.A * 255f, 0f, 255f);
                break;
        }   
    }
    public Color GetPixel(int x, int y)
    {
        uint idx = (uint)(y * stbImage.Width + x) * BytesPerPixel;
        return GetPixel(idx);
    }

    public void SetPixel(int x, int y, Color color)
    {
        uint idx = (uint)(y * stbImage.Width + x) * BytesPerPixel;
        SetPixel(idx, color);
    }
}