using Raylib_cs;
using RlManaged;
using System.Numerics;

namespace RainEd;

public class LevelRenderer
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

    public Vector2 ViewTopLeft;
    public Vector2 ViewBottomRight;

    public LevelRenderer(RainEd editor)
    {
        this.editor = editor;
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
                LevelCell c = Level.Layers[layer ,x,y];

                switch (c.Cell)
                {
                    case CellType.Solid:
                        Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, color);
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

                // draw horizontal beam
                if ((c.Objects & LevelObject.HorizontalBeam) != 0)
                {
                    Raylib.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, color);
                }

                // draw vertical beam
                if ((c.Objects & LevelObject.VerticalBeam) != 0)
                {
                    Raylib.DrawRectangle(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, color);
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
                var cell = Level.Layers[0, x, y];

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
            return Level.Layers[0,x,y].Has(LevelObject.Shortcut);
        }

        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                var cell = Level.Layers[0, x, y];

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
                            color
                        );
                    }
                }
            }
        }
    }

    public void RenderTiles(int layer, int alpha)
    {
        // draw tile previews
        for (int x = 0; x < Level.Width; x++)
        {
            for (int y = 0; y < Level.Height; y++)
            {
                var cell = Level.Layers[layer, x, y];
                
                if (cell.TileHead is Tiles.TileData tile)
                {
                    var tileLeft = x - tile.CenterX;
                    var tileTop = y - tile.CenterY;
                    var col = tile.Category.Color;

                    Raylib.DrawTextureEx(
                        tile.PreviewTexture,
                        new Vector2(tileLeft, tileTop) * Level.TileSize,
                        0,
                        (float)Level.TileSize / 16,
                        new Color(col.R, col.G, col.B, alpha)
                    );
                
                }
            }
        }

        // draw material color squares
        for (int x = 0; x < Level.Width; x++)
        {
            for (int y = 0; y < Level.Height; y++)
            {
                var cell = Level.Layers[layer, x, y];

                if (!cell.HasTile() && cell.Material != Material.None && cell.Cell != CellType.Air)
                {
                    var col = MaterialColors[(int) cell.Material - 1];
                    Raylib.DrawRectangle(
                        x * Level.TileSize + 8, y * Level.TileSize + 8,
                        Level.TileSize - 16, Level.TileSize - 16,
                        new Color(col.R, col.G, col.B, alpha)
                    );
                }
            }
        }
    }

    public void RenderGrid(float lineWidth)
    {
        if (!ViewGrid) return;

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
        }
    }

    public void RenderBorder(float lineWidth)
    {
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
    }
}