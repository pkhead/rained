namespace Drizzle.Lingo.Runtime;

public sealed class LingoMask
{
    // Currently 1-bit masks are assumed, higher-bit transparency masks not supported.

    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }

    public LingoMask(int width, int height, byte[] data)
    {
        Width = width;
        Height = height;
        Data = data;
    }
}