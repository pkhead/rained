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
        Select,
        MoveSelected,
        MoveSelection,
        MagicWand,

        Wall,
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
        { Tool.Select,          "Select"            },
        { Tool.MoveSelected,    "Move Selected"     },
        { Tool.MoveSelection,   "Move Selection"    },
        { Tool.MagicWand,       "Magic Wand"        },
        { Tool.Wall,            "Wall"              },
        { Tool.Air,             "Air"               },
        { Tool.Inverse,         "Toggle Wall/Air"   },
        { Tool.Slope,           "Slope"             },
        { Tool.Platform,        "Half-Block"        },
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
        { Tool.Select,          new(0, 6) },
        { Tool.MoveSelected,    new(1, 6) },
        { Tool.MoveSelection,   new(2, 6) },
        { Tool.MagicWand,       new(3, 6) },
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

    private static readonly Color[] LAYER_COLORS =
    [
        new(0, 0, 0, 255),
        new(89, 255, 89, 100),
        new(255, 30, 30, 70)
    ];

    private Tool selectedTool = Tool.Wall;
    private bool isToolActive = false;
    private readonly RlManaged.Texture2D toolIcons;

    // tool rect
    enum RectMode
    {
        None,
        Fill,
        Select
    };
    private RectMode toolRectMode;
    private int rectSX;
    private int rectSY;
    private int rectEX;
    private int rectEY;

    private bool isSelectionActive = false;
    private int selectSX, selectSY;
    private int selectEX, selectEY;

    private int lastMouseX, lastMouseY;
    private Vector2 lastMousePos;

    // work layer
    private readonly bool[] layerMask;

    private readonly static string OutlineMarqueeShaderSource = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        uniform float time;

        void main()
        {
            vec4 col = texture(texture0, fragTexCoord) * colDiffuse * fragColor;
            bool marquee = mod(gl_FragCoord.x + gl_FragCoord.y + time * 50.0, 10.0) < 5.0;
            finalColor = vec4(col.rgb, col.a * float(marquee));
        }
    ";
    private readonly RlManaged.Shader outlineMarqueeShader;
    private int uTime; // for outlineMarqueeShader

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

        outlineMarqueeShader = RlManaged.Shader.LoadFromMemory(null, OutlineMarqueeShaderSource);
        uTime = Raylib.GetShaderLocation(outlineMarqueeShader, "time");

        isSelectionActive = false;
        selectSX = 0;
        selectSY = 0;
        selectEX = 0;
        selectEY = 0;
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
        toolRectMode = RectMode.None;
        window.CellChangeRecorder.TryPushChange();

        window.LevelRenderer.Geometry.ClearOverlay();
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
            if (CanRectPlace(selectedTool))
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
        bool isMouseDown = window.IsMouseDown(ImGuiMouseButton.Left) || window.IsMouseDown(ImGuiMouseButton.Right);
        
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
        
        if (window.IsViewportHovered)
        {
            // rect fill mode
            if (toolRectMode != RectMode.None)
            {
                if (isMouseDown)
                {
                    rectEX = Math.Clamp(window.MouseCx, 0, level.Width - 1);
                    rectEY = Math.Clamp(window.MouseCy, 0, level.Height - 1);
                }
                
                if (toolRectMode == RectMode.Fill)
                {
                    if (!isMouseDown)
                    {
                        ApplyToolRect();
                    }
                }
                else if (toolRectMode == RectMode.Select)
                {
                    isSelectionActive = true;
                    selectSX = rectSX;
                    selectSY = rectSY;
                    selectEX = rectEX;
                    selectEY = rectEY;

                    if (!isMouseDown)
                    {
                        BeginSelection();
                        toolRectMode = RectMode.None;
                    }
                }
            }
            
            // normal cursor mode
            else
            {
                bool wasToolActive = isToolActive;
                toolRectMode = RectMode.None;

                // draw grid cursor
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(window.MouseCx * Level.TileSize, window.MouseCy * Level.TileSize, Level.TileSize, Level.TileSize),
                    1f / window.ViewZoom,
                    Color.White
                );

                // activate tool on click
                // or if user moves mouse on another tile space
                if (isMouseDown && !isToolActive)
                {
                    isToolActive = true;
                    window.CellChangeRecorder.BeginChange();
                }
                
                if (isToolActive)
                {
                    if (!wasToolActive || window.MouseCx != lastMouseX || window.MouseCy != lastMouseY)
                    {
                        if (toolRectMode == RectMode.None)
                        {
                            bool fillMode = EditorWindow.IsKeyDown(ImGuiKey.ModShift);
                            
                            if (fillMode && CanRectPlace(selectedTool))
                            {
                                toolRectMode = RectMode.Fill;
                                rectSX = window.MouseCx;
                                rectSY = window.MouseCy;
                                rectEX = rectSX;
                                rectEY = rectSY;
                            }
                            else
                            {
                                // left = place, right = erase
                                if (window.IsMouseDown(ImGuiMouseButton.Left))
                                    ActivateTool(window.MouseCx, window.MouseCy, !wasToolActive);
                                else if (window.IsMouseDown(ImGuiMouseButton.Right))
                                    Erase(window.MouseCx, window.MouseCy);
                            }
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

        // draw tool rect
        if (toolRectMode == RectMode.Fill)
        {
            var rectMinX = Math.Min(rectSX, rectEX);
            var rectMinY = Math.Min(rectSY, rectEY);
            var rectMaxX = Math.Max(rectSX, rectEX);
            var rectMaxY = Math.Max(rectSY, rectEY);
            var rectW = rectMaxX - rectMinX + 1;
            var rectH = rectMaxY - rectMinY + 1;

            bool marquee = toolRectMode == RectMode.Select;
            if (marquee)
            {
                Raylib.BeginShaderMode(outlineMarqueeShader);
                Raylib.SetShaderValue(outlineMarqueeShader, uTime, (float)Raylib.GetTime(), ShaderUniformDataType.Float);
            }

            Raylib.DrawRectangleLinesEx(
                new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                1f / window.ViewZoom,
                Color.White
            );

            if (marquee)
            {
                Raylib.EndShaderMode();
            }
        }

        // escape to clear selection
        if (window.IsFocused && EditorWindow.IsKeyPressed(ImGuiKey.Escape))
        {
            ClearSelection();
        }

        // draw selection rect
        if (isSelectionActive)
        {
            var rectMinX = Math.Min(selectSX, selectEX);
            var rectMinY = Math.Min(selectSY, selectEY);
            var rectMaxX = Math.Max(selectSX, selectEX);
            var rectMaxY = Math.Max(selectSY, selectEY);
            var rectW = rectMaxX - rectMinX + 1;
            var rectH = rectMaxY - rectMinY + 1;

            Raylib.BeginShaderMode(outlineMarqueeShader);
            Raylib.SetShaderValue(outlineMarqueeShader, uTime, (float)Raylib.GetTime(), ShaderUniformDataType.Float);

            Raylib.DrawRectangleLinesEx(
                new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                1f / window.ViewZoom,
                Color.White
            );
            
            Raylib.EndShaderMode();
        }
        
        lastMouseX = window.MouseCx;
        lastMouseY = window.MouseCy;
        lastMousePos = window.MouseCellFloat;

        Raylib.EndScissorMode();
    }

    private static bool CanRectPlace(Tool tool) =>
        tool switch
        {
            Tool.Slope => false,
            Tool.Select => false,
            Tool.MoveSelection => false,
            Tool.MoveSelected => false,
            Tool.MagicWand => false,
            Tool.ShortcutEntrance => false,
            _ => true
        };

    private bool toolPlaceMode;
    private int toolLastX = 0, toolLastY = 0;

    private void ActivateTool(int tx, int ty, bool pressed)
    {
        var level = window.Editor.Level;

        // handle the selection tools
        switch (selectedTool)
        {
            case Tool.Select:
                rectEX = tx;
                rectEY = ty;

                // only begin rect mode when user moved mouse
                // otherwise, a mouse tap will reset the selection
                if (pressed)
                {
                    toolRectMode = RectMode.None;
                    ClearSelection();
                    rectSX = rectEX;
                    rectSY = rectEY;
                }

                if (toolRectMode == RectMode.None && (rectEX != rectSX || rectEY != rectSY))
                {
                    toolRectMode = RectMode.Select;
                }
                return;
            
            case Tool.MoveSelection:
                if (isSelectionActive)
                {
                    if (pressed)
                    {
                        toolLastX = tx;
                        toolLastY = ty;
                    }
                    
                    selectSX += tx - toolLastX;
                    selectSY += ty - toolLastY;
                    selectEX += tx - toolLastX;;
                    selectEY += ty - toolLastY;

                    toolLastX = tx;
                    toolLastY = ty;
                }
                return;
            
            case Tool.MoveSelected:
                if (isSelectionActive)
                {
                    if (pressed)
                    {
                        toolLastX = tx;
                        toolLastY = ty;
                    }
                    
                    // move selected
                    window.LevelRenderer.Geometry.OverlayX += tx - toolLastX;
                    window.LevelRenderer.Geometry.OverlayY += ty - toolLastY;
                    
                    // move selection bounds too
                    selectSX += tx - toolLastX;
                    selectSY += ty - toolLastY;
                    selectEX += tx - toolLastX;;
                    selectEY += ty - toolLastY;

                    toolLastX = tx;
                    toolLastY = ty;
                }
                return;
        }

        // handle geo tools
        if (!level.IsInBounds(tx, ty)) return;
        for (int workLayer = 0; workLayer < 3; workLayer++)
        {
            if (!layerMask[workLayer]) continue;

            var cell = level.Layers[workLayer, tx, ty];
            LevelObject levelObject = LevelObject.None;

            switch (selectedTool)
            {
                case Tool.Wall:
                    cell.Geo = GeoType.Solid;
                    break;
                
                case Tool.Air:
                    cell.Geo = GeoType.Air;
                    break;

                case Tool.Platform:
                    cell.Geo = GeoType.Platform;
                    break;

                case Tool.Glass:
                    cell.Geo = GeoType.Glass;
                    break;
                
                case Tool.Inverse:
                    if (pressed) toolPlaceMode = cell.Geo == GeoType.Air;
                    cell.Geo = toolPlaceMode ? GeoType.Solid : GeoType.Air;
                    break;

                case Tool.ShortcutEntrance:
                    if (pressed) cell.Geo = cell.Geo == GeoType.ShortcutEntrance ? GeoType.Air : GeoType.ShortcutEntrance;
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
                    if (isSolid(level, workLayer, tx-1, ty) && isSolid(level, workLayer, tx, ty+1))
                    {
                        newType = GeoType.SlopeRightUp;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, workLayer, tx+1, ty) && isSolid(level, workLayer, tx, ty+1))
                    {
                        newType = GeoType.SlopeLeftUp;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, workLayer, tx-1, ty) && isSolid(level, workLayer, tx, ty-1))
                    {
                        newType = GeoType.SlopeRightDown;
                        possibleConfigs++;
                    }
                    
                    if (isSolid(level, workLayer, tx+1, ty) && isSolid(level, workLayer, tx, ty-1))
                    {
                        newType = GeoType.SlopeLeftDown;
                        possibleConfigs++;
                    }

                    if (possibleConfigs == 1)
                        cell.Geo = newType;

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
                // player can only place objects on work layer 1 (except if it's a beam or crack)
                if (workLayer == 0 || levelObject == LevelObject.HorizontalBeam || levelObject == LevelObject.VerticalBeam || levelObject == LevelObject.Crack)
                {
                    if (pressed) toolPlaceMode = cell.Has(levelObject);
                    if (toolPlaceMode)
                        cell.Remove(levelObject);
                    else
                        cell.Add(levelObject);
                }
            }

            level.Layers[workLayer, tx, ty] = cell;
            window.LevelRenderer.Geometry.MarkNeedsRedraw(tx, ty, workLayer);
        }
    }

    private void Erase(int x, int y)
    {
        var level = RainEd.Instance.Level;

        for (int workLayer = 0; workLayer < 3; workLayer++)
        {
            if (!layerMask[workLayer]) continue;
            ref var cell = ref level.Layers[workLayer, x, y];
            cell.Objects = LevelObject.None;

            window.LevelRenderer.Geometry.MarkNeedsRedraw(x, y, workLayer);
        }
    }

    private void ApplyToolRect()
    {
        // apply the rect to the tool by
        // applying the tool at every cell
        // in the rectangle.
        var rectMinX = Math.Min(rectSX, rectEX);
        var rectMinY = Math.Min(rectSY, rectEY);
        var rectMaxX = Math.Max(rectSX, rectEX);
        var rectMaxY = Math.Max(rectSY, rectEY);

        for (int x = rectMinX; x <= rectMaxX; x++)
        {
            for (int y = rectMinY; y <= rectMaxY; y++)
            {
                ActivateTool(x, y, true);
            }
        }
        
        toolRectMode = RectMode.None;
    }

    public void ClearSelection()
    {
        isSelectionActive = false;
        window.LevelRenderer.Geometry.ClearOverlay();
    }

    public void BeginSelection()
    {
        var level = RainEd.Instance.Level;

        var rectMinX = Math.Min(selectSX, selectEX);
        var rectMinY = Math.Min(selectSY, selectEY);
        var rectMaxX = Math.Max(selectSX, selectEX);
        var rectMaxY = Math.Max(selectSY, selectEY);
        var rectW = rectMaxX - rectMinX + 1;
        var rectH = rectMaxY - rectMinY + 1;

        // set geometry renderer overlay
        LevelCell[,,] overlayCells = new LevelCell[3, rectW, rectH];
        bool[,,] overlayMask = new bool[3, rectW, rectH];

        for (int x = 0; x < rectW; x++)
        {
            for (int y = 0; y < rectH; y++)
            {
                overlayCells[0, x, y] = new LevelCell();
                overlayCells[1, x, y] = new LevelCell();
                overlayCells[2, x, y] = new LevelCell();

                int cellX = rectMinX + x;
                int cellY = rectMinY + y;

                // if this cell is out of bounds, mask out the cell
                if (!level.IsInBounds(cellX, cellY))
                {
                    overlayMask[0, x, y] = false;
                    overlayMask[1, x, y] = false;
                    overlayMask[2, x, y] = false;
                }
                else
                {
                    // copy the cell from the level
                    for (int l = 0; l < 3; l++)
                    {
                        if (!layerMask[l])
                        {
                            overlayMask[l,x,y] = false;
                            continue;
                        }

                        overlayCells[l,x,y] = level.Layers[l, cellX, cellY];
                        overlayMask[l,x,y] = true;
                    }
                }
            }
        }

        window.LevelRenderer.Geometry.SetOverlay(rectMinX, rectMinY, rectW, rectH, overlayCells, overlayMask);
    }
}