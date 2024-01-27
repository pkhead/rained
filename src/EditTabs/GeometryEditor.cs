using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
using System.Diagnostics;

namespace RainEd;

public class GeometryEditor
{
    public bool IsWindowOpen = true;

    private readonly RainEd editor;
    private readonly Level level;

    private Vector2 viewOffset = new();
    private float viewZoom = 1f;

    public int Width;
    public int Height;
    private int workLayer = 0;

    public int WorkLayer { get => workLayer; set => workLayer = value; }

    public enum LayerViewMode : int
    {
        Overlay = 0,
        Stack = 1
    }

    public enum Tool : int
    {
        Wall = 0,
        Air,
        Inverse,
        Glass,
        Slope,
        Platform,
        HorizontalBeam,
        VerticalBeam,
        Rock,
        Spear,
        Crack,
        Waterfall,
        ShortcutEntrance,
        Shortcut,
        Entrance,
        CreatureDen,
        WhackAMoleHole,
        ScavengerHole,
        Hive,
        ForbidFlyChain,
        GarbageWorm,
        WormGrass,

        ToolCount // not an enum, just the number of tools
    }

    private static readonly Dictionary<Tool, string> ToolNames = new()
    {
        { Tool.Wall,            "Wall"              },
        { Tool.Air,             "Air"               },
        { Tool.Inverse,         "Toggle Wall/Air"   },
        { Tool.Slope,           "Slope"             },
        { Tool.Platform,        "Platform"          },
        { Tool.Rock,            "Rock"              },
        { Tool.Spear,           "Spear"             },
        { Tool.Crack,           "Fissure"           },
        { Tool.HorizontalBeam,  "Horizontal Beam"   },
        { Tool.VerticalBeam,    "Vertical Beam"     },
        { Tool.Glass,           "Invisible Wall"    },
        { Tool.ShortcutEntrance,"Shortcut Entrance" },
        { Tool.Shortcut,        "Shortcut Dot"      },
        { Tool.CreatureDen,     "Creature Den"      },
        { Tool.Entrance,        "Room Entrance"     },
        { Tool.Hive,            "Batfly Hive"       },
        { Tool.ForbidFlyChain,  "Forbid Fly Chain"  },
        { Tool.Waterfall,       "Waterfall"         },
        { Tool.WhackAMoleHole,  "Whack-a-mole Hole" },
        { Tool.ScavengerHole,   "Scavenger Hole"    },
        { Tool.GarbageWorm,     "Garbage Worm"      },
        { Tool.WormGrass,       "Worm Grass"        }
    };

    private static readonly Dictionary<Tool, Vector2> ToolTextureOffsets = new()
    {
        { Tool.Wall,            new(1, 0) },
        { Tool.Air,             new(2, 0) },
        { Tool.Inverse,         new(0, 0) },
        { Tool.Slope,           new(3, 0) },
        { Tool.Platform,        new(0, 1) },
        { Tool.Rock,            new(1, 1) },
        { Tool.Spear,           new(2, 1) },
        { Tool.Crack,           new(3, 1) },
        { Tool.HorizontalBeam,  new(0, 2) },
        { Tool.VerticalBeam,    new(1, 2) },
        { Tool.Glass,           new(2, 2) },
        { Tool.ShortcutEntrance,new(3, 2) },
        { Tool.Shortcut,        new(0, 3) },
        { Tool.CreatureDen,     new(1, 3) },
        { Tool.Entrance,        new(2, 3) },
        { Tool.Hive,            new(3, 3) },
        { Tool.ForbidFlyChain,  new(0, 4) },
        { Tool.Waterfall,       new(2, 4) },
        { Tool.WhackAMoleHole,  new(3, 4) },
        { Tool.ScavengerHole,   new(0, 5) },
        { Tool.GarbageWorm,     new(1, 5) },
        { Tool.WormGrass,       new(2, 5) }
    };

    private Tool selectedTool = Tool.Wall;
    public LayerViewMode layerViewMode = LayerViewMode.Overlay;
    private int mouseCx = 0;
    private int mouseCy = 0;
    private Vector2 mouseCellFloat = new();

    private static readonly Color[] LAYER_COLORS = new Color[3] {
        new(0, 0, 0, 255),
        new(0, 255, 0, 127),
        new(255, 0, 0, 127)
    };

    private readonly string[] _viewModes = new string[2] {
        "Overlay", "Stack"
    };

    private readonly UICanvasWidget canvasWidget;
    private readonly RlManaged.Texture2D toolIcons;

    // tool rect - for wall/air/inverse/geometry tools
    private bool isToolRectActive;
    private int toolRectX;
    private int toolRectY;

    public GeometryEditor(RainEd editor)
    {
        this.editor = editor;
        level = editor.Level;
        canvasWidget = new(1, 1);
        Width = 72;
        Height = 42;

        toolIcons = new("data/tool-icons.png");
    }

