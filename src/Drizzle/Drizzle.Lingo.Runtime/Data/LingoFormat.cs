namespace Drizzle.Lingo.Runtime;

public static class LingoFormat
{
    public static string LingoToString(object? obj)
    {
        if (obj is string str)
            return $"\"{str}\"";

        return obj?.ToString() ?? "<Void>";
    }
}
