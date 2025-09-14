using System.Diagnostics;
using System.Text.Json;
using Rained.LevelData;

namespace Rained.Assets;

class EffectPrefab
{
    public string Version { get; set; } = "1.0.0";
    public EffectPrefabItem[] Items { get; set; } = [];

    public class EffectPrefabItem
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = [];

        public EffectPrefabItem()
        { }

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

        public void Load(Level level, EffectsDatabase fxDb)
        {
            var init = fxDb.GetEffectFromName(Name);
            var effect = new Effect(level, init);

            if (Data.TryGetValue("3D", out var val3D) && val3D is not null)
                effect.Is3D = (bool)val3D;

            if (Data.TryGetValue("Affect Gradients And Decals", out var valGad) && valGad is not null)
                effect.AffectGradientsAndDecals = (bool)valGad;

            if (Data.TryGetValue("Color", out var valCol) && valCol is not null)
            {
                if (valCol is string colStr)
                {
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
                else if (valCol is int colInt)
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


class EffectPrefabDatabase
{
    private static readonly string prefabFolder = Path.Combine(Boot.AppDataPath, "config", "prefabs", "effects");
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DatabaseBranch Root { get; private set; }    

    public EffectPrefabDatabase()
    {
        Root = new DatabaseBranch("root");
        Refresh();
    }

    public void Refresh()
    {
        Root = Directory.Exists(prefabFolder) ?
            ScanFolder(prefabFolder, "root") :
            new DatabaseBranch("root");
    }

    private DatabaseBranch ScanFolder(string folderPath, string branchName)
    {
        var branch = new DatabaseBranch(branchName);
        var nodes = branch.Nodes;

        foreach (var dir in Directory.EnumerateDirectories(folderPath))
        {
            branch.Nodes.Add(ScanFolder(dir, Path.GetFileName(dir)));
        }

        foreach (var pfbPath in Directory.EnumerateFiles(folderPath, "*.json"))
        {
            try
            {
                var leaf = new DatabaseLeaf(Path.GetFileNameWithoutExtension(pfbPath), pfbPath);
                nodes.Add(leaf);
            }
            catch (Exception e)
            {
                Log.UserLogger.Error("Could not load \"{FilePath}\": {Message}", pfbPath, e.Message);
                Log.Error("{Exception}", e);
            }
        }

        return branch;
    }

    // since groups can be within groups, a recursive tree structure is
    // necessary to represent the data
    public abstract class DatabaseNode(string name)
    {
        public readonly string Name = name;
    }

    public class DatabaseBranch(string name) : DatabaseNode(name)
    {
        public readonly List<DatabaseNode> Nodes = [];
    }

    public class DatabaseLeaf(string name, string filePath) : DatabaseNode(name)
    {
        public readonly string FilePath = filePath;

        private EffectPrefab? _cache;
        private DateTime _cacheTime;

        public EffectPrefab? Load()
        {
            bool needsReload;
            if (_cache is not null)
            {
                needsReload = File.GetLastWriteTime(FilePath) > _cacheTime;
            }
            else
            {
                needsReload = true;
            }

            if (needsReload)
            {
                using var stream = File.OpenRead(FilePath);
                _cacheTime = File.GetLastWriteTime(FilePath);
                return _cache = JsonSerializer.Deserialize<EffectPrefab>(stream, jsonOptions);
            }
            else
            {
                return _cache;
            }
        }

        public void Save(EffectPrefab prefab)
        {
            var text = JsonSerializer.Serialize(prefab, jsonOptions);
            File.WriteAllText(FilePath, text);

            _cache = prefab;
            _cacheTime = File.GetLastWriteTime(FilePath);
        }
    }
}