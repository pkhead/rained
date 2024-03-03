using System.Numerics;
using System.Text;
using RainEd.Props;
using Raylib_cs;
namespace RainEd;

static class LevelSerialization
{
    public static Level Load(string path)
    {
        var levelData = File.ReadAllLines(path);
        var lingoParser = new Lingo.LingoParser();
           
        // obtain level data from lines
        Lingo.List levelGeometry = (Lingo.List)
            (lingoParser.Read(levelData[0]) ?? throw new Exception("No geometry data"));
        
        Lingo.List levelTileData = (Lingo.List)
            (lingoParser.Read(levelData[1]) ?? throw new Exception("No tile data"));
        
        Lingo.List levelEffectData = (Lingo.List)
            (lingoParser.Read(levelData[2]) ?? throw new Exception("No effects data"));

        Lingo.List levelLightData = (Lingo.List)
            (lingoParser.Read(levelData[3]) ?? throw new Exception("No light data"));
        
        Lingo.List levelMiscData = (Lingo.List)
            (lingoParser.Read(levelData[4]) ?? throw new Exception("No misc data"));
        
        Lingo.List levelProperties = (Lingo.List)
            (lingoParser.Read(levelData[5]) ?? throw new Exception("No properties"));
        
        Lingo.List? levelCameraData = (Lingo.List?) lingoParser.Read(levelData[6]);
        Lingo.List? levelWaterData = (Lingo.List?) lingoParser.Read(levelData[7]);
        Lingo.List? levelPropData = (Lingo.List?) lingoParser.Read(levelData[8]);

        // get level dimensions
        Vector2 levelSize = (Vector2) levelProperties.fields["size"];
        Lingo.List extraTiles = (Lingo.List) levelProperties.fields["extraTiles"];

        var level = new Level((int)levelSize.X, (int)levelSize.Y)
        {
            BufferTilesLeft = (int) extraTiles.values[0],
            BufferTilesTop = (int) extraTiles.values[1],
            BufferTilesRight = (int) extraTiles.values[2],
            BufferTilesBot = (int) extraTiles.values[3],
            DefaultMedium = (int) levelMiscData.fields["defaultTerrain"] != 0
        };

        // read tile seed and light type
        {
            var seed = (int) levelProperties.fields["tileSeed"];
            level.TileSeed = seed;

            if (levelProperties.fields.TryGetValue("light", out object? objLight))
            {
                var light = (int)objLight;
                level.HasSunlight = light != 0;
            }
            else
            {
                level.HasSunlight = true;
            }
        }

        // read level geometry
        int x, y, z;
        x = 0;
        foreach (var xv in levelGeometry.values.Cast<Lingo.List>())
        {
            y = 0;
            foreach (var yv in xv.values.Cast<Lingo.List>())
            {
                z = 0;
                foreach (var cellData in yv.values.Cast<Lingo.List>())
                {
                    level.Layers[z,x,y].Cell = (CellType) (int) cellData.values[0];
                    
                    var flags = (Lingo.List) cellData.values[1];
                    foreach (int flag in flags.values.Cast<int>())
                    {
                        if (flag != 4)
                            level.Layers[z,x,y].Add((LevelObject) (1 << (flag-1)));
                    }

                    z++;
                }
                y++;
            }
            x++;
        }

        // read tile data
        Lingo.List tileMatrix = (Lingo.List) levelTileData.fields["tlMatrix"];

        // get default material
        {
            var defaultMat = levelTileData.fields["defaultMaterial"];
            var matIndex = Array.IndexOf(Level.MaterialNames, defaultMat);
            if (matIndex == -1) throw new Exception($"Material \"{defaultMat}\" does not exist");
            level.DefaultMaterial = (Material) matIndex + 1;
        }

        // read tile matrix
        x = 0;
        foreach (var xv in tileMatrix.values.Cast<Lingo.List>())
        {
            y = 0;
            foreach (var yv in xv.values.Cast<Lingo.List>())
            {
                z = 0;
                foreach (Lingo.List cellData in yv.values.Cast<Lingo.List>())
                {
                    var tp = (string) cellData.fields["tp"];
                    if (!cellData.fields.TryGetValue("data", out object? dataObj))
                    {
                        // wtf???
                        dataObj = cellData.fields["Data"];
                    }
                    
                    switch (tp)
                    {
                        case "default":
                            break;
                        
                        case "material":
                        {
                            var data = (string) dataObj;
                            var matIndex = Array.IndexOf(Level.MaterialNames, data);
                            if (matIndex == -1) throw new Exception($"Material \"{data}\" does not exist");
                            level.Layers[z,x,y].Material = (Material) matIndex + 1;
                            break;
                        }

                        case "tileBody":
                        {
                            var data = (Lingo.List) dataObj;
                            var pos = (Vector2) data.values[0];
                            var layer = (int) data.values[1];

                            level.Layers[z,x,y].TileRootX = (int)pos.X - 1;
                            level.Layers[z,x,y].TileRootY = (int)pos.Y - 1;
                            level.Layers[z,x,y].TileLayer = layer - 1;
                            break;
                        }

                        case "tileHead":
                        {
                            var data = (Lingo.List) dataObj;
                            var tileID = (Vector2) data.values[0];
                            var name = (string) data.values[1];

                            if (!RainEd.Instance.TileDatabase.HasTile(name))
                                throw new Exception($"Unrecognized tile '{name}'");
                            
                            var tile = RainEd.Instance.TileDatabase.GetTileFromName(name);
                            level.Layers[z,x,y].TileHead = tile;
                            break;
                        }

                        default:
                            throw new Exception($"Invalid tile type {tp}");
                    }
                    z++;
                }
                y++;
            }
            x++;
        }

        // read effects data
        {
            var effectsList = (Lingo.List) levelEffectData.fields["effects"];
            foreach (var effectData in effectsList.values.Cast<Lingo.List>())
            {
                var nameStr = (string) effectData.fields["nm"];
                var type = (string) effectData.fields["tp"];
                var effectInit = RainEd.Instance.EffectsDatabase.GetEffectFromName(nameStr);
                var effect = new Effect(level, effectInit);
                level.Effects.Add(effect);

                // validate data
                var requiredType = effect.Data.type switch
                {
                    EffectType.NN => "nn",
                    EffectType.StandardErosion => "standardErosion",
                    _ => throw new Exception("Internal error: invalid EffectType")
                };

                // check type
                if (requiredType != (string) effectData.fields["tp"])
                    throw new Exception($"Effect '{nameStr}' has incompatible parameters");
                
                // check crossScreen
                // actually, nevermind. must maintain compatibility with all erroneously generated level files
                // that may or may not have missing data
                /*
                int crossScreen = effect.Data.crossScreen ? 1 : 0;
                if (crossScreen != (int) effectData.fields["crossScreen"])
                    throw new Exception($"Effect '{nameStr}' has incompatible parameters");

                // check repeats
                if (effect.Data.type == EffectType.StandardErosion)
                {
                    if (effect.Data.repeats != (int) effectData.fields["repeats"])
                        throw new Exception($"Effect '{nameStr}' has incompatible parameters");
                    
                    if (effect.Data.affectOpenAreas != (float) effectData.fields["affectOpenAreas"])
                        throw new Exception($"Effect '{nameStr}' has incompatible parameters");
                }
                */
                
                // read effect matrix
                var mtrxData = (Lingo.List) effectData.fields["mtrx"];
                x = 0;
                foreach (var xv in mtrxData.values.Cast<Lingo.List>())
                {
                    y = 0;
                    foreach (var vo in xv.values)
                    {
                        effect.Matrix[x,y] = vo is int v ? v : (float)vo;
                        y++;
                    }
                    x++;
                }

                // read effect options
                if (!effectData.fields.TryGetValue("options", out object? optionsObj))
                {
                    optionsObj = effectData.fields["Options"]; // wtf??? again???
                }
                var optionsData = (Lingo.List) optionsObj!;

                foreach (var optionData in optionsData.values.Cast<Lingo.List>())
                {
                    var optionName = (string) optionData.values[0];

                    if (optionName == "Seed")
                    {
                        var value = (int) optionData.values[2];
                        effect.Seed = value;
                    }
                    else if (optionName != "Delete/Move")
                    {
                        // the list of options are stored in the level file
                        // alongside the selected option as a string
                        var optionValues = ((Lingo.List) optionData.values[1]).values.Cast<string>().ToList();
                        var selectedValue = (string) optionData.values[2];
                        var optionIndex = 0;

                        if (optionValues.Count > 0)
                        {
                            optionIndex = optionValues.IndexOf(selectedValue);
                            if (optionIndex == -1) throw new Exception("Invalid option value in effect");
                        }

                        switch (optionName)
                        {
                            case "Layers":
                                effect.Layer = (Effect.LayerMode) optionIndex;
                                break;
                            case "Color":
                                effect.PlantColor = optionIndex;
                                break;
                            case "3D":
                                effect.Is3D = optionIndex != 0;
                                break;
                            default:
                                if (optionName == effect.Data.customSwitchName)
                                {
                                    effect.CustomValue = optionIndex;
                                }
                                else
                                {
                                    RainEd.Logger.Warning($"Unknown option '{optionName}' in effect '{nameStr}'");
                                }
                                
                                break;
                        }
                    }
                }
            }
        }

        // read camear data
        if (levelCameraData is not null)
        {
            var camerasList = (Lingo.List) levelCameraData.fields["cameras"];

            foreach (Vector2 cameraPos in camerasList.values.Cast<Vector2>())
            {
                level.Cameras.Add(new Camera(new Vector2(cameraPos.X / 20f, cameraPos.Y / 20f)));
            }

            if (levelCameraData.fields.TryGetValue("quads", out object? quadsListData))
            {
                var quadsList = (Lingo.List) quadsListData!;
                int camIndex = 0;
                foreach (Lingo.List quad in quadsList.values.Cast<Lingo.List>())
                {
                    var ptsList = quad.values.Cast<Lingo.List>().ToArray();
                    
                    for (int i = 0; i < 4; i++)
                    {
                        var a = ptsList[i].values[0];
                        var b = ptsList[i].values[1];

                        // i did not think this through
                        // although, i'm not sure how to do Newtonsoft.Json-esque
                        float angle = (a is float v0) ? v0 : (int)a;
                        float offset = (b is float v1) ? v1 : (int)b;
                        level.Cameras[camIndex].CornerAngles[i] = angle / 180f * MathF.PI; 
                        level.Cameras[camIndex].CornerOffsets[i] = offset;
                    }
                    camIndex++;
                }
            }
        }
        else
        {
            level.Cameras.Add(new Camera());   
        }

        // read water data
        if (levelWaterData is not null)
        {
            var waterLevel = (int) levelWaterData.fields["waterLevel"];
            var waterInFront = (int) levelWaterData.fields["waterInFront"];

            if (waterLevel >= 0)
            {
                level.HasWater = true;
                level.WaterLevel = waterLevel;
            }

            level.IsWaterInFront = waterInFront != 0;
        }

        // read light data
        var lightPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path) + ".png";
        if (File.Exists(lightPath))
        {
            var img = RlManaged.Image.Load(lightPath);
            Raylib.ImageFormat(ref img.Ref(), PixelFormat.UncompressedGrayscale);
            level.LoadLightMap(img);
        }

