namespace RainEd;
using System.Numerics;
using Raylib_cs;
using Autotiles;

class AutotileRectBuilder(Autotiles.Autotile autotile, Vector2i startPos) : IAutotileInputBuilder
{
    private readonly Autotile autotile = autotile;
    private readonly Vector2i startPos = startPos;
    private Vector2i endPos = startPos;

    public void Update()
    {
        var window = RainEd.Instance.LevelView;
        endPos = new Vector2i(window.MouseCx, window.MouseCy);

        var minX = Math.Min(startPos.X, endPos.X);
        var minY = Math.Min(startPos.Y, endPos.Y);
        var maxX = Math.Max(startPos.X, endPos.X);
        var maxY = Math.Max(startPos.Y, endPos.Y);

        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                x: minX * Level.TileSize,
                y: minY * Level.TileSize,
                width: (maxX - minX + 1) * Level.TileSize,
                height: (maxY - minY + 1) * Level.TileSize
            ),
            1f / window.ViewZoom,
            Color.White
        );
    }

    public void Finish(int layer, bool force, bool geometry)
    {
        var minX = Math.Min(startPos.X, endPos.X);
        var minY = Math.Min(startPos.Y, endPos.Y);
        var maxX = Math.Max(startPos.X, endPos.X);
        var maxY = Math.Max(startPos.Y, endPos.Y);

        autotile.TileRect(
            layer,
            rectMin: new Vector2i(minX, minY),
            rectMax: new Vector2i(maxX, maxY),
            force, geometry
        );
    }
}