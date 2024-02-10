using System;
using System.Collections.Generic;
using Drizzle.Lingo.Runtime.Cast;

namespace Drizzle.Lingo.Runtime;

public sealed class LingoCastLib
{
    private readonly CastMember[] _cast;
    private readonly Dictionary<string, int> _names = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly LingoRuntime _runtime;
    public int Offset;

    public string name { get; }
    private bool _nameIndexDirty;

    public dynamic member => throw new NotImplementedException();

    public int NumMembers => _cast.Length;

    public LingoCastLib(LingoRuntime runtime, string name, int offset)
    {
        this.name = name;
        _runtime = runtime;
        Offset = offset;

        _cast = new CastMember[1000];
        for (var i = 0; i < _cast.Length; i++)
        {
            _cast[i] = new CastMember(runtime, this, i + 1 + Offset, name);
        }
    }

    public CastMember? GetMember(object nameOrNum)
    {
        if (nameOrNum is string name)
        {
            if (_nameIndexDirty)
                UpdateNameIndex();

            if (_names.TryGetValue(name, out var castMember))
            {
                return _cast[castMember];
            }
        }

        if (nameOrNum is int num)
        {
            var numMember = GetMember(num);
            if (numMember != null)
                return numMember;
        }

        if (nameOrNum is LingoNumber lingoNum)
        {
            var numMember = GetMember(lingoNum.IntValue);
            if (numMember != null)
                return numMember;
        }

        return null;
    }

    public CastMember? GetMember(int num)
    {
        if (num > 0 && num <= _cast.Length)
            return _cast[num - 1];

        var idx = num - Offset;
        if (idx > 0 && idx < _cast.Length)
            return _cast[idx - 1];

        return null;
    }

    public void NameIndexDirty()
    {
        _nameIndexDirty = true;
        _runtime.NameIndexDirty();
    }

    private void UpdateNameIndex()
    {
        _names.Clear();

        for (var i = 0; i < _cast.Length; i++)
        {
            var castMember = _cast[i];

            if (castMember.name != null)
                _names.TryAdd(castMember.name, i);
        }

        _nameIndexDirty = false;
    }
}
