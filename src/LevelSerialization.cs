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

        Vector2 levelSize = (Vector2) levelProperties.fields["size"];
        Lingo.List extraTiles = (Lingo.List) levelProperties.fields["extraTiles"];

        var level = new Level(editor, (int)levelSize.X, (int)levelSize.Y)
        {
            BufferTilesLeft = (int) extraTiles.values[0],
            BufferTilesTop = (int) extraTiles.values[1],
            BufferTilesRight = (int) extraTiles.values[2],
            BufferTilesBot = (int) extraTiles.values[3]
        };
        
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

        return level;
    }
}