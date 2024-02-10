using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Drizzle.Lingo.Runtime;

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
public readonly struct LingoNumber : IEquatable<LingoNumber>, IComparable<LingoNumber>,
    IAdditionOperators<LingoNumber, LingoNumber, LingoNumber>,
    ISubtractionOperators<LingoNumber, LingoNumber, LingoNumber>,
    IMultiplyOperators<LingoNumber, LingoNumber, LingoNumber>,
    IDivisionOperators<LingoNumber, LingoNumber, LingoNumber>,
    IModulusOperators<LingoNumber, LingoNumber, LingoNumber>
{
    private readonly double _decimalValue;
    private readonly int _intValue;
    public readonly bool IsDecimal;

    public double DecimalValue => IsDecimal ? _decimalValue : _intValue;
    public int IntValue => IsDecimal ? (int)Math.Round(DecimalValue, MidpointRounding.AwayFromZero) : _intValue;

    public LingoNumber integer => IntValue;
    public LingoNumber @float => DecimalValue;

    public LingoNumber(double decimalValue)
    {
        IsDecimal = true;
        _intValue = default;
        _decimalValue = decimalValue;
    }

    public LingoNumber(int intValue)
    {
        IsDecimal = false;
        _decimalValue = default;
        _intValue = intValue;
    }

    // just...
    public LingoNumber findpos(object val) => int.MinValue;

    public static LingoNumber Parse(ReadOnlySpan<char> text)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return new LingoNumber(intValue);

        return new LingoNumber(double.Parse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture));
    }

    public static LingoNumber Abs(LingoNumber dec)
    {
        if (dec.IsDecimal)
            return new(Math.Abs(dec._decimalValue));

        return new(Math.Abs(dec.IntValue));
    }

    public static LingoNumber Sqrt(LingoNumber dec)
    {
        var val = new LingoNumber(Math.Sqrt(dec.DecimalValue));
        return dec.IsDecimal ? val : val.integer;
    }

    // Trig functions are always float.
    public static LingoNumber Cos(LingoNumber dec) => new(Math.Cos(dec.DecimalValue));
    public static LingoNumber Sin(LingoNumber dec) => new(Math.Sin(dec.DecimalValue));
    public static LingoNumber Tan(LingoNumber dec) => new(Math.Tan(dec.DecimalValue));
    public static LingoNumber Atan(LingoNumber dec) => new(Math.Atan(dec.DecimalValue));

    // Pow is always float
    public static LingoNumber Pow(LingoNumber @base, LingoNumber exp) =>
        new(Math.Pow(@base.DecimalValue, exp.DecimalValue));

    public override string ToString() => IsDecimal ? DecimalValue.ToString("F4", CultureInfo.InvariantCulture) : IntValue.ToString(CultureInfo.InvariantCulture);

    public static LingoNumber operator -(LingoNumber dec) =>
        dec.IsDecimal ? new(-dec.DecimalValue) : new(-dec.IntValue);

    public static LingoNumber operator +(LingoNumber dec) =>
        dec.IsDecimal ? new(+dec.DecimalValue) : new(+dec.IntValue);

    public static LingoNumber operator +(LingoNumber a, LingoNumber b)
    {
        if (!a.IsDecimal && !b.IsDecimal)
            return new(a._intValue + b._intValue);

        return new(a.DecimalValue + b.DecimalValue);
    }

    public static LingoNumber operator -(LingoNumber a, LingoNumber b)
    {
        if (!a.IsDecimal && !b.IsDecimal)
            return new(a._intValue - b._intValue);

        return new(a.DecimalValue - b.DecimalValue);
    }

    public static LingoNumber operator *(LingoNumber a, LingoNumber b)
    {
        if (!a.IsDecimal && !b.IsDecimal)
            return new(a._intValue * b._intValue);

        return new(a.DecimalValue * b.DecimalValue);
    }

    public static LingoNumber operator /(LingoNumber a, LingoNumber b)
    {
        if (!a.IsDecimal && !b.IsDecimal)
            return new(a._intValue / b._intValue);

        return new(a.DecimalValue / b.DecimalValue);
    }

    public static LingoNumber operator %(LingoNumber a, LingoNumber b)
    {
        if (!a.IsDecimal && !b.IsDecimal)
            return new(a._intValue % b._intValue);

        return new(a.DecimalValue % b.DecimalValue);
    }

    public static implicit operator LingoNumber(int x) => new(x);
    public static implicit operator LingoNumber(float x) => new(x);
    public static implicit operator LingoNumber(double x) => new(x);
    public static explicit operator int(LingoNumber x) => x.IntValue;
    public static explicit operator float(LingoNumber x) => (float)x.DecimalValue;
    public static explicit operator double(LingoNumber x) => x.DecimalValue;

    public bool Equals(LingoNumber other)
    {
        if (IsDecimal || other.IsDecimal)
            return DecimalValue.Equals(other.DecimalValue);

        return _intValue == other._intValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is LingoNumber other && Equals(other);
    }

    public override int GetHashCode()
    {
        return DecimalValue.GetHashCode();
    }

    public static bool operator ==(LingoNumber left, LingoNumber right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LingoNumber left, LingoNumber right)
    {
        return !left.Equals(right);
    }

    public int CompareTo(LingoNumber other)
    {
        return DecimalValue.CompareTo(other.DecimalValue);
    }

    public static bool operator <(LingoNumber left, LingoNumber right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(LingoNumber left, LingoNumber right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(LingoNumber left, LingoNumber right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(LingoNumber left, LingoNumber right)
    {
        return left.CompareTo(right) >= 0;
    }
}
