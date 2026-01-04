using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Ported;
namespace DrizzleExport;

public static class DrizzleEffectExport
{
    [method: SetsRequiredMembers]
    public class Effect(string name, string type)
    {
        public required string Name { get; set; } = name;
        public required string Type { get; set; } = type;
        public bool CrossScreen { get; set; } = false;
        public bool Binary { get; set; } = false;
        public bool Single { get; set; } = false;

        // only used by standard erosion type
        public int? Repeats { get; set; } = null;
        public double? AffectOpenAreas { get; set; } = null;

        public double FillWith { get; set; } = 0f;

        public List<object[]> Properties { get; set; } = [];
    };

    public class EffectGroup(string name)
    {
        public string Name { get; set; } = name;
        public Effect[] Effects { get; set; } = [];
    }

    public static void Export(string dataPath, string castPath, string outPath)
    {
        SixLabors.ImageSharp.Configuration.Default.PreferContiguousImageBuffers = true;
        LingoRuntime.MovieBasePath = dataPath + Path.DirectorySeparatorChar;
        LingoRuntime.CastPath = castPath + Path.DirectorySeparatorChar;

        var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
        runtime.Init();
        EditorRuntimeHelpers.RunStartup(runtime);

        List<EffectGroup> groups = [];

        var movieScript = runtime.MovieScript();
        var effectsEditor = runtime.CreateScript<effectsEditor>();
        var groupIndex = 1;
        var gEEprops = (LingoPropertyList) movieScript.gEEprops;

        var effectsList = (LingoList) gEEprops["effects"]!;
        List<Effect> groupEffectList = [];

        foreach (var group in movieScript.gEffects.Cast<LingoPropertyList>())
        {
            var jsonGroup = new EffectGroup(group["nm"]);
            gEEprops["emPos"]!.loch = groupIndex;
            groupEffectList.Clear();

            var effectIndex = 1;
            foreach (var effect in ((LingoList)group["efs"]!).Cast<LingoPropertyList>())
            {
                gEEprops["emPos"]!.locv = effectIndex;

                effectsEditor.neweffect();
                var instance = (LingoPropertyList) effectsList.List[^1]!;

                var jsonEffect = new Effect(instance["nm"], instance["tp"]);
                if (instance["tp"] == "standardErosion")
                {
                    jsonEffect.Repeats = instance["repeats"]!.IntValue;
                    jsonEffect.AffectOpenAreas = instance["affectOpenAreas"]!.DecimalValue;
                }
                else if (instance["tp"] != "nn")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("error: ");
                    Console.ResetColor();
                    Console.WriteLine("invalid effect type " + instance["tp"]?.ToString() ?? "null");
                    jsonEffect = null;
                }

                if (jsonEffect is not null)
                {
                    jsonEffect.CrossScreen = instance["crossScreen"] is null ? false : instance["crossScreen"]!.IntValue == 1;

                    // read effect options
                    foreach (var optObj in instance["options"]!)
                    {
                        var opt = (LingoList)optObj;
                        var optionName = (string)opt[1]!;
                        if (optionName == "Delete/Move" || optionName == "Seed") continue;

                        var propertyData = new object[3];
                        propertyData[0] = optionName;
                        propertyData[1] = ((LingoList)opt[2]!).List.Cast<string>().ToArray();

                        if (opt[3] is LingoNumber)
                            propertyData[2] = opt[3]!.DecimalValue;
                        else
                            propertyData[2] = (string)opt[3]!;

                        jsonEffect.Properties.Add(propertyData);
                    }

                    // get fillwith by reading matrix
                    jsonEffect.FillWith = instance["mtrx"]![1][1].DecimalValue;

                    // get binary and single by simulating mouse stroke
                    instance["mtrx"]![1][1] = new LingoNumber(0);
                    gEEprops["mode"] = "editEffect";
                    gEEprops["editEffect"] = effectsList.List.Count;
                    gEEprops["brushSize"] = new LingoNumber(10);

                    effectsEditor.usebrush(new LingoPoint(2, 2), new LingoNumber(1));

                    jsonEffect.Binary = instance["mtrx"]![2][2].DecimalValue >= 100;
                    jsonEffect.Single = gEEprops["brushSize"]!.DecimalValue == 1;

                    groupEffectList.Add(jsonEffect);
                }

                effectIndex++;
            }

            jsonGroup.Effects = [..groupEffectList];
            groups.Add(jsonGroup);

            groupIndex++;
        }

        File.WriteAllText(outPath, JsonSerializer.Serialize(groups, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }
}