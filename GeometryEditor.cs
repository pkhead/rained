using Raylib_cs;

namespace RainEd;

public class GeometryEditor : UICanvasWidget
{
    public int Width;
    public int Height;
    private Level level;
    private int workLayer = 0;

    public int WorkLayer { get => workLayer; set => workLayer = value; }

    public enum LayerViewMode : int
    {
        Overlay = 0,
        Stack = 1
    }
    public LayerViewMode layerViewMode = LayerViewMode.Overlay;

    private static readonly Color[] LAYER_COLORS = new Color[3] {
        new(0, 0, 0, 255),
        new(0, 255, 0, 127),
        new(255, 0, 0, 127)
    };

    private const int TILE_SIZE = 20;

    public GeometryEditor(Level level, int widgetWidth, int widgetHeight) : base(widgetWidth, widgetHeight)
    {
        id = "##geom_edit";
        Width = 72;
        Height = 42;
        this.level = level;
    }

    protected override void Draw()
    {
        Raylib.ClearBackground(Color.White);

        switch (layerViewMode)
        {
            case LayerViewMode.Overlay:
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    var color = LAYER_COLORS[l];

                    for (int x = 0; x < level.Width; x++)
                    {
                        for (int y = 0; y < level.Height; y++)
                        {
                            LevelCell c = level.Layers[l,x,y];

                            if (c.Cell != CellType.Air)
                            {
                                Raylib.DrawRectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE, color);
                            }
                        }
                    }
                }

                break;
            
            case LayerViewMode.Stack:
                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = l == WorkLayer ? 255 : 50;
                    var color = new Color(LAYER_COLORS[l].R, LAYER_COLORS[l].G, LAYER_COLORS[l].B, alpha);
                    int offset = l * 2;

                    for (int x = 0; x < level.Width; x++)
                    {
                        for (int y = 0; y < level.Height; y++)
                        {
                            LevelCell c = level.Layers[l,x,y];

                            if (c.Cell != CellType.Air)
                            {
                                Raylib.DrawRectangle(x * TILE_SIZE + offset, y * TILE_SIZE + offset, TILE_SIZE, TILE_SIZE, color);
                            }
                        }
                    }
                }

                break;
        }

        var mouseCx = (int)(MouseX / TILE_SIZE);
        var mouseCy = (int)(MouseY / TILE_SIZE);

        if (IsHovered && mouseCx >= 0 && mouseCy >= 0 && mouseCx < level.Width && mouseCy < level.Height)
        {
            Raylib.DrawRectangleLines(mouseCx * TILE_SIZE, mouseCy * TILE_SIZE, TILE_SIZE, TILE_SIZE, Color.White);

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
                level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Solid;

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
                level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Air;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }
    }
}