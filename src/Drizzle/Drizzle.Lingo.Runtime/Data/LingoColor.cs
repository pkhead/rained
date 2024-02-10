using System;
using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public struct LingoColor : IEquatable<LingoColor>
{
    public const int PackWhite = unchecked((int)0xFF_FF_FF_FF);
    public const int PackBlack = unchecked((int)0xFF_00_00_00);
    public const int PackRed = unchecked((int)0xFF_FF_00_00);

    // Doing a list ToArray() so we can use [0] = syntax.
    private static readonly int[] Palette = new int[256];

    static LingoColor()
    {
        // BGRA BUT little endian so this alpha red green blue.
        Palette[0] = PackWhite;
        Palette[6] = PackRed;
        Palette[255] = PackBlack;
    }

    public static readonly LingoColor White = new(255, 255, 255);
    public static readonly LingoColor Black = default;

    public byte RedByte;
    public byte GreenByte;
    public byte BlueByte;

    public LingoNumber red
    {
        get => RedByte;
        set => RedByte = (byte)value.IntValue;
    }

    public LingoNumber green
    {
        get => GreenByte;
        set => GreenByte = (byte)value.IntValue;
    }

    public LingoNumber blue
    {
        get => BlueByte;
        set => BlueByte = (byte)value.IntValue;
    }

    // Pack as BGRA32
    public int BitPack => (int)(0xFF_00_00_00
                                | (uint)(RedByte << 16)
                                | (uint)(GreenByte << 8)
                                | (uint)(BlueByte << 0));

    // Unpack as BGRA32
    public static LingoColor BitUnpack(int packed)
    {
        return new LingoColor(
            (packed & 0x00_FF_00_00) >> 16,
            (packed & 0x00_00_FF_00) >> 8,
            packed & 0x00_00_00_FF);
    }

    public LingoColor(int r, int g, int b)
    {
        RedByte = (byte)r;
        GreenByte = (byte)g;
        BlueByte = (byte)b;
    }

    public static implicit operator LingoColor(int paletteIndex)
    {
        var palCol = Palette[paletteIndex];
        if (palCol == 0)
        {
            Log.Warning("Unknown palette color: {PaletteIndex}", paletteIndex);
            return White;
        }

        return BitUnpack(palCol);
    }

    public static implicit operator LingoColor(LingoNumber paletteIndex)
    {
        return (int)paletteIndex;
    }

    public bool Equals(LingoColor other)
    {
        return RedByte == other.RedByte && GreenByte == other.GreenByte && BlueByte == other.BlueByte;
    }

    public override bool Equals(object? obj)
    {
        return obj is LingoColor other && Equals(other);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(RedByte, GreenByte, BlueByte);
    }

    public static bool operator ==(LingoColor left, LingoColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LingoColor left, LingoColor right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"color( {RedByte}, {GreenByte}, {BlueByte} )";
    }
}