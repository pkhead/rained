using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Rained.LevelData;
namespace Rained.Assets;

enum EffectType
{
    NN, // i have no idea what this abbreviates, lol
    StandardErosion
}

record CustomEffectConfig
{
    public readonly string Name;
    public CustomEffectConfig(string name)
    {
        Name = name;
    }
}

record CustomEffectString : CustomEffectConfig
{
    public readonly string Default;
    public readonly string[] Options;

    public readonly bool IsColorOption;

    public CustomEffectString(string name, string defaultOption, string[] options) : base(name)
    {
        Default = defaultOption;
        Options = options;

        IsColorOption = Options.Length == 3 && Options[0] == "Color1" && Options[1] == "Color2" && Options[2] == "Dead";
    }
}

// leo... why...
record CustomEffectInteger : CustomEffectConfig
{
    public readonly int MinInclusive;
    public readonly int MaxInclusive;

    public CustomEffectInteger(string name, int min, int max) : base(name)
    {
        MinInclusive = min;
        MaxInclusive = max;
    }
}

class EffectInit
{
    public string name;
    public EffectType type;
    public bool crossScreen = false;
    public bool binary = false; // if brush should only place 0s or 100s
    public bool single = false; // if brush size is fixed to 1 pixel

    // these properties are valid only for StandardErosion effect types
    public int repeats;
    public float affectOpenAreas;

    public float fillWith = 0f; // only effect to change this is BlackGoo, which sets fx matrix values to 100 (max) on creation
    public bool useLayers = false;
    public bool use3D = false;
    public bool usePlantColors = false;
    public bool useDecalAffect = false;
    public bool decalAffectDefault = false;
    public bool optionalInBounds = false;
    public bool deprecated = false;

    private static readonly Effect.LayerMode[] defaultAvailableLayers = [
        Effect.LayerMode.All, Effect.LayerMode.First, Effect.LayerMode.Second, Effect.LayerMode.Third, Effect.LayerMode.FirstAndSecond, Effect.LayerMode.SecondAndThird
    ];

    /// <summary>
    /// The default layer configuration for a newly created effect.
    /// Applicable only if useLayers is true.
    /// </summary>
    public Effect.LayerMode defaultLayer = Effect.LayerMode.All;
    public Effect.LayerMode[] availableLayers = defaultAvailableLayers;
    public int defaultPlantColor = 1; // Color2

    public readonly List<CustomEffectConfig> customConfigs;

    public EffectInit(string name, EffectType type)
    {
        this.name = name;
        this.type = type;
        customConfigs = [];
    }

    public int GetCustomConfigIndex(string name)
    {
        for (int i = 0; i < customConfigs.Count; i++)
        {
            if (customConfigs[i].Name == name)
                return i;
        }

        return -1;
    }
}

struct EffectGroup
{
    public string name;
    public List<EffectInit> effects;
}

/**
* hardcoded effects database >:)
* hardcoded cus, that's the way it is in the rain world level editor,
* and to create new effects you'd have to modify the renderer code anyway.
*/
class EffectsDatabase
{
    private readonly List<EffectGroup> groups;
    public List<EffectGroup> Groups { get => groups; }
    public bool HasErrors { get; private set; } = false;

