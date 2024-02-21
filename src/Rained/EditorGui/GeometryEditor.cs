using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;

namespace RainEd;

class GeometryEditor : IEditorMode
{
    public string Name { get => "Geometry"; }

    private readonly EditorWindow window;
    
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

    private static readonly Color[] LAYER_COLORS = new Color[3] {
        new(0, 0, 0, 255),
        new(0, 255, 0, 127),
        new(255, 0, 0, 127)
    };

    private Tool selectedTool = Tool.Wall;
    private bool isToolActive = false;
    private readonly RlManaged.Texture2D toolIcons;

    // tool rect - for wall/air/inverse/geometry tools
    private bool isToolRectActive;
    private int toolRectX;
    private int toolRectY;
    private int lastMouseX, lastMouseY;

    // work layer
    private bool[] layerMask;

    public GeometryEditor(EditorWindow editorWindow)
    {
        layerMask = new bool[3];
        layerMask[0] = true;

        window = editorWindow;
        toolIcons = RlManaged.Texture2D.Load("assets/tool-icons.png");
    }
    
    public enum LayerViewMode : int
    {
        Overlay = 0,
        Stack = 1
    }

    public LayerViewMode layerViewMode = LayerViewMode.Overlay;
    private readonly string[] viewModeNames = new string[2] {
        "Overlay", "Stack"
    };

    public void Unload()
    {
        isToolActive = false;
        isToolRectActive = false;
        window.CellChangeRecorder.TryPushChange();
    }

