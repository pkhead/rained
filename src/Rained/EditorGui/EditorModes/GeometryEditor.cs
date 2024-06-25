using Raylib_cs;
using ImGuiNET;

using System.Numerics;

namespace RainEd;

class GeometryEditor : IEditorMode
{
    public string Name { get => "Geometry"; }

    private readonly LevelView view;
    
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
        CopyBackwards, // TODO: this will be superseded whenever i finish the geo-improvements branch

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
        { Tool.WormGrass,       "Worm Grass"        },
        { Tool.CopyBackwards,   "Copy Backwards"    },
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

    public GeometryEditor(LevelView levelView)
    {
        layerMask = new bool[3];
        layerMask[0] = true;

        view = levelView;
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

    public void Load()
    {
        // if there is only a single geo layer active,
        // update the active geo layer to match the
        // global work layer.
        int activeLayer = -1;

        for (int i = 0; i < Level.LayerCount; i++)
        {
            if (layerMask[i])
            {
                if (activeLayer >= 0) return;
                activeLayer = i;
            }
        }

        layerMask[0] = false;
        layerMask[1] = false;
        layerMask[2] = false;
        layerMask[view.WorkLayer] = true;
    }

    public void Unload()
    {
        if (selectedTool == Tool.MoveSelected)
        {
            ClearSelection();
        }

        isToolActive = false;
        toolRectMode = RectMode.None;
        view.CellChangeRecorder.TryPushChange();

        // if there is only a single geo layer active,
        // update the global work layer variable to the
        // layer of the active geo layer.
        int activeLayer = -1;

        for (int i = 0; i < Level.LayerCount; i++)
        {
            if (layerMask[i])
            {
                if (activeLayer >= 0) return;
                activeLayer = i;
            }
            
        }

        view.WorkLayer = activeLayer;
    }

    public void ReloadLevel()
    {
        view.Renderer.Geometry.ClearOverlay();
        isSelectionActive = false;
        isToolActive = false;
        toolRectMode = RectMode.None;
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
                if (ImGuiExt.ImageButtonRect("ToolButton", toolIcons, 24, 24, new Rectangle(texOffset.X * 24, texOffset.Y * 24, 24, 24), textColor))
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
            
            if (toolRectMode != RectMode.None)
            {
                var rectMinX = Math.Min(rectSX, rectEX);
                var rectMaxX = Math.Max(rectSX, rectEX);
                var rectMinY = Math.Min(rectSY, rectEY);
                var rectMaxY = Math.Max(rectSY, rectEY);
                var rectW = rectMaxX - rectMinX + 1;
                var rectH = rectMaxY - rectMinY + 1;

                RainEd.Logger.Debug("({StartX}, {StartY}), ({EndX}, {EndY})", rectSX, rectSY, rectEX, rectEY);

                view.StatusText = $"({rectW}, {rectH})";
            }
            else if (CanRectPlace(selectedTool))
            {
                view.StatusText = "Shift+Drag to fill rect";
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
        bool wasToolActive = isToolActive;
        view.BeginLevelScissorMode();

        var level = RainEd.Instance.Level;
        var levelRender = view.Renderer;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, LevelView.BackgroundColor);

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
        var drawTiles = RainEd.Instance.Preferences.ViewTiles;
        var drawProps = RainEd.Instance.Preferences.ViewProps;

        switch (layerViewMode)
        {
            // overlay: each layer is drawn over each other with a unique transparent colors
            case LayerViewMode.Overlay:
                for (int l = 0; l < Level.LayerCount; l++)
                {
                    var color = LayerColors[l];
                    levelRender.RenderGeometry(l, color);

                    if (drawTiles)
                    {
                        levelRender.RenderTiles(l, color.A);
                    }

                    if (drawProps)
                    {
                        levelRender.RenderProps(l, color.A);
                    }
                }

                break;
            
            // stack: view each layer individually, each other layer is transparent
            case LayerViewMode.Stack:
                // the only layer that is shown completely opaque
                // is the first active layer
                int shownLayer = -1;

                for (int l = 0; l < Level.LayerCount; l++)
                {
                    if (layerMask[l]) 
                    {
                        shownLayer = l;
                        break;
                    }
                }

                for (int l = Level.LayerCount-1; l >= 0; l--)
                {
                    var alpha = (l == shownLayer) ? 255 : 50;
                    if (l == 0) foregroundAlpha = alpha;
                    var color = LevelView.GeoColor(alpha);
                    int offset = (l - shownLayer) * 2;

                    Rlgl.PushMatrix();
                    Rlgl.Translatef(offset, offset, 0f);
                    levelRender.RenderGeometry(l, color);

                    if (drawTiles)
                    {
                        // if alpha is 255, the product wil be 100 (like in every other edit mode)
                        // and a smaller geo alpha will thus have a smaller tile alpha value
                        levelRender.RenderTiles(l, (int)(alpha * (100.0f / 255.0f)));
                    }

                    if (drawProps)
                    {
                        levelRender.RenderProps(l, (int)(alpha * (100.0f / 255.0f)));
                    }

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
        bool isMouseDown = EditorWindow.IsMouseDown(ImGuiMouseButton.Left) || EditorWindow.IsMouseDown(ImGuiMouseButton.Right);
        
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
        
        // forward selection data to the cell change recorder
        view.CellChangeRecorder.IsSelectionActive = isSelectionActive;
        view.CellChangeRecorder.SelectionX = selectSX;
        view.CellChangeRecorder.SelectionY = selectSY;
        view.CellChangeRecorder.SelectionWidth = selectEX - selectSX + 1;
        view.CellChangeRecorder.SelectionHeight = selectEY - selectSY + 1;
        
        // begin mouse interaction code
        if (view.IsViewportHovered)
        {
            // rect fill mode
            if (toolRectMode != RectMode.None)
            {
                if (isMouseDown)
                {
                    rectEX = view.MouseCx;
                    rectEY = view.MouseCy;
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
                        toolRectMode = RectMode.None;
                    }
                }
            }
            
            // normal cursor mode
            else
            {
                toolRectMode = RectMode.None;

                // draw grid cursor
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(view.MouseCx * Level.TileSize, view.MouseCy * Level.TileSize, Level.TileSize, Level.TileSize),
                    1f / view.ViewZoom,
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
                    view.CellChangeRecorder.BeginChange();
                }
                
                // when user activates tool, or when mouse cell position moves while tool is active
                if (isToolActive)
                {
                    if (isClicked || view.MouseCx != lastMouseX || view.MouseCy != lastMouseY)
                    {
                        if (toolRectMode == RectMode.None)
                        {
                            bool fillMode = EditorWindow.IsKeyDown(ImGuiKey.ModShift);
                            
                            if (fillMode && CanRectPlace(selectedTool))
                            {
                                toolRectMode = RectMode.Fill;
                                rectSX = view.MouseCx;
                                rectSY = view.MouseCy;
                                rectEX = rectSX;
                                rectEY = rectSY;
                            }
                            else
                            {
                                // left = place, right = erase
                                if (EditorWindow.IsMouseDown(ImGuiMouseButton.Left))
                                    ActivateTool(view.MouseCx, view.MouseCy, !wasToolActive);
                                else if (EditorWindow.IsMouseDown(ImGuiMouseButton.Right))
                                    Erase(view.MouseCx, view.MouseCy);
                            }
                        }
                    }
                }
            }
        }

        // when user deactivates tool
        if (isToolActive && !isMouseDown)
        {
            isToolActive = false;
            view.CellChangeRecorder.PushChange();
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
                Raylib.BeginShaderMode(Shaders.OutlineMarqueeShader);
                Shaders.OutlineMarqueeShader.GlibShader.SetUniform("time", (float)Raylib.GetTime());
            }

            Raylib.DrawRectangleLinesEx(
                new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                1f / view.ViewZoom,
                Color.White
            );

            if (marquee)
            {
                Raylib.EndShaderMode();
            }
        }

        // escape to clear selection
        if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
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

            Raylib.BeginShaderMode(Shaders.OutlineMarqueeShader);
            Shaders.OutlineMarqueeShader.GlibShader.SetUniform("time", (float)Raylib.GetTime());

            Raylib.DrawRectangleLinesEx(
                new Rectangle(rectMinX * Level.TileSize, rectMinY * Level.TileSize, rectW * Level.TileSize, rectH * Level.TileSize),
                1f / view.ViewZoom,
                Color.White
            );
            
            Raylib.EndShaderMode();
        }
        
