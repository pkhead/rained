using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Drizzle.Lingo.Runtime.Parser;

namespace Drizzle.Lingo.Runtime.Scripting;

public static class ScriptCompiler
{
    /*
    public static Delegate CompileHandler(AstNode.Handler handler, LingoScriptRuntime scriptRuntime)
    {
        var scope = new CompileScope(scriptRuntime);

        var paramList = new List<ParameterExpression>();
        foreach (var paramName in handler.Parameters)
        {
            var paramExpr = Expression.Parameter(typeof(object), paramName);
            paramList.Add(paramExpr);
            scope.Locals.Add(paramName, paramExpr);
        }

        scope.RetLabel = Expression.Label(typeof(object), "ret");

        var topBlockExpressions = new List<Expression>();
        foreach (var statement in handler.Body.Statements)
        {
            var expr = CompileNode(statement, scope);

            topBlockExpressions.Add(expr);
        }

        topBlockExpressions.Add(Expression.Label(scope.RetLabel, Expression.Constant(null)));

        var block = Expression.Block(scope.DeclaredLocals, topBlockExpressions);
        var delegateType = Expression.GetFuncType(Enumerable.Repeat(typeof(object), paramList.Count + 1).ToArray());
        var lambda = Expression.Lambda(delegateType, block, handler.Name, paramList);

        return lambda.Compile();
    }
    */

    public static object Evaluate(AstNode.Base expression, LingoScriptRuntime scriptRuntime)
    {
        var scope = new CompileScope(scriptRuntime);

        scope.GlobalParameter = Expression.Parameter(typeof(LingoGlobal), "global");

        var exprTree = CompileNode(expression, scope);

        var lambdaExpr = Expression.Lambda<Func<LingoGlobal, object>>(
            exprTree, "eval", false, new[] {scope.GlobalParameter});

        var compiled = lambdaExpr.Compile();

        return compiled(scriptRuntime.Global);
    }

    private static Expression CompileNode(AstNode.Base node, CompileScope scope)
    {
        return node switch
        {
            AstNode.Assignment ass => CompileAssigment(ass, scope),
            AstNode.Return ret => CompileReturn(ret, scope),
            AstNode.String s => CompileString(s, scope),
            AstNode.Symbol symbol => CompileSymbol(symbol, scope),
            AstNode.VariableName varName => CompileVariableName(varName, scope),
            AstNode.GlobalCall globalCall => CompileGlobalCall(globalCall, scope),
            AstNode.List list => CompileList(list, scope),
            AstNode.PropertyList propList => CompilePropertyList(propList, scope),
            AstNode.BinaryOperator binOp => CompileBinaryOperator(binOp, scope),
            AstNode.Number number => CompileNumber(number, scope),
            AstNode.MemberProp memProp => CompileMemberProp(memProp, scope),
            AstNode.UnaryOperator unaryOp => CompileUnaryOperator(unaryOp, scope),
            AstNode.Constant constant => CompileConstant(constant, scope),
            _ => throw new NotImplementedException()
        };
    }

