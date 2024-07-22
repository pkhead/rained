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
    private readonly HashSet<CellPosition> dirtyHeads = [];
    private readonly HashSet<int> wasRendered = [];
    private readonly List<TileRender> tileRenders = [];

    public TileRenderer(LevelEditRender renderInfo)
    {
        this.renderInfo = renderInfo;
        ReloadLevel();
    }

    public void ReloadLevel()
    {
        dirtyHeads.Clear();
        tileRenders.Clear();

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
        dirtyHeads.Add(pos);
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

    private void UpdateRenderList()
    {
        var level = RainEd.Instance.Level;

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
                }
            }
        }

        // sort tile renders by draw index
        if (dirtyHeads.Count > 0)
        {
            tileRenders.Sort(static (TileRender a, TileRender b) => {
                var w = RainEd.Instance.Level.Height;
                return (a.X * w + a.Y) - (b.X * w + b.Y);
            });
        }

        dirtyHeads.Clear();
    }

    /// <summary>
    /// Render tiles using their preview graphics
    /// </summary>
    public void PreviewRender(int layer, int alpha)
    {
        var level = RainEd.Instance.Level;
        var drawTileHeads = renderInfo.ViewTileHeads;
        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y);

        // optimized method - assumes all tile bodies are within the bounds
        // of the tile head. this is the expected condition, but if the
        // user wishes, they can use the slower rendering method, which is
        // covered in the else branch.
        if (RainEd.Instance.Preferences.OptimizedTilePreviews)
        {
            UpdateRenderList();

            // draw the tile renders
            var gfxProvider = RainEd.Instance.AssetGraphics;

            foreach (var tileRender in tileRenders)
            {
                var init = tileRender.TileInit;
                if (tileRender.Layer != layer && init.HasSecondLayer && tileRender.Layer+1 != layer) continue;

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
                    var previewTextureFound = gfxProvider.GetTilePreviewTexture(init, out var previewTexture, out var previewSrcRect);
                    var col = previewTexture is null ? Color.White : init.Category.Color;
                    var drawColor = new Color(col.R, col.G, col.B, alpha);

                    // could not find the texture for the given tile
                    if (previewTexture is null)
                    {
                        var srcRec = new Rectangle(-0f, -0f, init.Width * 2f, init.Height * 2f);
                        var dstRec = new Rectangle(
                            (tileRender.X - init.CenterX) * Level.TileSize,
                            (tileRender.Y - init.CenterY) * Level.TileSize,
                            init.Width * Level.TileSize,
                            init.Height * Level.TileSize
                        );
                        Raylib.BeginShaderMode(Shaders.UvRepeatShader);
                        Raylib.DrawTexturePro(RainEd.Instance.PlaceholderTexture, srcRec, dstRec, Vector2.Zero, 0f, Color.White);
                        Raylib.EndShaderMode();
                        continue;
                    }
                    
                    // render a part of the texture for each tile within the
                    // tile bounds
                    for (int x = 0; x < init.Width; x++)
                    {
                        int gx = tileRender.X - init.CenterX + x;
                        for (int y = 0; y < init.Height; y++)
                        {
                            int gy = tileRender.Y - init.CenterY + y;
                            if (!level.IsInBounds(gx, gy)) continue;

                            for (int l = Math.Min(2, tileRender.Layer + (init.HasSecondLayer?1:0)); l >= tileRender.Layer; l--)
                            {
                                if (l != layer) continue;
                                
                                var rqArr = l == tileRender.Layer ? init.Requirements : init.Requirements2;
                                if (rqArr[x,y] == -1) continue;
                                
                                var cell = level.Layers[l, gx, gy];
                                bool isTileRoot = gx == tileRender.X && gy == tileRender.Y && l == tileRender.Layer;

                                // handle detached tile bodies.
                                // probably caused from comms move level tool,
                                // which does not correct tile pointers.
                                // draws a red checkerboard
                                if (!isTileRoot && cell.HasTile() && cell.TileHead is null &&
                                    (!level.IsInBounds(cell.TileRootX, cell.TileRootY) ||
                                    level.Layers[cell.TileLayer, cell.TileRootX, cell.TileRootY].TileHead is null))
                                {
                                    Raylib.DrawRectangleV(new Vector2(gx, gy) * Level.TileSize, Vector2.One * Level.TileSize, Color.Red);
                                    Raylib.DrawRectangleV(new Vector2(gx + 0.5f, gy) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                                    Raylib.DrawRectangleV(new Vector2(gx, gy + 0.5f) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                                    continue;
                                }
                                
                                // render the tile if the tile body belongs to the tile head
                                if (
                                    isTileRoot ||
                                    (cell.TileRootX == tileRender.X && cell.TileRootY == tileRender.Y && cell.TileLayer == tileRender.Layer)
                                )
                                {
                                    var srcRect = previewSrcRect!.Value;
                                    Raylib.DrawTexturePro(
                                        previewTexture,
                                        new Rectangle(srcRect.X + x * 16f, srcRect.Y + y * 16f, 16f, 16f),
                                        new Rectangle(gx * Level.TileSize, gy * Level.TileSize, Level.TileSize, Level.TileSize),
                                        Vector2.Zero,
                                        0f,
                                        drawColor
                                    );
                                }
                            }
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
        }

        // thorough tile preview rendering method
        else
        {
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
                    RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile, out var previewTexture, out var previewRect);
                    var col = previewTexture is null ? Color.White : tile.Category.Color;

                    var srcRect = previewTexture is not null
                        ? new Rectangle(previewRect!.Value.X + (x - tileLeft) * 16, previewRect!.Value.Y + (y - tileTop) * 16, 16, 16)
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
    }

    /// <summary>
    /// Render tiles using their render graphics
    /// </summary>
    public void Render(int layer, int alpha)
    {
        var level = RainEd.Instance.Level;

        UpdateRenderList();

        // draw the tile renders
        RlManaged.Shader shader;
        RlManaged.Shader? curShader = null;

        // palette rendering mode
        bool renderPalette;

        if (renderInfo.Palette >= 0)
        {
            renderPalette = true;
            shader = Shaders.PaletteShader;
            renderInfo.UpdatePaletteTexture();
        }

        // normal rendering mode
        else
        {
            renderPalette = false;
            shader = Shaders.TileShader;
        }

        var gfxProvider = RainEd.Instance.AssetGraphics;

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

                var catCol = init.Category.Color;
                var drawColor = new Color(catCol.R, catCol.G, catCol.B, alpha);

                // if the tile texture was not found, draw a
                // placeholder graphic
                if (tex is null)
                {
                    if (curShader != null)
                    {
                        curShader = null;
                        Raylib.EndShaderMode();
                    }

                    Raylib.EndShaderMode();
                    var srcRec = new Rectangle(0f, 0f, 2f, 2f);
                    Raylib.DrawTexturePro(RainEd.Instance.PlaceholderTexture, srcRec, dstRec, Vector2.Zero, 0f, Color.White);
                }
                else
                {
                    if (curShader != shader)
                    {
                        curShader = shader;

                        if (shader == Shaders.PaletteShader)
                            renderInfo.BeginPaletteShaderMode();
                        else
                            Raylib.BeginShaderMode(shader);
                    }

                    // draw front face of box tile
                    if (init.Type == TileType.Box)
                    {
                        var height = init.Height * 20;
                        var srcRec = new Rectangle(
                            0f,
                            init.ImageYOffset + height * init.Width,
                            (init.Width + init.BfTiles * 2) * 20, (init.Height + init.BfTiles * 2) * 20
                        );

                        // if rendering palette, R channel represents sublayer
                        // A channel is alpha, as usual
                        var col = renderPalette ? new Color(0, 0, 0, (int)drawColor.A) : drawColor;

                        Raylib.DrawTexturePro(tex, srcRec, dstRec, Vector2.Zero, 0f, renderPalette ? Color.Black : drawColor);
                    }

                    // draw the tile sublayers from back to front
                    else
                    {
                        for (int l = init.LayerCount-1; l >= 0; l--)
                        {
                            var srcRec = GetGraphicSublayer(init, l, 0);

                            // if rendering palette, R channel represents sublayer
                            // A channel is alpha, as usual
                            Color col = drawColor;
                            float lf = (float)l / init.LayerCount;

                            if (renderPalette)
                            {
                                var paletteIndex = lf * (init.HasSecondLayer ? 2f : 1f) / 3f;
                                col = new Color((int)MathF.Round(Math.Clamp(paletteIndex, 0f, 1f) * 255f), 0, 0, drawColor.A);
                            }
                            else
                            {
                                // fade to white as the layer is further away
                                // from the front
                                float a = lf;
                                col.R = (byte)(col.R * (1f - a) + (col.R * 0.5) * a);
                                col.G = (byte)(col.G * (1f - a) + (col.G * 0.5) * a);
                                col.B = (byte)(col.B * (1f - a) + (col.B * 0.5) * a);
                            }

                            Raylib.DrawTexturePro(tex, srcRec, dstRec, Vector2.Zero, 0f, col);
                        }

                    }
                }
            }
        }

        if (curShader != null)
        {
            Raylib.EndShaderMode();
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