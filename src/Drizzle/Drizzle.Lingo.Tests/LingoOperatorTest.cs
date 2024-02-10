using System.Reflection;
using Drizzle.Lingo.Runtime;
using NUnit.Framework;

namespace Drizzle.Lingo.Tests;

[TestFixture]
public sealed class LingoOperatorTest
{
    [Test]
    [TestCase("5", "7", ExpectedResult = "12")]
    [TestCase("point(100, 10)", "point(200, 20)", ExpectedResult = "point(300, 30)")]
    [TestCase("point(100, 10)", "5", ExpectedResult = "point(105, 15)")]
    [TestCase("5", "point(100, 10)", ExpectedResult = "point(105, 15)")]
    [TestCase("5", "rect(10, 100, 1000, 1)", ExpectedResult = "rect(15, 105, 1005, 6)")]
    [TestCase("rect(7, 77, 777, 7777)", "rect(10, 100, 1000, 1)", ExpectedResult = "rect(17, 177, 1777, 7778)")]
    [TestCase("point(10, 20)", "rect(5, 7, 10, 10)", ExpectedResult = "[15, 27]")]
    [TestCase("[1, 2]", "point(100, 200)", ExpectedResult = "[101, 202]")]
    [TestCase("[1, [2, 3]]", "100", ExpectedResult = "[101, [102, 103]]")]
    [TestCase("[1, [2, 3]]", "[100, 200]", ExpectedResult = "[101, [202, 203]]")]
    [TestCase("[1, [2, 3]]", "[100, [200, 300]]", ExpectedResult = "[101, [202, 303]]")]
    [TestCase("[1, 2, 3]", "rect(100, 200, 300, 400)", ExpectedResult = "[101, 202, 303]")]
    public string TestAdd(string a, string b)
    {
        var global = new LingoRuntime(Assembly.GetExecutingAssembly()).Global;

        var aVal = global.value(a);
        var bVal = global.value(b);

        return global.@string(LingoGlobal.op_add(aVal, bVal).ToString());
    }
}