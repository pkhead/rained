using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Drizzle.Lingo.Runtime.Parser;
using NUnit.Framework;
using Pidgin;

namespace Drizzle.Lingo.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public sealed class ParseTest
{
    private static readonly string SourcesRoot = Path.Combine("..", "..", "..", "..", "LingoSource");

    public static IEnumerable<string> GetSources()
    {
        return Directory.EnumerateFiles(SourcesRoot, "*.lingo").Select(Path.GetFileName);
    }

    [Test]
    public void Test([ValueSource(nameof(GetSources))] string fileName)
    {
        var fullPath = Path.Combine(SourcesRoot, fileName);

        var reader = new StreamReader(fullPath);
        var result = LingoParser.Script.ParseOrThrow(reader);

        TestContext.Out.Write("Parsed AST:");
        TestContext.Out.Write(DebugPrint.PrintAstNode(result));
    }
}