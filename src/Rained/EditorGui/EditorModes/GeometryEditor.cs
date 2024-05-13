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
        CopyBackwards, // TODO: this will be superseded whenever i finish the geo-improvements branch

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
        { Tool.WormGrass,       "Worm Grass"        },
        { Tool.CopyBackwards,   "Copy Backwards"    },
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
        { Tool.WormGrass,       new(2, 5) },
        { Tool.CopyBackwards,   new(3, 5) }
    };

    private static readonly Color[] LayerColors =
    [
        new(0, 0, 0, 255),
        new(89, 255, 89, 100),
        new(255, 30, 30, 70)
    ];

    private Tool selectedTool = Tool.Wall;
    private bool isToolActive = false;
    private bool isErasing = false;
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
        toolIcons = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","tool-icons.png"));

        switch (RainEd.Instance.Preferences.GeometryViewMode)
        {
            case "overlay":
                layerViewMode = LayerViewMode.Overlay;
                break;
            
            case "stack":
                layerViewMode = LayerViewMode.Stack;
                break;
            
            default:
                RainEd.Logger.Error("Invalid layer view mode '{ViewMode}' in preferences.json", RainEd.Instance.Preferences.GeometryViewMode);
                break;
        }
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

    public void SavePreferences(UserPreferences prefs)
    {
        switch (layerViewMode)
        {
            case LayerViewMode.Overlay:
                prefs.GeometryViewMode = "overlay";
                break;

            case LayerViewMode.Stack:
                prefs.GeometryViewMode = "stack";
                break;

            default:
                RainEd.Logger.Error("Invalid LayerViewMode {EnumID}", (int) layerViewMode);
                break;
        }
    }

    public void DrawToolbar()
    {
        Vector4 textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];
        var level = RainEd.Instance.Level;

        if (ImGui.Begin("Build", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // view mode
            {
                ImGui.Text("View Mode");
                ImGui.SetNextItemWidth(-0.0001f);
                if (ImGui.BeginCombo("##ViewMode", viewModeNames[(int)layerViewMode]))
                {
                    for (int i = 0; i < viewModeNames.Length; i++)
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
            ImGui.Text(ToolNames[selectedTool]);
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
                if (rlImGui.ImageButtonRect("ToolButton", toolIcons, 24, 24, new Rectangle(texOffset.X * 24, texOffset.Y * 24, 24, 24), textColor))
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
            if (isToolRectActive)
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

                window.StatusText = $"({rectW}, {rectH})";
            }
            else if (
                selectedTool == Tool.Wall ||
                selectedTool == Tool.Air ||
                selectedTool == Tool.Inverse ||
                selectedTool == Tool.Glass ||
                selectedTool == Tool.CopyBackwards
            )
            {
                window.StatusText = "Shift+Drag to fill rect";
            }
        } ImGui.End();

        // simple layer shortcut
        if (KeyShortcuts.Activated(KeyShortcut.SwitchLayer))
        {
            (layerMask[1], layerMask[2], layerMask[0]) = (layerMask[0], layerMask[1], layerMask[2]);
        }

        // layer mask toggle shortcuts
        if (KeyShortcuts.Activated(KeyShortcut.ToggleLayer1))
            layerMask[0] = !layerMask[0];

        if (KeyShortcuts.Activated(KeyShortcut.ToggleLayer2))
            layerMask[1] = !layerMask[1];

        if (KeyShortcuts.Activated(KeyShortcut.ToggleLayer3))
            layerMask[2] = !layerMask[2];
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        window.BeginLevelScissorMode();

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, EditorWindow.BackgroundColor);

        // update layer colors
        {
            var layerCol1 = RainEd.Instance.Preferences.LayerColor1;
            var layerCol2 = RainEd.Instance.Preferences.LayerColor2;
            var layerCol3 = RainEd.Instance.Preferences.LayerColor3;

            LayerColors[0] = new Color(layerCol1.R, layerCol1.G, layerCol1.B, (byte)255);
            LayerColors[1] = new Color(layerCol2.R, layerCol2.G, layerCol2.B, (byte)100);
            LayerColors[2] = new Color(layerCol3.R, layerCol3.G, layerCol3.B, (byte)70);
        }
        
        // draw the layers
        int foregroundAlpha = 255; // this is stored for drawing objects later

        switch (layerViewMode)
        {
            // overlay: each layer is drawn over each other with a unique transparent colors
            case LayerViewMode.Overlay:
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    var color = LayerColors[l];
                    levelRender.RenderGeometry(l, color);
                }

                break;
            
            // stack: view each layer individually, each other layer is transparent
            case LayerViewMode.Stack:
                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = layerMask[l] ? 255 : 50;
                    if (l == 0) foregroundAlpha = alpha;
                    var color = new Color(LayerColors[l].R, LayerColors[l].G, LayerColors[l].B, alpha);
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
        levelRender.RenderCameraBorders();

        // WASD navigation
        if (!ImGui.GetIO().WantCaptureKeyboard && !ImGui.GetIO().WantTextInput)
        {
            int toolRow = (int) selectedTool / 4;
            int toolCol = (int) selectedTool % 4;
            int toolCount = (int) Tool.ToolCount;
            
            if (KeyShortcuts.Activated(KeyShortcut.NavRight))
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

            if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
            {
                toolCol--;
                if (toolCol < 0)
                {
                    toolCol = 3;
                    toolRow--;
                }
            }

            if (KeyShortcuts.Activated(KeyShortcut.NavUp))
            {
                toolRow--;
            }

            if (KeyShortcuts.Activated(KeyShortcut.NavDown))
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

        bool isMouseDown = window.IsMouseDown(ImGuiMouseButton.Left) || window.IsMouseDown(ImGuiMouseButton.Right);
        
        if (window.IsViewportHovered)
        {
            // cursor rect mode
            if (isToolRectActive)
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

                if (window.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    ApplyToolRect();
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
                bool isClicked = false;
                if (!isToolActive && isMouseDown)
                {
                    isClicked = true;
                    isErasing = KeyShortcuts.Active(KeyShortcut.RightMouse);
                    isToolActive = true;
                    window.CellChangeRecorder.BeginChange();
                }
                
                if (isToolActive)
                {
                    if (isClicked || window.MouseCx != lastMouseX || window.MouseCy != lastMouseY)
                    {
                        if (isErasing)
                        {
                            for (int l = 0; l < 3; l++)
                            {
                                if (!layerMask[l]) continue;

                                ref var cell = ref level.Layers[l, window.MouseCx, window.MouseCy];
                                cell.Objects = 0;

                                if (cell.Geo == GeoType.ShortcutEntrance)
                                {
                                    cell.Geo = GeoType.Air;
                                }

                                window.LevelRenderer.MarkNeedsRedraw(window.MouseCx, window.MouseCy, l);
                            }
                        }
                        else
                        {
                            if (!isToolRectActive)
                                ActivateTool(window.MouseCx, window.MouseCy, window.IsMouseClicked(ImGuiMouseButton.Left), EditorWindow.IsKeyDown(ImGuiKey.ModShift));
                        }
                    }
                }
            }
        }

        if (isToolActive && !isMouseDown)
        {
            isToolActive = false;
            window.CellChangeRecorder.PushChange();
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

        for (int layer = 0; layer < 3; layer++)
        {
            if (!layerMask[layer]) continue;

            var cell = level.Layers[layer, tx, ty];
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
                        cell.Geo = GeoType.Solid;
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
                        cell.Geo = GeoType.Air;
                    }

                    break;

                case Tool.Platform:
                    cell.Geo = GeoType.Platform;
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
                        cell.Geo = GeoType.Glass;
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
                        if (pressed) toolPlaceMode = cell.Geo == GeoType.Air;
                        cell.Geo = toolPlaceMode ? GeoType.Solid : GeoType.Air;
                    }

                    break;

                case Tool.ShortcutEntrance:
                    if (layer == 0 && pressed)
                        cell.Geo = cell.Geo == GeoType.ShortcutEntrance ? GeoType.Air : GeoType.ShortcutEntrance;
                    break;
                
                case Tool.Slope:
                {
                    if (!pressed) break;
                    static bool isSolid(Level level, int l, int x, int y)
                    {
                        if (x < 0 || y < 0) return false;
                        if (x >= level.Width || y >= level.Height) return false;
                        return level.Layers[l,x,y].Geo == GeoType.Solid;
                    }

                    GeoType newType = GeoType.Air;
                    int possibleConfigs = 0;

                    // figure out how to orient the slope using solid neighbors
                    if (isSolid(level, layer, tx-1, ty) && isSolid(level, layer, tx, ty+1))
                    {
                        newType = GeoType.SlopeRightUp;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, layer, tx+1, ty) && isSolid(level, layer, tx, ty+1))
                    {
                        newType = GeoType.SlopeLeftUp;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, layer, tx-1, ty) && isSolid(level, layer, tx, ty-1))
                    {
                        newType = GeoType.SlopeRightDown;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, layer, tx+1, ty) && isSolid(level, layer, tx, ty-1))
                    {
                        newType = GeoType.SlopeLeftDown;
                        possibleConfigs++;
                    }

                    if (possibleConfigs == 1)
                        cell.Geo = newType;

                    break;
                }

                // TODO: this will be superseded by a finished geo-improvements branch
                case Tool.CopyBackwards:
                {
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        int dstLayer = (layer + 1) % 3;

                        ref var dstCell = ref level.Layers[dstLayer, tx, ty];
                        dstCell.Geo = cell.Geo;
                        dstCell.Objects = cell.Objects;
                        window.LevelRenderer.MarkNeedsRedraw(tx, ty, dstLayer);
                    }

                    break;
                }

                // the following will use the default object tool
                // handler
                case Tool.HorizontalBeam:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.HorizontalBeam;
                    }
                    break;

                case Tool.VerticalBeam:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.VerticalBeam;
                    }

                    break;
                    
                case Tool.Rock:
                    levelObject = LevelObject.Rock;
                    break;

                case Tool.Spear:
                    levelObject = LevelObject.Spear;
                    break;

                case Tool.Crack:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.Crack;
                    }

                    break;
                
                case Tool.Hive:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.Hive;
                    }

                    break;
                
                case Tool.ForbidFlyChain:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.ForbidFlyChain;
                    }

                    break;
                
                case Tool.Waterfall:
                    levelObject = LevelObject.Waterfall;
                    break;
                
                case Tool.WormGrass:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.WormGrass;
                    }

                    break;

                case Tool.Shortcut:
                    if (shift)
                    {
                        isToolRectActive = true;
                        toolRectX = tx;
                        toolRectY = ty;
                    }
                    else
                    {
                        levelObject = LevelObject.Shortcut;
                    }

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
                // player can only place objects on work layer 1 (except if it's a beam or crack)
                if (layer == 0 || levelObject == LevelObject.HorizontalBeam || levelObject == LevelObject.VerticalBeam || levelObject == LevelObject.Crack)
                {
                    if (pressed) toolPlaceMode = cell.Has(levelObject);
                    if (toolPlaceMode)
                        cell.Remove(levelObject);
                    else
                        cell.Add(levelObject);
                }
            }

            level.Layers[layer, tx, ty] = cell;
            window.LevelRenderer.MarkNeedsRedraw(tx, ty, layer);
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
    }
}