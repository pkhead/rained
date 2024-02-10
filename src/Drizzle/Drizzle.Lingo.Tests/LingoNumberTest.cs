using Drizzle.Lingo.Runtime;
using NUnit.Framework;

namespace Drizzle.Lingo.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class LingoNumberTest
{
    [Test]
    [TestCase("2", ExpectedResult = "1")]
    [TestCase("2.0", ExpectedResult = "1.4142")]
    public string TestSqrt(string num)
    {
        return LingoNumber.Sqrt(LingoNumber.Parse(num)).ToString();
    }
}