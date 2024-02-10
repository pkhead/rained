using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable SuggestBaseTypeForParameter

namespace Drizzle.Lingo.Runtime.Parser;

public static class AstNode
{
    public abstract record Base
    {
        public virtual void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append(ToString());
            sb.Append(' ');
        }
    }

    public sealed record Assignment(
        Base Assigned,
        string? Type,
        Base Value
    ) : Base;

    public sealed record Return(
        Base? Value
    ) : Base;

    public sealed record ExitRepeat : Base;

    public sealed record Global(
        string[] Identifiers
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append("Global { ");
            sb.Append(string.Join(", ", Identifiers));
            sb.Append(" }\n");
        }
    }


    public sealed record Property(
        string[] Identifiers
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append("Property { ");
            sb.Append(string.Join(", ", Identifiers));
            sb.Append(" }\n");
        }
    }

    public sealed record Number(
        LingoNumber Value
    ) : Base;

    public sealed record String(
        string Value
    ) : Base;

    public sealed record Symbol(
        string Value
    ) : Base;

    public sealed record List(
        Base[] Values
    ) : Base;

    public sealed record PropertyList(
        KeyValuePair<Base, Base>[] Values
    ) : Base;

    public sealed record NewCastLib(
        Base Type,
        Base CastLib
    ) : Base;

    public sealed record NewScript(
        Base Type,
        Base[] Args
    ) : Base;

    public sealed record TypeSpec(
        string Name,
        string Type
    ) : Base;

    public sealed record Handler(
        string Name,
        TypedVariable[] Parameters,
        StatementBlock Body
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append($"Handler '{Name}' (\n{string.Join('\n', Parameters.Select(p => p.ToString()))}\n) {{\n");

            foreach (var node in Body.Statements)
            {
                node.DebugPrint(sb, indentation + 1);
                sb.Append('\n');
            }

            sb.Append(Indent(indentation));
            sb.Append(")\n");
        }
    }

    public sealed record VariableName(
        string Name
    ) : Base;

    public sealed record The(
        string Name
    ) : Base;

    public sealed record TheNumberOf(
        Base Expr
    ) : Base;

    public sealed record TheNumberOfLines(
        Base Text
    ) : Base;

    public sealed record ThingOf(
        ThingOfType Type,
        Base Index,
        Base Collection
    ) : Base;

    public enum ThingOfType
    {
        Item,
        Line,
        Char
    }

    public sealed record Constant(
        string Name
    ) : Base;

    public sealed record MemberCall(
        Base Expression,
        string Name,
        Base[] Parameters
    ) : Base;

    public sealed record MemberIndex(
        Base Expression,
        Base Index
    ) : Base;

    public sealed record MemberSlice(
        Base Expression,
        Base Start,
        Base End
    ) : Base;

    public sealed record StatementBlock(
        Base[] Statements
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            foreach (var node in Statements)
            {
                node.DebugPrint(sb, indentation);
                sb.Append('\n');
            }
        }
    }

    public sealed record If(
        Base Condition,
        StatementBlock Statements,
        StatementBlock? Else
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append("if (\n");
            Condition.DebugPrint(sb, indentation + 1);
            sb.Append(Indent(indentation));
            sb.Append(") {\n");

            Statements.DebugPrint(sb, indentation + 1);

            if (Else != null)
            {
                sb.Append(Indent(indentation));
                sb.Append("} Else {\n");
                Else.DebugPrint(sb, indentation + 1);
            }

            sb.Append(Indent(indentation));
            sb.Append("}\n");
        }
    }

    public sealed record RepeatWhile(
        Base Condition,
        StatementBlock Block
    ) : Base;

    public sealed record RepeatWithCounter(
        string Variable,
        Base Start,
        Base Finish,
        StatementBlock Block
    ) : Base;

    public sealed record RepeatWithList(
        string Variable,
        Base ListExpr,
        StatementBlock Block
    ) : Base;

    public sealed record Case(
        Base Expression,
        (Base[] exprs, StatementBlock)[] Cases,
        StatementBlock? Otherwise
    ) : Base;

    public sealed record GlobalCall(
        string Name,
        Base[] Arguments
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append($"GlobalCall '{Name}' (\n");

            foreach (var node in Arguments)
            {
                node.DebugPrint(sb, indentation + 1);
                sb.Append('\n');
            }

            sb.Append(Indent(indentation));
            sb.Append(")\n");
        }
    }

    public sealed record MemberProp(
        Base Expression,
        string Property
    ) : Base;

    public sealed record UnaryOperator(
        UnaryOperatorType Type,
        Base Expression
    ) : Base;

    public sealed record BinaryOperator(
        BinaryOperatorType Type,
        Base Left,
        Base Right
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append($"{Type} (\n");

            Left.DebugPrint(sb, indentation + 1);
            sb.Append('\n');
            Right.DebugPrint(sb, indentation + 1);
            sb.Append('\n');

            sb.Append(Indent(indentation));
            sb.Append(")\n");
        }
    }

    public sealed record PutInto(
        Base Expression,
        PutType Type,
        Base Collection
    ) : Base;

    public enum PutType
    {
        Before,
        After,
        Into
    }

    public sealed record Script(
        Base[] Nodes
    ) : Base
    {
        public override void DebugPrint(StringBuilder sb, int indentation)
        {
            sb.Append(Indent(indentation));
            sb.Append("Script {\n");

            foreach (var node in Nodes)
            {
                node.DebugPrint(sb, indentation + 1);
            }

            sb.Append(Indent(indentation));
            sb.Append("}\n");
        }
    }

    public sealed record TypedVariable(string Name, string? Type) : Base
    {
        public override string ToString()
        {
            if (Type == null)
                return Name;

            return $"{Name}: {Type}";
        }
    }

    private static string Indent(int count) => new string(' ', count * 4);

    public enum UnaryOperatorType
    {
        // Precedence 3
        Negate,

        // Precedence 5
        Not
    }

    public enum BinaryOperatorType
    {
        // Precedence 1
        LessThan,
        LessThanOrEqual,
        NotEqual,
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        Contains,
        Starts,

        // Precedence 2
        ConcatSpace,
        Concat,

        // Precedence 4
        Add,
        Multiply,
        Divide,
        And,
        Or,
        Sand,
        Sor,
        Mod,

        // Precedence 5
        Subtract,
    }
}
