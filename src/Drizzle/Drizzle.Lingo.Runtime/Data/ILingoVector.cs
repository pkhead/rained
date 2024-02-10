namespace Drizzle.Lingo.Runtime;

public interface ILingoVector
{
    int CountElems { get; }
    object? this[int index] { get; }
}