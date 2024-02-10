using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public class LingoPropertyList : DynamicObject, ILingoListDuplicate
{
    public Dictionary<object, dynamic?> Dict { get; }

    public LingoNumber length => Dict.Count;

    public LingoPropertyList(Dictionary<object, dynamic?> dict)
    {
        Dict = dict;
    }

    public LingoPropertyList()
    {
        Dict = new Dictionary<object, dynamic?>();
    }

    public LingoPropertyList(int capacity)
    {
        Dict = new Dictionary<object, dynamic?>(capacity);
    }

    public LingoPropertyList(IEnumerable<KeyValuePair<object, dynamic?>> source)
    {
        Dict = new Dictionary<object, dynamic?>(source);
    }

    public dynamic? this[object index]
    {
        get => Dict[index];
        set => Dict[index] = value;
    }

    public LingoPropertyList duplicate()
    {
        return new LingoPropertyList(
            Dict.Select(kv => new KeyValuePair<object, dynamic?>(kv.Key, LingoList.DuplicateIfList(kv.Value))));
    }

    ILingoListDuplicate ILingoListDuplicate.duplicate()
    {
        return duplicate();
    }

    public void addprop(object? key, object? value)
    {
        // Void is a valid dict key in Lingo, not in C#.
        // Yeah I don't think anything relies on the former property.
        if (Dict.ContainsKey(key!))
            Log.Warning("addprop duplicate key: {Key}", key);

        Dict[key!] = value;
    }

    private static readonly object FindPosTrueResult = new();

    public object? findpos(object key)
    {
        // findpos is only used as a "does it exist in the list" check so this is fine.
        return Dict.ContainsKey(key) ? FindPosTrueResult : null;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (Dict.TryGetValue(binder.Name, out result) ||
            Dict.TryGetValue(new LingoSymbol(binder.Name), out result)) return true;

        result = null;
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        Dict[new LingoSymbol(binder.Name)] = value;
        return true;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('[');

        var first = true;
        foreach (var (k, v) in Dict)
        {
            if (!first)
                sb.Append(", ");

            first = false;

            sb.Append(LingoFormat.LingoToString(k));
            sb.Append(": ");
            sb.Append(LingoFormat.LingoToString(v));
        }

        sb.Append(']');
        return sb.ToString();
    }
}
