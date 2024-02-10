using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Drizzle.Lingo.Runtime.Scripting;

internal static class BinderHelpers
{
    public static Expression EnsureObjectResult(Expression expr)
    {
        if (!expr.Type.IsValueType)
            return expr;
        if (expr.Type == typeof(void))
            return Expression.Block(
                expr, Expression.Default(typeof(object)));
        else
            return Expression.Convert(expr, typeof(object));
    }
}

public sealed class LingoGetMemberBinder : GetMemberBinder
{
    public LingoGetMemberBinder(string name) : base(name, ignoreCase: true)
    {
    }

    public override DynamicMetaObject FallbackGetMember(
        DynamicMetaObject target,
        DynamicMetaObject? errorSuggestion)
    {
        if (!target.HasValue)
            return Defer(target);

        var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;
        var members = target.LimitType.GetMember(Name, flags);
        var mem = members.Single();

        return new DynamicMetaObject(
            BinderHelpers.EnsureObjectResult(
                Expression.MakeMemberAccess(
                    Expression.Convert(
                        target.Expression,
                        mem.DeclaringType!), mem)),
            BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));

        /*
        return errorSuggestion ??
               new DynamicMetaObject(
                   Expression.Constant(null),
                   target.Restrictions.Merge(
                       BindingRestrictions.GetTypeRestriction(
                           target.Expression, target.LimitType)));*/
    }
}

public sealed class LingoBinaryOperationBinder : BinaryOperationBinder
{
    public LingoBinaryOperationBinder(ExpressionType operation) : base(operation)
    {
    }

    public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg,
        DynamicMetaObject? errorSuggestion)
    {
        // Defer if any object has no value so that we evaulate their
        // Expressions and nest a CallSite for the InvokeMember.
        if (!target.HasValue || !arg.HasValue)
        {
            return Defer(target, arg);
        }

        var restrictions = target.Restrictions.Merge(arg.Restrictions)
            .Merge(BindingRestrictions.GetTypeRestriction(
                target.Expression, target.LimitType))
            .Merge(BindingRestrictions.GetTypeRestriction(
                arg.Expression, arg.LimitType));
        return new DynamicMetaObject(
            BinderHelpers.EnsureObjectResult(
                Expression.MakeBinary(
                    this.Operation,
                    Expression.Convert(target.Expression, target.LimitType),
                    Expression.Convert(arg.Expression, arg.LimitType))),
            restrictions
        );
    }
}