using System.Numerics;
using Raylib_cs;
namespace RainEd;

public static class LevelSerialization
{
    public static Level Load(RainEd editor, string path)
    {
        var levelData = File.ReadAllLines(path);
        var lingoParser = new Lingo.LingoParser();
           
        // obtain level data from lines
        Lingo.List levelGeometry = (Lingo.List)
            (lingoParser.Read(levelData[0]) ?? throw new Exception("No geometry data"));
        
        Lingo.List levelTileData = (Lingo.List)
            (lingoParser.Read(levelData[1]) ?? throw new Exception("No tile data"));

        Lingo.List levelLightData = (Lingo.List)
            (lingoParser.Read(levelData[3]) ?? throw new Exception("No light data"));
        
        Lingo.List levelMiscData = (Lingo.List)
            (lingoParser.Read(levelData[4]) ?? throw new Exception("No misc data"));
        
        Lingo.List levelProperties = (Lingo.List)
            (lingoParser.Read(levelData[5]) ?? throw new Exception("No properties"));
        
        Lingo.List? levelCameraData = (Lingo.List?) lingoParser.Read(levelData[6]);
        Lingo.List? levelWaterData = (Lingo.List?) lingoParser.Read(levelData[7]);

        // get level dimensions
        Vector2 levelSize = (Vector2) levelProperties.fields["size"];
        Lingo.List extraTiles = (Lingo.List) levelProperties.fields["extraTiles"];

        var level = new Level(editor, (int)levelSize.X, (int)levelSize.Y)
        {
            BufferTilesLeft = (int) extraTiles.values[0],
            BufferTilesTop = (int) extraTiles.values[1],
            BufferTilesRight = (int) extraTiles.values[2],
            BufferTilesBot = (int) extraTiles.values[3],
            DefaultMedium = (int) levelMiscData.fields["defaultTerrain"] != 0
        };

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

                            if (!editor.TileDatabase.HasTile(name))
                                throw new Exception($"Unrecognized tile '{name}'");
                            
                            var tile = editor.TileDatabase.GetTileFromName(name);
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
                var quadsList = (Lingo.List) (quadsListData ?? throw new NullReferenceException());
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
            level.LightMap = img;
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

        return level;
    }
}