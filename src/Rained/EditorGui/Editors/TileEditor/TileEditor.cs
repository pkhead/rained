namespace Rained.EditorGui.Editors;
using Raylib_cs;
using ImGuiNET;
using Rained.LevelData;
using CellSelection = CellEditing.CellSelection;

[Flags]
enum TilePlacementFlags
{
    Force = 1,
    Geometry = 2,
    SameOnly = 4,
}

partial class TileEditor : IEditorMode
{
    public string Name { get => "Tiles"; }
    public bool SupportsCellSelection => true;

    private readonly LevelWindow window;
    private bool isToolActive = false;
    private bool wasToolActive = false;

    private TileEditorMode[] editModes;
    private int currentMode = 0;
    private int lastMode = 0;
    private int forceSelection = -1;
    public bool isMouseHeldInMode = false;

    // true if attaching a chain on a chain holder
    private bool chainHolderMode = false;
    private int chainHolderX = -1;
    private int chainHolderY = -1;
    private int chainHolderL = -1;

    private bool removedOnSameCell = false;
    private int lastMouseX = -1;
    private int lastMouseY = -1;
    

    enum PlacementMode { None, Force, Geometry };
    private PlacementMode placementMode = PlacementMode.None;

    public TilePlacementFlags PlacementFlags { get; private set; }
    public bool WasModifierTapped => ((float)Raylib.GetTime() - modifierButtonPressTime) < 0.3;

    // time at which the geo modifier key was pressed
    // used for the tile geoification feature.
    private float modifierButtonPressTime = 0f;
    
