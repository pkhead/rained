using System.Numerics;
using RainEd.Tiles;
using Raylib_cs;
namespace RainEd;

class TileRenderer
{
    struct TileRender(int x, int y, int layer, Tile init)
    {
        public int X = x;
        public int Y = y;
        public int Layer = layer;
        public Tile TileInit = init;
    }

    private readonly LevelEditRender renderInfo;
    private readonly List<CellPosition> dirtyHeads = [];
    private readonly HashSet<int> wasRendered = [];
    private readonly List<TileRender> tileRenders = [];

    public TileRenderer(LevelEditRender renderInfo)
    {
        this.renderInfo = renderInfo;
        ReloadLevel();
    }

    public void ReloadLevel()
    {
        for (int x = 0; x < RainEd.Instance.Level.Width; x++)
        {
            for (int y = 0; y < RainEd.Instance.Level.Height; y++)
            {
                dirtyHeads.Add(new CellPosition(x, y, 0));
                dirtyHeads.Add(new CellPosition(x, y, 1));
                dirtyHeads.Add(new CellPosition(x, y, 2));
            }
        }
    }

    public void Invalidate(int x, int y, int layer)
    {
        var pos = new CellPosition(x, y, layer);
        if (!dirtyHeads.Contains(pos))
            dirtyHeads.Add(pos);
    }

    private static int Pair2(int a, int b)
    {
        return a >= b ? (a * a) + a + b : (b * b) + a;
    }

    private static int Pair3(int a, int b, int c)
    {
        return Pair2(Pair2(a, b), c);
    }

    private int GetTileRender(int x, int y, int layer)
    {
        for (int i = 0; i < tileRenders.Count; i++)
        {
            var tile = tileRenders[i];
            if (tile.X == x && tile.Y == y && tile.Layer == layer)
            {
                return i;
            }
        }
        
        return -1;
    }

    /// <summary>
    /// Render tiles using their preview graphics
    /// </summary>
    public void PreviewRender(int layer, int alpha)
    {
        var level = RainEd.Instance.Level;
        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y);
        var drawTileHeads = renderInfo.ViewTileHeads;

        for (int x = Math.Max(0, viewL); x < Math.Min(level.Width, viewR); x++)
            {
                for (int y = Math.Max(0, viewT); y < Math.Min(level.Height, viewB); y++)
                {
                    ref var cell = ref level.Layers[layer, x, y];
                    if (!cell.HasTile()) continue;

                    Tile? tile;
                    int tx;
                    int ty;

                    if (cell.TileHead is not null)
                    {
                        tile = cell.TileHead;
                        tx = x;
                        ty = y;
                    }
                    else
                    {
                        tile = level.Layers[cell.TileLayer, cell.TileRootX, cell.TileRootY].TileHead;
                        tx = cell.TileRootX;
                        ty = cell.TileRootY;
                    }

                    // detached tile body
                    // probably caused from comms move level tool,
                    // which does not correct tile pointers
                    if (tile == null)
                    {
                        Raylib.DrawRectangleV(new Vector2(x, y) * Level.TileSize, Vector2.One * Level.TileSize, Color.Red);
                        Raylib.DrawRectangleV(new Vector2(x + 0.5f, y) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                        Raylib.DrawRectangleV(new Vector2(x, y + 0.5f) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                        continue;
                    }

                    var tileLeft = tx - tile.CenterX;
                    var tileTop = ty - tile.CenterY;
                    var previewTexture = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile);
                    var col = previewTexture is null ? Color.White : tile.Category.Color;

                    var srcRect = previewTexture is not null
                        ? new Rectangle((x - tileLeft) * 16, (y - tileTop) * 16, 16, 16)
                        : new Rectangle((x - tileLeft) * 2, (y - tileTop) * 2, 2, 2); 

                    Raylib.DrawTexturePro(
                        previewTexture ?? RainEd.Instance.PlaceholderTexture,
                        srcRect,
                        new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                        Vector2.Zero,
                        0f,
                        new Color(col.R, col.G, col.B, alpha)
                    );

                    // highlight tile head
                    if (cell.TileHead is not null && drawTileHeads)
                    {
                        Raylib.DrawRectangle(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            new Color(col.R, col.G, col.B, (int)(alpha * 0.2f))  
                        );

                        Raylib.DrawLineV(
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            col
                        );

                        Raylib.DrawLineV(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            col
                        );
                    }
                }
            }
    }

