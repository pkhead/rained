using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Drizzle.Lingo.Runtime.Cast;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public partial class LingoRuntime
{
    private static readonly bool LoadCastParallel = true;

    private static readonly Regex CastPathRegex = new Regex(@"([^_]+)_(\d+)_(.+)?\.([a-z]*)");

    // pkhead modification
    /*private*/ public static /*readonly*/ string CastPath;

    private readonly Dictionary<string, LingoCastLib>
        _castLibNames = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly LingoCastLib[] _castLibs = new LingoCastLib[8];

    // This is intentionally case sensitive, see GetCastMemberAnyCast.
    private readonly Dictionary<string, CastMember> _castMemberNameIndex = new();
    private bool _castMemberNameIndexDirty;

    public LingoCastLib GetCastLib(string castName)
    {
        return _castLibNames[castName];
    }

    public CastMember? GetCastMember(object nameOrNum, object? cast=null)
    {
        var found = cast switch
        {
            string castName => _castLibNames[castName].GetMember(nameOrNum),
            int castNumber => _castLibs[castNumber - 1].GetMember(nameOrNum),
            LingoNumber castNumber => _castLibs[castNumber.IntValue - 1].GetMember(nameOrNum),
            null => GetCastMemberAnyCast(nameOrNum),
            _ => throw new ArgumentException("Invalid cast name")
        };

        if (found == null)
            Log.Warning(
                "Failed to find member with name {MissingMemberName} cast {MissingMemberCast}",
                nameOrNum, cast);

        return found;
    }

    public CastMember? GetCastMember(string name)
    {
        var found = GetCastMemberAnyCast(name);

        if (found == null)
            Log.Warning("Failed to find member with name {MissingMemberName}", found);

        return found;
    }

    private CastMember? GetCastMemberAnyCast(object nameOrNum)
    {
        if (nameOrNum is string name)
        {
            if (_castMemberNameIndexDirty)
                UpdateMemberNameIndex();

            // NOTE: This is a case-sensitive lookup,
            // since those are significantly more efficient than case-insensitive ones.
            // If the lookup fails, we still go through the cast libs like normal since they do insensitive lookups.
            if (_castMemberNameIndex.TryGetValue(name, out var member))
                return member;
        }

        foreach (var castLib in _castLibs)
        {
            var mem = castLib.GetMember(nameOrNum);
            if (mem != null)
                return mem;
        }

        return null;
    }

    private void LoadCast()
    {
        Log.Debug("Loading cast...");

        InitCastLibs();

        var sw = Stopwatch.StartNew();
        var files = Directory.EnumerateFiles(CastPath);

        var count = 0;
        if (LoadCastParallel)
        {
            Parallel.ForEach(files, DoWork);
        }
        else
        {
            foreach (var s in files)
            {
                DoWork(s);
            }
        }

        void DoWork(string s)
        {
            var member = LoadSingleCastMember(s);
            if (member != null)
            {
                Interlocked.Increment(ref count);
                //Log.Debug("Loading cast member: {MemberName} {MemberNum} {MemberCast}",
                //    member!.name, member.Number, member.Cast);
            }
        }

        Log.Debug("Loaded {CastSize} cast members in {Time}", count, sw.Elapsed);

        // Set up pxl special case.
        GetCastMember("pxl")!.image!.IsPxl = true;
    }

    private void InitCastLibs()
    {
        var i = 0;
        // These offsets make no sense wtf director
        InitLib("Internal", 0);
        InitLib("customMems", 131072);
        InitLib("soundCast", 196608);
        InitLib("levelEditor", 262144);
        InitLib("exportBitmaps", 327680);
        InitLib("Drought", 393216);
        InitLib("Dry Editor", 458752);
        InitLib("MSC", 524288);

        void InitLib(string name, int offset)
        {
            var castLib = new LingoCastLib(this, name, offset);
            _castLibNames.Add(name, castLib);
            _castLibs[i] = castLib;
            i += 1;
        }
    }

    private CastMember? LoadSingleCastMember(string file)
    {
        var fileName = Path.GetFileName(file);
        var match = CastPathRegex.Match(fileName);
        if (!match.Success)
        {
            Log.Warning("Warning: Unable to parse {CastFileName} for cast file name", fileName);
            return null;
        }

        var cast = match.Groups[1].Value;
        var number = int.Parse(match.Groups[2].Value);
        var ext = match.Groups[4].Value;
        string? name = null;

        if (match.Groups[3].Success)
            name = match.Groups[3].Value;

        var member = GetCastMember(number, cast)!;
        member.ImportFile(file, ext, name);

        if (member.Type == CastMemberType.Empty)
            Log.Warning("Warning: unrecognized cast member type {CastFileName}", file);

        return member;
    }

    private void UpdateMemberNameIndex()
    {
        _castMemberNameIndex.Clear();

        foreach (var castLib in _castLibs)
        {
            for (var i = 0; i < castLib.NumMembers; i++)
            {
                var member = castLib.GetMember(i);
                if (member?.name == null)
                    continue;

                if (!_castMemberNameIndex.ContainsKey(member.name))
                    _castMemberNameIndex.Add(member.name, member);
            }
        }

        _castMemberNameIndexDirty = false;
    }

    public void NameIndexDirty()
    {
        _castMemberNameIndexDirty = true;
    }
}
