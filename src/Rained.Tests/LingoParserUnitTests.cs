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
        var res = lingoParser.Read("[1, 2, 3]") as Rained.Lingo.List;
        
        // check value list
        Assert.True(res is not null);

        var valueList = res.values.Cast<int>().ToArray();
        Assert.True(valueList.Length == 3);
        Assert.True(valueList[0] == 1 && valueList[1] == 2 && valueList[2] == 3);
    }

    [Fact]
    public void SymbolListTest()
    {
        var lingoParser = new LingoParser();
        var res = lingoParser.Read("[#foo: \"a\" & \"b\", #bar: \"cd\"]") as Rained.Lingo.List;

        // check fields
        Assert.True(res is not null);

        Assert.True(res.fields.ContainsKey("foo") && res.fields["foo"] as string == "ab");
        Assert.True(res.fields.ContainsKey("bar") && res.fields["bar"] as string == "cd");
    }
}