using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Drizzle.Lingo.Runtime.Cast;
using Drizzle.Lingo.Runtime.Xtra;

namespace Drizzle.Lingo.Runtime;

/// <summary>
///     Implementation of Lingo's global properties and methods.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class LingoGlobal
{
    public const string BACKSPACE = "\x08";
    public const string EMPTY = "";
    public const string ENTER = "\x03";
    public static readonly LingoNumber TRUE = 1;
    public static readonly LingoNumber FALSE = 0;
    public static readonly LingoNumber PI = new LingoNumber(Math.PI);
    public const string QUOTE = "\"";
    public const string RETURN = "\r";
    public const string SPACE = " ";
    public const object? VOID = null;

    public string the_moviePath => LingoRuntime.MovieBasePath;
    public string the_dirSeparator => Path.DirectorySeparatorChar.ToString();

    public static LingoNumber abs(LingoNumber value) => LingoNumber.Abs(value);

    public static LingoNumber sqrt(LingoNumber value) => LingoNumber.Sqrt(value);

    public static LingoNumber contains(string container, string value) =>
        container.Contains(value, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0;

    public static LingoNumber starts(string container, string value) =>
        container.StartsWith(value, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0;

    public static string concat(object a, object b) => $"{a}{b}";
    public static string concat(string a, string b) => $"{a}{b}";
    public static string concat(object a, object b, object c) => $"{a}{b}{c}";
    public static string concat(string a, string b, string c) => $"{a}{b}{c}";
    public static string concat(params object[] items) => string.Concat(items);
    public static string concat(params string[] items) => string.Concat(items);
    public static string concat_space(object a, object b) => $"{a} {b}";
    public static string concat_space(string a, string b) => $"{a} {b}";
    public static string concat_space(object a, object b, object c) => $"{a} {b} {c}";
    public static string concat_space(string a, string b, string c) => $"{a} {b} {c}";

    public static string concat_space(params object[] a)
    {
        var sb = new StringBuilder();

        var first = true;
        foreach (var item in a)
        {
            if (!first)
            {
                sb.Append(' ');
            }

            first = false;
            sb.Append(item);
        }

        return sb.ToString();
    }

    public dynamic slice_helper(dynamic obj, LingoNumber start, LingoNumber end)
    {
        if (obj is ISliceable sliceable)
            return sliceable[start.IntValue..end.IntValue];

        throw new NotSupportedException();
    }

    public dynamic new_castlib(object type, object lib)
    {
        throw new NotImplementedException();
    }

    public dynamic new_script(string type, LingoList list)
    {
        return LingoRuntime.CreateScript(type, list);
    }

    public static LingoNumber thenumberof_helper(dynamic value)
    {
        if (value is ICollection c)
            return c.Count;

        throw new ArgumentException();
    }

    public static dynamic itemof_helper(dynamic idx, dynamic collection)
    {
        return collection[idx];
    }

    public static LingoPoint point(LingoNumber h, LingoNumber v) => new(h, v);

    public static LingoRect rect(LingoNumber l, LingoNumber t, LingoNumber r, LingoNumber b) =>
        new(l, t, r, b);

    public static LingoRect rect(LingoPoint lt, LingoPoint rb) => new(lt, rb);

    public static LingoNumber cos(LingoNumber d) => LingoNumber.Cos(d);
    public static LingoNumber sin(LingoNumber d) => LingoNumber.Sin(d);
    public static LingoNumber tan(LingoNumber d) => LingoNumber.Tan(d);
    public static LingoNumber atan(LingoNumber d) => LingoNumber.Atan(d);

    public static LingoNumber power(LingoNumber @base, LingoNumber exp) => LingoNumber.Pow(@base, exp);
    public static LingoSymbol symbol(string s) => new(s);

    public void put(object d)
    {
        Console.WriteLine(d);
    }

    public dynamic xtra(object xtraNameOrNum)
    {
        var xtraName = (string)xtraNameOrNum;
        xtraName = xtraName.ToLower();

        return xtraName switch
        {
            "fileio" => new FileIOXtra(),
            "imgxtra" => new ImgXtra(),
            _ => throw new NotSupportedException($"Unsupported xtra: {xtraNameOrNum}")
        };
    }

    public dynamic @new(object a)
    {
        if (a is BaseXtra xtra)
            return xtra.Duplicate();

        throw new NotImplementedException();
    }

    public void basetdisplay(object w, object h, object idk3, object idk, object idk2)
    {
        throw new NotImplementedException();
    }

    public dynamic bascreeninfo(string prop)
    {
        throw new NotImplementedException();
    }

    public string getnthfilenameinfolder(string folderPath, LingoNumber fileNumber)
    {
        var idx = (int)fileNumber - 1;
        var entries = Directory.GetFileSystemEntries(folderPath);
        return idx >= entries.Length ? "" : Path.GetFileName(entries[idx]);
    }

    public dynamic script(string a)
    {
        return LingoRuntime.CreateScript(a, new LingoList());
    }

    public LingoNumber the_milliseconds => _system.milliseconds;
    public string the_moviepath => the_moviePath;
    public string the_dirseparator => Path.DirectorySeparatorChar.ToString();
    public LingoNumber objectp(dynamic d) => throw new NotImplementedException();

    public CastMember? member(object memberNameOrNum, object? castNameOrNum = null) =>
        LingoRuntime.GetCastMember(memberNameOrNum, castNameOrNum);

    public CastMember? member(string name) =>
        LingoRuntime.GetCastMember(name);

    public LingoColor color(LingoNumber r, LingoNumber g, LingoNumber b) => new(
        Math.Clamp((int)r, 0, 255),
        Math.Clamp((int)g, 0, 255),
        Math.Clamp((int)b, 0, 255));

    public LingoColor color(LingoNumber palIdx) => palIdx;

    public LingoImage image(LingoNumber w, LingoNumber h, LingoNumber bitDepth) =>
        new LingoImage((int)w, (int)h, (int)bitDepth);

    public LingoImage image(LingoNumber w, LingoNumber h, LingoSymbol type)
    {
        var typeEnum = Enum.Parse<ImageType>(type.Value, ignoreCase: true);

        return new LingoImage((int)w, (int)h, typeEnum);
    }

    public string @string(object value) => value.ToString() ?? "";
    public string @string(LingoNumber value) => value.ToString();

    public static LingoNumber op_add(LingoNumber a, LingoNumber b) => a + b;
    public static LingoNumber op_sub(LingoNumber a, LingoNumber b) => a - b;
    public static LingoNumber op_mul(LingoNumber a, LingoNumber b) => a * b;
    public static LingoNumber op_div(LingoNumber a, LingoNumber b) => a / b;
    public static LingoNumber op_mod(LingoNumber a, LingoNumber b) => a % b;

    public static LingoPoint op_add(LingoPoint a, LingoPoint b) => a + b;
    public static LingoPoint op_sub(LingoPoint a, LingoPoint b) => a - b;
    public static LingoPoint op_mul(LingoPoint a, LingoPoint b) => a * b;
    public static LingoPoint op_div(LingoPoint a, LingoPoint b) => a / b;

    public static LingoPoint op_add(LingoPoint a, LingoNumber b) => a + b;
    public static LingoPoint op_sub(LingoPoint a, LingoNumber b) => a - b;
    public static LingoPoint op_mul(LingoPoint a, LingoNumber b) => a * b;
    public static LingoPoint op_div(LingoPoint a, LingoNumber b) => a / b;

    public static LingoRect op_add(LingoRect a, LingoRect b) => a + b;
    public static LingoRect op_sub(LingoRect a, LingoRect b) => a - b;
    public static LingoRect op_mul(LingoRect a, LingoRect b) => a * b;
    public static LingoRect op_div(LingoRect a, LingoRect b) => a / b;

    public static LingoRect op_add(LingoRect a, LingoNumber b) => a + b;
    public static LingoRect op_sub(LingoRect a, LingoNumber b) => a - b;
    public static LingoRect op_mul(LingoRect a, LingoNumber b) => a * b;
    public static LingoRect op_div(LingoRect a, LingoNumber b) => a / b;

    public static dynamic op_add(object? a, object? b)
    {
        if (a is LingoNumber na && b is null)
            return na + 0;

        if (a is null && b is LingoNumber nb)
            return 0 + nb;

        if (a?.GetType() == b?.GetType() && a is LingoNumber or LingoPoint or LingoRect)
            // R# analysis is wrong, this is totally reachable.
            // ReSharper disable once HeuristicUnreachableCode
            return (dynamic?)a + (dynamic?)b;

        if (a is ILingoVector nva && b is ILingoVector nvb)
        {
            var minSize = Math.Min(nva.CountElems, nvb.CountElems);
            var res = new LingoList(minSize);

            for (var i = 0; i < minSize; i++)
            {
                var elemA = nva[i];
                var elemB = nvb[i];

                res.add(op_add(elemA, elemB));
            }

            return res;
        }

        return (dynamic?)a + (dynamic?)b;
    }

    public static dynamic op_sub(dynamic? a, dynamic? b)
    {
        if (a is LingoNumber na && b is null)
            return na - 0;

        if (a is null && b is LingoNumber nb)
            return 0 - nb;

        if (a?.GetType() == b?.GetType() && a is LingoNumber or LingoPoint or LingoRect)
            // R# analysis is wrong, this is totally reachable.
            // ReSharper disable once HeuristicUnreachableCode
            return a - b;

        if (a is ILingoVector nva && b is ILingoVector nvb)
        {
            var minSize = Math.Min(nva.CountElems, nvb.CountElems);
            var res = new LingoList(minSize);

            for (var i = 0; i < minSize; i++)
            {
                var elemA = nva[i];
                var elemB = nvb[i];

                res.add(op_sub(elemA, elemB));
            }

            return res;
        }


        return a - b;
    }

    public static dynamic op_mul(dynamic? a, dynamic? b)
    {
        if (a is LingoNumber na && b is null)
            return na * 0;

        if (a is null && b is LingoNumber nb)
            return 0 * nb;

        if (a?.GetType() == b?.GetType() && a is LingoNumber or LingoPoint or LingoRect)
            // R# analysis is wrong, this is totally reachable.
            // ReSharper disable once HeuristicUnreachableCode
            return a * b;

        if (a is ILingoVector nva && b is ILingoVector nvb)
        {
            var minSize = Math.Min(nva.CountElems, nvb.CountElems);
            var res = new LingoList(minSize);

            for (var i = 0; i < minSize; i++)
            {
                var elemA = nva[i];
                var elemB = nvb[i];

                res.add(op_mul(elemA, elemB));
            }

            return res;
        }


        return a * b;
    }

    public static dynamic op_div(object? a, object? b)
    {
        if (a is LingoNumber na && b is null)
            return na / 0;

        if (a is null && b is LingoNumber nb)
            return 0 / nb;

        if (a?.GetType() == b?.GetType() && a is LingoNumber or LingoPoint or LingoRect)
            // R# analysis is wrong, this is totally reachable.
            // ReSharper disable once HeuristicUnreachableCode
            return (dynamic?)a / (dynamic?)b;

        if (a is ILingoVector nva && b is ILingoVector nvb)
        {
            var minSize = Math.Min(nva.CountElems, nvb.CountElems);
            var res = new LingoList(minSize);

            for (var i = 0; i < minSize; i++)
            {
                var elemA = nva[i];
                var elemB = nvb[i];

                res.add(op_div(elemA, elemB));
            }

            return res;
        }

        return (dynamic?)a / (dynamic?)b;
    }

    public static dynamic op_mod(dynamic? a, dynamic? b)
    {
        if (a is LingoNumber na && b is null)
            return na % 0;

        if (a is null && b is LingoNumber nb)
            return 0 % nb;

        if (a?.GetType() == b?.GetType() && a is LingoNumber or LingoPoint or LingoRect)
            // R# analysis is wrong, this is totally reachable.
            // ReSharper disable once HeuristicUnreachableCode
            return a % b;

        if (a is ILingoVector nva && b is ILingoVector nvb)
        {
            var minSize = Math.Min(nva.CountElems, nvb.CountElems);
            var res = new LingoList(minSize);

            for (var i = 0; i < minSize; i++)
            {
                var elemA = nva[i];
                var elemB = nvb[i];

                res.add(op_mod(elemA, elemB));
            }

            return res;
        }

        return a % b;
    }

    public static bool op_eq_b(LingoNumber a, LingoNumber b)
    {
        return a == b;
    }

    public static bool op_eq_b(object? a, object? b)
    {
        if ((a is LingoNumber { DecimalValue: 0 } && b is null) || (a is null && b is LingoNumber { DecimalValue: 0 }))
            return true;

        if (a?.GetType() != b?.GetType())
        {
            // Log.Warning("Invalid type comparison: {A} == {B}", a, b);
            return false;
        }

        if (a is string strA && b is string strB)
            return string.Equals(strA, strB, StringComparison.InvariantCultureIgnoreCase);

        return (dynamic?)a == (dynamic?)b;
    }

    public static LingoNumber op_eq(object? a, object? b)
    {
        return op_eq_b(a, b) ? 1 : 0;
    }

    public static LingoNumber op_eq(LingoNumber a, LingoNumber b)
    {
        return op_eq_b(a, b) ? 1 : 0;
    }

    public static bool op_ne_b(object? a, object? b)
    {
        return !op_eq_b(a, b);
    }

    public static bool op_ne_b(LingoNumber a, LingoNumber b)
    {
        return !op_eq_b(a, b);
    }

    public static LingoNumber op_ne(object? a, object? b)
    {
        return op_eq_b(a, b) ? 0 : 1;
    }

    public static LingoNumber op_ne(LingoNumber a, LingoNumber b)
    {
        return op_eq_b(a, b) ? 0 : 1;
    }

    public static LingoNumber op_lt(dynamic? a, dynamic? b) => a < b ? 1 : 0;
    public static LingoNumber op_lt(LingoNumber a, LingoNumber b) => a < b ? 1 : 0;
    public static LingoNumber op_le(dynamic? a, dynamic? b) => a >= b ? 1 : 0;
    public static LingoNumber op_le(LingoNumber a, LingoNumber b) => a >= b ? 1 : 0;
    public static LingoNumber op_gt(dynamic? a, dynamic? b) => a > b ? 1 : 0;
    public static LingoNumber op_gt(LingoNumber a, LingoNumber b) => a > b ? 1 : 0;
    public static LingoNumber op_ge(dynamic? a, dynamic? b) => a <= b ? 1 : 0;
    public static LingoNumber op_ge(LingoNumber a, LingoNumber b) => a <= b ? 1 : 0;

    public static LingoNumber op_and(object? a, object? b)
    {
        // Lingo does not short circuit because of course not...
        var bA = ToBool(a);
        var bB = ToBool(b);

        return bA && bB ? 1 : 0;
    }

    public static LingoNumber op_or(object? a, object? b)
    {
        // Lingo does not short circuit because of course not...
        var bA = ToBool(a);
        var bB = ToBool(b);

        return bA || bB ? 1 : 0;
    }

    public static bool ToBool(object? a)
    {
        if (a is LingoNumber i)
            return i.IntValue != 0;

        return a != null;
    }

    public static bool ToBool(LingoNumber a)
    {
        return a != 0;
    }

    public string the_platform => "win";

    public LingoSprite sprite(object a)
    {
        // Log.Warning("sprite() not implemented");
        return new LingoSprite();
    }

    public LingoSprite sprite(LingoNumber x)
    {
        return new LingoSprite();
    }

    public dynamic sound(object a) => throw new NotImplementedException();
    public dynamic call(LingoSymbol a) => throw new NotImplementedException();
    public dynamic call(LingoSymbol a, dynamic a1) => throw new NotImplementedException();
    public dynamic call(LingoSymbol a, dynamic a1, dynamic a2) => throw new NotImplementedException();
    public dynamic call(LingoSymbol a, dynamic a1, dynamic a2, dynamic a3) => throw new NotImplementedException();

    public dynamic call(LingoSymbol a, dynamic a1, dynamic a2, dynamic a3, dynamic a4) =>
        throw new NotImplementedException();

    public static string chars(string str, LingoNumber first, LingoNumber last)
    {
        if (first == last)
            return str[first.IntValue - 1].ToString();

        return str[(first.IntValue - 1)..Math.Min(str.Length, last.IntValue)];
    }

    public void alert(string msg) => throw new NotImplementedException();

    public LingoSymbol ilk(object obj)
    {
        var val = obj switch
        {
            LingoNumber { IsDecimal: false } => "integer",
            LingoNumber { IsDecimal: true } => "float",
            LingoList => "list",
            LingoPropertyList => "proplist",
            string => "string",
            LingoRect => "rect",
            LingoPoint => "point",
            LingoColor => "color",
            LingoSymbol => "symbol",
            LingoImage => "image",
            // Lol apparently ilk() is called once and it's to check if something is a list
            // so I didn't even need to fill out this much.
            null => "void",
            _ => throw new NotSupportedException()
        };

        return new LingoSymbol(val);
    }

    public void createfile(dynamic d, string f) => d.createfile(f);

    public LingoScriptRuntimeBase MovieScriptInstance => LingoRuntime.MovieScriptInstance;
    public static string last_char(string str) => str[^1].ToString();
    public static LingoNumber stringp(object str) => str is string ? 1 : 0;
    public LingoCastLib castlib(dynamic nameOrNum) => throw new NotImplementedException();
}
