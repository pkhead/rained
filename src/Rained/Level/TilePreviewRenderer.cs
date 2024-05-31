using System.Numerics;
using RainEd.Tiles;
using Raylib_cs;
namespace RainEd;

class TileRenderer
{
    private readonly LevelEditRender renderInfo;

    public TileRenderer(LevelEditRender renderInfo)
    {
        this.renderInfo = renderInfo;
    }
    
    private HashSet<int> wasRendered = [];

    private static int Pair2(int a, int b)
    {
        return a >= b ? (a * a) + a + b : (b * b) + a;
    }

    private static int Pair3(int a, int b, int c)
    {
        return Pair2(Pair2(a, b), c);
    }

    public void Render(int layer, int alpha)
    {
        Raylib.BeginShaderMode(renderInfo.TilePreviewShader);

        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y);
        var level = RainEd.Instance.Level;

        var gfxProvider = RainEd.Instance.AssetGraphics;
        var drawColor = new Color(255, 255, 255, alpha);

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
                }*/
            }
        }

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