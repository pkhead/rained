namespace Rained.Autotiles;
using System.Numerics;
using Raylib_cs;
using LevelData;

class AutotileRectBuilder(Autotile autotile, Vector2i startPos) : IAutotileInputBuilder
{
    private readonly Autotile autotile = autotile;
    public Autotile Autotile => autotile;
    
    private readonly Vector2i startPos = startPos;
    private Vector2i endPos = startPos;

    private bool sizeInit = true;
    private Vector2i curMin = new();
    private Vector2i curMax = new();
    private bool isSizeValid = true;

    private void GetRectBounds(int startX, int startY, int endX, int endY, out int minX, out int minY, out int maxX, out int maxY)
    {
        if (autotile.ConstrainToSquare)
        {
            var dx = endX - startX;
            var dy = endY - startY;

            int size = Math.Max(Math.Abs(dx), Math.Abs(dy));

            endX = startX + size * (dx >= 0 ? 1 : -1);
            endY = startY + size * (dy >= 0 ? 1 : -1);

            minX = Math.Min(startX, endX);
            maxX = Math.Max(startX, endX);
            minY = Math.Min(startY, endY);
            maxY = Math.Max(startY, endY);
        }
        else
        {
            minX = Math.Min(startX, endX);
            minY = Math.Min(startY, endY);
            maxX = Math.Max(startX, endX);
            maxY = Math.Max(startY, endY);
        }
    }

    public void Update()
    {
        var window = RainEd.Instance.LevelView;
        endPos = new Vector2i(window.MouseCx, window.MouseCy);

        GetRectBounds(
            startPos.X, startPos.Y, endPos.X, endPos.Y,
            out var minX, out var minY, out var maxX, out var maxY
        );

        var minVec = new Vector2i(minX, minY);
        var maxVec = new Vector2i(maxX, maxY);
        if (sizeInit || minVec != curMin || maxVec != curMax)
        {
            sizeInit = false;
            isSizeValid = autotile.VerifySize(minVec, maxVec);
            curMin = minVec;
            curMax = maxVec;
        }

        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                x: minX * Level.TileSize,
                y: minY * Level.TileSize,
                width: (maxX - minX + 1) * Level.TileSize,
                height: (maxY - minY + 1) * Level.TileSize
            ),
            1f / window.ViewZoom,
            isSizeValid ? Color.White : Color.Red
        );
    }

    public void Finish(int layer, bool force, bool geometry)
    {
        if (isSizeValid)
        {
            GetRectBounds(
                startPos.X, startPos.Y, endPos.X, endPos.Y,
                out var minX, out var minY, out var maxX, out var maxY
            );

            autotile.TileRect(
                layer,
                rectMin: new Vector2i(minX, minY),
                rectMax: new Vector2i(maxX, maxY),
                force, geometry
            );
        }
    }
}