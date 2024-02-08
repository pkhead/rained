namespace RainEd;

public enum EffectType
{
    NN, // i have no idea what this abbreviates, lol
    StandardErosion
}

public record EffectInit
{
    public string name;
    public EffectType type;
    public bool crossScreen = false;

    // these properties are valid only for StandardErosion effect types
    public int repeats;
    public float affectOpenAreas;

    public int fillWith = 0; // only effect to change this is BlackGoo, which sets fx matrix values to 100 (max) on creation
    public bool useLayers = false;
    public bool use3D = false;
    public bool usePlantColors = false;

    public string customSwitchName = string.Empty;
    public string customSwitchDefault = string.Empty;
    public string[]? customSwitchOptions = null;

    public EffectInit(string name, EffectType type)
    {
        this.name = name;
        this.type = type;
    }
}

public struct EffectGroup
{
    public string name;
    public List<EffectInit> effects;
}

/**
* hardcoded effects database >:)
* hardcoded cus, that's the way it is in the rain world level editor,
* and to create new effects you'd have to modify the renderer code anyway.
*/
public class EffectsDatabase
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
                use3D = true
            });
            
            CreateEffect(new EffectInit("Melt", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.5f
            });

            CreateEffect(new EffectInit("Rust", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.2f,
                use3D = true
            });

            CreateEffect(new EffectInit("Barnacles", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.3f,
                use3D = true
            });

            CreateEffect(new EffectInit("Rubble", EffectType.NN)
            {
                useLayers = true
            });

            CreateEffect(new EffectInit("DecalsOnlySlime", EffectType.StandardErosion)
            {
                repeats = 130,
                affectOpenAreas = 0.5f,
                use3D = true
            });
        }

        /////////////
        // Erosion //
        /////////////
        BeginGroup("Erosion");
        {
            CreateEffect(new EffectInit("Roughen", EffectType.StandardErosion)
            {
                repeats = 30,
                affectOpenAreas = 0.05f
            });

            CreateEffect(new EffectInit("SlimeX3", EffectType.StandardErosion)
            {
                repeats = 130 * 3,
                affectOpenAreas = 0.5f,
                use3D = true
            });

            CreateEffect(new EffectInit("Super Melt", EffectType.StandardErosion)
            {
                repeats = 50,
                affectOpenAreas = 0.5f,
                use3D = true
            });

            CreateEffect(new EffectInit("Destructive Melt", EffectType.StandardErosion)
            {
                repeats = 50,
                affectOpenAreas = 0.5f,
                use3D = true
            });

            CreateEffect(new EffectInit("Erode", EffectType.StandardErosion)
            {
                repeats = 80,
                affectOpenAreas = 0.5f
            });

            CreateEffect(new EffectInit("Super Erode", EffectType.StandardErosion)
            {
                repeats = 60,
                affectOpenAreas = 0.5f
            });

            CreateEffect(new EffectInit("DaddyCorruption", EffectType.NN));
        }

        ///////////////
        // Artifical //
        ///////////////
        BeginGroup("Artificial");
        {
            CreateEffect(new EffectInit("Wires", EffectType.NN)
            {
                useLayers = true,
                customSwitchName = "Fatness",
                customSwitchOptions = new string[] { "1px", "2px", "3px", "random" },
                customSwitchDefault = "2px"
            });

            CreateEffect(new EffectInit("Chains", EffectType.NN)
            {
                useLayers = true,
                crossScreen = true,
                customSwitchName = "Size",
                customSwitchOptions = new string[] { "Small", "FAT" },
                customSwitchDefault = "Small"
            });
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
                useLayers = true
            });

            CreateEffect(new EffectInit("Lighthouse Flowers", EffectType.NN)
            {
                useLayers = true
            });

            CreateEffect(new EffectInit("Fern", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Giant Mushroom", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Sprawlbush", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("featherFern", EffectType.NN)
            {
                useLayers = true,
                usePlantColors = true
            });

            CreateEffect(new EffectInit("Fungus Tree", EffectType.NN)
            {
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
                fillWith = 100
            });

            CreateEffect(new EffectInit("DarkSlime", EffectType.NN));
        }

        /////////////////
        // Restoration //
        /////////////////
        BeginGroup("Restoration");
        {
            CreateEffect(new EffectInit("Restore As Scaffolding", EffectType.NN)
            {
                useLayers = true
            });

            CreateEffect(new EffectInit("Ceramic Chaos", EffectType.NN)
            {
                customSwitchName = "Colored",
                customSwitchOptions = new string[] { "None", "White" },
                customSwitchDefault = "White"
            });
        }

        // TODO: community effects
    }

#region Helpers
    EffectGroup activeGroup;
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
        activeGroup.effects.Add(effect);
    }
#endregion
}