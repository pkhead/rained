using System;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Drizzle.Lingo.Runtime;

public static class ImageSharpExt
{
    private const string KRITA = @"C:\Program Files\Krita (x64)\bin\krita.exe";

    public static void ShowImage(this Image img)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        {
            using var file = File.Create(tmp);
            img.SaveAsPng(file);
        }

        Process.Start(new ProcessStartInfo(KRITA)
        {
            UseShellExecute = true,
            ArgumentList = { tmp }
        });
    }

    public static Span<T> GetSinglePixelSpan<T>(this Image<T> img) where T : unmanaged, IPixel<T>
    {
        if (!img.DangerousTryGetSinglePixelMemory(out var memory))
            throw new InvalidOperationException("Unable to get single pixel span!");

        return memory.Span;
    }
}
