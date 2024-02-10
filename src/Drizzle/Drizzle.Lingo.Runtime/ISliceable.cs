using System;

namespace Drizzle.Lingo.Runtime;

public interface ISliceable
{
    public object this[Range idx] { get; }
}