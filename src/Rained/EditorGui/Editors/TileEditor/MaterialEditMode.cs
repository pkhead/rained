namespace Rained.EditorGui.Editors;
using ImGuiNET;
using Rained.LevelData;
using Raylib_cs;
using System.Numerics;

class MaterialCatalogWidget(MaterialEditMode editor) : TileEditorCatalog
{
    private readonly List<int> matSearchResults = [];
    private readonly MaterialEditMode editor = editor;

    private RlManaged.Texture2D? _loadedMatPreview = null;
    private string _activeMatPreview = "";

    protected override void ProcessSearch(string searchQuery)
    {
        var matDb = RainEd.Instance.MaterialDatabase;

        matSearchResults.Clear();

        // find material groups that have any entires that pass the searchq uery
        for (int i = 0; i < matDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the results
            if (searchQuery == "")
            {
                matSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the materials in this group
            // if there is one material that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < matDb.Categories[i].Materials.Count; j++)
            {
                // this material passes the search, so add this group to the search results
                if (matDb.Categories[i].Materials[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    matSearchResults.Add(i);
                    break;
                }
            }
        }
    }

    public override void ShowGroupList()
    {
        var matDb = RainEd.Instance.MaterialDatabase;

        foreach (var i in matSearchResults)
        {
            var group = matDb.Categories[i];
            
            if (ImGui.Selectable(group.Name, editor.SelectedGroup == i) || matSearchResults.Count == 1)
                editor.SelectedGroup = i;
        }
    }

    public override void ShowAssetList()
    {
        var matDb = RainEd.Instance.MaterialDatabase;
        var prefs = RainEd.Instance.Preferences;

        var drawList = ImGui.GetWindowDrawList();
        float textHeight = ImGui.GetTextLineHeight();

        var matList = matDb.Categories[editor.SelectedGroup].Materials;

        for (int i = 0; i < matList.Count; i++)
        {
            var mat = matList[i];

            // don't show this prop if it doesn't pass search test
            if (!mat.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase))
                continue;

            const string leftPadding = "  ";
            float colorWidth = ImGui.CalcTextSize(leftPadding).X - ImGui.GetStyle().ItemInnerSpacing.X;
            
            var cursor = ImGui.GetCursorScreenPos();
            if (ImGui.Selectable(leftPadding + mat.Name, mat.ID == editor.SelectedMaterial))
            {
                editor.SelectedMaterial = mat.ID;
            }

            drawList.AddRectFilled(
                p_min: cursor,
                p_max: cursor + new Vector2(colorWidth, textHeight),
                ImGui.ColorConvertFloat4ToU32(new Vector4(mat.Color.R / 255f, mat.Color.G / 255f, mat.Color.B / 255f, 1f))
            );

            // show material preview when hovered
            if (prefs.MaterialSelectorPreview && ImGui.IsItemHovered())
            {
                if (_activeMatPreview != mat.Name)
                {
                    _activeMatPreview = mat.Name;
                    _loadedMatPreview?.Dispose();
                    _loadedMatPreview = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "mat-previews", mat.Name + ".png"));
                }

                if (_loadedMatPreview is not null && Raylib_cs.Raylib.IsTextureReady(_loadedMatPreview))
                {
                    ImGui.BeginTooltip();
                    ImGuiExt.ImageSize(_loadedMatPreview, _loadedMatPreview.Width * Boot.PixelIconScale, _loadedMatPreview.Height * Boot.PixelIconScale);
                    ImGui.EndTooltip();
                }
            }
        }
    }
}

class MaterialEditMode : TileEditorMode
{
    public override string TabName => "Materials";
    
    private int selectedMaterial = 1;
    private int selectedMatGroup = 0;
    private int materialBrushSize = 1;

    // this bool makes it so only one item (material, tile) can be removed
    // while the momuse is hovered over the same cell
    private bool removedOnSameCell = false;
    private int lastMouseX = -1;
    private int lastMouseY = -1;

    public int SelectedMaterial { get => selectedMaterial; set => selectedMaterial = value; }
    public int SelectedGroup { get => selectedMatGroup; set => selectedMatGroup = value; }

    private readonly MaterialCatalogWidget catalog;
    private bool wasToolActive = false;

    public MaterialEditMode(TileEditor editor) : base(editor)
    {
        catalog = new MaterialCatalogWidget(this);
    }