    /// <summary>
    /// Render tiles using their render graphics
    /// </summary>
    public void Render(int layer, int alpha)
    {
        var level = RainEd.Instance.Level;

        // first, update the tile render list
        foreach (var cellPos in dirtyHeads)
        {
            var i = GetTileRender(cellPos.X, cellPos.Y, cellPos.Layer);

            // if tile render already exists
            if (i >= 0)
            {
                var newHead = level.Layers[cellPos.Layer, cellPos.X, cellPos.Y].TileHead;
                
                // if the tile was removed
                if (newHead is null)
                {
                    tileRenders.RemoveAt(i);
                    RainEd.Logger.Information("Remove tile render");
                }

                else
                {
                    tileRenders[i] = new TileRender(cellPos.X, cellPos.Y, cellPos.Layer, newHead);
                }
            }

            // if tile render did not already exist
            else
            {
                var tileInit = level.Layers[cellPos.Layer, cellPos.X, cellPos.Y].TileHead;
                if (tileInit != null)
                {
                    tileRenders.Add(new TileRender(cellPos.X, cellPos.Y, cellPos.Layer, tileInit));
                    RainEd.Logger.Information("Add tile render");
                }
            }
        }

        dirtyHeads.Clear();

        // draw the tile renders
        Raylib.BeginShaderMode(renderInfo.TilePreviewShader);
        var gfxProvider = RainEd.Instance.AssetGraphics;
        var drawColor = new Color(255, 255, 255, alpha);

        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y);

        foreach (var tileRender in tileRenders)
        {
            if (tileRender.Layer != layer) continue;

            var init = tileRender.TileInit;

            var rectPos = new Vector2(tileRender.X - init.CenterX - init.BfTiles, tileRender.Y - init.CenterY - init.BfTiles);
            var rectSize = new Vector2(init.Width + init.BfTiles * 2, init.Height + init.BfTiles * 2);

            // if levelRec is within screen bounds?
            if (
                rectPos.X < viewR &&
                rectPos.Y < viewB &&
                rectPos.X + rectSize.X > viewL &&
                rectPos.Y + rectSize.Y > viewT
            )
            {
                var tex = gfxProvider.GetTileTexture(init.Name);
                var dstRec = new Rectangle(rectPos * Level.TileSize, rectSize * Level.TileSize);

                // if the tile texture was not found, draw a
                // placeholder graphic
                if (tex is null)
                {
                    Raylib.EndShaderMode();
                    var srcRec = new Rectangle(0f, 0f, 2f, 2f);
                    Raylib.DrawTexturePro(RainEd.Instance.PlaceholderTexture, srcRec, dstRec, Vector2.Zero, 0f, Color.White);
                    Raylib.BeginShaderMode(renderInfo.PropPreviewShader);
                }
                else
                {
                    // draw the tile sublayers from back to front
                    for (int l = init.LayerCount-1; l >= 0; l--)
                    {
                        var srcRec = GetGraphicSublayer(init, l, 0);
                        Raylib.DrawTexturePro(tex, srcRec, dstRec, Vector2.Zero, 0f, drawColor);
                    }
                }
            }
        }

        // highlight tile heads
        if (renderInfo.ViewTileHeads)
        {
            foreach (var tileRender in tileRenders)
            {
                if (tileRender.Layer != layer) continue;
                var x = tileRender.X;
                var y = tileRender.Y;
                var col = tileRender.TileInit.Category.Color;

                Raylib.DrawRectangle(
                    x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                    new Color(col.R, col.G, col.B, (int)(alpha * 0.2f))  
                );

                Raylib.DrawLineV(
                    new Vector2(x, y) * Level.TileSize,
                    new Vector2(x+1, y+1) * Level.TileSize,
                    col
                );

                Raylib.DrawLineV(
                    new Vector2(x+1, y) * Level.TileSize,
                    new Vector2(x, y+1) * Level.TileSize,
                    col
                );
            }

        }

