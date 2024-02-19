using Raylib_cs;
using System.Numerics;

namespace RainEd;

class LevelEditRender
{
    private static readonly Dictionary<LevelObject, Vector2> ObjectTextureOffsets = new()
    {
        { LevelObject.Rock,             new(0, 0) },
        { LevelObject.Spear,            new(1, 0) },
        { LevelObject.Shortcut,         new(2, 1) },
        { LevelObject.CreatureDen,      new(3, 1) },
        { LevelObject.Entrance,         new(4, 1) },
        { LevelObject.Hive,             new(0, 2) },
        { LevelObject.ForbidFlyChain,   new(1, 2) },
        { LevelObject.Waterfall,        new(2, 2) },
        { LevelObject.WhackAMoleHole,   new(3, 2) },
        { LevelObject.ScavengerHole,    new(4, 2) },
        { LevelObject.GarbageWorm,      new(0, 3) },
        { LevelObject.WormGrass,        new(1, 3) },
    };

    public static readonly Color[] MaterialColors = new Color[23]
    {
        new(148,    148,    148,    255),
        new(148,    255,    255,    255),
        new(0,      0,      255,    255),
        new(206,    148,    99,     255),
        new(255,    0,      0,      255),
        new(255,    206,    255,    255),
        new(57,     57,     41,     255),
        new(0,      0,      148,    255),
        new(165,    181,    255,    255),
        new(189,    165,    0,      255),
        new(99,     0,      255,    255),
        new(255,    0,      255,    255),
        new(255,    255,    0,      255),
        new(90,     255,    0,      255),
        new(206,    206,    206,    255),
        new(173,    24,     255,    255),
        new(255,    148,    0,      255),
        new(74,     115,    82,     255),
        new(123,    74,     49,     255),
        new(57,     57,     99,     255),
        new(0,      123,    181,    255),
        new(0,      148,    0,      255),
        new(206,    8,      57,     255),
    };

    // all objects associated with shortcuts
    // (these are tracked because they will be rendered separately from other objects
    // (i.e. without transparency regardless of the user's work layer)
    private static readonly LevelObject[] ShortcutObjects = new[] {
        LevelObject.Shortcut, LevelObject.CreatureDen, LevelObject.Entrance,
        LevelObject.WhackAMoleHole, LevelObject.ScavengerHole, LevelObject.GarbageWorm,
    };

    private readonly RainEd editor;
    private Level Level { get => editor.Level; }

    public bool ViewGrid = true;
    public bool ViewObscuredBeams = false;

    public Vector2 ViewTopLeft;
    public Vector2 ViewBottomRight;
    public float ViewZoom = 1f;
    private float lastViewZoom = 0f;

    private RlManaged.Texture2D gridTexture = null!;

    public LevelEditRender()
    {
        editor = RainEd.Instance;
        ReloadGridTexture();
    }

    public void ReloadGridTexture()
    {
        if (ViewZoom == lastViewZoom) return;
        lastViewZoom = ViewZoom;

        var imageW = (int)(Level.TileSize * ViewZoom * 2); 
        var imageH = (int)(Level.TileSize * ViewZoom * 2);
        var image = RlManaged.Image.GenColor(imageW, imageH, new Color(0, 0, 0, 0));

        var majorLineCol = new Color(255, 255, 255, 100);
        var minorLineCol = new Color(255, 255, 255, 60);
        //var majorLineCol = new Color(255, 255, 255, 255);

        // minor grid lines
        Raylib.ImageDrawLine(ref image.Ref(), imageW / 2, 0, imageW / 2, imageH, minorLineCol);
        Raylib.ImageDrawLine(ref image.Ref(), 0, imageH / 2, imageW, imageH / 2, minorLineCol);

        // major lines
        Raylib.ImageDrawLine(ref image.Ref(), 0, 0, imageW, 0, majorLineCol);
        Raylib.ImageDrawLine(ref image.Ref(), 0, 0, 0, imageH, majorLineCol);

        gridTexture?.Dispose();
        gridTexture = RlManaged.Texture2D.LoadFromImage(image);
        image.Dispose();
    }

