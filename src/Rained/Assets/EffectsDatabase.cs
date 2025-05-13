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
    public bool optionalInBounds = false;
    public bool deprecated = false;
    
    /// <summary>
    /// The default layer configuration for a newly created effect.
    /// Applicable only if useLayers is true.
    /// </summary>
    public Effect.LayerMode defaultLayer = Effect.LayerMode.All;
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
                usePlantColors = true,
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
                usePlantColors = true,
                optionalInBounds = true,
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
                optionalInBounds = true,
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
                usePlantColors = true,
                optionalInBounds = true,
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
                usePlantColors = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Rollers", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Garbage Spirals", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                usePlantColors = true,
                optionalInBounds = true,
            });

            /////////////
            // Plants3 //
            /////////////
            
            CreateEffect(new EffectInit("Thick Roots", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Shadow Plants", EffectType.NN)
            {
                crossScreen = true,
                useLayers = true,
                optionalInBounds = true,
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
                useLayers = true,
                defaultLayer = Effect.LayerMode.First
            });

            CreateEffect(new EffectInit("Lighthouse Flowers", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                defaultLayer = Effect.LayerMode.First
            });

            CreateEffect(new EffectInit("Fern", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First,
            });

            CreateEffect(new EffectInit("Giant Mushroom", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First
            });

            CreateEffect(new EffectInit("Sprawlbush", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First
            });

            CreateEffect(new EffectInit("featherFern", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First,
            });

            CreateEffect(new EffectInit("Fungus Tree", EffectType.NN)
            {
                binary = true,
                single = true,
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First
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

        ///////////////
        // LB Plants //
        ///////////////
        BeginGroup("LB Plants");
        {
            CreateEffect(new EffectInit("Colored Hang Roots", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Colored Thick Roots", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Colored Shadow Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Colored Lighthouse Flowers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First,
            });

            CreateEffect(new EffectInit("Colored Fungi Flowers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                defaultLayer = Effect.LayerMode.First,
            });

            CreateEffect(new EffectInit("Root Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
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
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Small Springs", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Mini Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
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

        ////////////////
        // LB Erosion //
        ////////////////
        BeginGroup("LB Erosion");
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

        //////////////////////
        // LB Paint Effects //
        //////////////////////
        BeginGroup("LB Paint Effects");
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

        ////////////////
        // LB Natural //
        ////////////////
        BeginGroup("LB Natural");
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

        ///////////////////
        // LB Artificial //
        ///////////////////
        BeginGroup("LB Artificial");
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

        ///////////////////
        // Dakras Plants //
        ///////////////////
        BeginGroup("Dakras Plants");
        {
            CreateEffect(new EffectInit("Left Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Right Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Mixed Facing Kelp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Bubble Grower", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
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

            CreateEffect(new EffectInit("Dandelions", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });
        }

        ////////////////
        // Leo Plants //
        ////////////////
        BeginGroup("Leo Plants");
        {
            CreateEffect(new EffectInit("Ivy", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });
            CustomConfig("Color Intensity", "Medium", ["High", "Medium", "Low", "None", "Random"]);
            CustomConfig("Fruit Density", "None", ["High", "Medium", "Low", "None"]);
            CustomConfig("Leaf Density", 1, 100);
        }

        /////////////////////
        // Nautillo Plants //
        /////////////////////
        BeginGroup("Nautillo Plants");
        {
            CreateEffect(new EffectInit("Fuzzy Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Leaf Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Meat Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Hyacinths", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Seed Grass", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Orb Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Storm Plants", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true
            });

            CreateEffect(new EffectInit("Coral Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Horror Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });
        }

        ///////////////////
        // Tronsx Plants //
        ///////////////////
        BeginGroup("Tronsx Plants");
        {
            CreateEffect(new EffectInit("Thunder Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });
        }

        /////////////////////
        // Intrepid Plants //
        /////////////////////
        BeginGroup("Intrepid Plants");
        {
            CreateEffect(new EffectInit("Ice Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Grass Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Fancy Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });
        }

        //////////////////////
        // LudoCrypt Plants //
        //////////////////////
        BeginGroup("LudoCrypt Plants");
        {
            CreateEffect(new EffectInit("Mushroom Stubs", EffectType.NN)
            {
                usePlantColors = true,
                useLayers = true,
            });
            CustomConfig("Mushroom Size", ["Small", "Medium", "Random"], "Medium");
            CustomConfig("Mushroom Width", ["Small", "Medium", "Wide", "Random"], "Medium");
        }

        /////////////////////
        // Aldruis Effects //
        /////////////////////
        BeginGroup("Alduris Effects");
        {
            CreateEffect(new EffectInit("Mosaic Plants", EffectType.NN)
            {
                useLayers = true,
                defaultLayer = Effect.LayerMode.First,
                usePlantColors = true,
                crossScreen = true
            });
            CustomConfig("Color Intensity", "Medium", ["High", "Medium", "Low", "None", "Random"]);
            CustomConfig("Flowers", "Off", ["Off", "On"]);
            CustomConfig("Detail Color", "Color1", ["Color1", "Color2", "Dead"]);

            CreateEffect(new EffectInit("Lollipop Mold", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
            });

            CreateEffect(new EffectInit("Cobwebs", EffectType.NN)
            {
                useLayers = true,
                defaultLayer = Effect.LayerMode.First,
                crossScreen = true
            });
            CustomConfig("Effect Color", ["EffectColor1", "EffectColor2", "None"], "None");
            CustomConfig("Color Intensity", ["High", "Medium", "Low", "None"], "Medium");

            CreateEffect(new EffectInit("Fingers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                defaultPlantColor = 2, // Dead
            });
            CustomConfig("Finger Thickness", ["Small", "Medium", "FAT", "Random"], "Medium");
            CustomConfig("Finger Length", ["Short", "Medium", "Tall", "Random"], "Medium");
        }

        //////////////////
        // April Plants //
        //////////////////
        BeginGroup("April Plants");
        {
            CreateEffect(new EffectInit("Grape Roots", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Og Grass", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Hand Growers", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                crossScreen = true,
                binary = true,
                single = true,
                optionalInBounds = true,
            });

            CreateEffect(new EffectInit("Head Lamp", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true,
                defaultPlantColor = 2, // Dead
                binary = true,
                single = true
            });
            CustomConfig("Lamp Color", ["Color1", "Color2", "Dead"], "Dead");
        }

        RegisterCustomEffects();
    }

    private void RegisterCustomEffects()
    {
        var lingoParser = new Lingo.LingoParser();
        var initFile = Path.Combine(RainEd.Instance.AssetDataPath, "Effects", "Init.txt");

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