        // read light parameters
        {
            // i did not think this through
            // although, i'm not sure how to do Newtonsoft.Json-esque
            var angleData = levelLightData.fields["lightAngle"];
            var flatnessData = levelLightData.fields["flatness"];

            float angle = (angleData is float v0) ? v0 : (int)angleData;
            float offset = (flatnessData is float v1) ? v1 : (int)flatnessData;
            level.LightAngle = angle / 180f * MathF.PI;
            level.LightDistance = offset;
        }

        // READ PROP DATA
        if (levelPropData is not null)
        {
            var propDb = RainEd.Instance.PropDatabase;
            var propsList = (Lingo.List) levelPropData.fields["props"];
            List<Vector2> pointList = new();
            
            foreach (var propData in propsList.values.Cast<Lingo.List>())
            {
                var depth = (int) propData.values[0];
                var name = (string) propData.values[1];
                var quadCornersData = (Lingo.List) propData.values[3];
                var moreData = (Lingo.List) propData.values[4];
                var settingsData = (Lingo.List) moreData.fields["settings"];

                // create prop
                if (!propDb.TryGetPropFromName(name, out PropInit? propInit) || propInit is null)
                    throw new Exception($"Unrecognized prop '{name}'");

                Prop prop;

                pointList.Clear();
                foreach (var pt in quadCornersData.values.Cast<Vector2>())
                {
                    pointList.Add(pt / 16f);
                }

                prop = new Prop(propInit, pointList.ToArray())
                {
                    DepthOffset = -depth,
                    RenderOrder = (int) settingsData.fields["renderorder"],
                    Seed = (int) settingsData.fields["seed"],
                    RenderTime = (Prop.PropRenderTime) (int) settingsData.fields["renderTime"]
                };

                prop.TryConvertToAffine();
                level.Props.Add(prop);

                // read rope points if needed
                if (propInit.Rope is not null)
                {
                    var pointsData = (Lingo.List) moreData.fields["points"];
                    pointList.Clear();

                    foreach (var pt in pointsData.values.Cast<Vector2>())
                    {
                        pointList.Add(pt / 20f);
                    }

                    prop.Rope!.LoadPoints(pointList.ToArray());
                }

                // read optional settings
                object? tempObject;
                if (settingsData.fields.TryGetValue("customDepth", out tempObject) && tempObject is not null)
                {
                    prop.CustomDepth = (int) tempObject;
                }

                if (settingsData.fields.TryGetValue("color", out tempObject) && tempObject is not null)
                {
                    prop.CustomColor = (int) tempObject - 1;
                }

                if (settingsData.fields.TryGetValue("variation", out tempObject) && tempObject is not null)
                {
                    prop.Variation = (int) tempObject;
                }

                if (settingsData.fields.TryGetValue("applyColor", out tempObject) && tempObject is not null)
                {
                    prop.ApplyColor = (int)tempObject != 0;
                }

                if (settingsData.fields.TryGetValue("release", out tempObject) && tempObject is not null)
                {
                    if (prop.Rope is not null)
                    {
                        int v = (int) tempObject;
                        prop.Rope.ReleaseMode = v switch
                        {
                            0 => RopeReleaseMode.None,
                            -1 => RopeReleaseMode.Left,
                            1 => RopeReleaseMode.Right,
                            _ => throw new Exception("Invalid rope release mode")
                        };
                    }
                    else
                    {
                        RainEd.Logger.Warning("Rope release mode was specified for a regular prop {PropName}", propInit.Name);
                    }
                }
            }
        }