    public void Render()
    {
        if (IsWindowOpen && ImGui.Begin("Geometry Editor", ref IsWindowOpen))
        {
            // work layer
            {
                var workLayerV = workLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4);
                ImGui.InputInt("Work Layer", ref workLayerV);
                workLayerV = Math.Clamp(workLayerV, 1, 3);    
                workLayer = workLayerV - 1;
            }

            // view mode
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 8);
                if (ImGui.BeginCombo("View Mode", _viewModes[(int)layerViewMode]))
                {
                    for (int i = 0; i < _viewModes.Count(); i++)
                    {
                        bool isSelected = i == (int)layerViewMode;
                        if (ImGui.Selectable(_viewModes[i], isSelected))
                        {
                            layerViewMode = (GeometryEditor.LayerViewMode) i;
                        }

                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                if (ImGui.Button("Reset View"))
                {
                    viewOffset = new();
                    viewZoom = 1f;
                }
            }

            // toolbar
            {
                ImGui.BeginGroup();

                ImGui.Text("Tools");

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

                for (int i = 0; i < (int) Tool.ToolCount; i++)
                {
                    Tool toolEnum = (Tool) i;

                    if (i % 4 > 0) ImGui.SameLine();

                    string toolName = ToolNames[toolEnum];
                    Vector2 texOffset = ToolTextureOffsets[toolEnum];

                    // highlight selected tool
                    if (toolEnum == selectedTool)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered]);

                    // tool buttons will have a more transparent hover color
                    } else {
                        Vector4 col = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered];
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                            new Vector4(col.X, col.Y, col.Z, col.W / 4f));
                    }
                    
                    ImGui.PushID(i);
                    
                    // create tool button, select if clicked
                    if (rlImGui.ImageButtonRect("ToolButton", toolIcons, 24, 24, new Rectangle(texOffset.X * 24, texOffset.Y * 24, 24, 24)))
                    {
                        selectedTool = toolEnum;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(toolName);
                    }

                    ImGui.PopID();
                    ImGui.PopStyleColor();
                }
                
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();

                // show stats & hints
                ImGui.Text($"Mouse X: {mouseCx}");
                ImGui.Text($"Mouse Y: {mouseCy}");
                ImGui.NewLine();

                if (selectedTool == Tool.Wall || selectedTool == Tool.Air || selectedTool == Tool.Inverse || selectedTool == Tool.Glass)
                {
                    ImGui.Text("Shift+Drag to\nfill rect");
                }

                ImGui.EndGroup();
            }

            // canvas widget
            ImGui.SameLine();
            {
                var regionMax = ImGui.GetWindowContentRegionMax();
                var regionMin = ImGui.GetCursorPos();

                canvasWidget.Resize((int)(regionMax.X - regionMin.X), (int)(regionMax.Y - regionMin.Y));
                
                if (canvasWidget.RenderTexture is not null)
                {
                    Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                    DrawCanvas();
                    Raylib.EndTextureMode();
                }

                canvasWidget.Draw();
            }
        }
        ImGui.End();
    }

    private void Zoom(float factor, Vector2 mpos)
    {
        Console.WriteLine(viewOffset);

        viewZoom *= factor;
        viewOffset = -(mpos - viewOffset) / factor + mpos;

        Console.WriteLine(viewOffset);
    }

    private void DrawCanvas()
    {
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        Rlgl.PushMatrix();
        Rlgl.Scalef(viewZoom, viewZoom, 1f);
        Rlgl.Translatef(-viewOffset.X, -viewOffset.Y, 0);

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));
        
        // draw the layers
        int foregroundAlpha = 255; // this is stored for drawing objects later

        switch (layerViewMode)
        {
            // overlay: each layer is drawn over each other with a unique transparent colors
            case LayerViewMode.Overlay:
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    var color = LAYER_COLORS[l];
                    level.RenderLayer(l, color);
                }

                break;
            
            // stack: view each layer individually, each other layer is transparent
            case LayerViewMode.Stack:
                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = l == WorkLayer ? 255 : 50;
                    if (l == 0) foregroundAlpha = alpha;
                    var color = new Color(LAYER_COLORS[l].R, LAYER_COLORS[l].G, LAYER_COLORS[l].B, alpha);
                    int offset = l * 2;

                    Rlgl.PushMatrix();
                    Rlgl.Translatef(offset, offset, 0f);
                    level.RenderLayer(l, color);
                    Rlgl.PopMatrix();
                }

                break;
        }

        // draw object graphics
        var objColor = new Color(255, 255, 255, foregroundAlpha);
        level.RenderObjects(objColor);
        level.RenderShortcuts(Color.White);

        // draw grid squares
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                
                var cellRect = new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize);
                Raylib.DrawRectangleLinesEx(
                    cellRect,
                    1.0f / viewZoom,
                    new Color(255, 255, 255, 60)
                );
            }
        }

        // draw bigger grid squares
        for (int x = 0; x < level.Width; x += 2)
        {
            for (int y = 0; y < level.Height; y += 2)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize * 2, Level.TileSize * 2),
                    1.0f / viewZoom,
                    new Color(255, 255, 255, 60)
                );
            }
        }

        // draw level border outline
        {
            int borderRight = level.Width - level.BufferTilesRight;
            int borderBottom = level.Height - level.BufferTilesBot;
            int borderW = borderRight - level.BufferTilesLeft;
            int borderH = borderBottom - level.BufferTilesTop;

            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    level.BufferTilesLeft * Level.TileSize, level.BufferTilesTop * Level.TileSize,
                    borderW * Level.TileSize, borderH * Level.TileSize
                ),
                1f / viewZoom,
                Color.White
            );
        }

        // obtain mouse coordinates
        int lastMouseCx = mouseCx;
        int lastMouseCy = mouseCy;

        mouseCellFloat.X = (canvasWidget.MouseX / viewZoom + viewOffset.X) / Level.TileSize;
        mouseCellFloat.Y = (canvasWidget.MouseY / viewZoom + viewOffset.Y) / Level.TileSize;
        mouseCx = (int) mouseCellFloat.X;
        mouseCy = (int) mouseCellFloat.Y;

        if (canvasWidget.IsHovered)
        {
            // middle click pan
            if (Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                viewOffset -= mouseDelta / viewZoom;
            }

            // scroll wheel zooming
            var wheelMove = Raylib.GetMouseWheelMove();
            if (wheelMove > 0f)
            {
                Zoom(1.5f, mouseCellFloat * Level.TileSize);
            }
            else if (wheelMove < 0f)
            {
                Zoom(1f / 1.5f, mouseCellFloat * Level.TileSize);
            }

            // cursor rect mode
            if (isToolRectActive && Raylib.IsKeyDown(KeyboardKey.LeftShift))
            {
                var mx = Math.Clamp(mouseCx, 0, level.Width - 1);
                var my = Math.Clamp(mouseCy, 0, level.Height - 1);

                // draw tool rect
                var rectMinX = Math.Min(mx, toolRectX);
                var rectMinY = Math.Min(my, toolRectY);
                var rectMaxX = Math.Max(mx, toolRectX);
                var rectMaxY = Math.Max(my, toolRectY);
                var rectW = rectMaxX - rectMinX + 1;
                var rectH = rectMaxY - rectMinY + 1;

                Raylib.DrawRectangleLinesEx(
                    new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                    1f / viewZoom,
                    Color.White
                );

                if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                {
                    ApplyToolRect();
                }
            }
            
            // normal cursor mode
            // if mouse is within level bounds?
            else if (mouseCx >= 0 && mouseCy >= 0 && mouseCx < level.Width && mouseCy < level.Height)
            {
                isToolRectActive = false;

                // draw grid cursor otherwise
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(mouseCx * Level.TileSize, mouseCy * Level.TileSize, Level.TileSize, Level.TileSize),
                    1f / viewZoom,
                    Color.White
                );

                // activate tool on click
                // or if user moves mouse on another tile space
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) ||
                    (Raylib.IsMouseButtonDown(MouseButton.Left) && (mouseCx != lastMouseCx || mouseCy != lastMouseCy)))
                {
                    if (!isToolRectActive)
                        ActivateTool(mouseCx, mouseCy, Raylib.IsMouseButtonPressed(MouseButton.Left), Raylib.IsKeyDown(KeyboardKey.LeftShift));
                }
            }
        }

        // keybind to switch layer
        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }

        Rlgl.PopMatrix();
    }

    private bool toolPlaceMode;

    private void ActivateTool(int tx, int ty, bool pressed, bool shift)
    {
        isToolRectActive = false;

        var cell = level.Layers[workLayer, tx, ty];
        LevelObject levelObject = LevelObject.None;

        switch (selectedTool)
        {
            case Tool.Wall:
                if (shift)
                {
                    isToolRectActive = true;
                    toolRectX = tx;
                    toolRectY = ty;
                }
                else
                {
                    cell.Cell = CellType.Solid;
                }

                break;
            
            case Tool.Air:
                if (shift)
                {
                    isToolRectActive = true;
                    toolRectX = tx;
                    toolRectY = ty;
                }
                else
                {
                    cell.Cell = CellType.Air;
                }

                break;

            case Tool.Platform:
                cell.Cell = CellType.Platform;
                break;

            case Tool.Glass:
                if (shift)
                {
                    isToolRectActive = true;
                    toolRectX = tx;
                    toolRectY = ty;
                }
                else
                {
                    cell.Cell = CellType.Glass;
                }

                break;
            
            case Tool.Inverse:
                if (shift)
                {
                    isToolRectActive = true;
                    toolRectX = tx;
                    toolRectY = ty;
                }
                else
                {
                    if (pressed) toolPlaceMode = cell.Cell == CellType.Air;
                    cell.Cell = toolPlaceMode ? CellType.Solid : CellType.Air;
                }

                break;

            case Tool.ShortcutEntrance:
                if (pressed) cell.Cell = cell.Cell == CellType.ShortcutEntrance ? CellType.Air : CellType.ShortcutEntrance;
                break;
            
            case Tool.Slope:
            {
                if (!pressed) break;
                static bool isSolid(Level level, int l, int x, int y)
                {
                    if (x < 0 || y < 0) return false;
                    if (x >= level.Width || y >= level.Height) return false;
                    return level.Layers[l,x,y].Cell == CellType.Solid;
                }

                CellType newType = CellType.Air;
                int possibleConfigs = 0;

                // figure out how to orient the slope using solid neighbors
                if (isSolid(level, workLayer, tx-1, ty) && isSolid(level, workLayer, tx, ty+1))
                {
                    newType = CellType.SlopeRightUp;
                    possibleConfigs++;
                }
                
                if (isSolid(level, workLayer, tx+1, ty) && isSolid(level, workLayer, tx, ty+1))
                {
                    newType = CellType.SlopeLeftUp;
                    possibleConfigs++;
                }
                
                if (isSolid(level, workLayer, tx-1, ty) && isSolid(level, workLayer, tx, ty-1))
                {
                    newType = CellType.SlopeRightDown;
                    possibleConfigs++;
                }
                
                if (isSolid(level, workLayer, tx+1, ty) && isSolid(level, workLayer, tx, ty-1))
                {
                    newType = CellType.SlopeLeftDown;
                    possibleConfigs++;
                }

                if (possibleConfigs == 1)
                    cell.Cell = newType;

                break;
            }

            // the following will use the default object tool
            // handler
            case Tool.HorizontalBeam:
                levelObject = LevelObject.HorizontalBeam;
                break;

            case Tool.VerticalBeam:
                levelObject = LevelObject.VerticalBeam;
                break;
                
            case Tool.Rock:
                levelObject = LevelObject.Rock;
                break;

            case Tool.Spear:
                levelObject = LevelObject.Spear;
                break;

            case Tool.Crack:
                levelObject = LevelObject.Crack;
                break;
            
            case Tool.Hive:
                levelObject = LevelObject.Hive;
                break;
            
            case Tool.ForbidFlyChain:
                levelObject = LevelObject.ForbidFlyChain;
                break;
            
            case Tool.Waterfall:
                levelObject = LevelObject.Waterfall;
                break;
            
            case Tool.WormGrass:
                levelObject = LevelObject.WormGrass;
                break;

            case Tool.Shortcut:
                levelObject = LevelObject.Shortcut;
                break;

            case Tool.Entrance:
                levelObject = LevelObject.Entrance;
                break;

            case Tool.CreatureDen:
                levelObject = LevelObject.CreatureDen;
                break;
            
            case Tool.WhackAMoleHole:
                levelObject = LevelObject.WhackAMoleHole;
                break;
            
            case Tool.GarbageWorm:
                levelObject = LevelObject.GarbageWorm;
                break;
            
            case Tool.ScavengerHole:
                levelObject = LevelObject.ScavengerHole;
                break;
        }

        if (levelObject != LevelObject.None)
        {
            // player can only place objects on work layer 1 (except if it's a beam)
            if (workLayer == 0 || levelObject == LevelObject.HorizontalBeam || levelObject == LevelObject.VerticalBeam)
            {
                if (pressed) toolPlaceMode = cell.Has(levelObject);
                if (toolPlaceMode)
                    cell.Remove(levelObject);
                else
                    cell.Add(levelObject);
            }
            else
            {
                editor.ShowError($"This object is only placeable on layer 1");
            }
        }

        level.Layers[workLayer, tx, ty] = cell;
    }

    private void ApplyToolRect()
    {
        // apply the rect to the tool by
        // applying the tool at every cell
        // in the rectangle.
        var mx = Math.Clamp(mouseCx, 0, level.Width - 1);
        var my = Math.Clamp(mouseCy, 0, level.Height - 1);
        var rectMinX = Math.Min(mx, toolRectX);
        var rectMinY = Math.Min(my, toolRectY);
        var rectMaxX = Math.Max(mx, toolRectX);
        var rectMaxY = Math.Max(my, toolRectY);

        for (int x = rectMinX; x <= rectMaxX; x++)
        {
            for (int y = rectMinY; y <= rectMaxY; y++)
            {
                ActivateTool(x, y, true, false);
            }
        }
        
        isToolRectActive = false;
    }
}