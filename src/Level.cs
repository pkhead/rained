using System.Numerics;
using Raylib_cs;
using RlManaged;

namespace RainEd;

public enum CellType : sbyte
{
    Air,
    Solid,
    SlopeRightUp,
    SlopeLeftUp,
    SlopeRightDown,
    SlopeLeftDown,
    Platform,
    ShortcutEntrance,
    // 8 is empty
    Glass = 9,
}

public enum Material : byte
{
    None,
    Standard,
    Concrete,
    RainStone,
    Bricks,
    BigMetal,
    TinySigns,
    Scaffolding,
    DensePipes,
    SuperStructure,
    SuperStructure2,
    TiledStone,
    ChaoticStone,
    SmallPipes,
    Trash,
    Invisible,
    LargeTrash,
    ThreeDBricks,
    RandomMachines,
    Dirt,
    CeramicTile,
    TempleStone,
    Circuits,
    Ridge
};

[Flags]
public enum LevelObject : uint
{
    None = 0u,
    HorizontalBeam = 1u,
    VerticalBeam = 2u,
    Hive = 4u,
    // (always appears with shortcut entrance?) 8
    Shortcut = 16u,
    Entrance = 32u,
    CreatureDen = 64u,
    // (empty slot?) 128,
    Rock = 256u,
    Spear = 512u,
    Crack = 1024u,
    ForbidFlyChain = 2048u,
    GarbageWorm = 4096u,
    // IDs jump from 13 to 18
    Waterfall = 131072u,
    WhackAMoleHole = 262144u,
    WormGrass = 524288u,
    ScavengerHole = 1048576u
}

public struct LevelCell
{
    public CellType Cell = CellType.Air;
    public LevelObject Objects = 0;
    public Material Material = Material.None;

    // X position of the tile root, -1 if there is no tile here
    public int TileRootX = -1;

    // Y position of the tile root, -1 if there is no tile here
    public int TileRootY = -1;
    // Layer of tile root, -1 if there is no tile here
    public int TileLayer = -1;

    // As the tile root, reference to the tile, or null if no tile here
    public Tiles.TileData? TileHead = null;

    public readonly bool HasTile() => TileRootX >= 0 || TileRootY >= 0 || TileHead is not null;

    public LevelCell() {}

    public void Add(LevelObject obj) => Objects |= obj;
    public void Remove(LevelObject obj) => Objects &= ~obj;
    public readonly bool Has(LevelObject obj) => Objects.HasFlag(obj);
}

public class Level
{
    private readonly RainEd editor;

    public LevelCell[,,] Layers;
    private int _width, _height;
    public int BufferTilesLeft, BufferTilesTop;
    public int BufferTilesRight, BufferTilesBot;
    public Material DefaultMaterial = Material.Standard;

    public int Width { get => _width; }
    public int Height { get => _height; }

    public const int LayerCount = 3;
    public const int TileSize = 20;

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

