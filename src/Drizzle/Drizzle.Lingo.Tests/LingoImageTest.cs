using System;
using System.Reflection;
using Drizzle.Lingo.Runtime;
using NUnit.Framework;
using SixLabors.ImageSharp;

namespace Drizzle.Lingo.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(LingoImage))]
public sealed class ImageTest
{
    [OneTimeSetUp]
    public void Setup()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
    }

    [Test]
    public void TestCopyPixelsMove()
    {
        // Set up source image to have a blue-bordered red/green checkerboard in the center.
        // Then copy the shape to another image.
        var src = new LingoImage(128, 127, 32);
        var dst = new LingoImage(128, 127, 32);

        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                if (x == 32 || x == 95 || y == 32 || y == 95)
                {
                    src.setpixel(x, y, new LingoColor(0, 0, 255));
                }
                else
                {
                    var color = x % 2 == 0 ^ y % 2 == 0 ? new LingoColor(255, 0, 0) : new LingoColor(0, 255, 0);
                    src.setpixel(x, y, color);
                }
            }
        }

        // copy
        dst.copypixels(src, new LingoRect(16, 16, 80, 80), new LingoRect(32, 32, 96, 96));

        // assert
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var srcPx = src.getpixel(x + 32, y + 32);
                var dstPx = dst.getpixel(x + 16, y + 16);

                Assert.That(srcPx, Is.EqualTo(dstPx));
            }
        }
    }

    [Test]
    public void TestCopyPixelsMove1Bit()
    {
        // Like before, but with a 1 bit image. No border since that isn't possible on one bit.
        var src = new LingoImage(128, 124, 1);
        var dst = new LingoImage(128, 124, 1);
        dst.fill(LingoColor.Black);

        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                var color = x % 2 == 0 ^ y % 2 == 0 ? 255 : 0;
                src.setpixel(x, y, color);
            }
        }

        // copy
        dst.copypixels(src, new LingoRect(-16, -16, 111, 111), new LingoRect(0, 0, 127, 127));

        // assert
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var srcPx = src.getpixel(x + 32, y + 32);
                var dstPx = dst.getpixel(x + 16, y + 16);

                Assert.That(srcPx, Is.EqualTo(dstPx));
            }
        }
    }

    [Test]
    public void TestCopyPixelsMoveRed1Bit()
    {
        // Copy to a 1-bit image but set the color to red. Should write black
        var src = new LingoImage(128, 128, 32);
        var dst = new LingoImage(128, 128, 1);

        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                src.setpixel(x, y, 6);
            }
        }

        // copy
        dst.copypixels(src, src.rect - new LingoRect(16, 16, 16, 16), src.rect);

        // assert
        for (var y = 0; y < 128; y++)
        {
            for (var x = 0; x < 128; x++)
            {
                var dstPx = dst.getpixel(x, y);

                if (x is >= 16 and < 80 && y is >= 16 and < 80)
                    Assert.That(dstPx, Is.EqualTo(LingoColor.Black));
                else
                    Assert.That(dstPx, Is.EqualTo(LingoColor.White));
            }
        }
    }


    [Test]
    public void TestCopyPixelsBlend()
    {
        // Copy to a 1-bit image but set the color to red. Should write black
        var src = new LingoImage(128, 127, 32);
        var dst = new LingoImage(128, 127, 32);

        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                src.setpixel(x, y, new LingoColor(255, 0, 0));
                dst.setpixel(x, y, new LingoColor(0, 255, 0));
            }
        }

        // copy
        dst.copypixels(src, src.rect, src.rect, new LingoPropertyList { [new LingoSymbol("blend")] = 50 });

        // assert
        for (var y = 0; y < 127; y++)
        {
            for (var x = 0; x < 127; x++)
            {
                var dstPx = dst.getpixel(x, y);

                if (x is >= 32 and < 96 && y is >= 32 and < 96)
                    Assert.That(dstPx, Is.EqualTo(new LingoColor(127, 127, 0)));
                else
                    Assert.That(dstPx, Is.EqualTo(LingoColor.White));
            }
        }
    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void TestCopyPixelsPxl(bool pxlFlag)
    {
        var pxl = MakePxl(pxlFlag);
        var dst = new LingoImage(128, 128, 1);

        dst.copypixels(pxl, new LingoRect(32, 32, 96, 96), pxl.rect);

        // assert
        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                var dstPx = dst.getpixel(x, y);

                Assert.That(dstPx, Is.EqualTo(new LingoColor(0, 0, 0)));
            }
        }
    }

    [Test]
    public void TestMakeSilhouette()
    {
        var src = new LingoImage(32, 32, 32);
        for (var y = 8; y < 24; y++)
        {
            for (var x = 8; x < 24; x++)
            {
                src.setpixel(x, y, 255);
            }
        }

        var silhouette = src.makesilhouette(1);

        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var px = silhouette.getpixel(x, y);
                var shouldBeInSilhouette = x is >= 8 and < 24 && y is >= 8 and < 24;
                LingoColor expectedPixel = shouldBeInSilhouette ? 0 : 255;

                Assert.That(px, Is.EqualTo(expectedPixel));
            }
        }
    }

    [Test]
    public void TestCopyPixelsQuad()
    {
        var pxl = MakePxl();
        var dest = new LingoImage(32, 32, 32);

        dest.copypixels(pxl, new LingoList
        {
            new LingoPoint(16, 0),
            new LingoPoint(32, 16),
            new LingoPoint(16, 32),
            new LingoPoint(0, 16),
        }, pxl.rect);
    }

    [Test]
    public void TestCopyPixelsBitWriteMaskOob()
    {
        // This pattern (dst is 12 bits) caused an OOB in bit writing,
        // the total pixel count is divisible by 32 so no rounding up,
        // and due to how pixels are written,
        // the last byte written is split past the end of the buffer but *with* write mask.
        // so effectively we're making sure the write mask prevents oob error in write8 bits.
        var src = new LingoImage(1, 1, 1);
        var dst = new LingoImage(12, 8, 1);
        src.fill(LingoColor.Black);
        dst.fill(LingoColor.Black);

        dst.copypixels(src, new LingoRect(0, 0, 12, 8), new LingoRect(0, 0, 1, 1));
    }

    [Test]
    public void TestCopyPixelsMask()
    {
        var maskImg = new LingoImage(2, 2, 1);
        maskImg.setpixel(0, 0, LingoColor.Black);
        maskImg.setpixel(1, 1, LingoColor.Black);
        var mask = maskImg.createmask();

        var src = new LingoImage(3, 3, 32);
        src.fill(new LingoColor(255, 0, 0));

        var dst = new LingoImage(4, 4, 32);
        dst.fill(new LingoColor(0, 255, 0));

        dst.copypixels(src, src.rect, src.rect, new LingoPropertyList { [new LingoSymbol("mask")] = mask});

        Assert.Multiple(() =>
        {
            for (var y = 0; y < dst.Height; y++)
            {
                for (var x = 0; x < dst.Width; x++)
                {
                    if (x == 0 && y == 0 || y == 1 && x == 1)
                        Assert.That(dst.getpixel(x, y) == new LingoColor(255, 0, 0));
                    else
                        Assert.That(dst.getpixel(x, y) == new LingoColor(0, 255, 0));
                }
            }
        });
    }

    [Test]
    public void TestCopyPixelsMaskEffect()
    {
        var flattenedGradientB = ImageFromResource("Drizzle.Lingo.Tests.Images.effectmask.flattenedgradientB.png");
        var layer9 = ImageFromResource("Drizzle.Lingo.Tests.Images.effectmask.layer9.png");
        var dumpImage = ImageFromResource("Drizzle.Lingo.Tests.Images.effectmask.dumpimage.png");

        flattenedGradientB.copypixels(
            dumpImage,
            new LingoRect(-43, -47, 1443, 844),
            new LingoRect(400, 310, 1900, 1210),
            new LingoPropertyList
            {
                [new LingoSymbol("maskimage")] = layer9.makesilhouette(0).createmask()
            });
    }

    [Test]
    public void TestCopyPixelsTriangle()
    {
        var pxl = MakePxl();
        var baseImg = new LingoImage(20, 20, 32);

        baseImg.fill(new LingoColor(255, 0, 0));

        baseImg.copypixels(
            pxl,
            new LingoList
            {
                new LingoPoint(0, 0),
                new LingoPoint(0, 0),
                new LingoPoint(20, 20),
                new LingoPoint(0, 20),
            },
            new LingoRect(0, 0, 1, 1),
            new LingoPropertyList
            {
                [new LingoSymbol("color")] = LingoColor.White
            });

        //baseImg.ShowImage();

        baseImg.fill(new LingoColor(255, 0, 0));

        baseImg.copypixels(
            pxl,
            new LingoList
            {
                new LingoPoint(20, 0),
                new LingoPoint(20, 0),
                new LingoPoint(0, 20),
                new LingoPoint(20, 20),
            },
            new LingoRect(0, 0, 1, 1),
            new LingoPropertyList
            {
                [new LingoSymbol("color")] = LingoColor.White
            });

        //baseImg.ShowImage();

        baseImg.fill(new LingoColor(255, 0, 0));

        baseImg.copypixels(
            pxl,
            new LingoList
            {
                new LingoPoint(0, 20),
                new LingoPoint(0, 20),
                new LingoPoint(20, 0),
                new LingoPoint(0, 0),
            },
            new LingoRect(0, 0, 1, 1),
            new LingoPropertyList
            {
                [new LingoSymbol("color")] = LingoColor.White
            });

        //baseImg.ShowImage();

        baseImg.fill(new LingoColor(255, 0, 0));

        baseImg.copypixels(
            pxl,
            new LingoList
            {
                new LingoPoint(20, 20),
                new LingoPoint(20, 20),
                new LingoPoint(0, 0),
                new LingoPoint(20, 0),
            },
            new LingoRect(0, 0, 1, 1),
            new LingoPropertyList
            {
                [new LingoSymbol("color")] = LingoColor.White
            });

        //baseImg.ShowImage();


    }

    private static LingoImage ImageFromResource(string name)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (stream == null)
            throw new ArgumentException();

        return LingoImage.LoadFromStream(stream);
    }

    private static LingoImage MakePxl(bool markAsSuch=false)
    {
        var img = new LingoImage(1, 1, 32);
        img.setpixel(0, 0, LingoColor.Black);
        img.IsPxl = markAsSuch;
        return img;
    }
}