        return level;
    }

    private readonly static string[] layerEnums = new string[]
    {
        "All", "1", "2", "3", "1:st and 2:nd", "2:nd and 3:rd"
    };

    private readonly static string[] plantColorEnums = new string[]
    {
        "Color1", "Color2", "Dead"
    };

    public static void Save(string path)
    {
        // open text file
        var outputTxtFile = new StreamWriter(path);

        var level = RainEd.Instance.Level;

        StringBuilder output = new();
        var newLine = Environment.NewLine;
        var workLayer = RainEd.Instance.Window.WorkLayer + 1;

        // geometry data
        output.Append('[');
        for (int x = 0; x < level.Width; x++)
        {
            if (x > 0) output.Append(", ");
            output.Append('[');
            for (int y = 0; y < level.Height; y++)
            {
                if (y > 0) output.Append(", ");
                output.Append('[');
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    if (l > 0) output.Append(", ");
                    output.Append('[');

                    ref var cell = ref level.Layers[l,x,y];
                    output.Append((int)cell.Cell);
                    output.Append(", [");

                    // objects
                    bool hasObject = false;
                    if (cell.Cell == CellType.ShortcutEntrance)
                    {
                        hasObject = true;
                        output.Append('4');
                    }
                    
                    for (int i = 0; i < 32; i++)
                    {
                        if (cell.Has((LevelObject) (1 << i)))
                        {
                            if (hasObject) output.Append(", ");
                            hasObject = true;
                            output.Append(i+1);
                        }
                    }

                    output.Append("]]");
                }
                output.Append(']');
            }
            output.Append(']');
        }
        output.Append(']');
        output.Append(newLine);

        // tiles
        output.Append($"[#lastKeys: [], #Keys: [], #workLayer: {workLayer}, #lstMsPs: point(0, 0), #tlMatrix: [");
        for (int x = 0; x < level.Width; x++)
        {
            if (x > 0) output.Append(", ");
            output.Append('[');
            for (int y = 0; y < level.Height; y++)
            {
                if (y > 0) output.Append(", ");
                output.Append('[');
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    if (l > 0) output.Append(", ");
                    var cell = level.Layers[l,x,y];

                    // tile head
                    if (cell.TileHead is not null)
                    {
                        int group = cell.TileHead.Category.Index + 3;
                        int sub = cell.TileHead.Category.Tiles.IndexOf(cell.TileHead) + 1;
                        string name = cell.TileHead.Name;
                        output.AppendFormat("[#tp: \"tileHead\", #data: [point({0}, {1}), \"{2}\"]]", group, sub, name);
                    }
                    // tile body
                    else if (cell.HasTile())
                    {
                        output.AppendFormat(
                            "[#tp: \"tileBody\", #data: [point({0}, {1}), {2}]]",
                            cell.TileRootX + 1,
                            cell.TileRootY + 1,
                            cell.TileLayer + 1
                        );
                    }
                    // material
                    else if (cell.Material != Material.None)
                    {
                        output.AppendFormat("[#tp: \"material\", #data: \"{0}\"]", Level.MaterialNames[(int)cell.Material-1]);
                    }
                    // no tile/material data here
                    else
                        output.Append("[#tp: \"default\", #data: 0]");
                }
                output.Append(']');
            }
            output.Append(']');
        }
        output.AppendFormat("], #defaultMaterial: \"{0}\", ", Level.MaterialNames[(int)level.DefaultMaterial-1]);

        // some lingo tile editor data, irrelevant to this editor
        output.Append("#toolType: \"material\", #toolData: \"Big Metal\", #tmPos: point(1, 1), #tmSavPosL: [], #specialEdit: 0]");
        output.Append(newLine);

        // effect data
        output.Append("[#lastKeys: [], #Keys: [], #lstMsPs: point(0, 0), #effects: [");

        for (int i = 0; i < level.Effects.Count; i++)
        {
            if (i > 0) output.Append(", ");
            var effect = level.Effects[i];
            output.Append('[');

            output.AppendFormat("#nm: \"{0}\", #tp: \"{1}\", #crossScreen: {2}, #mtrx: [",
                effect.Data.name, effect.Data.type == EffectType.StandardErosion ? "standardErosion" : "nn",
                effect.Data.crossScreen ? 1 : 0
            );

            // matrix data
            for (int x = 0; x < level.Width; x++)
            {
                if (x > 0) output.Append(", ");
                output.Append('[');
                for (int y = 0; y < level.Height; y++)
                {
                    if (y > 0) output.Append(", ");
                    output.Append(effect.Matrix[x,y].ToString("0.0000"));
                }
                output.Append(']');
            }
            output.Append("], ");

            // effect options
            output.Append("#Options: [[\"Delete/Move\", [\"Delete\", \"Move Back\", \"Move Forth\"], \"\"]");
            if (effect.Data.useLayers)
            {
                output.AppendFormat(", [\"Layers\", [\"All\", \"1\", \"2\", \"3\", \"1:st and 2:nd\", \"2:nd and 3:rd\"], \"{0}\"]",
                    layerEnums[(int) effect.Layer]
                );
            }

            if (effect.Data.use3D)
            {
                output.AppendFormat(", [\"3D\", [\"Off\", \"On\"], \"{0}\"]", effect.Is3D ? "On" : "Off");
            }

            if (effect.Data.usePlantColors)
            {
                output.AppendFormat(", [\"Color\", [\"Color1\", \"Color2\", \"Dead\"], \"{0}\"]", plantColorEnums[effect.PlantColor]);
            }

            // custom option
            if (!string.IsNullOrEmpty(effect.Data.customSwitchName))
            {
                output.AppendFormat(", [\"{0}\", [", effect.Data.customSwitchName);

                for (int j = 0; j < effect.Data.customSwitchOptions.Length; j++)
                {
                    if (j > 0) output.Append(", ");
                    output.Append('"');
                    output.Append(effect.Data.customSwitchOptions[j]);
                    output.Append('"');
                }

                output.AppendFormat("], \"{0}\"]", effect.Data.customSwitchOptions[effect.CustomValue]);
            }

            // seed
            output.AppendFormat(", [\"Seed\", [], {0}]", effect.Seed);

            // end effects options
            output.Append("], ");

            // effect parameters & end effect
            output.AppendFormat("#repeats: {0}, #affectOpenAreas: {1}]", effect.Data.repeats, effect.Data.affectOpenAreas);
        }

        output.Append("], #emPos: point(1, 1), #editEffect: 1, #selectEditEffect: 1, #mode: \"createNew\", #brushSize: 5]");
        output.Append(newLine);

        // light data
        output.Append("[#pos: point(0, 0), #rot: 0, #sz: point(50, 70), #col: 1, #Keys: 0, #lastKeys: 0, #lastTm: 0, ");
        output.AppendFormat("#lightAngle: {0}, #flatness: {1}, ", level.LightAngle / MathF.PI * 180f, level.LightDistance);
        output.Append("#lightRect: rect(1000, 1000, -1000, -1000), #paintShape: \"px1\"]");
        output.Append(newLine);

        // default medium and light type
        // otherwise, filled with useless data
        output.AppendFormat(
            "[#timeLimit: 4800, #defaultTerrain: {0}, #maxFlies: 10, #flySpawnRate: 50, #lizards: [], #ambientSounds: [], #music: \"NONE\", #tags: [], #lightType: \"{1}\", #waterDrips: 1, #lightRect: rect(0, 0, 1040, 800), #Matrix: []]]",
            level.DefaultMedium ? 1 : 0,
            "Static"    
        );
        output.Append(newLine);

        // level properties
        output.AppendFormat(
            "[#mouse: 1, #lastMouse: 0, #mouseClick: 0, #pal: 1, #pals: [[#detCol: color( 255, 0, 0 )]], #eCol1: 1, #eCol2: 2, #totEcols: 5, #tileSeed: {0}, #colGlows: [0, 0], #size: point({1}, {2}), #extraTiles: [{3}, {4}, {5}, {6}], #light: {7}]",
            level.TileSeed,
            level.Width, level.Height,
            level.BufferTilesLeft,
            level.BufferTilesTop,
            level.BufferTilesRight,
            level.BufferTilesBot,
            level.HasSunlight ? 1 : 0
        );
        output.Append(newLine);

        // cameras
        output.Append("[#cameras: [");
        for (int i = 0; i < level.Cameras.Count; i++)
        {
            var cam = level.Cameras[i];
            output.AppendFormat("point({0}, {1})", cam.Position.X * 20f, cam.Position.Y * 20f);
            if (i < level.Cameras.Count - 1)
                output.Append(", ");
        }
        output.Append("], #selectedCamera: 0, #Keys: [#n: 0, #d: 0, #e: 0, #p: 0], #lastKeys: [#n: 0, #d: 0, #e: 0, #p: 0], ");

        // camera corner data
        output.Append("#quads: [");
        for (int i = 0; i < level.Cameras.Count; i++)
        {
            var cam = level.Cameras[i];
            output.Append('[');

            for (int j = 0; j < 4; j++)
            {
                output.AppendFormat(
                    "[{0}, {1}]",
                    cam.CornerAngles[j] / MathF.PI * 180f,
                    cam.CornerOffsets[j]
                );

                if (j < 3) output.Append(", ");
            }

            output.Append(']');
            if (i < level.Cameras.Count - 1)
                output.Append(", ");
        }
        output.Append("]]");
        output.Append(newLine);

        // water data
        output.AppendFormat(
            "[#waterLevel: {0}, #waterInFront: {1}, #waveLength: 60, #waveAmplitude: 5, #waveSpeed: 10]",
            level.HasWater ? level.WaterLevel : -1,
            level.IsWaterInFront ? 1 : 0
        );
        output.Append(newLine);

        // props data
        output.AppendFormat(
            "[#props: [], #lastKeys: [#w: 0, #a: 0, #s: 0, #d: 0, #L: 0, #n: 0, #m1: 0, #m2: 0, #c: 0, #z: 0], #Keys: [#w: 0, #a: 0, #s: 0, #d: 0, #L: 0, #n: 0, #m1: 0, #m2: 0, #c: 0, #z: 0], #workLayer: {0}, #lstMsPs: point(0, 0), #pmPos: point(1, 1), #pmSavPosL: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1], #propRotation: 325, #propStretchX: 1, #propStretchY: 1, #propFlipX: 1, #propFlipY: 1, #depth: 0, #color: 0]",
            workLayer
        );
        output.Append(newLine);

        // finish writing to txt file
        outputTxtFile.Write(output);
        outputTxtFile.Close();

        // write light image
        var lightPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path) + ".png";
        using var lightMapImg = level.LightMap.GetImage();
        Raylib.ExportImage(lightMapImg, lightPath);
    }
}