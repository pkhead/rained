using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Drizzle.Lingo.Runtime.Scripting;

public sealed class LingoScriptRuntime
{
    private readonly Dictionary<string, GetMemberBinder> _getMemberBinders = new();
    private readonly Dictionary<ExpressionType, BinaryOperationBinder> _binaryOperationBinders = new();

    public LingoScriptRuntime(LingoGlobal global)
    {
        Global = global;
    }

    public LingoGlobal Global { get; }

    public GetMemberBinder GetGetMemberBinder(string memberName)
    {
        if (!_getMemberBinders.TryGetValue(memberName, out var binder))
            _getMemberBinders[memberName] = binder = new LingoGetMemberBinder(memberName);

        return binder;
    }

    public BinaryOperationBinder GetBinaryOperationBinder(ExpressionType type)
    {
        if (!_binaryOperationBinders.TryGetValue(type, out var binder))
            _binaryOperationBinders[type] = binder = new LingoBinaryOperationBinder(type);

        return binder;
    }
}