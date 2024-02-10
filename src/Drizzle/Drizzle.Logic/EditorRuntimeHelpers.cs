using System.IO;
using Drizzle.Lingo.Runtime;
using Drizzle.Ported;

namespace Drizzle.Logic;

public static class EditorRuntimeHelpers
{
    public static void RunStartup(LingoRuntime runtime)
    {
        var startUp = runtime.CreateScript<startUp>();

        startUp.exitframe();
    }

    public static void RunLoadLevel(LingoRuntime runtime, string filePath)
    {
        var abs = Path.GetFullPath(filePath);

        var withoutExt = Path.Combine(
            Path.GetDirectoryName(abs)!,
            Path.GetFileNameWithoutExtension(abs));

        runtime.CreateScript<loadLevel>().loadlevel(withoutExt, new LingoNumber(1));
    }

    // Effectively afaMvLvlEdit in spelrelaterat
    public static TileGeometry GetTileGeomBordered(LingoRuntime runtime, Vector2i vec, int layer)
    {
        var mv = runtime.MovieScript();
        var size = (LingoPoint) mv.gLOprops.size;
        if (vec.X < 1 || vec.Y < 1 || vec.X > size.loch || vec.Y > size.locv)
            return TileGeometry.SolidWall;

        return (TileGeometry)(int)(LingoNumber)mv.gLEProps.matrix[vec.X][vec.Y][layer][1];
    }
}