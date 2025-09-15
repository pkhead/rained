using System.Diagnostics;
using System.Text.Json;
using Rained.LevelData;

namespace Rained.Assets;

class EffectPrefab
{
    public string Version { get; set; } = "1.0.0";
    public EffectPrefabItem[] Items { get; set; } = [];

    private static JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void WriteToFile(string filePath)
    {
        var text = JsonSerializer.Serialize(this, jsonOptions);
        File.WriteAllText(filePath, text);
    }

    public static EffectPrefab? ReadFromFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<EffectPrefab>(stream, jsonOptions);
    }

    public class EffectPrefabItem
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = [];

        public EffectPrefabItem()
        { }

        public EffectPrefabItem(Effect src)
        {
            LoadFromEffect(src);
        }

        public void LoadFromEffect(Effect src)
        {
            Name = src.Data.name;
            Data = [];

            if (src.Data.use3D)
                Data["3D"] = src.Is3D;

            if (src.Data.useDecalAffect)
                Data["Affect Gradients And Decals"] = src.AffectGradientsAndDecals;

            if (src.Data.usePlantColors)
                Data["Color"] = src.PlantColor == 2 ? "X" : src.PlantColor;

            if (src.Data.optionalInBounds)
                Data["Require In-Bounds"] = src.RequireInBounds;

            if (src.Data.useLayers)
                Data["Layers"] = src.Layer switch
                {
                    Effect.LayerMode.All => "all",
                    Effect.LayerMode.First => "1",
                    Effect.LayerMode.Second => "2",
                    Effect.LayerMode.Third => "3",
                    Effect.LayerMode.FirstAndSecond => "1+2",
                    Effect.LayerMode.SecondAndThird => "2+3",
                    _ => throw new UnreachableException("invalid Effect.LayerMode")
                };

            for (int i = 0; i < src.Data.customConfigs.Count; ++i)
            {
                switch (src.Data.customConfigs[i])
                {
                    case CustomEffectString strConfig:
                        Data[strConfig.Name] = strConfig.Options[src.CustomValues[i]];
                        break;

                    case CustomEffectInteger intConfig:
                        Data[intConfig.Name] = src.CustomValues[i];
                        break;
                }
            }
        }

        public void Load(Effect effect)
        {
            if (Data.TryGetValue("3D", out var val3D) && val3D is not null)
                effect.Is3D = ((JsonElement)val3D).GetBoolean();

            if (Data.TryGetValue("Affect Gradients And Decals", out var valGad) && valGad is not null)
                effect.AffectGradientsAndDecals = ((JsonElement)valGad).GetBoolean();

            if (Data.TryGetValue("Color", out var valColObj) && valColObj is not null)
            {
                var valCol = (JsonElement)valColObj;
                if (valCol.ValueKind == JsonValueKind.String)
                {
                    var colStr = valCol.GetString()!;

                    if (colStr.Equals("x", StringComparison.InvariantCultureIgnoreCase) ||
                        colStr.Equals("dead", StringComparison.InvariantCultureIgnoreCase))
                    {
                        effect.PlantColor = 2;
                    }
                    else
                    {
                        Log.UserLogger.Warning("\"{Name}\": invalid Color value \"{Value}\"; ignoring.", Name, colStr);
                    }
                }
                else if (valCol.TryGetInt32(out var colInt))
                {
                    if (colInt >= 0 && colInt <= 2)
                    {
                        effect.PlantColor = colInt;
                    }
                    else
                    {
                        Log.UserLogger.Warning("\"{Name}\": invalid Color value {Value}; ignoring.", colInt);
                    }
                }
            }
        }
    }
}