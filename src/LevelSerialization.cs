using System.Numerics;
namespace RainEd;

public static class LevelSerialization
{
    public static Level Load(RainEd editor, string path)
    {
        var parser = new Lingo.Parser(new StreamReader(path));
        List<List<object>> dataTables = parser.Read();

        List<Lingo.List> levelData = dataTables[0].Cast<Lingo.List>().ToList();

        Lingo.List levelGeometry = levelData[0];
        Lingo.List levelProperties = levelData[5];
        Lingo.List levelCameraData = levelData[6];

        Vector2 levelSize = (Vector2) levelProperties.fields["size"];
        Lingo.List extraTiles = (Lingo.List) levelProperties.fields["extraTiles"];

        var level = new Level(editor, (int)levelSize.X, (int)levelSize.Y)
        {
            BufferTilesLeft = (int) extraTiles.values[0],
            BufferTilesTop = (int) extraTiles.values[1],
            BufferTilesRight = (int) extraTiles.values[2],
            BufferTilesBot = (int) extraTiles.values[3]
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
        Lingo.List levelTileData = levelData[1];
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
        var camerasList = (Lingo.List) levelCameraData.fields["cameras"];
        foreach (Vector2 cameraPos in camerasList.values.Cast<Vector2>())
        {
            // unsure why X unit scale is different than Y unit scale
            level.Cameras.Add(new Camera(new Vector2(cameraPos.X / 20f, cameraPos.Y / 30f)));
        }

        return level;
    }
}