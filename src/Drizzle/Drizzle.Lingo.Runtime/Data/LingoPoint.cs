using System;
using System.Numerics;

namespace Drizzle.Lingo.Runtime;

public struct LingoPoint : IEquatable<LingoPoint>, ILingoVector,
    IAdditionOperators<LingoPoint, LingoPoint, LingoPoint>,
    ISubtractionOperators<LingoPoint, LingoPoint, LingoPoint>,
    IMultiplyOperators<LingoPoint, LingoPoint, LingoPoint>,
    IDivisionOperators<LingoPoint, LingoPoint, LingoPoint>,
    IAdditionOperators<LingoPoint, LingoNumber, LingoPoint>,
    ISubtractionOperators<LingoPoint, LingoNumber, LingoPoint>,
    IMultiplyOperators<LingoPoint, LingoNumber, LingoPoint>,
    IDivisionOperators<LingoPoint, LingoNumber, LingoPoint>
{
    // Yes, of course
    // Despite what the documentation clearly states
    // These contain float coordinates, not int.

    public LingoNumber loch;
    public LingoNumber locv;

    public Vector2 AsVector2 => new((float)loch.DecimalValue, (float)locv.DecimalValue);

    int ILingoVector.CountElems => 2;

    object ILingoVector.this[int index] => index switch
    {
        0 => loch,
        1 => locv,
        _ => throw new ArgumentOutOfRangeException()
    };

    public LingoPoint(LingoNumber loch, LingoNumber locv)
    {
        this.loch = loch;
        this.locv = locv;
    }

    public static LingoPoint operator +(LingoPoint a, LingoPoint b) => new(a.loch + b.loch, a.locv + b.locv);
    public static LingoPoint operator -(LingoPoint a, LingoPoint b) => new(a.loch - b.loch, a.locv - b.locv);
    public static LingoPoint operator *(LingoPoint a, LingoPoint b) => new(a.loch * b.loch, a.locv * b.locv);
    public static LingoPoint operator /(LingoPoint a, LingoPoint b) => new(a.loch / b.loch, a.locv / b.locv);

    public static LingoPoint operator +(LingoPoint a, LingoNumber b) => new(a.loch + b, a.locv + b);
    public static LingoPoint operator +(LingoNumber a, LingoPoint b) => new(a + b.loch, a + b.locv);
    public static LingoPoint operator -(LingoPoint a, LingoNumber b) => new(a.loch - b, a.locv - b);
    public static LingoPoint operator -(LingoNumber a, LingoPoint b) => new(a - b.loch, a - b.locv);
    public static LingoPoint operator *(LingoPoint a, LingoNumber b) => new(a.loch * b, a.locv * b);
    public static LingoPoint operator *(LingoNumber a, LingoPoint b) => new(a * b.loch, a * b.locv);
    public static LingoPoint operator /(LingoPoint a, LingoNumber b) => new(a.loch / b, a.locv / b);
    public static LingoPoint operator /(LingoNumber a, LingoPoint b) => new(a / b.loch, a / b.locv);

    public static LingoPoint operator -(LingoPoint a) => new(-a.loch, -a.locv);

    public LingoNumber inside(LingoRect rect)
    {
        var b = rect.left <= loch && rect.top <= locv &&
                rect.right > loch && rect.bottom > locv;

        return b ? 1 : 0;
    }

    public bool Equals(LingoPoint other)
    {
        return loch.Equals(other.loch) && locv.Equals(other.locv);
    }

    public override bool Equals(object? obj)
    {
        return obj is LingoPoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(loch, locv);
    }

    public static bool operator ==(LingoPoint left, LingoPoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LingoPoint left, LingoPoint right)
    {
        return !left.Equals(right);
    }

    public override string ToString() => $"point({loch}, {locv})";
}
