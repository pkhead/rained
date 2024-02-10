using System;
using System.Diagnostics.CodeAnalysis;
using Drizzle.Lingo.Runtime.Parser;
using Drizzle.Lingo.Runtime.Scripting;
using Pidgin;

namespace Drizzle.Lingo.Runtime;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class LingoGlobal
{
    public LingoScriptRuntime ScriptRuntime { get; private set; } = default!;

    public dynamic? value(string a)
    {
        var trimmed = a.AsSpan().Trim();
        if (trimmed.IsEmpty)
            return new LingoNumber(0); // value() returns zero on empty string.

        // NOTE: This uses ExpressionNoOps, so expressions like "5 + 10" aren't gonna be parsed correctly.
        // This is fine for the level editor, but if you ever do something funny, you've been warned.
        try
        {
            var parsedExpression = LingoParser.Expression.ParseOrThrow(trimmed);
            return Interpreter.Evaluate(parsedExpression, LingoRuntime);
        }
        catch
        {
            return null;
        }
    }
}
