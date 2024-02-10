using System;

namespace Drizzle.Lingo.Runtime;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MovieScriptAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ParentScriptAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BehaviorScriptAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class LingoGlobalAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class LingoPropertyAttribute : Attribute
{
}