using BenchmarkDotNet.Attributes;
using Drizzle.Lingo.Runtime;

namespace Drizzle.Benchmarks;

[SimpleJob]
public class ImageQuadCopy
{
    private readonly LingoImage _srcImage = new(2000, 1200, 32);
    private readonly LingoImage _dstImage = new(2000, 1200, 32);
    private readonly LingoList _quad = new LingoList
    {
        new LingoPoint(-64.7406, -62.5193),
        new LingoPoint(1911.9667, -18.7121),
        new LingoPoint(2087.3266, 1221.7730),
        new LingoPoint(-60.2218, 1266.8830)
    };

    [GlobalSetup]
    public void Setup()
    {
        const int checkerSize = 20;
        var pxl = MakePxl(true);
        for (var y = 0; y < 1200/checkerSize; y++)
        for (var x = 0; x < 2000/checkerSize; x++)
        {
            if (x % 2 == 0 ^ y % 2 == 0)
                _srcImage.copypixels(pxl, new LingoRect(x * checkerSize, y * checkerSize, (x + 1) * checkerSize, (y + 1) * checkerSize), pxl.rect);
        }
    }

    [Benchmark]
    public void Bench()
    {
        _dstImage.copypixels(_srcImage, _quad, _srcImage.rect);
    }

    private static LingoImage MakePxl(bool markAsSuch = false)
    {
        var img = new LingoImage(1, 1, 32);
        img.setpixel(0, 0, LingoColor.Black);
        img.IsPxl = markAsSuch;
        return img;
    }
}