    static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EffectsDatabase()
    {
        groups = [];

        using var stream = typeof(RainEd).Assembly.GetManifestResourceStream("Rained.embed.effects")
            ?? throw new Exception("Could not create internal effects JSON resource stream");
        
        var jsonGroups = JsonSerializer.Deserialize<DrizzleExport.DrizzleEffectExport.EffectGroup[]>(stream, jsonOptions)
            ?? throw new Exception("Internal effects JSON is invalid");
        
        string? lastGroupBaseName = null;
        int lastGroupNumber = 1;

        foreach (var group in jsonGroups)
        {
            var groupName = group.Name.Trim();

            // find start index of suffix digit
            var groupNameDigitIdx = -1;
            if (groupName.Length > 2)
            {
                for (int i = groupName.Length - 2; i >= 0; i--)
                {
                    if (char.IsDigit(groupName[i+1]) && !char.IsDigit(groupName[i]))
                    {
                        groupNameDigitIdx = i+1;
                        break;
                    }
                }

            }

            // if suffix digit exists, only make a new group if base name changed
            if (groupNameDigitIdx != -1)
            {
                var groupNumber = int.Parse(groupName[groupNameDigitIdx..]);
                var baseName = groupName[..groupNameDigitIdx].TrimEnd();

                if (lastGroupBaseName is null || baseName != lastGroupBaseName || groupNumber != lastGroupNumber+1)
                {
                    BeginGroup(groupName);
                }

                lastGroupNumber = groupNumber;
                lastGroupBaseName = baseName;
            }
            else
            {
                BeginGroup(groupName);
                lastGroupBaseName = groupName;
                lastGroupNumber = 1;
            }

            foreach (var effect in group.Effects)
            {
                // read effect properties
                var effectInit = new EffectInit(effect.Name, effect.Type switch
                {
                    "nn" => EffectType.NN,
                    "standardErosion" => EffectType.StandardErosion,
                    _ => throw new Exception("Internal effects JSON is invalid.")
                })
                {
                    single = effect.Single,
                    binary = effect.Binary,
                    crossScreen = effect.CrossScreen,
                    fillWith = (float)effect.FillWith,

                    use3D = false,
                    useDecalAffect = false,
                    useLayers = false,
                    usePlantColors = false
                };

                // read standard erosion properties
                if (effectInit.type == EffectType.StandardErosion)
                {
                    effectInit.affectOpenAreas = (float)effect.AffectOpenAreas!.Value;
                    effectInit.repeats = effect.Repeats!.Value;
                }

                CreateEffect(effectInit);

                // read effect options
                foreach (var prop in effect.Properties)
                {
                    var propName = ((JsonElement)prop[0]).GetString()!;
                    var optionList = ((JsonElement)prop[1]).EnumerateArray().Select(x => x.GetString()!).ToArray();
                    var defaultValue = (JsonElement)prop[2];
                    var defaultValueStr = defaultValue.ValueKind == JsonValueKind.String ? defaultValue.GetString() : null;

                    // hardcoded: layers
                    if (propName == "Layers" && optionList.Length > 0)
                    {
                        effectInit.useLayers = true;

                        // easy check for if the options array is ["All", "1", "2", "3", "1:st and 2:nd", "2:nd and 3:rd"]
                        // if it is less, then it is omitting some options.
                        Debug.Assert(optionList.Length <= 6);
                        if (optionList.Length != 6)
                        {
                            effectInit.availableLayers = new Effect.LayerMode[optionList.Length];
                            for (int i = 0; i < optionList.Length; i++)
                            {
                                effectInit.availableLayers[i] = optionList[i] switch
                                {
                                    "All" => Effect.LayerMode.All,
                                    "1" => Effect.LayerMode.First,
                                    "2" => Effect.LayerMode.Second,
                                    "3" => Effect.LayerMode.Third,
                                    "1:st and 2:nd" => Effect.LayerMode.FirstAndSecond,
                                    "2:nd and 3:rd" => Effect.LayerMode.SecondAndThird,
                                    _ => throw new Exception("Internal effects JSON is invalid")
                                };
                            }
                        }

                        effectInit.defaultLayer = effectInit.availableLayers[Array.IndexOf(optionList, defaultValueStr!)];
                    }

                    // hardcoded: 3d
                    else if (propName == "3D" && optionList.Length == 2 && optionList[0] == "Off" && optionList[1] == "On" && defaultValueStr! == "Off")
                    {
                        effectInit.use3D = true;
                    }

                    // hardcoded: Affect Gradients and Decals
                    else if (propName == "Affect Gradients and Decals" && optionList.Length == 2 && optionList[0] == "Yes" && optionList[1] == "No")
                    {
                        effectInit.useDecalAffect = true;
                        effectInit.decalAffectDefault = defaultValueStr! == "Yes";
                    }
                    
                    // hardcoded: plant colors
                    else if (propName == "Color" && optionList.Length == 3 && optionList[0] == "Color1" && optionList[1] == "Color2" && optionList[2] == "Dead")
                    {
                        effectInit.usePlantColors = true;
                        effectInit.defaultPlantColor = Array.IndexOf(optionList, defaultValueStr!);
                    }

                    // hardcoded: Require In-Bounds
                    else if (propName == "Require In-Bounds" && optionList.Length == 2)
                    {
                        effectInit.optionalInBounds = true;
                    }

                    // custom option
                    else
                    {
                        if (defaultValueStr is not null)
                        {
                            CustomConfig(propName, optionList, defaultValueStr);
                        }
                        else
                        {
                            // non-string values are hardcoded...
                            if (propName == "Leaf Density" && effect.Name == "Ivy")
                            {
                                CustomConfig(propName, 1, 100);
                            }
                            else
                            {
                                throw new NotImplementedException($"Unimplemented effect option: {effect.Name}/{propName}");
                            }
                        }
                    }
                }
            }
        }

        // deprecated effects
        {
            BeginGroup("_deprecated_");

            CreateEffect(new EffectInit("Slag", EffectType.NN)
            {
                deprecated = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Corruption No Eye", EffectType.NN)
            {
                deprecated = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Restore As Pipes", EffectType.NN)
            {
                deprecated = true,
                binary = true,
                single = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Scales", EffectType.StandardErosion) // DEPRECATED
            {
                deprecated = true,
                repeats = 200,
                affectOpenAreas = 0.05f,
                useLayers = true
            });

            // CreateEffect(new EffectInit("Sand", EffectType.StandardErosion) // DEPRECATED
            // {
            //     deprecated = true,
            //     repeats = 80,
            //     affectOpenAreas = 0.5f,
            //     useLayers = true,
            // });
            // CustomConfig("Effect Color", "None", ["EffectColor1", "EffectColor2", "None"]);
        }

        RegisterCustomEffects();
    }

    private void RegisterCustomEffects()
    {
        var lingoParser = new Lingo.LingoParser();
        var initFile = Path.Combine(AssetDataPath.GetPath(), "Effects", "Init.txt");

        if (!File.Exists(initFile))
        {
            Log.UserLogger.Information("Effects/Init.txt not found.");
            return;
        }

        Log.UserLogger.Information("Reading Effects/Init.txt...");

        bool groupCheck = false;
        var lineNo = 0;
        
        foreach (var line in File.ReadLines(initFile))
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line[0] == '-')
            {
                BeginGroup(line[1..]);
                groupCheck = true;
            }
            else
            {
                if (!groupCheck) throw new Exception(ErrorString(lineNo, "The first category header is missing"));

                // check for parse exception
                var parsedLine = lingoParser.Read(line, out Lingo.ParseException? parseErr);
                if (parseErr is not null)
                {
                    HasErrors = true;
                    LogError(lineNo, parseErr.Message + " (line ignored)");
                    continue;
                }

                // check for... a different parse exception? i know this emulates lingo behavior but
                // why didn't i just make it throw a ParseExcep- whatever
                if (parsedLine is null)
                {
                    HasErrors = true;
                    LogError(lineNo, "Malformed effect init (line ignored)");
                    continue;
                }

                var effectInit = (Lingo.PropertyList) parsedLine;
                object? temp;

                // read name field
                if (!effectInit.TryGetValue("nm", out temp))
                {
                    HasErrors = true;
                    LogError(lineNo, "Effect init does not have required field 'nm'.");
                    continue;
                }
                string name = (string) temp;

                // read type field
                if (!effectInit.TryGetValue("tp", out temp))
                {
                    HasErrors = true;
                    LogError(lineNo, "Effect init does not have required field 'tp'.");
                    continue;
                }
                string type = (string) temp;

                // emit a warning if effect type is not a valid value. (warning instead of error for future-proofing)
                switch (type)
                {
                    case "standardPlant":
                    case "grower":
                    case "hanger":
                    case "clinger":
                    case "standardClinger":
                    case "individual":
                    case "individualHanger":
                    case "individualClinger":
                    case "wall":
                        break;
                    
                    default:
                        Log.UserLogger.Warning(ErrorString(lineNo, "Effect init does not have a valid 'tp' field."));
                        break;
                }

                var hasColor = effectInit.TryGetValue("pickColor", out temp) ? Lingo.LingoNumber.AsInt(temp) : 0;
                var individual = type == "individual";
                var wall = type == "wall";
                var has3D = effectInit.TryGetValue("can3D", out temp) ? Lingo.LingoNumber.AsInt(temp) : 0;
                var isPlantType = type == "grower" || type == "hanger" || type == "clinger";

                CreateEffect(new EffectInit(name, EffectType.NN) {
                    usePlantColors = hasColor == 1,
                    useLayers = true,
                    binary = individual,
                    single = individual,
                    defaultLayer = individual ? Effect.LayerMode.First : Effect.LayerMode.All,
                    crossScreen = isPlantType,
                    optionalInBounds = isPlantType,
                    use3D = wall && has3D == 2
                });

                if (type == "clinger" || type == "standardClinger")
                    CustomConfig("Side", ["Left", "Right", "Random"], "Random");
            }
        }
    }

    private static string ErrorString(int lineNo, string msg)
        => "Line " + lineNo + ": " + msg;
    
    private void LogError(int lineNo, string template, params string[] values)
    {
        HasErrors = true;
        Log.UserLogger.Error(ErrorString(lineNo, template), template, values);
    }

    public EffectInit GetEffectFromName(string name)
    {
        foreach (var group in groups)
        {
            foreach (var effect in group.effects)
            {
                if (effect.name == name)
                    return effect;
            }
        }

        throw new Exception($"Effect '{name}' not found");
    }

    public bool TryGetEffectFromName(string name, [NotNullWhen(true)] out EffectInit? effect)
    {
        foreach (var group in groups)
        {
            foreach (var e in group.effects)
            {
                if (e.name == name)
                {
                    effect = e;
                    return true;
                }
            }
        }

        effect = null;
        return false;
    }

#region Helpers
    EffectGroup activeGroup;
    EffectInit activeEffect = null!;

    private void BeginGroup(string name)
    {
        // try to find group if it already exists
        foreach (var g in groups)
        {
            if (g.name == name)
            {
                activeGroup = g;
                return;
            }
        }

        // if not, create the new group.
        activeGroup = new EffectGroup()
        {
            name = name,
            effects = new List<EffectInit>()
        };

        groups.Add(activeGroup);
    }

    private void CreateEffect(EffectInit effect)
    {
        activeEffect = effect;
        activeGroup.effects.Add(effect);
    }

    // why the hell did i organize it this way it's stupid
    private void CustomConfig(string name, string defaultOption, string[] options)
    {
        activeEffect.customConfigs.Add(new CustomEffectString(name, defaultOption, options));
    }

    // this is better because you can directly copy+paste the values from startUp.lingo without reordering
    private void CustomConfig(string name, string[] options, string defaultOption) =>
        CustomConfig(name, defaultOption, options);

    private void CustomConfig(string name, int min, int max)
    {
        activeEffect.customConfigs.Add(new CustomEffectInteger(name, min, max));
    }
#endregion
}