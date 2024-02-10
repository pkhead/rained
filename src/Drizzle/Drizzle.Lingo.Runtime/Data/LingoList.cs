using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public class LingoList : IEnumerable<object>, ILingoListDuplicate, IEquatable<LingoList>, ILingoVector
{
    public List<object?> List { get; }

    int ILingoVector.CountElems => List.Count;

    object? ILingoVector.this[int index] => List[index];

    public dynamic? this[int index]
    {
        get => List[index - 1];
        set => List[index - 1] = value;
    }

    public dynamic? this[LingoNumber index]
    {
        get => this[(int)index];
        set => this[(int)index] = value;
    }

    public LingoNumber count => List.Count;

    public LingoList()
    {
        List = new List<object?>();
    }

    public LingoList(int capacity)
    {
        List = new List<object?>(capacity);
    }

    public LingoList(IEnumerable<object?> items)
    {
        List = new List<object?>(items);
    }

    public LingoNumber getpos(object? value)
    {
        return List.IndexOf(value) + 1;
    }

    public object? findpos(object? value) => null;

    public void add(object? value)
    {
        Add(value);
    }

    public void Add(object? value)
    {
        List.Add(value);
    }

    public void append(object? value)
    {
        List.Add(value);
    }

    public void deleteat(int number)
    {
        if (number > List.Count || number <= 0)
        {
            Log.Warning("Invalid deleteAt() index");
            return;
        }

        List.RemoveAt(number - 1);
    }

    public void deleteat(LingoNumber number) => deleteat(number.IntValue);

    public void addat(int number, object? value)
    {
        List.Insert(number - 1, value);
    }

    public void addat(LingoNumber number, object? value) => addat(number.IntValue, value);

    public void deleteone(object? value)
    {
        List.Remove(value);
    }

    public LingoList duplicate()
    {
        return new LingoList(List.Select(i => DuplicateIfList(i)));
    }

    public void sort()
    {
        List.Sort(LingoComparer.Instance);
    }

    ILingoListDuplicate ILingoListDuplicate.duplicate()
    {
        return duplicate();
    }

    public IEnumerator<object> GetEnumerator()
    {
        return List.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static LingoList operator +(LingoList a, dynamic b)
    {
        if (b is LingoList lb)
            return new(a.Zip(lb).Select(e => (dynamic)e.First + (dynamic)e.Second));
        return new(a.Select(e => (dynamic)e + b));
    }

    public static LingoList operator -(LingoList a, dynamic b)
    {
        if (b is LingoList lb)
            return new(a.Zip(lb).Select(e => (dynamic)e.First - (dynamic)e.Second));
        return new(a.Select(e => (dynamic)e - b));
    }

    public static LingoList operator *(LingoList a, dynamic b)
    {
        if (b is LingoList lb)
            return new(a.Zip(lb).Select(e => (dynamic)e.First * (dynamic)e.Second));
        return new(a.Select(e => (dynamic)e * b));
    }

    public static LingoList operator /(LingoList a, dynamic b)
    {
        if (b is LingoList lb)
            return new(a.Zip(lb).Select(e => (dynamic)e.First / (dynamic)e.Second));
        return new(a.Select(e => (dynamic)e / b));
    }

    public static bool operator ==(LingoList? a, LingoList? b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        if (a.count != b.count)
            return false;

        for (var i = 0; i < a.count; i++)
        {
            var itemA = a.List[i];
            var itemB = b.List[i];

            if (itemA != itemB)
                return false;
        }

        return true;
    }


    public static bool operator !=(LingoList? a, LingoList? b)
    {
        return !(a == b);
    }

    public static object? DuplicateIfList(object? obj)
    {
        return obj is ILingoListDuplicate dup ? dup.duplicate() : obj;
    }

    public bool Equals(LingoList? other)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object? other)
    {
        return other is LingoList otherList && Equals(otherList);
    }

    public override int GetHashCode()
    {
        // todo: do we care, I don't think lists are used as dict keys anywhere.
        return default;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('[');

        var first = true;
        foreach (var item in List)
        {
            if (!first)
                sb.Append(", ");
            first = false;

            sb.Append(LingoFormat.LingoToString(item));
        }

        sb.Append(']');
        return sb.ToString();
    }

    private sealed class LingoComparer : IComparer<object?>
    {
        public static readonly LingoComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (x == y)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            // Number <-> number.
            if (x is LingoNumber decX && y is LingoNumber decY)
                return decX.CompareTo(decY);

            // String <-> string.
            if (x is string strX && y is string strY)
                return string.CompareOrdinal(strX, strY);

            // Number <-> vector comparisons.
            if (x is LingoNumber decXc && y is ILingoVector vecY)
                return CompareNumVec(decXc, vecY);

            if (x is ILingoVector vecX && y is LingoNumber decYc)
                return -CompareNumVec(decYc, vecX);

            // Vector <-> Vector comparisons.
            if (x is ILingoVector vecXc && y is ILingoVector vecYc)
            {
                // If lengths don't match, vector with smaller length is always smaller.
                var count = vecXc.CountElems;
                var cmpLength = count.CompareTo(vecYc.CountElems);
                if (cmpLength != 0)
                    return cmpLength;

                // If lengths DO match...
                for (var i = 0; i < count; i++)
                {
                    var valX = vecXc[i];
                    var valY = vecYc[i];

                    // Compare by element.
                    var cmp = Compare(valX, valY);
                    if (cmp != 0)
                        return cmp;
                }

                // All elements equal.
                return 0;
            }

            // Last case fallback: compare by string.
            strX = x.ToString()!;
            strY = y.ToString()!;

            return string.Compare(strX, strY, StringComparison.Ordinal);
        }

        private static int CompareNumVec(LingoNumber num, ILingoVector vec)
        {
            throw new NotImplementedException();
        }
    }
}
