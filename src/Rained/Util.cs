namespace Rained;

/// <summary>
/// Provides generalized functions where I don't know a good class to put it in.
/// </summary>
static class Util
{
    public static int Mod(int a, int b)
        => (a%b + b)%b;
}