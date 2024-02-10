namespace Drizzle.Lingo.Runtime;

public partial class LingoRuntime
{
    // Yeah, I reverse engineered Director for this.
    // Please don't sue me, Adobe.

    private RngState _rngState;

    public uint RngSeed
    {
        get => _rngState.Seed;
        set
        {
            InitRng(ref _rngState);
            _rngState.Seed = value;
        }
    }

    public int Random(int clamp)
    {
        return Random(ref _rngState, clamp);
    }

    private static int RandomDerive(uint param)
    {
        var var1 = (int) ((param << 0xd ^ param) - ((int) param >> 0x15));
        var var2 = (uint)(((var1 * var1 * 0x3d73 + 0xc0ae5) * var1 + 0xd208dd0d & 0x7fffffff) + var1);
        return (int) ((var2 * 0x2000 ^ var2) - ((int) var2 >> 0x15));
    }

    internal static void InitRng(ref RngState state)
    {
        state.Seed = 1;
        // A note here:
        // This init field is actually initialized based to another parameter to this function in Director's code.
        // It's calculated by substringing a region of the string "Macromedia Director", based on said param.
        // I simply hardcoded the value calculated for the Lingo RNG here instead.
        state.Init = 0xA3000000;
    }

    internal static int Random(ref RngState state, int clamp)
    {
        if (state.Seed == 0)
            InitRng(ref state);

        if ((state.Seed & 1) == 0)
            state.Seed = (uint)state.Seed >> 1;
        else
            state.Seed = (uint)state.Seed >> 1 ^ state.Init;

        int var1 = RandomDerive((uint) (state.Seed * 0x47));
        if (clamp > 0)
            var1 = (int)((long)(ulong)(var1 & 0x7FFFFFFF) % (long) clamp);

        return var1 + 1;
    }

    internal struct RngState
    {
        public uint Seed;
        public uint Init;
    }
}