    private static Expression CompileConstant(AstNode.Constant constant, CompileScope scope)
    {
        return constant.Name switch
        {
            "backspace" => Expression.Constant("\x08"),
            "empty" => Expression.Constant(""),
            "enter" => Expression.Constant("\x03"),
            "false" => Expression.Constant(0),
            "pi" => Expression.Constant(new LingoNumber(Math.PI)),
            "quote" => Expression.Constant("\""),
            "return" => Expression.Constant("\r"),
            "space" => Expression.Constant(" "),
            "true" => Expression.Constant(1),
            "void" => Expression.Constant(null),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static Expression CompileUnaryOperator(AstNode.UnaryOperator unaryOp, CompileScope scope)
    {
        var exprType = unaryOp.Type switch
        {
            AstNode.UnaryOperatorType.Negate => ExpressionType.Negate,
            AstNode.UnaryOperatorType.Not => ExpressionType.Not,
            _ => throw new NotSupportedException()
        };

        return Expression.MakeUnary(exprType, CompileNode(unaryOp.Expression, scope), typeof(object));
    }

    private static Expression CompilePropertyList(AstNode.PropertyList propList, CompileScope scope)
    {
        var ctorMethod = typeof(LingoPropertyList).GetConstructor(new[] {typeof(int)})!;
        var ctor = Expression.New(ctorMethod, Expression.Constant(propList.Values.Length));
        var local = Expression.Parameter(typeof(LingoPropertyList), "propListInstanceTmp");

        var indexer = typeof(LingoPropertyList).GetProperty("Item");

        var initBlock = Expression.Block(propList.Values.Select(v =>
        {
            var key = CompileNode(v.Key, scope);
            var value = CompileNode(v.Value, scope);

            return Expression.Assign(
                Expression.MakeIndex(local, indexer, new[] {Expression.Convert(key, typeof(object))}),
                Expression.Convert(value, typeof(object)));
        }));

        return Expression.Block(
            typeof(LingoPropertyList), new[] {local},
            // Code
            Expression.Assign(local, ctor),
            initBlock,
            local);
    }

    private static Expression CompileList(AstNode.List list, CompileScope scope)
    {
        var ctor = typeof(LingoList).GetConstructor(new[] {typeof(IEnumerable<dynamic>)})!;

        return Expression.New(ctor,
            Expression.NewArrayInit(
                typeof(object),
                list.Values.Select(v => Expression.Convert(CompileNode(v, scope), typeof(object)))
            )
        );
    }

    private static Expression CompileNumber(AstNode.Number node, CompileScope scope)
    {
        return Expression.Constant(node.Value, typeof(LingoNumber));
    }

    private static Expression CompileSymbol(AstNode.Symbol node, CompileScope scope)
    {
        return Expression.Constant(new LingoSymbol(node.Value), typeof(LingoSymbol));
    }

    private static Expression CompileString(AstNode.String node, CompileScope scope)
    {
        return Expression.Constant(node.Value, typeof(string));
    }

    private static Expression CompileMemberProp(AstNode.MemberProp node, CompileScope scope)
    {
        return Expression.Dynamic(
            scope.ScriptRuntime
                .GetGetMemberBinder(node.Property),
            typeof(object),
            CompileNode(node.Expression, scope)
        );
    }

    private static Expression CompileBinaryOperator(AstNode.BinaryOperator node, CompileScope scope)
    {
        var mappedBinOp = node.Type switch
        {
            AstNode.BinaryOperatorType.Subtract => ExpressionType.Subtract,
            AstNode.BinaryOperatorType.Add => ExpressionType.Add,
            AstNode.BinaryOperatorType.Multiply => ExpressionType.Multiply,
            _ => throw new NotSupportedException()
        };

        return Expression.Dynamic(
            scope.ScriptRuntime.GetBinaryOperationBinder(mappedBinOp),
            typeof(object),
            CompileNode(node.Left, scope),
            CompileNode(node.Right, scope)
        );
    }

    private static Expression CompileGlobalCall(AstNode.GlobalCall node, CompileScope scope)
    {
        var members = typeof(LingoGlobal).GetMember(node.Name.ToLower())!;
        var method = members.OfType<MethodInfo>().First(m => m.GetParameters().Length == node.Arguments.Length);

        var args = node.Arguments.Select(n => CompileNode(n, scope));

        if (method.IsStatic)
            return Expression.Call(null, method, args);

        Debug.Assert(scope.GlobalParameter != null);
        return Expression.Call(scope.GlobalParameter!, method, args);
    }

    private static Expression CompileVariableName(AstNode.VariableName node, CompileScope scope)
    {
        var localName = node.Name;
        if (!scope.Locals.TryGetValue(localName, out var local))
        {
            local = Expression.Parameter(typeof(object), localName);
            scope.Locals.Add(localName, local);
            scope.DeclaredLocals.Add(local);
        }

        return local;
    }

    private static Expression CompileReturn(AstNode.Return ret, CompileScope scope)
    {
        var value = ret.Value != null ? CompileNode(ret.Value, scope) : Expression.Constant(null);

        return Expression.Goto(scope.RetLabel, value);
    }

    private static Expression CompileAssigment(AstNode.Assignment node, CompileScope scope)
    {
        var local = CompileVariableName((AstNode.VariableName) node.Assigned, scope);
        var value = CompileNode(node.Value, scope);

        return Expression.Assign(local, value);
    }


    private sealed class CompileScope
    {
        public readonly LingoScriptRuntime ScriptRuntime;

        public readonly Dictionary<string, ParameterExpression> Locals = new();
        public readonly List<ParameterExpression> DeclaredLocals = new();
        public LabelTarget RetLabel = default!;
        public ParameterExpression? GlobalParameter;

        public CompileScope(LingoScriptRuntime scriptRuntime)
        {
            ScriptRuntime = scriptRuntime;
        }
    }
}