    public static readonly string[] MaterialNames = new string[23]
    {
        "Standard",
        "Concrete",
        "Rain Stone",
        "Bricks",
        "Big Metal",
        "Tiny Signs",
        "Scaffolding",
        "Dense Pipes",
        "SuperStructure",
        "SuperStructure2",
        "Tiled Stone",
        "Chaotic Stone",
        "Small Pipes",
        "Trash",
        "Invisible",
        "Large Trash",
        "3D Bricks",
        "Random Machines",
        "Dirt",
        "Ceramic Tile",
        "Temple Stone",
        "Circuits",
        "Ridge"
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

    public Level(RainEd editor)
    {
        this.editor = editor;

        _width = 72;
        _height = 42;
        BufferTilesLeft = 12;
        BufferTilesTop = 3;
        BufferTilesRight = 12;
        BufferTilesBot = 5;

        Layers = new LevelCell[LayerCount,Width,Height];

        for (int l = 0; l < LayerCount; l++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Layers[l,x,y] = new LevelCell();
                    Layers[l,x,y].Cell = l == 2 ? CellType.Air : CellType.Solid;
                }
            }
        }
    }

    public bool IsInBounds(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;
    
    public LevelCell GetClamped(int layer, int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Layers[layer, x, y];
    }
    
    public void RenderLayer(int layer, Color color)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                LevelCell c = Layers[layer ,x,y];

                switch (c.Cell)
                {
                    case CellType.Solid:
                        Raylib.DrawRectangle(x * TileSize, y * TileSize, TileSize, TileSize, color);
                        break;
                        
                    case CellType.Platform:
                        Raylib.DrawRectangle(x * TileSize, y * TileSize, TileSize, 10, color);
                        break;
                    
                    case CellType.Glass:
                        Raylib.DrawRectangleLines(x * TileSize, y * TileSize, TileSize, TileSize, color);
                        break;

                    case CellType.ShortcutEntrance:
                        // draw a lighter square
                        Raylib.DrawRectangle(
                            x * TileSize, y * TileSize, TileSize, TileSize,
                            new Color(color.R, color.G, color.B, color.A / 2)
                        );
                        break;

                    case CellType.SlopeLeftDown:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y+1) * TileSize,
                            new Vector2(x+1, y) * TileSize,
                            new Vector2(x, y) * TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeLeftUp:
                        Raylib.DrawTriangle(
                            new Vector2(x, y+1) * TileSize,
                            new Vector2(x+1, y+1) * TileSize,
                            new Vector2(x+1, y) * TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightDown:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y) * TileSize,
                            new Vector2(x, y) * TileSize,
                            new Vector2(x, y+1) * TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightUp:
                        Raylib.DrawTriangle(
                            new Vector2(x+1, y+1) * TileSize,
                            new Vector2(x, y) * TileSize,
                            new Vector2(x, y+1) * TileSize,
                            color
                        );
                        break;
                }

                // draw horizontal beam
                if ((c.Objects & LevelObject.HorizontalBeam) != 0)
                {
                    Raylib.DrawRectangle(x * TileSize, y * TileSize + 8, TileSize, 4, color);
                }

                // draw vertical beam
                if ((c.Objects & LevelObject.VerticalBeam) != 0)
                {
                    Raylib.DrawRectangle(x * TileSize + 8, y * TileSize, 4, TileSize, color);
                }
            }
        }
    }

    public void RenderObjects(Color color)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = Layers[0, x, y];

                // draw object graphics
                for (int i = 1; i < 32; i++)
                {
                    LevelObject objType = (LevelObject) (1 << (i-1));
                    if (cell.Has(objType) && !ShortcutObjects.Contains(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
                    {
                        Raylib.DrawTextureRec(
                            editor.LevelGraphicsTexture,
                            new Rectangle(offset.X * 20, offset.Y * 20, 20, 20),
                            new Vector2(x, y) * TileSize,
                            color
                        );
                    }
                }
            }
        }
    }
    public void RenderShortcuts(Color color)
    {
        static bool isShortcut(Level level, int x, int y)
        {
            if (x < 0 || y < 0) return false;
            if (x >= level.Width || y >= level.Height) return false;
            return level.Layers[0,x,y].Has(LevelObject.Shortcut);
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = Layers[0, x, y];

                // shortcut entrance changes appearance
                // based on neighbor Shortcuts
                if (cell.Cell == CellType.ShortcutEntrance)
                {
                    int neighborCount = 0;
                    int texX = 0;
                    int texY = 0;

                    // left
                    if (isShortcut(this, x-1, y))
                    {
                        texX = 1;
                        texY = 1;
                        neighborCount++;
                    }

                    // right
                    if (isShortcut(this, x+1, y))
                    {
                        texX = 4;
                        texY = 0;
                        neighborCount++;
                    }

                    // up
                    if (isShortcut(this, x, y-1))
                    {
                        texX = 3;
                        texY = 0;
                        neighborCount++;
                    }

                    // down
                    if (isShortcut(this, x, y+1))
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
                        new Vector2(x, y) * TileSize,
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
                            new Vector2(x, y) * TileSize,
                            color
                        );
                    }
                }
            }
        }
    }

    public void RenderTiles(int layer, int alpha)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = Layers[layer, x, y];
                
                //debug render tile data
                /*if (cell.HasTile())
                {
                    Tiles.TileData tile = cell.TileHead
                        ?? Layers[cell.TileLayer, cell.TileRootX, cell.TileRootY].TileHead
                        ?? throw new NullReferenceException();
                    
                    if (cell.TileHead is null)
                    {
                        Raylib.DrawRectangleLines(x * TileSize, y * TileSize, TileSize, TileSize, new Color(255, 255, 0, 50));
                    }
                    else
                    {
                        Raylib.DrawRectangle(x * TileSize, y * TileSize, TileSize, TileSize, new Color(255, 255, 0, 50));
                    }
                }*/

                if (cell.TileHead is Tiles.TileData tile)
                {
                    var tileLeft = x - tile.CenterX;
                    var tileTop = y - tile.CenterY;
                    var col = tile.Category.Color;

                    // draw tile preview
                    Raylib.DrawTextureEx(
                        tile.PreviewTexture,
                        new Vector2(tileLeft, tileTop) * TileSize,
                        0,
                        (float)TileSize / 16,
                        new Color(col.R, col.G, col.B, alpha)
                    );

                // draw material
                } else if (!cell.HasTile() && cell.Material != Material.None && cell.Cell != CellType.Air)
                {
                    var col = MaterialColors[(int) cell.Material - 1];
                    Raylib.DrawRectangle(
                        x * TileSize + 8, y * TileSize + 8,
                        TileSize - 16, TileSize - 16,
                        new Color(col.R, col.G, col.B, alpha)
                    );
                }
            }
        }    
    }

    public void RenderGrid(float lineWidth)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cellRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                Raylib.DrawRectangleLinesEx(
                    cellRect,
                    lineWidth,
                    new Color(255, 255, 255, 60)
                );
            }
        }

        // draw bigger grid squares
        for (int x = 0; x < Width; x += 2)
        {
            for (int y = 0; y < Height; y += 2)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(x * TileSize, y * TileSize, TileSize * 2, TileSize * 2),
                    lineWidth,
                    new Color(255, 255, 255, 60)
                );
            }
        }
    }

    public void RenderBorder(float lineWidth)
    {
        int borderRight = Width - BufferTilesRight;
        int borderBottom = Height - BufferTilesBot;
        int borderW = borderRight - BufferTilesLeft;
        int borderH = borderBottom - BufferTilesTop;

        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                BufferTilesLeft * TileSize, BufferTilesTop * TileSize,
                borderW * TileSize, borderH * TileSize
            ),
            lineWidth,
            Color.White
        );
    }
}