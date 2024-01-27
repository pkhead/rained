using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
using System.Diagnostics;

namespace RainEd;

public class GeometryEditor
{
    public bool IsWindowOpen = true;

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

    public static readonly Dictionary<Tool, string> ToolNames = new()
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

    public static readonly Dictionary<Tool, Vector2> ToolTextureOffsets = new()
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

    private const int TILE_SIZE = 20;

    private readonly UICanvasWidget canvasWidget;
    private RlManaged.Texture2D toolIcons;

    public GeometryEditor(Level level)
    {
        this.level = level;
        canvasWidget = new(1, 1);
        Width = 72;
        Height = 42;

        toolIcons = new("data/geometry-icons.png");
    }

    public void Render()
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

            // show stats
            ImGui.Text($"Mouse X: {mouseCx}");
            ImGui.Text($"Mouse Y: {mouseCy}");

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
        Raylib.DrawRectangle(0, 0, level.Width * TILE_SIZE, level.Height * TILE_SIZE, Color.White);
        
        // draw the layers
        switch (layerViewMode)
        {
            // overlay: each layer is drawn over each other with a unique transparent colors
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
            
            // stack: view each layer individually, each other layer is transparent
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

        // obtain mouse coordinates
        mouseCellFloat.X = (canvasWidget.MouseX / viewZoom + viewOffset.X) / TILE_SIZE;
        mouseCellFloat.Y = (canvasWidget.MouseY / viewZoom + viewOffset.Y) / TILE_SIZE;
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
                Zoom(1.5f, mouseCellFloat * TILE_SIZE);
            }
            else if (wheelMove < 0f)
            {
                Zoom(1f / 1.5f, mouseCellFloat * TILE_SIZE);
            }

            // if mouse is within level bounds?
            if (mouseCx >= 0 && mouseCy >= 0 && mouseCx < level.Width && mouseCy < level.Height)
            {
                // draw grid cursor
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(mouseCx * TILE_SIZE, mouseCy * TILE_SIZE, TILE_SIZE, TILE_SIZE),
                    1f / viewZoom,
                    Color.White
                );

                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                    level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Solid;

                if (Raylib.IsMouseButtonDown(MouseButton.Right))
                    level.Layers[workLayer, mouseCx, mouseCy].Cell = CellType.Air;
            }
        }

        // keybind to switch layer
        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }

        Rlgl.PopMatrix();
    }
}