    private bool IsInBorder(int x, int y)
    {
        return
            x >= Level.BufferTilesLeft && y >= Level.BufferTilesTop &&
            x < Level.Width - Level.BufferTilesRight && y < Level.Height - Level.BufferTilesBot;
    }

    public void RenderGeometry(int layer, Color color)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref LevelCell c = ref Level.Layers[layer ,x,y];

                var hasHBeam = (c.Objects & LevelObject.HorizontalBeam) != 0;
                var hasVBeam = (c.Objects & LevelObject.VerticalBeam) != 0;

                switch (c.Cell)
                {
                    case CellType.Solid:
                        if (ViewObscuredBeams)
                        {
                            // extra logic to signify that there is a beam here
                            // when beam is completely covered
                            // this is done by not drawing on the space where there is a beam
                            if (hasHBeam && hasVBeam)
                            {
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 8, 8, color);
                                Raylib.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize, 8, 8, color);
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 12, 8, 8, color);
                                Raylib.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize + 12, 8, 8, color);
                            }
                            else if (hasHBeam)
                            {
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 8, color);
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 12, Level.TileSize, 8, color);
                            }
                            else if (hasVBeam)
                            {
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 8, Level.TileSize, color);
                                Raylib.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize, 8, Level.TileSize, color);
                            }
                            else
                            {
                                Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, color);
                            }
                        }
                        else
                        {
                            // view obscured beams is off, draw as normal
                            Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, color);
                        }

                        break;
                        
                    case CellType.Platform:
                        Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10, color);
                        break;
                    
                    case CellType.Glass:
                        Raylib.DrawRectangleLines(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, color);
                        break;

                    case CellType.ShortcutEntrance:
                        // draw a lighter square
                        Raylib.DrawRectangle(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            new Color(color.R, color.G, color.B, color.A / 2)
                        );
                        break;

                    case CellType.SlopeLeftDown:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeLeftUp:
                        Raylib.DrawTriangle(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightDown:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightUp:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            color
                        );
                        break;
                }

                if (c.Cell != CellType.Solid)
                {
                    // draw horizontal beam
                    if (hasHBeam)
                    {
                        Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, color);
                    }

                    // draw vertical beam
                    if (hasVBeam)
                    {
                        Raylib.DrawRectangle(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, color);
                    }
                }
            }
        }
    }

    public void RenderObjects(Color color)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[0, x, y];

                // draw object graphics
                for (int i = 1; i < 32; i++)
                {
                    LevelObject objType = (LevelObject) (1 << (i-1));
                    if (cell.Has(objType) && !ShortcutObjects.Contains(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
                    {
                        Raylib.DrawTextureRec(
                            editor.LevelGraphicsTexture,
                            new Rectangle(offset.X * 20, offset.Y * 20, 20, 20),
                            new Vector2(x, y) * Level.TileSize,
                            color
                        );
                    }
                }
            }
        }
    }
    public void RenderShortcuts(Color color)
    {
        static bool isShortcut(Level Level, int x, int y)
        {
            if (x < 0 || y < 0) return false;
            if (x >= Level.Width || y >= Level.Height) return false;

            foreach (var objType in ShortcutObjects)
                if (Level.Layers[0,x,y].Has(objType)) return true;

            return false;
        }

        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[0, x, y];

                // shortcut entrance changes appearance
                // based on neighbor Shortcuts
                if (cell.Cell == CellType.ShortcutEntrance)
                {
                    int neighborCount = 0;
                    int texX = 0;
                    int texY = 0;

                    // left
                    if (isShortcut(Level, x-1, y))
                    {
                        texX = 1;
                        texY = 1;
                        neighborCount++;
                    }

                    // right
                    if (isShortcut(Level, x+1, y))
                    {
                        texX = 4;
                        texY = 0;
                        neighborCount++;
                    }

                    // up
                    if (isShortcut(Level, x, y-1))
                    {
                        texX = 3;
                        texY = 0;
                        neighborCount++;
                    }

                    // down
                    if (isShortcut(Level, x, y+1))
                    {
                        texX = 0;
                        texY = 1;
                        neighborCount++;
                    }

                    // "invalid" shortcut graphic
                    if (neighborCount != 1)
                    {
                        texX = 2;
                        texY = 0;
                    }

                    Raylib.DrawTextureRec(
                        editor.LevelGraphicsTexture,
                        new Rectangle(texX*20, texY*20, 20, 20),
                        new Vector2(x, y) * Level.TileSize,
                        color
                    );
                }

                // draw other objects
                foreach (LevelObject objType in ShortcutObjects)
                {
                    if (cell.Has(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
                    {
                        Raylib.DrawTextureRec(
                            editor.LevelGraphicsTexture,
                            new Rectangle(offset.X * 20, offset.Y * 20, 20, 20),
                            new Vector2(x, y) * Level.TileSize,
                            IsInBorder(x, y) ? color : new Color(255, 0, 0, 255)
                        );
                    }
                }
            }
        }
    }

    public void RenderTiles(int layer, int alpha)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        // draw tile previews
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[layer, x, y];
                if (!cell.HasTile()) continue;

                Tiles.TileData? tile;
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

                // TODO: why can this happen?
                if (tile == null) continue;

                var tileLeft = tx - tile.CenterX;
                var tileTop = ty - tile.CenterY;
                var col = tile.Category.Color;

                Raylib.DrawTexturePro(
                    tile.PreviewTexture,
                    new Rectangle((x - tileLeft) * 16, (y - tileTop) * 16, 16, 16),
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                    Vector2.Zero,
                    0f,
                    new Color(col.R, col.G, col.B, alpha)
                );
            }
        }

        // draw material color squares
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[layer, x, y];

                if (!cell.HasTile() && cell.Material != Material.None && cell.Cell != CellType.Air)
                {
                    var col = MaterialColors[(int) cell.Material - 1];
                    Raylib.DrawRectangle(
                        x * Level.TileSize + 7, y * Level.TileSize + 7,
                        Level.TileSize - 14, Level.TileSize - 14,
                        new Color(col.R, col.G, col.B, alpha)
                    );
                }
            }
        }
    }

    public void RenderGrid()
    {
        if (!ViewGrid) return;

        ReloadGridTexture();
        
        var levelW = editor.Level.Width;
        var levelH = editor.Level.Height;

        Raylib.DrawTexturePro(
            texture:    gridTexture,
            source:     new Rectangle(0, 0, gridTexture.Width * levelW / 2f, gridTexture.Height * levelH / 2f),
            dest:       new Rectangle(0, 0, Level.TileSize * levelW, Level.TileSize * levelH),
            origin:     Vector2.Zero,
            rotation:   0f,
            tint:       Color.White
        );
        
        /*var lineWidth = 0.5f / ViewZoom;
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);
        
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                var cellRect = new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize);
                Raylib.DrawRectangleLinesEx(
                    cellRect,
                    lineWidth,
                    new Color(255, 255, 255, 60)
                );
            }
        }

        // draw bigger grid squares
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x += 2)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y += 2)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize * 2, Level.TileSize * 2),
                    lineWidth,
                    new Color(255, 255, 255, 100)
                );
            }
        }*/
    }

    public void RenderBorder()
    {
        var lineWidth = 1f / ViewZoom;

        int borderRight = Level.Width - Level.BufferTilesRight;
        int borderBottom = Level.Height - Level.BufferTilesBot;
        int borderW = borderRight - Level.BufferTilesLeft;
        int borderH = borderBottom - Level.BufferTilesTop;

        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                Level.BufferTilesLeft * Level.TileSize, Level.BufferTilesTop * Level.TileSize,
                borderW * Level.TileSize, borderH * Level.TileSize
            ),
            lineWidth,
            Color.White
        );

        // draw water height
        if (Level.HasWater)
        {
            float waterHeight = Level.WaterLevel + Level.BufferTilesBot + 0.5f;
            int waterDrawY = (int)((Level.Height - waterHeight) * Level.TileSize);
            Raylib.DrawLine(
                0, waterDrawY,
                Level.Width * Level.TileSize, waterDrawY,
                Color.Blue
            );
        }
    }
}