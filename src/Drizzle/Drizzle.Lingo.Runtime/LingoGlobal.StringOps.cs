using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    // Cache these values because Lingo accesses them a lot and we want to avoid O(n^2) behavior here.
    private static readonly ConditionalWeakTable<string, StringLineData> StringLineCache = new();

    private static StringLineData GetCachedStringLineData(string str)
    {
        return StringLineCache.GetValue(str, CacheStringLineData);
    }

    private static StringLineData CacheStringLineData(string key)
    {
        var list = new List<int>();

        for (var i = 0; i < key.Length; i++)
        {
            var chr = key[i];
            // Lingo cares ONLY about CR, nothing else.
            if (chr == '\r')
                list.Add(i);
        }

        return new StringLineData
        {
            NewlineIndices = list.ToArray()
        };
    }

    public static LingoNumber thenumberoflines_helper(string value)
    {
        var cacheData = GetCachedStringLineData(value);
        return cacheData.NewlineIndices.Length + 1;
    }

    public static string lineof_helper(LingoNumber idx, string collection)
    {
        return linemember_helper(collection)[idx];
    }

    public static string charof_helper(LingoNumber idx, string str)
    {
        var iVal = idx.IntValue;
        // This is such a cursed program god damn.
        if (iVal < 1)
            return str;

        if (iVal > str.Length)
            return "";

        return str[iVal - 1].ToString();
    }

    public static StringCharIndex charmember_helper(string d)
    {
        return new StringCharIndex(d);
    }

    public static StringLineIndex linemember_helper(string d)
    {
        return new StringLineIndex(d);
    }

    public static LingoNumber lengthmember_helper(string d)
    {
        return d.Length;
    }

    public static LingoNumber lengthmember_helper(dynamic d)
    {
        return d.length;
    }

    public sealed class StringCharIndex : DynamicObject, ISliceable
    {
        public StringCharIndex(string s)
        {
            String = s;
        }

        public string String { get; }

        public string? this[int idx] => idx >= 1 && idx < String.Length ? String[idx - 1].ToString() : null;
        public string? this[LingoNumber idx] => this[(int) idx];

        // I have no idea why this is necessary.
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = this[(int)(LingoNumber)indexes[0]];
            return true;
        }

        public object this[Range idx]
        {
            get
            {
                if (idx.Start.IsFromEnd || idx.End.IsFromEnd)
                    throw new ArgumentException();

                return String[(idx.Start.Value - 1)..(idx.End.Value)];
            }
        }
    }

    public sealed class StringLineIndex : DynamicObject, ISliceable
    {
        public StringLineIndex(string s)
        {
            String = s;
        }

        public string String { get; }

        public string this[LingoNumber value]
        {
            get
            {
                if (value <= 0)
                    return String;

                var cacheData = GetCachedStringLineData(String);
                var indices = cacheData.NewlineIndices;
                var idx = (int) value - 1;

                if (idx > indices.Length)
                    return "";

                var endIdx = String.Length;
                if (idx < indices.Length)
                    endIdx = indices[idx];

                var startIdx = 0;
                if (idx > 0)
                    startIdx = indices[idx-1] + 1;

                return String[startIdx..endIdx];

                /*var curLine = 1;


                var sr = new StringReader(String);

                while (sr.ReadLine() is { } line)
                {
                    if (curLine++ == value)
                        return line;
                }

                throw new IndexOutOfRangeException();*/
            }
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = this[(int)(LingoNumber)indexes[0]];
            return true;
        }

        public object this[Range idx] => throw new NotImplementedException();
    }

    private object DoSliceString(string str, int start, int end)
    {
        throw new NotImplementedException();
    }

    public string numtochar(int num)
    {
        return ((char)num).ToString();
    }

    public string numtochar(LingoNumber num) => numtochar((int)num);

    private sealed class StringLineData
    {
        public int[] NewlineIndices = default!;
    }
}
