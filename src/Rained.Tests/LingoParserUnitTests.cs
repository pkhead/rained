namespace Rained.Tests;
using LingoParser = Rained.Lingo.LingoParser;

public class LingoParserUnitTests
{
    [Fact]
    public void StringTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("\"Hello, world!\"") as string;
        Assert.True(res == "Hello, world!");
    }

    [Fact]
    public void IntegerTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("823") as int?;
        Assert.True(res == 823);
    }

    [Fact]
    public void FloatTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("3.14") as float?;
        Assert.True(res == 3.14f);
    }
    
    [Fact]
    public void NegativeIntTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("-823") as int?;
        Assert.True(res == -823);
    }

    [Fact]
    public void NegativeFloatTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("-3.14") as float?;
        Assert.True(res == -3.14f);
    }

    [Fact]
    public void FloatConstantTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("PI") as float?;
        Assert.True(res == MathF.PI);
    }

    [Fact]
    public void IntConstantTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("TRUE") as int?;
        Assert.True(res == 1);
    }

    [Fact]
    public void StringConstantTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("QUOTE") as string;
        Assert.True(res == "\"");
    }

    [Fact]
    public void StringConcatenationTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("\"Hello, \" & \"world!\" & RETURN") as string;
        Assert.True(res == "Hello, world!\r");
    }

    [Fact]
    public void ListTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[1, 2, 3]") as Rained.Lingo.LinearList;
        
        // check value list
        Assert.True(res is not null);

        var valueList = res.Cast<int>().ToArray();
        Assert.True(valueList.Length == 3);
        Assert.True(valueList[0] == 1 && valueList[1] == 2 && valueList[2] == 3);
    }

    [Fact]
    public void PropertyListTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[#foo: \"a\" & \"b\", #bar: \"cd\"]") as Rained.Lingo.PropertyList;

        // check fields
        Assert.True(res is not null);

        Assert.True(res.ContainsKey("foo") && res["foo"] as string == "ab");
        Assert.True(res.ContainsKey("bar") && res["bar"] as string == "cd");
    }

    [Fact]
    public void PropertyListCaseInsensitivityTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[#FoO: \"a\" & \"b\", #BAR: \"cd\"]") as Rained.Lingo.PropertyList;

        // check fields
        Assert.True(res is not null);

        Assert.True(res.ContainsKey("foo") && res["foo"] as string == "ab");
        Assert.True(res.ContainsKey("bar") && res["bar"] as string == "cd");
    }

    [Fact]
    public void EmptyPropertyListTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[:]") as Rained.Lingo.PropertyList;

        Assert.NotNull(res);
        Assert.True(res.Count == 0);
    }

    [Fact]
    public void EmptyLinearListTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[]") as Rained.Lingo.LinearList;

        Assert.NotNull(res);
        Assert.True(res.Count == 0);
    }
}