    public void DrawToolbar()
    {
        if (ImGui.Begin("Build", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // view mode
            {
                ImGui.Text("View Mode");
                ImGui.SetNextItemWidth(-0.0001f);
                if (ImGui.BeginCombo("##ViewMode", viewModeNames[(int)layerViewMode]))
                {
                    for (int i = 0; i < viewModeNames.Count(); i++)
                    {
                        bool isSelected = i == (int)layerViewMode;
                        if (ImGui.Selectable(viewModeNames[i], isSelected))
                        {
                            layerViewMode = (LayerViewMode) i;
                        }

                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
            }

            // draw toolbar
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
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
            
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();

            // show work layers
            for (int i = 0; i < 3; i++)
            {
                ImGui.Checkbox("Layer " + (i+1), ref layerMask[i]);
            }

            // show fill rect hint
            if (selectedTool == Tool.Wall || selectedTool == Tool.Air || selectedTool == Tool.Inverse || selectedTool == Tool.Glass)
            {
                ImGui.Text("Shift+Drag to\nfill rect");
            }
        } ImGui.End();

        // layer mask toggle shortcuts
        if (ImGui.IsKeyPressed(ImGuiKey.E))
            layerMask[0] = !layerMask[0];

        if (ImGui.IsKeyPressed(ImGuiKey.R))
            layerMask[1] = !layerMask[1];

        if (ImGui.IsKeyPressed(ImGuiKey.T))
            layerMask[2] = !layerMask[2];
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        window.BeginLevelScissorMode();

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

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
                    levelRender.RenderGeometry(l, color);
                }

                break;
            
            // stack: view each layer individually, each other layer is transparent
            case LayerViewMode.Stack:
                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = layerMask[l] ? 255 : 50;
                    if (l == 0) foregroundAlpha = alpha;
                    var color = new Color(LAYER_COLORS[l].R, LAYER_COLORS[l].G, LAYER_COLORS[l].B, alpha);
                    int offset = l * 2;

                    Rlgl.PushMatrix();
                    Rlgl.Translatef(offset, offset, 0f);
                    levelRender.RenderGeometry(l, color);
                    Rlgl.PopMatrix();
                }

                break;
        }

        // draw object graphics
        var objColor = new Color(255, 255, 255, foregroundAlpha);
        levelRender.RenderObjects(objColor);
        levelRender.RenderShortcuts(Color.White);
        levelRender.RenderGrid();
        levelRender.RenderBorder();

        // WASD navigation
        if (!ImGui.GetIO().WantCaptureKeyboard && !ImGui.GetIO().WantTextInput)
        {
            int toolRow = (int) selectedTool / 4;
            int toolCol = (int) selectedTool % 4;
            int toolCount = (int) Tool.ToolCount;
            
            if (window.IsShortcutActivated("NavRight"))
            {
                if ((int) selectedTool == (toolCount-1))
                {
                    toolCol = 0;
                    toolRow = 0;
                }
                else if (++toolCol > 4)
                {
                    toolCol = 0;
                    toolRow++;
                }
            }

            if (window.IsShortcutActivated("NavLeft"))
            {
                toolCol--;
                if (toolCol < 0)
                {
                    toolCol = 3;
                    toolRow--;
                }
            }

            if (window.IsShortcutActivated("NavUp"))
            {
                toolRow--;
            }

            if (window.IsShortcutActivated("NavDown"))
            {
                // if on the last row, wrap back to first row
                // else, just go to next row
                if (toolRow == (toolCount-1) / 4)
                    toolRow = 0;
                else
                    toolRow++;
            }
            
            if (toolRow < 0)
            {
                toolRow = (toolCount-1) / 4;
            }

            selectedTool = (Tool) Math.Clamp(toolRow*4 + toolCol, 0, toolCount-1);
        }
        
        if (window.IsViewportHovered)
        {
            // cursor rect mode
            if (isToolRectActive && Raylib.IsKeyDown(KeyboardKey.LeftShift))
            {
                var mx = Math.Clamp(window.MouseCx, 0, level.Width - 1);
                var my = Math.Clamp(window.MouseCy, 0, level.Height - 1);

                // draw tool rect
                var rectMinX = Math.Min(mx, toolRectX);
                var rectMinY = Math.Min(my, toolRectY);
                var rectMaxX = Math.Max(mx, toolRectX);
                var rectMaxY = Math.Max(my, toolRectY);
                var rectW = rectMaxX - rectMinX + 1;
                var rectH = rectMaxY - rectMinY + 1;

                Raylib.DrawRectangleLinesEx(
                    new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                    1f / window.ViewZoom,
                    Color.White
                );

                if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                {
                    ApplyToolRect();
                    window.CellChangeRecorder.PushChange();
                }
            }
            
            // normal cursor mode
            // if mouse is within level bounds?
            else if (window.IsMouseInLevel())
            {
                isToolRectActive = false;

                // draw grid cursor otherwise
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(window.MouseCx * Level.TileSize, window.MouseCy * Level.TileSize, Level.TileSize, Level.TileSize),
                    1f / window.ViewZoom,
                    Color.White
                );

                // activate tool on click
                // or if user moves mouse on another tile space
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    isToolActive = true;
                    window.CellChangeRecorder.BeginChange();
                }
                
                if (isToolActive)
                {
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left) || (window.MouseCx != lastMouseX || window.MouseCy != lastMouseY))
                    {
                        if (!isToolRectActive)
                            ActivateTool(window.MouseCx, window.MouseCy, Raylib.IsMouseButtonPressed(MouseButton.Left), Raylib.IsKeyDown(KeyboardKey.LeftShift));
                    }

                    if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                    {
                        isToolActive = false;
                        window.CellChangeRecorder.PushChange();
                    }
                }
            }
        }
        
        lastMouseX = window.MouseCx;
        lastMouseY = window.MouseCy;

        Raylib.EndScissorMode();
    }

    private bool toolPlaceMode;

    private void ActivateTool(int tx, int ty, bool pressed, bool shift)
    {
        var level = window.Editor.Level;

        isToolRectActive = false;

        for (int workLayer = 0; workLayer < 3; workLayer++)
        {
            if (!layerMask[workLayer]) continue;

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
            }

            level.Layers[workLayer, tx, ty] = cell;
            window.LevelRenderer.MarkNeedsRedraw(workLayer);
        }
    }

    private void ApplyToolRect()
    {
        // apply the rect to the tool by
        // applying the tool at every cell
        // in the rectangle.
        var mx = Math.Clamp(window.MouseCx, 0, window.Editor.Level.Width - 1);
        var my = Math.Clamp(window.MouseCy, 0, window.Editor.Level.Height - 1);
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
        isToolActive = false;
    }
}