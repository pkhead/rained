using System.Globalization;
using System.Numerics;
using System.Text;
using Rained.Assets;
using Raylib_cs;
namespace Rained.LevelData;

record LevelLoadResult(Level Level)
{
    public Level Level = Level;
    public bool HadUnrecognizedAssets = false;
    public string[] UnrecognizedMaterials = [];
    public string[] UnrecognizedTiles = [];
    public string[] UnrecognizedProps = [];
    public string[] UnrecognizedEffects = [];
}

static class LevelSerialization
{
    public static LevelLoadResult Load(string path)
    {
        List<string> unknownMats = [];
        List<string> unknownTiles = [];
        List<string> unknownProps = [];
        List<string> unknownEffects = [];
        bool loadSuccess = true;

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
        
        // it is valid for these lines to be omitted
        // i assume these were features not present in older versions of the RWLE
        Lingo.List? levelCameraData = null;
        Lingo.List? levelWaterData = null;
        Lingo.List? levelPropData = null;
        
        if (levelData.Length >= 7)
            levelCameraData = lingoParser.Read(levelData[6]) as Lingo.List;
        
        if (levelData.Length >= 8)
            levelWaterData = lingoParser.Read(levelData[7]) as Lingo.List;

        if (levelData.Length >= 9)
            levelPropData = lingoParser.Read(levelData[8]) as Lingo.List;

        // get level dimensions
        Vector2 levelSize = (Vector2) levelProperties.fields["size"];
        Lingo.List extraTiles = (Lingo.List) levelProperties.fields["extraTiles"];

        var level = new Level((int)levelSize.X, (int)levelSize.Y)
        {
            BufferTilesLeft = Lingo.LingoNumber.AsInt(extraTiles.values[0]),
            BufferTilesTop = Lingo.LingoNumber.AsInt(extraTiles.values[1]),
            BufferTilesRight = Lingo.LingoNumber.AsInt(extraTiles.values[2]),
            BufferTilesBot = Lingo.LingoNumber.AsInt(extraTiles.values[3]),
            DefaultMedium = Lingo.LingoNumber.AsInt(levelMiscData.fields["defaultTerrain"]) != 0
        };

        // read tile seed and light type
        {
            var seed = Lingo.LingoNumber.AsInt(levelProperties.fields["tileSeed"]);
            level.TileSeed = seed;

            if (levelProperties.fields.TryGetValue("light", out object? objLight))
            {
                var light = Lingo.LingoNumber.AsInt(objLight);
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
                    level.Layers[z,x,y].Geo = (GeoType) Lingo.LingoNumber.AsInt(cellData.values[0]);
                    
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
            var defaultMat = (string) levelTileData.fields["defaultMaterial"];
            var matInfo = RainEd.Instance.MaterialDatabase.GetMaterial(defaultMat);

            if (matInfo is not null)
            {
                level.DefaultMaterial = matInfo.ID;
            }
            else
            {
                // unrecognized material
                loadSuccess = false;
                if (!unknownMats.Contains(defaultMat))
                    unknownMats.Add(defaultMat);
                Log.Warning("Unrecognized material '{Name}'", defaultMat);
            }
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
                            var matInfo = RainEd.Instance.MaterialDatabase.GetMaterial(data);

                            if (matInfo is not null)
                            {
                                level.Layers[z,x,y].Material = matInfo.ID;
                            }
                            else
                            {
                                // unrecognized material
                                Log.Warning("Material \"{Name}\" does not exist", data);
                                loadSuccess = false;
                                if (!unknownMats.Contains(data))
                                    unknownMats.Add(data);
                            }
                            break;
                        }

                        case "tileBody":
                        {
                            var data = (Lingo.List) dataObj;
                            var pos = (Vector2) data.values[0];
                            var layer = Lingo.LingoNumber.AsInt(data.values[1]);

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
                            {
                                // unrecognized tile
                                Log.Warning("Unrecognized tile '{Name}'", name);
                                loadSuccess = false;
                                if (!unknownTiles.Contains(name))
                                    unknownTiles.Add(name);
                            }
                            else
                            {
                                var tile = RainEd.Instance.TileDatabase.GetTileFromName(name);
                                ref var cell = ref level.Layers[z,x,y]; 
                                cell.TileHead = tile;

                                if (tile.Tags.Contains("Chain Holder") && data.values.Count > 2 && data.values[2] as string != "NONE")
                                {
                                    var chainPos = (Vector2) data.values[2];
                                    level.SetChainData(z, x, y, (int)chainPos.X - 1, (int)chainPos.Y - 1);
                                }
                            }

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

                if (!RainEd.Instance.EffectsDatabase.TryGetEffectFromName(nameStr, out EffectInit? effectInit))
                {
                    // unrecognized effect
                    Log.Warning("Unrecognized effect '{Name}'", nameStr);
                    loadSuccess = false;
                    if (!unknownEffects.Contains(nameStr))
                        unknownEffects.Add(nameStr);
                }
                else
                {
                    var effect = new Effect(level, effectInit!);
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
                            effect.Matrix[x,y] = Lingo.LingoNumber.AsFloat(vo);
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
                            var value = Lingo.LingoNumber.AsInt(optionData.values[2]);
                            effect.Seed = value;
                        }
                        else if (optionName != "Delete/Move")
                        {
                            // the list of options are stored in the level file
                            // alongside the selected option as a string
                            var optionValues = ((Lingo.List) optionData.values[1]).values.Cast<string>().ToList();
                            var optionIndex = 0;

                            if (optionValues.Count > 0)
                            {
                                var selectedValue = (string) optionData.values[2];
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
                                case "Affect Gradients and Decals":
                                    effect.AffectGradientsAndDecals = optionIndex == 0;
                                    break;
                                default:
                                {
                                    int cfgIndex = effect.Data.GetCustomConfigIndex(optionName);
                                    if (cfgIndex == -1)
                                    {
                                        Log.UserLogger.Warning($"Unknown option '{optionName}' in effect '{nameStr}'");
                                    }
                                    else
                                    {
                                        if (effect.Data.customConfigs[cfgIndex] is CustomEffectInteger)
                                        {
                                            effect.CustomValues[cfgIndex] = Lingo.LingoNumber.AsInt(optionData.values[2]);
                                        }
                                        else
                                        {
                                            effect.CustomValues[cfgIndex] = optionIndex;
                                        }
                                    }

                                    break;
                                }
                            }
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
                        var angle = Lingo.LingoNumber.AsFloat(ptsList[i].values[0]);
                        var offset = Lingo.LingoNumber.AsFloat(ptsList[i].values[1]);

                        // i did not think this through
                        // although, i'm not sure how to do Newtonsoft.Json-esque
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
            var waterLevel = Lingo.LingoNumber.AsInt(levelWaterData.fields["waterLevel"]);
            var waterInFront = Lingo.LingoNumber.AsInt(levelWaterData.fields["waterInFront"]);

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
            using var img = RlManaged.Image.Load(lightPath);

            // some level pngs have no data written to them.
            // wtf??
            if (img.Width == 0 && img.Height == 0)
            {
                Log.UserLogger.Warning("Invalid lightmap image, loaded fallback");
            }
            else
            {
                Raylib.ImageFormat(ref img.Ref(), PixelFormat.UncompressedGrayscale);
                level.LoadLightMap(img);
            }
        }

        // read light parameters
        {
            var angle = Lingo.LingoNumber.AsFloat(levelLightData.fields["lightAngle"]);
            var offset = Lingo.LingoNumber.AsFloat(levelLightData.fields["flatness"]);
            level.LightAngle = angle / 180f * MathF.PI;
            level.LightDistance = offset;
        }

        // READ PROP DATA
        if (levelPropData is not null)
        {
            var propDb = RainEd.Instance.PropDatabase;
            var propsList = (Lingo.List) levelPropData.fields["props"];
            List<Vector2> pointList = [];
            
            foreach (var propData in propsList.values.Cast<Lingo.List>())
            {
                var depth = Lingo.LingoNumber.AsInt(propData.values[0]);
                var name = (string) propData.values[1];
                var quadCornersData = (Lingo.List) propData.values[3];
                var moreData = (Lingo.List) propData.values[4];
                var settingsData = (Lingo.List) moreData.fields["settings"];

                // create prop
                if (!propDb.TryGetPropFromName(name, out PropInit? propInit) || propInit is null)
                {
                    // unrecognized prop
                    Log.Warning("Unrecognized prop '{Name}'", name);
                    loadSuccess = false;
                    if (!unknownProps.Contains(name))
                        unknownProps.Add(name);
                }
                else
                {
                    Prop prop;

                    pointList.Clear();
                    foreach (var pt in quadCornersData.values.Cast<Vector2>())
                    {
                        pointList.Add(pt / 16f);
                    }

                    prop = new Prop(propInit, pointList.ToArray())
                    {
                        DepthOffset = -depth,
                        RenderOrder = Lingo.LingoNumber.AsInt(settingsData.fields["renderorder"]),
                        Seed = Lingo.LingoNumber.AsInt(settingsData.fields["seed"]),
                        RenderTime = (PropRenderTime) Lingo.LingoNumber.AsInt(settingsData.fields["renderTime"])
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
                        prop.CustomDepth = Lingo.LingoNumber.AsInt(tempObject);
                    }

                    if (settingsData.fields.TryGetValue("color", out tempObject) && tempObject is not null)
                    {
                        prop.CustomColor = Lingo.LingoNumber.AsInt(tempObject) - 1;
                    }

                    if (settingsData.fields.TryGetValue("variation", out tempObject) && tempObject is not null)
                    {
                        prop.Variation = Lingo.LingoNumber.AsInt(tempObject) - 1;
                    }

                    if (settingsData.fields.TryGetValue("applyColor", out tempObject) && tempObject is not null)
                    {
                        prop.ApplyColor = Lingo.LingoNumber.AsInt(tempObject) != 0;
                    }

                    if (settingsData.fields.TryGetValue("release", out tempObject) && tempObject is not null)
                    {
                        if (prop.Rope is not null)
                        {
                            int v = Lingo.LingoNumber.AsInt(tempObject);
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
                            Log.UserLogger.Warning("Rope release mode was specified for a regular prop {PropName}", propInit.Name);
                        }
                    }

                    if (settingsData.fields.TryGetValue("thickness", out tempObject) && tempObject is not null)
                    {
                        if (prop.Rope is not null && prop.PropInit.PropFlags.HasFlag(PropFlags.CanSetThickness))
                        {
                            prop.Rope.Thickness = Lingo.LingoNumber.AsFloat(tempObject);
                        }
                        else
                        {
                            Log.UserLogger.Warning("Wire thickness was specified for an incompatible prop {PropName}", propInit.Name);
                        }
                    }
                }
            }
        }

        // return load result
        var loadResult = new LevelLoadResult(level)
        {
            HadUnrecognizedAssets = !loadSuccess
        };

        if (!loadSuccess)
        {
            loadResult.UnrecognizedEffects = [..unknownEffects];
            loadResult.UnrecognizedMaterials = [..unknownMats];
            loadResult.UnrecognizedProps = [..unknownProps];
            loadResult.UnrecognizedTiles = [..unknownTiles];
        }

        return loadResult;
    }

    private readonly static string[] layerEnums = new string[]
    {
        "All", "1", "2", "3", "1:st and 2:nd", "2:nd and 3:rd"
    };

    private readonly static string[] plantColorEnums = new string[]
    {
        "Color1", "Color2", "Dead"
    };

    public static void SaveLevelTextFile(Level level, string path)
    {
        var outputTxtFile = new StreamWriter(path);
        StringBuilder output = new();

        // Wtf this sucks i spent like an hour trying to figure out why drizzle wasn't rendering the props it was because
        // i saved newlines as Environment.NewLine which was \r\n on windows
        // Wtf this sucks why does lingo expect \r newlines that's bs \r newlines were deprecated on mac software like 20 years ago
        // Why is there no compatibilty for \r\n newlines wtf is this bs
        var newLine = "\r";
        var workLayer = RainEd.Instance.LevelView.WorkLayer + 1;

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
                    output.Append((int) cell.Geo);
                    output.Append(", [");

                    // objects
                    bool hasObject = false;
                    if (cell.Geo == GeoType.ShortcutEntrance)
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
                        int group = cell.TileHead.Category.Index + 2 + RainEd.Instance.MaterialDatabase.Categories.Count;
                        int sub = cell.TileHead.Category.Tiles.IndexOf(cell.TileHead) + 1;
                        string name = cell.TileHead.Name;

                        if (cell.TileHead.Tags.Contains("Chain Holder"))
                        {
                            string chainData;
                            if (level.TryGetChainData(l, x, y, out var chainEndPos))
                            {
                                chainData = $"point({chainEndPos.X + 1}, {chainEndPos.Y + 1})";
                            }
                            else
                            {
                                chainData = "\"NONE\"";
                            }

                            output.AppendFormat(
                                "[#tp: \"tileHead\", #Data: [point({0}, {1}), \"{2}\", {3}]]",
                                group, sub, name, chainData
                            );
                        }
                        else
                        {
                            output.AppendFormat("[#tp: \"tileHead\", #Data: [point({0}, {1}), \"{2}\"]]", group, sub, name);
                        }
                    }
                    // tile body
                    else if (cell.HasTile())
                    {
                        output.AppendFormat(
                            "[#tp: \"tileBody\", #Data: [point({0}, {1}), {2}]]",
                            cell.TileRootX + 1,
                            cell.TileRootY + 1,
                            cell.TileLayer + 1
                        );
                    }
                    // material
                    else if (cell.Material != 0)
                    {
                        output.AppendFormat("[#tp: \"material\", #Data: \"{0}\"]", RainEd.Instance.MaterialDatabase.GetMaterial(cell.Material).Name);
                    }
                    // no tile/material data here
                    else
                        output.Append("[#tp: \"default\", #Data: 0]");
                }
                output.Append(']');
            }
            output.Append(']');
        }
        output.AppendFormat("], #defaultMaterial: \"{0}\", ", RainEd.Instance.MaterialDatabase.GetMaterial(level.DefaultMaterial).Name);

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
                    output.Append(effect.Matrix[x,y].ToString("0.0000", CultureInfo.InvariantCulture));
                }
                output.Append(']');
            }
            output.Append("], ");

            // effect options
            output.Append("#Options: [[\"Delete/Move\", [\"Delete\", \"Move Back\", \"Move Forth\"], \"\"]");
            if (effect.Data.useLayers)
            {
                output.AppendFormat(", [\"Layers\", [\"All\", \"1\", \"2\", \"3\", \"1:st and 2:nd\", \"2:nd and 3:rd\"], \"{0}\"]",
                    layerEnums[(int)effect.Layer]
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

            if (effect.Data.useDecalAffect)
            {
                output.AppendFormat(", [\"Affect Gradients and Decals\", [\"Yes\", \"No\"], \"{0}\"]", effect.AffectGradientsAndDecals ? "Yes": "No");
            }

            // custom options
            for (int cfgIndex = 0; cfgIndex < effect.CustomValues.Length; cfgIndex++)
            {
                CustomEffectConfig configInfo = effect.Data.customConfigs[cfgIndex];

                if (configInfo is CustomEffectString strConfig)
                {
                    output.AppendFormat(", [\"{0}\", [", strConfig.Name);

                    for (int j = 0; j < strConfig.Options.Length; j++)
                    {
                        if (j > 0) output.Append(", ");
                        output.Append('"');
                        output.Append(strConfig.Options[j]);
                        output.Append('"');
                    }

                    output.AppendFormat("], \"{0}\"]", strConfig.Options[effect.CustomValues[cfgIndex]]);
                }

                else if (configInfo is CustomEffectInteger intConfig)
                {
                    output.AppendFormat(", [\"{0}\", [], {1}]", intConfig.Name, effect.CustomValues[cfgIndex]);
                }
            }

            // seed
            output.AppendFormat(", [\"Seed\", [], {0}]", effect.Seed);

            // end effects options
            output.Append("], ");

            // effect parameters & end effect
            output.AppendFormat("#repeats: {0}, #affectOpenAreas: {1}]", effect.Data.repeats, effect.Data.affectOpenAreas);
        }

        output.AppendFormat(
            "], #emPos: point(1, 1), #editEffect: {0}, #selectEditEffect: {0}, #mode: \"createNew\", #brushSize: 5]",
            level.Effects.Count > 0 ? 1 : 0
        );
        output.Append(newLine);

        // light data
        output.Append("[#pos: point(0, 0), #rot: 0, #sz: point(50, 70), #col: 1, #Keys: [#m1: 0, #m2: 0, #w: 0, #a: 0, #s: 0, #d: 0, #r: 0, #f: 0, #z: 0, #m: 0], #lastKeys: [#m1: 0, #m2: 0, #w: 0, #a: 0, #s: 0, #d: 0, #r: 0, #f: 0, #z: 0, #m: 0], #lastTm: 0, ");
        output.AppendFormat("#lightAngle: {0}, #flatness: {1}, ",
            (level.LightAngle / MathF.PI * 180f).ToString(CultureInfo.InvariantCulture),
            level.LightDistance.ToString(CultureInfo.InvariantCulture)
        );
        output.Append("#lightRect: rect(1000, 1000, -1000, -1000), #paintShape: \"pxl\"]");
        output.Append(newLine);

        // default medium and light type
        // otherwise, filled with useless data
        output.AppendFormat(
            "[#timeLimit: 4800, #defaultTerrain: {0}, #maxFlies: 10, #flySpawnRate: 50, #lizards: [], #ambientSounds: [], #music: \"NONE\", #tags: [], #lightType: \"{1}\", #waterDrips: 1, #lightRect: rect(0, 0, 1040, 800), #Matrix: []]",
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
        output.Append("], #selectedCamera: 0, ");

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
                    (cam.CornerAngles[j] / MathF.PI * 180f).ToString(CultureInfo.InvariantCulture),
                    cam.CornerOffsets[j].ToString(CultureInfo.InvariantCulture)
                );

                if (j < 3) output.Append(", ");
            }

            output.Append(']');
            if (i < level.Cameras.Count - 1)
                output.Append(", ");
        }
        output.Append("], #Keys: [#n: 0, #d: 0, #e: 0, #p: 0], #lastKeys: [#n: 0, #d: 0, #e: 0, #p: 0]]");
        output.Append(newLine);

        // water data
        output.AppendFormat(
            "[#waterLevel: {0}, #waterInFront: {1}, #waveLength: 60, #waveAmplitude: 5, #waveSpeed: 10]",
            level.HasWater ? level.WaterLevel : -1,
            level.IsWaterInFront ? 1 : 0
        );
        output.Append(newLine);

        // props data
        output.Append("[#props: [");

        for (int i = 0; i < level.Props.Count; i++)
        {
            if (i > 0) output.Append(", ");
            var prop = level.Props[i];
            var propInit = prop.PropInit;
            var catIndex = propInit.Category.Index;
            var propIndex = propInit.Category.Props.IndexOf(propInit);

            output.AppendFormat("[{0}, \"{1}\", point({2}, {3}), [", -prop.DepthOffset, propInit.Name, catIndex+1, propIndex+1);

            // quad data
            var quadPoints = prop.QuadPoints;
            for (int j = 0; j < 4; j++)
            {
                if (j > 0) output.Append(", ");
                output.AppendFormat("point({0}, {1})",
                    (quadPoints[j].X * 16f).ToString("0.0000", CultureInfo.InvariantCulture),
                    (quadPoints[j].Y * 16f).ToString("0.0000", CultureInfo.InvariantCulture)
                );
            }

            // prop settings
            output.Append("], [#settings: [");
            output.AppendFormat("#renderorder: {0}, #seed: {1}, #renderTime: {2}",
                prop.RenderOrder,
                prop.Seed,
                prop.RenderTime == PropRenderTime.PostEffects ? 1 : 0
            );

            if (propInit.PropFlags.HasFlag(PropFlags.CustomDepthAvailable))
                output.AppendFormat(", #customDepth: {0}", prop.CustomDepth);
            
            if (propInit.PropFlags.HasFlag(PropFlags.CustomColorAvailable))
                output.AppendFormat(", #color: {0}", prop.CustomColor + 1);
            
            if (propInit.VariationCount > 1)
                output.AppendFormat(", #variation: {0}", prop.Variation + 1);
            
            if (propInit.PropFlags.HasFlag(PropFlags.Colorize))
                output.AppendFormat(", #applyColor: {0}", prop.ApplyColor ? 1 : 0);
            
            if (propInit.Rope is not null)
            {
                output.Append(", #release: ");
                switch (prop.Rope!.ReleaseMode)
                {
                    case RopeReleaseMode.None:
                        output.Append('0');
                        break;
                    case RopeReleaseMode.Left:
                        output.Append("-1");
                        break;
                    case RopeReleaseMode.Right:
                        output.Append('1');
                        break;
                    default:
                        throw new Exception($"Invalid rope release mode for '{propInit.Name}");
                }

                if (propInit.PropFlags.HasFlag(PropFlags.CanSetThickness))
                    output.AppendFormat(", #thickness: {0}", prop.Rope!.Thickness.ToString("0.0000", CultureInfo.InvariantCulture));
            }

            // done settings
            output.Append(']');

            // save rope points
            if (prop.Rope is not null)
            {
                output.Append(", #points: [");
                var model = prop.Rope.Model!;

                for (int j = 0; j < model.SegmentCount; j++)
                {
                    if (j > 0) output.Append(", ");
                    var segPos = model.GetSmoothSegmentPos(j);

                    output.AppendFormat("point({0}, {1})",
                        (segPos.X * 20f).ToString("0.0000", CultureInfo.InvariantCulture),
                        (segPos.Y * 20f).ToString("0.0000", CultureInfo.InvariantCulture)
                    );
                }

                output.Append(']');
            }

            // done prop data
            output.Append("]]");
        }

        output.AppendFormat(
            "], #lastKeys: [#w: 0, #a: 0, #s: 0, #d: 0, #L: 0, #n: 0, #m1: 0, #m2: 0, #c: 0, #z: 0], #Keys: [#w: 0, #a: 0, #s: 0, #d: 0, #L: 0, #n: 0, #m1: 0, #m2: 0, #c: 0, #z: 0], #workLayer: {0}, #lstMsPs: point(0, 0), #pmPos: point(1, 1), #pmSavPosL: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1], #propRotation: 0, #propStretchX: 1, #propStretchY: 1, #propFlipX: 1, #propFlipY: 1, #depth: 0, #color: 0]",
            workLayer
        );
        output.Append(newLine);

        // rained-specific data
        // hopefully other leditors will ignore this because it's not on a line
        // that's used by the original editor
        /*output.AppendFormat("[#extraPropData: [");

        for (int i = 0; i < level.Props.Count; i++)
        {
            if (i > 0) output.Append(", ");
            var prop = level.Props[i];

            output.Append('[');
            bool needComma = false;

            // affine rect data
            if (prop.IsAffine)
            {
                needComma = true;

                output.Append("#rect: [");

                var rect = prop.Rect;
                output.AppendFormat("#center: point({0}, {1})",
                    (rect.Center.X * 16f).ToString("0.0000", CultureInfo.InvariantCulture),
                    (rect.Center.Y * 16f).ToString("0.0000", CultureInfo.InvariantCulture)
                );

                output.AppendFormat(", #size: point({0}, {1})",
                    (rect.Size.X * 16f).ToString("0.0000", CultureInfo.InvariantCulture),
                    (rect.Size.Y * 16f).ToString("0.0000", CultureInfo.InvariantCulture)
                );

                output.AppendFormat(", #rotation: {0}", (rect.Rotation / MathF.PI * 180f).ToString("0.0000", CultureInfo.InvariantCulture));

                output.Append(']');
            }

            // rope segment velocity data
            if (prop.Rope is not null)
            {
                var model = prop.Rope.Model!;

                if (needComma) output.Append(", ");
                needComma = true;

                output.Append("#ropeVels: [");

                for (int j = 0; j < model.SegmentCount; j++)
                {
                    if (j > 0) output.Append(", ");
                    var vel = model.GetSegmentVel(j);

                    output.AppendFormat("point({0}, {1})",
                        (vel.X * 20f).ToString("0.0000", CultureInfo.InvariantCulture),
                        (vel.Y * 20f).ToString("0.0000", CultureInfo.InvariantCulture)
                    );
                }

                output.Append(']');
            }

            // done prop data
            output.Append(']');
        }

        output.Append("]]");
        output.Append(newLine);*/

        // finish writing to txt file
        outputTxtFile.Write(output);
        outputTxtFile.Close();
    }

    public static void SaveLevelLightMap(Level level, string path)
    {
        var lightPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path) + ".png";
        using var lightMapImg = level.LightMap.GetImage();
        lightMapImg.DrawPixel(0, 0, Color.Black); // the magic black pixel
        lightMapImg.DrawPixel(lightMapImg.Width - 1, lightMapImg.Height - 1, Color.Black); // the other magic black pixel
        Raylib.ExportImage(lightMapImg, lightPath);
    }
}