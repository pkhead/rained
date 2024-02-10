namespace Drizzle.Logic;

public static class MathHelper
{
    public static void SatAdd(ref byte val, byte add)
    {
        val = SatAdd(val, add);
    }

    public static byte SatAdd(byte val, byte add)
    {
        var added = val + add;
        if (added > byte.MaxValue)
            return byte.MaxValue;

        return (byte)added;
    }
}
