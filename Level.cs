using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace RainEd;

public enum CellType
{
    Air,
    Solid,
    Floor
}

public enum LevelObject : uint
{
    HorizontalBeam = 1,
    VerticalBeam = 2,
    Rock = 4,
    Spear = 8,
}

public struct LevelCell
{
    public CellType Cell = CellType.Air;
    public LevelObject Objects = 0;
    public LevelCell() {}
}

public class Level
{
    public LevelCell[,,] Layers;
    private int _width, _height;

    public int Width { get => _width; }
    public int Height { get => _height; }
    public const int LayerCount = 3;

    public Level()
    {
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
}