    public override void UndidOrRedid()
    {
        base.UndidOrRedid();
        removedOnSameCell = false;
    }

    public override void Focus()
    {
        base.Focus();
        catalog.ProcessSearch();
        wasToolActive = false;
    }

    public override void Unfocus()
    {
        base.Unfocus();
        wasToolActive = false;
    }

    public override void Process()
    {
        base.Process();
        
        var level = RainEd.Instance.Level;
        var window = RainEd.Instance.LevelView;
        
        if (lastMouseX != window.MouseCx || lastMouseY != window.MouseCy)
        {
            lastMouseX = window.MouseCx;
            lastMouseY = window.MouseCy;
            removedOnSameCell = false;
        }

        var isToolActive = LeftMouseDown || RightMouseDown;
        if (isToolActive && !wasToolActive)
        {
            window.CellChangeRecorder.BeginChange();
        }

        var disallowMatOverwrite = editor.PlacementFlags.HasFlag(TilePlacementFlags.SameOnly);
        var modifyGeometry = editor.PlacementFlags.HasFlag(TilePlacementFlags.Geometry);

        // rect place mode
        if (rectMode != RectMode.Inactive)
        {
            var rMinX = Math.Min(rectStart.X, window.MouseCx);
            var rMaxX = Math.Max(rectStart.X, window.MouseCx);
            var rMinY = Math.Min(rectStart.Y, window.MouseCy);
            var rMaxY = Math.Max(rectStart.Y, window.MouseCy);
            var rWidth = rMaxX - rMinX + 1;
            var rHeight = rMaxY - rMinY + 1;

            Raylib.DrawRectangleLinesEx(
                new Rectangle(rMinX * Level.TileSize, rMinY * Level.TileSize, rWidth * Level.TileSize, rHeight * Level.TileSize),
                2f / window.ViewZoom,
                RainEd.Instance.MaterialDatabase.GetMaterial(selectedMaterial).Color
            );

            if (!isToolActive)
            {
                if (rectMode == RectMode.Place)
                {
                    for (int x = rMinX; x <= rMaxX; x++)
                    {
                        for (int y = rMinY; y <= rMaxY; y++)
                        {
                            if (!level.IsInBounds(x, y)) continue;

                            if (!disallowMatOverwrite || level.Layers[window.WorkLayer, x, y].Material == 0)
                            {
                                if (modifyGeometry)
                                {
                                    level.Layers[window.WorkLayer, x, y].Geo = GeoType.Solid;
                                    window.InvalidateGeo(x, y, window.WorkLayer);
                                }

                                level.Layers[window.WorkLayer, x, y].Material = selectedMaterial;
                            }
                        }
                    }
                }
                else if (rectMode == RectMode.Remove)
                {
                    for (int x = rMinX; x <= rMaxX; x++)
                    {
                        for (int y = rMinY; y <= rMaxY; y++)
                        {
                            if (!level.IsInBounds(x, y)) continue;

                            if (!disallowMatOverwrite || level.Layers[window.WorkLayer, x, y].Material == selectedMaterial)
                            {
                                level.Layers[window.WorkLayer, x, y].Material = 0;

                                if (modifyGeometry)
                                {
                                    level.Layers[window.WorkLayer, x, y].Geo = GeoType.Air;
                                    window.InvalidateGeo(x, y, window.WorkLayer);
                                }
                            }

                            if (!disallowMatOverwrite && level.Layers[window.WorkLayer, x, y].HasTile())
                            {
                                level.RemoveTileCell(window.WorkLayer, x, y, modifyGeometry);
                            }
                        }
                    }
                }

                rectMode = RectMode.Inactive;
            }
        }

        // check if rect place mode will start
        else if (/*editor.isMouseHeldInMode &&*/ isToolActive && !wasToolActive && EditorWindow.IsKeyDown(ImGuiKey.ModShift))
        {
            if (LeftMouseDown)
            {
                rectMode = RectMode.Place;
            }
            else if (RightMouseDown)
            {
                rectMode = RectMode.Remove;
            }

            if (rectMode != RectMode.Inactive)
                rectStart = new CellPosition(window.MouseCx, window.MouseCy, window.WorkLayer);
        }

        // normal material mode
        else
        {
            bool brushSizeKey =
                KeyShortcuts.Activated(KeyShortcut.IncreaseBrushSize) || KeyShortcuts.Activated(KeyShortcut.DecreaseBrushSize);

            if (EditorWindow.IsKeyDown(ImGuiKey.ModShift) || brushSizeKey)
            {
                window.OverrideMouseWheel = true;

                if (Raylib.GetMouseWheelMove() > 0.0f || KeyShortcuts.Activated(KeyShortcut.IncreaseBrushSize))
                    materialBrushSize += 2;
                else if (Raylib.GetMouseWheelMove() < 0.0f || KeyShortcuts.Activated(KeyShortcut.DecreaseBrushSize))
                    materialBrushSize -= 2;
                
                materialBrushSize = Math.Clamp(materialBrushSize, 1, 21);
            }

            // draw grid cursor
            int cursorLeft = window.MouseCx - materialBrushSize / 2;
            int cursorTop = window.MouseCy - materialBrushSize / 2;

            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    cursorLeft * Level.TileSize,
                    cursorTop * Level.TileSize,
                    Level.TileSize * materialBrushSize,
                    Level.TileSize * materialBrushSize
                ),
                2f / window.ViewZoom,
                RainEd.Instance.MaterialDatabase.GetMaterial(selectedMaterial).Color
            );

