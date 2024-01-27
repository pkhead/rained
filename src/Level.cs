using System.Numerics;
using Raylib_cs;
using RlManaged;

namespace RainEd;

public enum CellType
{
    Air,
    Solid,
    Platform,
    Glass,
    ShortcutEntrance,

    SlopeRightUp,
    SlopeRightDown,
    SlopeLeftUp,
    SlopeLeftDown
}

[Flags]
public enum LevelObject : uint
{
    None = 0,
    HorizontalBeam = 1,
    VerticalBeam = 2,
    Rock = 4,
    Spear = 8,
    Crack = 16,
    Shortcut = 32,
    CreatureDen = 64,
    Entrance = 128,
    Hive = 256,
    ForbidFlyChain = 512,
    Waterfall = 1024,
    WhackAMoleHole = 2048,
    ScavengerHole = 4096,
    GarbageWorm = 8192,
    WormGrass = 16384,
}

public struct LevelCell
{
    public CellType Cell = CellType.Air;
    public LevelObject Objects = 0;
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

    public int Width { get => _width; }
    public int Height { get => _height; }
    public const int LayerCount = 3;

    public const int TileSize = 20;

    public Level(RainEd editor)
    {
        this.editor = editor;

        _width = 72;
        _height = 42;

        Layers = new LevelCell[LayerCount,Width,Height];

        for (int l = 0; l < LayerCount; l++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Layers[l,x,y].Cell = l == 2 ? CellType.Air : CellType.Solid;
                }
            }
        }
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

    public void RenderShortcuts(Color color)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var cell = Layers[0, x, y];

                if (cell.Has(LevelObject.Shortcut))
                {
                    Raylib.DrawTextureRec(
                        editor.LevelGraphicsTexture,
                        new Rectangle(2 * 20, 1 * 20, 20, 20),
                        new Vector2(x, y) * TileSize,
                        color
                    );
                }
            }
        }
    }
}