using System;
using System.Threading.Tasks;
using Drizzle.Lingo.Runtime;

namespace Drizzle.Logic;

/// <summary>
/// Represents an "owner" of an isolated Lingo runtime.
/// Provides some thread-safe APIs for stuff like cast inspection.
/// </summary>
/// <remarks>
/// At least, thread-safe from the main thread.
/// </remarks>
public interface ILingoRuntimeManager
{
    /// <summary>
    /// Executes an action on the thread of the lingo runtime.
    /// </summary>
    Task Exec(Action<LingoRuntime> action);

    /// <summary>
    /// Executes an action on the thread of the lingo runtime.
    /// </summary>
    Task<T> Exec<T>(Func<LingoRuntime, T> func);
}