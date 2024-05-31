using System.Text;
using System.Numerics;
namespace RainEd;

/// <summary>
/// Exports the geometry in a level to the format that
/// the Rain World game can read.
/// Identical to the "newMakeLevel" function from the Lingo source.
/// </summary>
class GeometryExporter
{
    private static Vector2 RadToVec(float rad)
    {
        rad = -(rad + MathF.PI / 2f);
        return new Vector2(-MathF.Cos(rad), MathF.Sin(rad));
    }

    public static string Export(Level level, string lvlName)
    {
        RainEd.Logger.Information($"saving: {lvlName}...");

        var LN = "\r\n";

        var lightAngle = RadToVec(level.LightAngle) * level.LightDistance;

        // line 1: level name
        var strOutput = new StringBuilder(lvlName);
        strOutput.Append(LN);

        // line 2: level dimensions
        var bfLf = level.BufferTilesLeft;
        var bfRt = level.BufferTilesRight;
        var bfTp = level.BufferTilesTop;
        var bfBt = level.BufferTilesBot;
        strOutput.Append($"{level.Width - bfLf - bfRt}*${level.Height - bfTp - bfBt}");

        // water
        if (level.HasWater)
        {
            strOutput.Append($"|{level.WaterLevel}|{level.IsWaterInFront}");
        }
        strOutput.Append(LN);

        // line 3: light angle
        strOutput.Append($"{lightAngle.X}*{lightAngle.Y}|0|0");
        strOutput.Append(LN);

        // line 4: camera positions
        for (int i = 0; i < level.Cameras.Count; i++)
        {
            var cam = level.Cameras[i];
            strOutput.Append($"{(cam.Position.X - bfLf) * 20f},{(cam.Position.Y - bfTp) * 20f}");

            if (i < level.Cameras.Count - 1)
                strOutput.Append('|');
        }
        strOutput.Append(LN);

        // line 5: default material
        if (level.DefaultMedium)
        {
            strOutput.Append("Border: Solid");
        }
        else
        {
            strOutput.Append("Border: Passable");
        }
        strOutput.Append(LN);

        // line 6: placed spears and rocks
        for (int x = bfLf; x < level.Width - bfRt; x++)
        {
            for (int y = bfTp; y < level.Height - bfBt; y++)
            {
                if (level.Layers[0, x, y].Has(LevelObject.Rock))
                    strOutput.Append($"0,${x-bfLf+1},{y-bfTp+1}|");
                else if (level.Layers[0, x, y].Has(LevelObject.Spear))
                    strOutput.Append($"1,${x-bfLf+1},{y-bfTp+1}|");
            }
        }

        strOutput.Append(LN);
        strOutput.Append(LN);
        strOutput.Append(LN);
        strOutput.Append(LN);
        strOutput.Append('0'); // connmap
        strOutput.Append(LN); // connmap
        strOutput.Append(LN); // line for baked AI info

        // geometry array
        for (int x = bfLf; x < level.Width - bfRt; x++)
        {
            for (int y = bfTp; y < level.Height - bfBt; y++)
            {
                var cell = level.Layers[0, x, y];

                // first, write geo data
                switch (cell.Geo)
                {
                    // wall
                    case GeoType.Solid:
                        strOutput.Append('1');
                        break;
                    
                    // slopes
                    case GeoType.SlopeRightUp:
                    case GeoType.SlopeLeftUp:
                    case GeoType.SlopeRightDown:
                    case GeoType.SlopeLeftDown:
                        strOutput.Append('2');
                        break;

                    // floor
                    case GeoType.Platform:
                        strOutput.Append('3');
                        break;

                    // shortcut entrance
                    case GeoType.ShortcutEntrance:
                        strOutput.Append("4,3");
                        break;

                    // air
                    default:
                        strOutput.Append('0');
                        break;
                }

                // then, write objects
                if (cell.Has(LevelObject.VerticalBeam) && cell.Geo != GeoType.Solid)
                    strOutput.Append(",1");

                if (cell.Has(LevelObject.HorizontalBeam) && cell.Geo != GeoType.Solid)
                    strOutput.Append(",2");

                if (cell.Has(LevelObject.Shortcut))
                    strOutput.Append(",3");

                // room exit
                if (cell.Has(LevelObject.Entrance))
                    strOutput.Append(",4");

                // hiding hole
                if (cell.Has(LevelObject.CreatureDen))
                    strOutput.Append(",5");

                // WHAM
                if (cell.Has(LevelObject.WhackAMoleHole))
                    strOutput.Append(",9");

                // scavenger hole
                if (cell.Has(LevelObject.ScavengerHole))
                    strOutput.Append(",12");

                // hive!
                if (cell.Has(LevelObject.Hive) && cell.Geo == GeoType.Air && level.Layers[0, x, y+1].Geo == GeoType.Solid)
                    strOutput.Append(",7");
                
                // waterfall!
                if (cell.Has(LevelObject.Waterfall))
                    strOutput.Append(",8");

                // garbage hole
                if (cell.Has(LevelObject.GarbageWorm))
                    strOutput.Append(",10");

                // worm grass
                if (cell.Has(LevelObject.Waterfall))
                    strOutput.Append(",11");
                
                // wall behind
                if (cell.Geo == GeoType.Air && level.Layers[1,x,y].Geo == GeoType.Solid)
                    strOutput.Append(",6");
                
                strOutput.Append('|');
            }
        }

        return strOutput.ToString();
    }

    public static void ExportToFile(Level level, string path)
    {
        var lvlName = Path.GetFileNameWithoutExtension(path);
        RainEd.Logger.Information($"saving: {lvlName}...");
        File.WriteAllText(path, Export(level, lvlName));
        RainEd.Logger.Information($"saved22: {lvlName}...");
    }
}