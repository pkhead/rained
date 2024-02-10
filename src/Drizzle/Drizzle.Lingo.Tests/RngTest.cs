using Drizzle.Lingo.Runtime;
using NUnit.Framework;

namespace Drizzle.Lingo.Tests;

[TestFixture]
public class RngTest
{
    [Test]
    public void Test()
    {
        LingoRuntime.RngState state = default;
        LingoRuntime.InitRng(ref state);
        state.Seed = 5;
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(3));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(3));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(2));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(5));
    }

    [Test]
    public void TestNoClamp()
    {
        LingoRuntime.RngState state = default;
        LingoRuntime.InitRng(ref state);
        state.Seed = 5;
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(3));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(1));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(3));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(2));
        Assert.That(LingoRuntime.Random(ref state, 5), Is.EqualTo(5));
    }
}