        lastMouseX = view.MouseCx;
        lastMouseY = view.MouseCy;
        lastMousePos = view.MouseCellFloat;

        Raylib.EndScissorMode();
    }

    private static bool CanRectPlace(Tool tool) =>
        tool switch
        {
            Tool.Slope => false, // TODO: rect place slope
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
        var level = RainEd.Instance.Level;

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
                        EndSelectedMovement();
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
                        StartSelectedMovement(false);
                    }
                    
                    // move selected
                    view.Renderer.Geometry.OverlayX += tx - toolLastX;
                    view.Renderer.Geometry.OverlayY += ty - toolLastY;
                    
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
        if (pressed && isSelectionActive)
        {
            ClearSelection();
        }

        if (!level.IsInBounds(tx, ty)) return;
        for (int layer = 0; layer < 3; layer++)
        {
            if (!layerMask[layer]) continue;

            var cell = level.Layers[layer, tx, ty];
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

                case Tool.CopyBackwards:
                {
                    int dstLayer = (layer + 1) % 3;

                    ref var dstCell = ref level.Layers[dstLayer, tx, ty];
                    dstCell.Geo = cell.Geo;
                    dstCell.Objects = cell.Objects;
                    view.Renderer.InvalidateGeo(tx, ty, dstLayer);

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
            view.Renderer.InvalidateGeo(tx, ty, layer);
        }
    }

    private void Erase(int x, int y)
    {
        var level = RainEd.Instance.Level;
        
        for (int l = 0; l < 3; l++)
        {
            if (!layerMask[l]) continue;

            ref var cell = ref level.Layers[l, x, y];
            cell.Objects = 0;

            if (cell.Geo == GeoType.ShortcutEntrance)
            {
                cell.Geo = GeoType.Air;
            }

            view.Renderer.InvalidateGeo(x, y, l);
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

        if (isErasing)
        {
            for (int x = rectMinX; x <= rectMaxX; x++)
            {
                for (int y = rectMinY; y <= rectMaxY; y++)
                {
                    Erase(x, y);
                }
            }
        }
        else
        {
            toolPlaceMode = isErasing; // if right-click, activate tool in erase mode

            for (int x = rectMinX; x <= rectMaxX; x++)
            {
                for (int y = rectMinY; y <= rectMaxY; y++)
                {
                    ActivateTool(x, y, false);
                }
            }
        }
        
        toolRectMode = RectMode.None;
    }

    private void ClearSelection()
    {
        isSelectionActive = false;
        EndSelectedMovement();
    }
    
    private void StartSelectedMovement(bool canOverwrite)
    {
        RainEd.Logger.Information("Start selected movement");

        // if an overlay already exists, only continue if canOverwrite is true
        if (view.Renderer.Geometry.Overlay is not null && !canOverwrite)
        {
            return;
        }

        // set the geometry renderer overlay to the selection,
        // then clearing the cells in the level data that correspond to the overlay
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

                // if this cell is out of bounds, ignore the cell
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
                        // if this layer is not active, ignore the cell
                        if (!layerMask[l])
                        {
                            overlayMask[l,x,y] = false;
                            continue;
                        }

                        // set this overlay cell
                        overlayCells[l,x,y] = level.Layers[l, cellX, cellY];
                        overlayMask[l,x,y] = true;

                        // clear original cell
                        level.Layers[l, cellX, cellY] = new LevelCell();
                        view.Renderer.InvalidateGeo(cellX, cellY, l);
                    }
                }
            }
        }

        view.Renderer.Geometry.SetOverlay(rectMinX, rectMinY, rectW, rectH, overlayCells, overlayMask);
    }

    private void EndSelectedMovement()
    {
        RainEd.Logger.Debug("End selected movement");

        // apply geometry overlay into the actual level
        var level = RainEd.Instance.Level;

        var overlayCells = view.Renderer.Geometry.Overlay;
        var overlayMask = view.Renderer.Geometry.OverlayMask;

        if (overlayCells is not null && overlayMask is not null)
        {
            int overlayX = view.Renderer.Geometry.OverlayX;
            int overlayY = view.Renderer.Geometry.OverlayY;
            int overlayW = view.Renderer.Geometry.OverlayWidth;
            int overlayH = view.Renderer.Geometry.OverlayHeight;

            for (int x = 0; x < overlayW; x++)
            {
                for (int y = 0; y < overlayH; y++)
                {
                    var cellX = x + overlayX;
                    var cellY = y + overlayY;
                    if (!level.IsInBounds(cellX, cellY)) continue;

                    for (int l = 0; l < 3; l++)
                    {
                        if (overlayMask[l, x, y])
                        {
                            level.Layers[l, cellX, cellY] = overlayCells[l, x, y];
                            view.Renderer.InvalidateGeo(cellX, cellY, l);
                        }      
                    }
                }
            }
        }

        view.Renderer.Geometry.ClearOverlay();
    }

    public void SetSelection(ChangeHistory.SelectionRecord selection)
    {
        view.Renderer.Geometry.ClearOverlay();

        isSelectionActive = selection.IsActive;
        selectSX = selection.X;
        selectSY = selection.Y;
        selectEX = selection.X + selection.Width - 1;
        selectEY = selection.Y + selection.Height - 1;
    }
}