        /*int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y);

        int minX = Math.Max(0, viewL);
        int maxX = Math.Min(level.Width, viewR);
        int minY = Math.Max(0, viewT);
        int maxY = Math.Min(level.Height, viewB);
        
        int viewW = maxX - minX;
        int viewH = maxY - minY;

        // the wasRendererd hashset keeps track of which tile heads were already
        // rendered
        wasRendered.Clear();

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                // get location of tile head
                ref var cell = ref level.Layers[layer, x, y];
                if (!cell.HasTile()) continue;
                var tileOrigin = level.GetTileHead(layer, x, y);

                // check that the location is valid and that
                // the location was not already rendered. if both pass,
                // the location will be added to the wasRenderered
                // hashset.
                int pair;
                if (tileOrigin.Layer == -1 || !wasRendered.Add(pair = Pair3(tileOrigin.X, tileOrigin.Y, tileOrigin.Layer))) continue;
                
                // get the tile head data
                var tile = level.Layers[tileOrigin.Layer, tileOrigin.X, tileOrigin.Y].TileHead;
                if (tile is null) continue;

                var tex = gfxProvider.GetTileTexture(tile.Name);
                var dstRec = new Rectangle(
                    new Vector2(tileOrigin.X - tile.CenterX - tile.BfTiles, tileOrigin.Y - tile.CenterY - tile.BfTiles) * Level.TileSize,
                    new Vector2(tile.Width + tile.BfTiles * 2, tile.Height + tile.BfTiles * 2) * Level.TileSize
                );

                // if the tile texture was not found, draw a
                // placeholder graphic
                if (tex is null)
                {
                    Raylib.EndShaderMode();
                    var srcRec = new Rectangle(0f, 0f, 2f, 2f);
                    Raylib.DrawTexturePro(RainEd.Instance.PlaceholderTexture, srcRec, dstRec, Vector2.Zero, 0f, Color.White);
                    Raylib.BeginShaderMode(renderInfo.PropPreviewShader);
                }
                else
                {
                    // draw the tile sublayers from back to front
                    for (int l = tile.LayerCount-1; l >= 0; l--)
                    {
                        var srcRec = GetGraphicSublayer(tile, l, 0);
                        Raylib.DrawTexturePro(tex, srcRec, dstRec, Vector2.Zero, 0f, drawColor);
                    }
                }




                /*Tiles.Tile? tile;
                int tx;
                int ty;

                if (cell.TileHead is not null)
                {
                    tile = cell.TileHead;
                    tx = x;
                    ty = y;
                }
                else
                {
                    tile = Level.Layers[cell.TileLayer, cell.TileRootX, cell.TileRootY].TileHead;
                    tx = cell.TileRootX;
                    ty = cell.TileRootY;
                }

                // detached tile body
                // probably caused from comms move level tool,
                // which does not correct tile pointers
                if (tile == null)
                {
                    Raylib.DrawRectangleV(new Vector2(x, y) * Level.TileSize, Vector2.One * Level.TileSize, Color.Red);
                    Raylib.DrawRectangleV(new Vector2(x + 0.5f, y) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                    Raylib.DrawRectangleV(new Vector2(x, y + 0.5f) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                    continue;
                }

                var tileLeft = tx - tile.CenterX;
                var tileTop = ty - tile.CenterY;
                var previewTexture = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile);
                var col = previewTexture is null ? Color.White : tile.Category.Color;

                var srcRect = previewTexture is not null
                    ? new Rectangle((x - tileLeft) * 16, (y - tileTop) * 16, 16, 16)
                    : new Rectangle((x - tileLeft) * 2, (y - tileTop) * 2, 2, 2); 

                Raylib.DrawTexturePro(
                    previewTexture ?? RainEd.Instance.PlaceholderTexture,
                    srcRect,
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                    Vector2.Zero,
                    0f,
                    new Color(col.R, col.G, col.B, alpha)
                );

                // highlight tile head
                if (cell.TileHead is not null && ViewTileHeads)
                {
                    Raylib.DrawRectangle(
                        x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                        new Color(col.R, col.G, col.B, (int)(alpha * 0.2f))  
                    );

                    Raylib.DrawLineV(
                        new Vector2(x, y) * Level.TileSize,
                        new Vector2(x+1, y+1) * Level.TileSize,
                        col
                    );

                    Raylib.DrawLineV(
                        new Vector2(x+1, y) * Level.TileSize,
                        new Vector2(x, y+1) * Level.TileSize,
                        col
                    );
                }
            }
        }*/

        Raylib.EndShaderMode();
    }

    private static Rectangle GetGraphicSublayer(Tile tile, int sublayer, int variation)
    {
        var width = (tile.Width + tile.BfTiles * 2) * 20;
        var height = (tile.Height + tile.BfTiles * 2) * 20;

        return new Rectangle(
            width * variation,
            height * sublayer + tile.ImageYOffset,
            width, height
        );
    }
}