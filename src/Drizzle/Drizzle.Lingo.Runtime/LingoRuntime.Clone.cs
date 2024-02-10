using System;
using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Drizzle.Lingo.Runtime;

public partial class LingoRuntime
{
    public LingoRuntime Clone()
    {
        Log.Debug("Cloning Lingo runtime...");
        var newRuntime = new LingoRuntime(_assembly);
        newRuntime.InitNoCast();
        CloneCast(this, newRuntime);
        CloneGlobals(this, newRuntime);

        return newRuntime;
    }

    private static void CloneCast(LingoRuntime src, LingoRuntime dst)
    {
        Log.Debug("Cloning cast...");
        dst.InitCastLibs();
        dst._castMemberNameIndexDirty = true;

        for (var i = 0; i < src._castLibs.Length; i++)
        {
            var srcLib = src._castLibs[i];
            var dstLib = dst._castLibs[i];

            for (var j = 1; j <= srcLib.NumMembers; j++)
            {
                var srcMem = srcLib.GetMember(j)!;
                var dstMem = dstLib.GetMember(j)!;

                dstMem.CloneFrom(srcMem);
            }
        }
    }

    private static void CloneGlobals(LingoRuntime src, LingoRuntime dst)
    {
        Log.Debug("Cloning globals...");

        var srcMovieScript = src.MovieScriptInstance;
        var dstMovieScript = dst.MovieScriptInstance;
        foreach (var field in srcMovieScript.GetType().GetFields())
        {
            if (!Attribute.IsDefined(field, typeof(LingoGlobalAttribute)))
                continue;

            var srcValue = field.GetValue(srcMovieScript);
            field.SetValue(dstMovieScript, DeepClone(srcValue));
        }

        [return: NotNullIfNotNull("value")]
        object? DeepClone(object? value)
        {
            if (value is LingoList list)
            {
                var newList = new LingoList(list.List.Count);
                foreach (var listValue in list.List)
                {
                    newList.List.Add(DeepClone(listValue));
                }
                return newList;
            }

            if (value is LingoPropertyList propList)
            {
                var newList = new LingoPropertyList(propList.Dict.Count);
                foreach (var (key, dictValue) in propList.Dict)
                {
                    newList.Dict.Add(DeepClone(key), DeepClone(dictValue));
                }
                return newList;
            }

            if (value is LingoScriptRuntimeBase script)
            {
                var scriptType = script.GetType();
                var newScript = dst.InstantiateScriptType(scriptType);
                newScript.Init(dstMovieScript, dst.Global);

                foreach (var field in scriptType.GetFields())
                {
                    if (!Attribute.IsDefined(field, typeof(LingoPropertyAttribute)))
                        continue;

                    var srcValue = field.GetValue(script);
                    field.SetValue(newScript, DeepClone(srcValue));
                }
            }

            return value;
        }
    }
}