    public TileEditor(LevelWindow window) {
        this.window = window;

        editModes = new TileEditorMode[3];
        editModes[0] = new MaterialEditMode(this);
        editModes[1] = new TileEditMode(this);
        editModes[2] = new AutotileEditMode(this);

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            window.CellChangeRecorder.CancelChange();
            chainHolderMode = false;
            editModes[currentMode].UndidOrRedid();
        };
    }

    public void Load()
    {
        editModes[currentMode].Unfocus();
        editModes[currentMode].Focus();

        isToolActive = false;
    }

    public void Unload()
    {
        window.CellChangeRecorder.TryPushChange();
        editModes[currentMode].Unfocus();
        chainHolderMode = false;
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        window.BeginLevelScissorMode();
        wasToolActive = isToolActive;
        var wasInChainholderMode = chainHolderMode;
        isToolActive = false;

        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;
        var matDb = RainEd.Instance.MaterialDatabase;

        // draw level background (solid white)
        //RainEd.RenderContext!.BlendMode = Glib.BlendMode.Normal;
        levelRender.RenderLevelComposite(mainFrame, layerFrames, new Rendering.LevelRenderConfig()
        {
            DrawTiles = true,
            ActiveLayer = window.WorkLayer
        });
        //RainEd.RenderContext!.BlendMode = Glib.BlendMode.CorrectedFramebufferNormal;

        // determine if the user is hovering over a shortcut block
        // if so, make the shortcuts more apparent
        {
            var shortcutAlpha = 127;

            if (window.IsMouseInLevel())
            {
                var cell = level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy];
                bool shortcut = cell.Geo == GeoType.ShortcutEntrance;

                if (!shortcut)
                {
                    foreach (var obj in Level.ShortcutObjects)
                    {
                        if (cell.Has(obj))
                        {
                            shortcut = true;
                            break;
                        }
                    }
                }

                if (shortcut) shortcutAlpha = 255;
            }

            levelRender.RenderShortcuts(new Color(255, 255, 255, shortcutAlpha));

        }
        levelRender.RenderGrid();
        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();

        bool modifyGeometry, forcePlace, disallowMatOverwrite;

        if (KeyShortcuts.Activated(KeyShortcut.TileForceGeometry))
        {
            modifierButtonPressTime = (float)Raylib.GetTime();
        }
        
        if (RainEd.Instance.Preferences.TilePlacementModeToggle)
        {
            if (KeyShortcuts.Activated(KeyShortcut.TileForceGeometry))
            {
                placementMode = placementMode == PlacementMode.Geometry
                    ? PlacementMode.None : PlacementMode.Geometry;
            }

            if (KeyShortcuts.Activated(KeyShortcut.TileForcePlacement) && editModes[currentMode] is not MaterialEditMode)
            {
                placementMode = placementMode == PlacementMode.Force
                    ? PlacementMode.None : PlacementMode.Force;
            }
            
            modifyGeometry = placementMode == PlacementMode.Geometry;
            forcePlace = placementMode == PlacementMode.Force;
        }
        else
        {
            modifyGeometry = KeyShortcuts.Active(KeyShortcut.TileForceGeometry);
            forcePlace = KeyShortcuts.Active(KeyShortcut.TileForcePlacement);
        }

        disallowMatOverwrite = KeyShortcuts.Active(KeyShortcut.TileIgnoreDifferent);

        // update placement flags
        PlacementFlags = 0;
        if (forcePlace) PlacementFlags |= TilePlacementFlags.Force;
        if (modifyGeometry) PlacementFlags |= TilePlacementFlags.Geometry;
        if (disallowMatOverwrite) PlacementFlags |= TilePlacementFlags.SameOnly;

        if (lastMouseX != window.MouseCx || lastMouseY != window.MouseCy)
        {
            lastMouseX = window.MouseCx;
            lastMouseY = window.MouseCy;
            removedOnSameCell = false;
        }

        // begin selection
        if (KeyShortcuts.Activated(KeyShortcut.Select))
        {
            CellSelection.Instance ??= new CellSelection();
            CellSelection.Instance.PasteMode = false;
        }
        
        // paste
        // (copy is handled by CellSelection)
        if (KeyShortcuts.Activated(KeyShortcut.Paste))
        {
            CellSelection.BeginPaste();
        }

        if (CellSelection.Instance is not null)
        {
            Span<bool> layerMask = [false, false, false];
            layerMask[window.WorkLayer] = true;

            CellSelection.Instance.AffectTiles = true;
            CellSelection.Instance.Update(layerMask, window.WorkLayer);
            if (!CellSelection.Instance.Active)
            {
                CellSelection.Instance.Deactivate();
                CellSelection.Instance = null;
            }
        }
        else
        {
            if (chainHolderMode)
            {
                window.WriteStatus("Chain Attach");
            }
            else
            {
                // show hovered tile/material in status text
                if (window.IsMouseInLevel())
                {
                    ref var mouseCell = ref level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy];

                    if (mouseCell.HasTile())
                    {
                        var tile = level.GetTile(mouseCell);
                        if (tile is not null)
                        {
                            window.WriteStatus(tile.Name, 3);
                        }
                        else
                        {
                            window.WriteStatus("Stray tile fragment", 3);
                        }
                    }
                    else if (mouseCell.Material != 0)
                    {
                        var matInfo = RainEd.Instance.MaterialDatabase.GetMaterial(mouseCell.Material);
                        window.WriteStatus(matInfo.Name, 3);
                    }
                }

                var editMode = editModes[currentMode];

                if (editMode is TileEditMode or AutotileEditMode)
                {
                    if (modifyGeometry)
                        window.WriteStatus("Force Geometry");
                    else if (forcePlace)
                        window.WriteStatus("Force Placement");
                    
                    if (disallowMatOverwrite)
                        window.WriteStatus("Ignore Materials");
                }
                else if (editMode is MaterialEditMode)
                {
                    if (disallowMatOverwrite)
                        window.WriteStatus("Disallow Overwrite");

                    if (modifyGeometry)
                        window.WriteStatus("Force Geometry");
                }
            }

            if (window.IsViewportHovered)
            {
                // begin change if left or right button is down
                // regardless of what it's doing
                if (chainHolderMode)
                {
                    ProcessChainAttach();
                    isMouseHeldInMode = false;
                    editModes[currentMode].ResetInput();
                }
                else
                {
                    if (EditorWindow.IsMouseDown(ImGuiMouseButton.Left) || KeyShortcuts.Active(KeyShortcut.RightMouse))
                    {
                        //if (!wasToolActive) window.CellChangeRecorder.BeginChange();
                        isToolActive = true;
                    }

                    if (isToolActive && !wasToolActive)
                    {
                        isMouseHeldInMode = true;
                    }

                    if (!isToolActive && wasToolActive)
                    {
                        isMouseHeldInMode = false;
                    }
                    
                    editModes[currentMode].Process();

                    // material and tile eyedropper and removal
                    if (window.IsMouseInLevel() && editModes[currentMode].CurrentRectMode == TileEditorMode.RectMode.Inactive)
                    {
                        int tileLayer = window.WorkLayer;
                        int tileX = window.MouseCx;
                        int tileY = window.MouseCy;

                        ref var mouseCell = ref level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy];

                        // if this is a tile body, find referenced tile head
                        if (mouseCell.HasTile() && mouseCell.TileHead is null)
                        {
                            tileLayer = mouseCell.TileLayer;
                            tileX = mouseCell.TileRootX;
                            tileY = mouseCell.TileRootY;

                            //if (!level.IsInBounds(tileX, tileY) || level.Layers[tileLayer, tileX, tileY].TileHead is null)
                            //    ImGui.SetTooltip("Detached tile body");
                        }


                        // eyedropper
                        if (KeyShortcuts.Activated(KeyShortcut.Eyedropper))
                        {
                            // tile eyedropper
                            if (mouseCell.HasTile())
                            {
                                var tile = level.Layers[tileLayer, tileX, tileY].TileHead;
                                
                                if (tile is null)
                                {
                                    Log.UserLogger.Error("Could not find tile head");
                                }
                                else
                                {
                                    var tileEdit = GetEditMode<TileEditMode>(out int i);
                                    forceSelection = i;
                                    tileEdit.SelectTile(tile);
                                    tileEdit.SelectedTileGroup = tile.Category.Index;
                                }
                            }

                            // material eyedropper
                            else
                            {
                                if (mouseCell.Material > 0)
                                {
                                    var matEdit = GetEditMode<MaterialEditMode>(out int i);
                                    matEdit.SelectedMaterial = mouseCell.Material;
                                    var matInfo = matDb.GetMaterial(matEdit.SelectedMaterial);

                                    // select tile group that contains this material
                                    var idx = matDb.Categories.IndexOf(matInfo.Category);
                                    if (idx == -1)
                                    {
                                        EditorWindow.ShowNotification("Error");
                                        Log.UserLogger.Error("Error eyedropping material '{MaterialName}' (ID {ID})", matInfo.Name, matEdit.SelectedMaterial);
                                    }
                                    else
                                    {
                                        matEdit.SelectedGroup = idx;
                                        forceSelection = i;
                                    }
                                }
                            }
                        }

                        // remove tile on right click
                        var editMode = editModes[currentMode];
                        if (!removedOnSameCell && isMouseHeldInMode && EditorWindow.IsMouseDown(ImGuiMouseButton.Right) && mouseCell.HasTile())
                        {
                            if ((editMode is AutotileEditMode or TileEditMode) ||
                                (editMode is MaterialEditMode && !disallowMatOverwrite)
                            )
                            {
                                removedOnSameCell = true;
                                level.RemoveTileCell(window.WorkLayer, window.MouseCx, window.MouseCy, modifyGeometry);
                            }
                        }
                    }
                }
            }

            if (!chainHolderMode)
            {
                editModes[currentMode].IdleProcess();
            }
        }

        if (!wasInChainholderMode && !chainHolderMode)
        {
            if (wasToolActive && !isToolActive)
            {
                // process once again in case it does something
                // when user lets go of mouse
                //editModes[currentMode].Process();

                //window.CellChangeRecorder.PushChange();
                //lastPlaceX = -1;
                //lastPlaceY = -1;
                //lastPlaceL = -1;
                removedOnSameCell = false;
            }
        }
        
        Raylib.EndScissorMode();
    }

    public void DrawStatusBar()
    {
        CellSelection.Instance?.DrawStatusBar();
    }

    private T GetEditMode<T>() where T : TileEditorMode
    {
        return GetEditMode<T>(out _);
    }

    private T GetEditMode<T>(out int index) where T : TileEditorMode
    {
        for (int i = 0; i < editModes.Length; i++)
        {
            if (editModes[i] is T t)
            {
                index = i;
                return t;
            }
        }

        throw new InvalidOperationException($"No tile editor mode of type {typeof(T).Name} exists.");
    }

    private void ProcessChainAttach()
    {
        var chainX = (int) Math.Floor(window.MouseCellFloat.X + 0.5f);
        var chainY = (int) Math.Floor(window.MouseCellFloat.Y + 0.5f);

        RainEd.Instance.Level.SetChainData(
            chainHolderL, chainHolderX, chainHolderY,
            chainX - 1, chainY - 1
        );

        // left-click to confirm
        if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
        {
            chainHolderMode = false;
            window.CellChangeRecorder.PushChange();
        }
        // escape or right click to abort
        else if (EditorWindow.IsKeyPressed(ImGuiKey.Escape) || KeyShortcuts.Activated(KeyShortcut.RightMouse))
        {
            chainHolderMode = false;
            RainEd.Instance.Level.RemoveChainData(chainHolderL, chainHolderX, chainHolderY);
            window.CellChangeRecorder.PushChange();
        }
    }

    public void BeginChainAttach(int x, int y, int layer)
    {
        chainHolderMode = true;
        chainHolderX = x;
        chainHolderY = y;
        chainHolderL = layer;
    }
}