            // place material
            int placeMode = 0;
            if (LeftMouseDown)
                placeMode = 1;
            else if (RightMouseDown)
                placeMode = 2;
            
            if (placeMode != 0 && (placeMode == 1 || !removedOnSameCell))
            {
                // place or remove materials inside cursor
                for (int x = cursorLeft; x <= window.MouseCx + materialBrushSize / 2; x++)
                {
                    for (int y = cursorTop; y <= window.MouseCy + materialBrushSize / 2; y++)
                    {
                        if (!level.IsInBounds(x, y)) continue;

                        ref var cell = ref level.Layers[window.WorkLayer, x, y];
                        if (cell.HasTile()) continue;

                        if (placeMode == 1)
                        {
                            if (!disallowMatOverwrite || cell.Material == 0)
                            {
                                if (modifyGeometry)
                                {
                                    level.Layers[window.WorkLayer, x, y].Geo = GeoType.Solid;
                                    window.InvalidateGeo(x, y, window.WorkLayer);
                                }

                                cell.Material = selectedMaterial;
                            }
                        }
                        else
                        {
                            if (!disallowMatOverwrite || cell.Material == selectedMaterial)
                            {
                                if (modifyGeometry)
                                {
                                    level.Layers[window.WorkLayer, x, y].Geo = GeoType.Air;
                                    window.InvalidateGeo(x, y, window.WorkLayer);
                                }

                                cell.Material = 0;
                                removedOnSameCell = true;
                            }
                        }
                    }
                }
            }
        }

        if (!isToolActive && wasToolActive)
        {
            window.CellChangeRecorder.TryPushChange();
            removedOnSameCell = false;
        }

        wasToolActive = isToolActive;
    }

    public override void IdleProcess()
    {
        base.IdleProcess();

        var matDb = RainEd.Instance.MaterialDatabase;
        
        // A/D to change selected group
        if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
        {
            selectedMatGroup = Util.Mod(selectedMatGroup - 1, matDb.Categories.Count);
            selectedMaterial = matDb.Categories[selectedMatGroup].Materials[0].ID;
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavRight))
        {
            selectedMatGroup = Util.Mod(selectedMatGroup + 1, matDb.Categories.Count);
            selectedMaterial = matDb.Categories[selectedMatGroup].Materials[0].ID;
        }

        // W/S to change selected tile in group
        if (KeyShortcuts.Activated(KeyShortcut.NavUp))
        {
            var mat = matDb.GetMaterial(selectedMaterial);
            var matList = mat.Category.Materials;
            selectedMaterial = matList[Util.Mod(matList.IndexOf(mat) - 1, matList.Count)].ID;
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavDown))
        {
            var mat = matDb.GetMaterial(selectedMaterial);
            var matList = mat.Category.Materials;
            selectedMaterial = matList[Util.Mod(matList.IndexOf(mat) + 1, matList.Count)].ID;
        }
    }

    public override void DrawToolbar()
    {
        catalog.Draw();
    }
}