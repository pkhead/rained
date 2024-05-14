namespace Rained.Tests;
using RainEd.Lingo;

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
}