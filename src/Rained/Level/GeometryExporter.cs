using System.Text;
using System.Numerics;
using System.Globalization;
namespace RainEd;

/// <summary>
/// Work In Progress
/// (aka, i'll never finish this because this is stupid
/// and is taking too long to get right)
/// 
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

    [Flags]
    enum PlayObject : uint
    {
        None = 0,
        VerticalBeam = 1 << 0,
        HorizontalBeam = 1 << 1,
        ShortcutType = 1 << 2,
        Entrance = 1 << 3,
        CreatureDen = 1 << 4,
        WallBehind = 1 << 5,
        Hive = 1 << 6,
        Waterfall = 1 << 7,
        // 9 is skipped
        GarbageWorm = 1 << 9,
        WormGrass = 1 << 10
    }

    // exporting converts the geo data to a "play matrix",
    // basically just processing the geo array to the IDs
    // used by the rain world game
    struct PlayMatrixCell
    {
        public GeoType geo;
        public LevelObject objs;
        public bool hasWallBehind;
    }

    static PlayMatrixCell[,] ChangeToPlayMatrix(Level level)
    {
        var playMatrix = new PlayMatrixCell[level.Width, level.Height];
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                var l1Cell = level.Layers[0,x,y];
                var l2Cell = level.Layers[1,x,y];

                // cell[1][1] is playGeo
                // cell[1][2] is objects
                var playGeo = l1Cell.Geo;
                var objects = l1Cell.Objects;

                // cell[2][1]
                bool hasWallBehind = (l2Cell.Geo == GeoType.Solid || l2Cell.Geo == GeoType.Glass) && !l2Cell.Has(LevelObject.Crack);

                if (playGeo == GeoType.Glass)
                {
                    playGeo = GeoType.Solid;
                    // cell[1][2].add(8)
                }

                if (objects.HasFlag(LevelObject.Entrance) || objects.HasFlag(LevelObject.CreatureDen) || objects.HasFlag(LevelObject.WhackAMoleHole) || objects.HasFlag(LevelObject.ScavengerHole))
                {
                    if (!objects.HasFlag(LevelObject.Shortcut))
                        objects |= LevelObject.Shortcut;
                }

                if (objects.HasFlag(LevelObject.Crack))
                {
                    playGeo = GeoType.Air;
                    if (y > 0)
                    {
                        if (level.Layers[0,x,y-1].Geo == GeoType.Air &&
                            level.Layers[0,x-1,y].Geo == GeoType.Solid &&
                            !level.Layers[0,x-1,y].Has(LevelObject.Crack) &&
                            level.Layers[0,x+1,y].Geo == GeoType.Solid &&
                            !level.Layers[0,x+1,y].Has(LevelObject.Crack)
                        )
                        {
                            playGeo = GeoType.Platform;
                        }
                    }
                }

                playMatrix[x,y] = new PlayMatrixCell()
                {
                    geo = playGeo,
                    objs = objects,
                    hasWallBehind = hasWallBehind
                };
            }
        }

        return playMatrix;
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
        strOutput.Append($"{level.Width - bfLf - bfRt}*{level.Height - bfTp - bfBt}");

        // water
        if (level.HasWater)
        {
            strOutput.Append($"|{level.WaterLevel}|{level.IsWaterInFront}");
        }
        strOutput.Append(LN);

        // line 3: light angle
        // drizzle outputs 0.0000 where c# would sometimes output -0.0000,
        // so, i just do this lel
        var lightAngleX = (MathF.Abs(lightAngle.X - 0f) < 0.00005) ? "0.0000" : lightAngle.X.ToString("0.0000", CultureInfo.InvariantCulture);
        var lightAngleY = (MathF.Abs(lightAngle.Y - 0f) < 0.00005) ? "0.0000" : lightAngle.Y.ToString("0.0000", CultureInfo.InvariantCulture);
        strOutput.Append($"{lightAngleX}*{lightAngleY}|0|0");
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
        var playMatrix = ChangeToPlayMatrix(level);
        for (int x = bfLf; x < level.Width - bfRt; x++)
        {
            for (int y = bfTp; y < level.Height - bfBt; y++)
            {
                var cell = playMatrix[x,y];

                switch (cell.geo)
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
                if (cell.objs.HasFlag(LevelObject.VerticalBeam) && cell.geo != GeoType.Solid)
                    strOutput.Append(",1");

                if (cell.objs.HasFlag(LevelObject.HorizontalBeam) && cell.geo != GeoType.Solid)
                    strOutput.Append(",2");
                
                if (cell.objs.HasFlag(LevelObject.Shortcut))
                    strOutput.Append(",3");

                // room exit
                if (cell.objs.HasFlag(LevelObject.Entrance))
                    strOutput.Append(",4");

                // hiding hole
                if (cell.objs.HasFlag(LevelObject.CreatureDen))
                    strOutput.Append(",5");

                // WHAM
                if (cell.objs.HasFlag(LevelObject.WhackAMoleHole))
                    strOutput.Append(",9");

                // scavenger hole
                if (cell.objs.HasFlag(LevelObject.ScavengerHole))
                    strOutput.Append(",12");

                // hive!
                if (cell.objs.HasFlag(LevelObject.Hive))
                {
                    if (level.Layers[0,x,y].Geo == GeoType.Solid && (y < level.Height - 1 || level.Layers[0,x,y+1].Geo == GeoType.Solid))
                    {
                        strOutput.Append(",7");
                    }
                }

                // waterfall!
                if (cell.objs.HasFlag(LevelObject.Waterfall))
                    strOutput.Append(",8");

                // garbage hole
                if (cell.objs.HasFlag(LevelObject.GarbageWorm))
                    strOutput.Append(",10");

                // worm grass
                if (cell.objs.HasFlag(LevelObject.Waterfall))
                    strOutput.Append(",11");
                
                // wall behind
                if (cell.geo != GeoType.Solid && cell.hasWallBehind)
                    strOutput.Append(",6");
                
                strOutput.Append('|');
            }
        }

        strOutput.Append(LN);

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