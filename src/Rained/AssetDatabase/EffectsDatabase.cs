namespace RainEd;

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

    public CustomEffectString(string name, string defaultOption, string[] options) : base(name)
    {
        Default = defaultOption;
        Options = options;
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
    public bool deprecated = false;

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

    public EffectsDatabase()
    {
        groups = new List<EffectGroup>();

        /////////////
        // Natural //
        /////////////
        BeginGroup("Natural");
        {
            CreateEffect(new EffectInit("Slime", EffectType.StandardErosion)
            {
                repeats = 130,
                affectOpenAreas = 0.5f,
                use3D = true,
                useLayers = true,

                useDecalAffect = true,
                decalAffectDefault = true
            });
            
            CreateEffect(new EffectInit("Melt", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.5f,
                useLayers = true,
                
                useDecalAffect = true,
                decalAffectDefault = false
            });

            CreateEffect(new EffectInit("Rust", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.2f,
                use3D = true,
                useLayers = true,
                
                useDecalAffect = true,
                decalAffectDefault = false
            });

            CreateEffect(new EffectInit("Barnacles", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.3f,
                use3D = true,
                useLayers = true,
                
                useDecalAffect = true,
                decalAffectDefault = false
            });

            CreateEffect(new EffectInit("Rubble", EffectType.NN)
            {
                useLayers = true
            });

            CreateEffect(new EffectInit("DecalsOnlySlime", EffectType.StandardErosion)
            {
                repeats = 130,
                affectOpenAreas = 0.5f,
                use3D = true,
                useLayers = true
            });
        }

        /////////////
        // Erosion //
        /////////////
        BeginGroup("Erosion");
        {
            //////////////
            // Erosion1 //
            //////////////
            CreateEffect(new EffectInit("Roughen", EffectType.StandardErosion)
            {
                repeats = 30,
                affectOpenAreas = 0.05f,
                useLayers = true,
            });

            CreateEffect(new EffectInit("SlimeX3", EffectType.StandardErosion)
            {
                repeats = 130 * 3,
                affectOpenAreas = 0.5f,
                use3D = true,
                useLayers = true,

                useDecalAffect = true,
                decalAffectDefault = true
            });

            CreateEffect(new EffectInit("Super Melt", EffectType.StandardErosion)
            {
                repeats = 50,
                affectOpenAreas = 0.5f,
                use3D = true,
                useLayers = true,
                
                useDecalAffect = true,
                decalAffectDefault = false
            });

            CreateEffect(new EffectInit("Destructive Melt", EffectType.StandardErosion)
            {
                repeats = 50,
                affectOpenAreas = 0.5f,
                use3D = true,
                useLayers = true,
                
                useDecalAffect = true,
                decalAffectDefault = false
            });

            CreateEffect(new EffectInit("Erode", EffectType.StandardErosion)
            {
                repeats = 80,
                affectOpenAreas = 0.5f,
                useLayers = true
            });

            CreateEffect(new EffectInit("Super Erode", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.5f,
                useLayers = true,
            });

            CreateEffect(new EffectInit("DaddyCorruption", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Slag", EffectType.NN) // DEPRECATED
            {
                deprecated = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Corruption No Eye", EffectType.NN) // DEPRECATED
            {
                deprecated = true,
                useLayers = true
            });
        }

        ///////////////
        // Artifical //
        ///////////////
        BeginGroup("Artificial");
        {
            CreateEffect(new EffectInit("Wires", EffectType.NN)
            {
                useLayers = true
            });
            CustomConfig("Fatness", "2px", [ "1px", "2px", "3px", "random" ]);

            CreateEffect(new EffectInit("Chains", EffectType.NN)
            {
                useLayers = true,
                crossScreen = true
            });
            CustomConfig("Size", "Small", [ "Small", "FAT" ]);
        }

        ////////////
        // Plants //
        ////////////
        BeginGroup("Plants");
        {
            /////////////
            // Plants1 //
            /////////////
            CreateEffect(new EffectInit("Root Grass", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Seed Pods", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Growers", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Cacti", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Rain Moss", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Hang Roots", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
            
            });

            CreateEffect(new EffectInit("Grass", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            /////////////
            // Plants2 //
            /////////////
            CreateEffect(new EffectInit("Arm Growers", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Horse Tails", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Circuit Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Feather Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Thorn Growers", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Rollers", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Garbage Spirals", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true
            });

            /////////////
            // Plants3 //
            /////////////
            
            CreateEffect(new EffectInit("Thick Roots", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
            });

            CreateEffect(new EffectInit("Shadow Plants", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
            });
        }

        /////////////////////////
        // Plants (Individual) //
        /////////////////////////
        BeginGroup("Plants (Individual)");
        {
            CreateEffect(new EffectInit("Fungi Flowers", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Lighthouse Flowers", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Fern", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Giant Mushroom", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Sprawlbush", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("featherFern", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Fungus Tree", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true
            });
        }

        ///////////////////
        // Paint Effects //
        //////////////////
        BeginGroup("Paint Effects");
        {
            CreateEffect(new EffectInit("BlackGoo", EffectType.NN)
            {
                binary = true,
                fillWith = 100f
            });

            CreateEffect(new EffectInit("DarkSlime", EffectType.NN)
            {
                useLayers = true
            });
        }

        /////////////////
        // Restoration //
        /////////////////
        BeginGroup("Restoration");
        {
            CreateEffect(new EffectInit("Restore As Scaffolding", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Ceramic Chaos", EffectType.NN)
            {
                useLayers = true,
            });
            CustomConfig("Ceramic Color", "Colored", ["None", "Colored"]);
            //CustomConfig("Colored", "White", [ "None", "White" ]);

            CreateEffect(new EffectInit("Restore As Pipes", EffectType.NN) // DEPRECATED
            {
                deprecated = true,
                binary = true,
                single = true,
                useLayers = true
            });
        }

        ////////////////////
        // Drought Plants //
        ////////////////////
        BeginGroup("Drought Plants");
        {
            CreateEffect(new EffectInit("Colored Hang Roots", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Colored Thick Roots", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Colored Shadow Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Colored Lighthouse Flowers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Colored Fungi Flowers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Root Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Foliage", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Mistletoe", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("High Fern", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("High Grass", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Little Flowers", EffectType.NN)
            {
                usePlantColors = true,
                useLayers = true
            });
            CustomConfig("Detail Color", "Color2", [ "Color1", "Color2", "Dead" ]);
            CustomConfig("Rotate", "Off", ["Off", "On"]);

            CreateEffect(new EffectInit("Wastewater Mold", EffectType.NN)
            {
                usePlantColors = true,
                useLayers = true
            });

            CreateEffect(new EffectInit("Spinets", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Small Springs", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Mini Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Clovers", EffectType.StandardErosion)
            {
                repeats = 20,
                affectOpenAreas = 0.2f,
                useLayers = true,
                use3D = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Reeds", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Lavenders", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Dense Mold", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });
        }

        BeginGroup("Drought Erosion");
        {
            CreateEffect(new EffectInit("Ultra Super Erode", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.5f,
                useLayers = true
            });

            CreateEffect(new EffectInit("Impacts", EffectType.StandardErosion)
            {
                repeats = 75,
                affectOpenAreas = 0.05f,
                useLayers = true
            });
        }

        BeginGroup("Drought Paint Effects");
        {
            CreateEffect(new EffectInit("Super BlackGoo", EffectType.NN)
            {
                fillWith = 100,
                binary = true
            });

            CreateEffect(new EffectInit("Scales", EffectType.StandardErosion) // DEPRECATED
            {
                deprecated = true,
                repeats = 200,
                affectOpenAreas = 0.05f,
                useLayers = true
            });

            CreateEffect(new EffectInit("Stained Glass Properties", EffectType.NN)
            {
            });
            CustomConfig("Variation", "1", ["1", "2", "3"]);
            CustomConfig("Color 1", "EffectColor1", ["EffectColor1", "EffectColor2", "None"]);
            CustomConfig("Color 2", "EffectColor2", ["EffectColor1", "EffectColor2", "None"]);
        }

        BeginGroup("Drought Natural");
        {
            CreateEffect(new EffectInit("Colored Barnacles", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.3f,
                useLayers = true,
                use3D = true
            });
            CustomConfig("Effect Color", "EffectColor2", ["EffectColor1", "EffectColor2", "None"]);
            
            CreateEffect(new EffectInit("Colored Rubble", EffectType.NN)
            {
                useLayers = true,
            });
            CustomConfig("Effect Color", "EffectColor2", ["EffectColor1", "EffectColor2", "None"]);

            CreateEffect(new EffectInit("Sand", EffectType.StandardErosion) // DEPRECATED
            {
                deprecated = true,
                repeats = 80,
                affectOpenAreas = 0.5f,
                useLayers = true,
            });
            CustomConfig("Effect Color", "None", ["EffectColor1", "EffectColor2", "None"]);

            CreateEffect(new EffectInit("Fat Slime", EffectType.StandardErosion)
            {
                repeats = 200,
                affectOpenAreas = 0.5f,
                useLayers = true,
                use3D = true,

                useDecalAffect = true,
                decalAffectDefault = true
            });
        }

        BeginGroup("Drought Artificial");
        {
            CreateEffect(new EffectInit("Assorted Trash", EffectType.NN)
            {
                useLayers = true,
            });
            CustomConfig("Effect Color", "None", ["EffectColor1", "EffectColor2", "None"]);

            CreateEffect(new EffectInit("Colored Wires", EffectType.NN)
            {
                useLayers = true,
            });
            CustomConfig("Effect Color", "EffectColor2", ["EffectColor1", "EffectColor2", "None"]);
            CustomConfig("Fatness", "2px", ["1px", "2px", "3px", "random"]);

            CreateEffect(new EffectInit("Colored Chains", EffectType.NN)
            {
                useLayers = true,
            });
            CustomConfig("Effect Color", "EffectColor2", ["EffectColor1", "EffectColor2", "None"]);
            CustomConfig("Size", "Small", ["Small", "FAT"]);

            CreateEffect(new EffectInit("Ring Chains", EffectType.NN)
            {
                useLayers = true
            });
            CustomConfig("Effect Color", "None", ["EffectColor1", "EffectColor2", "None"]);
        }

        BeginGroup("Dakras Plants");
        {
            CreateEffect(new EffectInit("Left Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Right Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Mixed Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Bubble Grower", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Moss Wall", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Club Moss", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });
        }

        BeginGroup("Leo Plants");
        {
            CreateEffect(new EffectInit("Ivy", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });
            CustomConfig("Color Intensity", "Medium", ["High", "Medium", "Low", "None", "Random"]);
            CustomConfig("Fruit Density", "None", ["High", "Medium", "Low", "None"]);
            CustomConfig("Leaf Density", 1, 100);
        }

        BeginGroup("Nautillo Plants");
        {
            CreateEffect(new EffectInit("Fuzzy Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });
        }
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

    public bool TryGetEffectFromName(string name, out EffectInit? effect)
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

    private void CustomConfig(string name, string defaultOption, string[] options)
    {
        activeEffect.customConfigs.Add(new CustomEffectString(name, defaultOption, options));
    }

    private void CustomConfig(string name, int min, int max)
    {
        activeEffect.customConfigs.Add(new CustomEffectInteger(name, min, max));
    }
#endregion
}