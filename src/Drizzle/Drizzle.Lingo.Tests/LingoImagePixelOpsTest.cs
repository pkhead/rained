using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Drizzle.Lingo.Runtime;
using NUnit.Framework;
using SixLabors.ImageSharp.PixelFormats;

namespace Drizzle.Lingo.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(LingoImage))]
public sealed class LingoImagePixelOpsTest
{
    [Test]
    public void TestL8Read8()
    {
        var data = new byte[16];
        data[3] = 123;

        var result = LingoImage.PixelOpsL8.Read8(
            MemoryMarshal.Cast<byte, L8>(data),
            0,
            Vector256<int>.AllBitsSet);
    }

    [Test]
    public void TestL8Write8()
    {
        var data = new byte[16];
        data[3] = 123;

        LingoImage.PixelOpsL8.Write8(
            MemoryMarshal.Cast<byte, L8>(data),
            1,
            Vector256<int>.AllBitsSet,
            Vector256<int>.AllBitsSet.WithElement(3, 0).WithElement(2, 0));
    }

    [Test]
    public void TestB8G8R8A8Read8()
    {
        var data = new byte[32];
        data[3] = 123;

        var result = LingoImage.PixelOpsBgra32.Read8(
            MemoryMarshal.Cast<byte, Bgra32>(data),
            0,
            Vector256<int>.AllBitsSet);
    }
}
