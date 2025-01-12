namespace Rained.EditorGui.Editors;
using Raylib_cs;
using System.Numerics;
using Rained.LevelData;
using Rained.Assets;
using ImGuiNET;
using Rained.Rendering;

class TileEditMode : TileEditorMode, ITileSelectionState
{
    public override string TabName => "Tiles";
    
    private int selectedTileGroup = 0;
    private Tile selectedTile = RainEd.Instance.TileDatabase.Categories[0].Tiles[0];

    public int SelectedTileGroup { get => selectedTileGroup; set => selectedTileGroup = value; }
    public Tile? SelectedTile { get => selectedTile; }
    public void SelectTile(Tile tile)
    {
        selectedTile = tile;
    }

    private bool placeTiles = false;
    private bool placeTilesJustStarted = false;

    // this is used to fix force placement when
    // holding down lmb
    private int lastPlaceX = -1;
    private int lastPlaceY = -1;
    private int lastPlaceL = -1;

    // this bool makes it so only one item (material, tile) can be removed
    // while the momuse is hovered over the same cell
    private bool removedOnSameCell = false;
    private int lastMouseX = -1;
    private int lastMouseY = -1;

    private bool wasToolActive = false;

    private readonly TileCatalogWidget catalogWidget;
    public TileEditMode(TileEditor editor) : base(editor)
    {
        catalogWidget = new TileCatalogWidget(this);
    }

    public override void UndidOrRedid()
    {
        base.UndidOrRedid();
        removedOnSameCell = false;
    }

    public override void Focus()
    {
        base.Focus();
        catalogWidget.ProcessSearch();
        placeTiles = false;
    }

    public override void Unfocus()
    {
        base.Unfocus();
        lastPlaceX = -1;
        lastPlaceY = -1;
        lastPlaceL = -1;

        wasToolActive = false;
        placeTiles = false;
    }

    public override void Process()
    {
        base.Process();
        
        var level = RainEd.Instance.Level;
        var window = RainEd.Instance.LevelView;
        var placeChainholder = false;

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

        var forcePlace = editor.PlacementFlags.HasFlag(TilePlacementFlags.Force);
        var modifyGeometry = editor.PlacementFlags.HasFlag(TilePlacementFlags.Geometry);
        var disallowMatOverwrite = editor.PlacementFlags.HasFlag(TilePlacementFlags.SameOnly);

        // mouse position is at center of tile
        // tileOrigin is the top-left of the tile, so some math to adjust
        //var tileOriginFloat = window.MouseCellFloat + new Vector2(0.5f, 0.5f) - new Vector2(selectedTile.Width, selectedTile.Height) / 2f;
        var tileOriginX = window.MouseCx - selectedTile.CenterX;
        int tileOriginY = window.MouseCy - selectedTile.CenterY;

        // rect place mode
        if (rectMode != RectMode.Inactive)
        {
            var tileWidth = selectedTile.Width;
            var tileHeight = selectedTile.Height;

            if (rectMode == RectMode.Remove)
            {
                tileWidth = 1;
                tileHeight = 1;
            }

            var gridW = (float)(window.MouseCellFloat.X - rectStart.X) / tileWidth;
            var gridH = (float)(window.MouseCellFloat.Y - rectStart.Y) / tileHeight;
            var rectW = (int) (gridW > 0f ? MathF.Ceiling(gridW) : (MathF.Floor(gridW) - 1)) * tileWidth;
            var rectH = (int) (gridH > 0f ? MathF.Ceiling(gridH) : (MathF.Floor(gridH) - 1)) * tileHeight;

            // update minX and minY to fit new rect size
            float minX, minY;

            if (gridW > 0f)
                minX = rectStart.X;
            else
                minX = rectStart.X + rectW + tileWidth;
            
            if (gridH > 0f)
                minY = rectStart.Y;
            else
                minY = rectStart.Y + rectH + tileHeight;

            Raylib.DrawRectangleLinesEx(
                new Rectangle(minX * Level.TileSize, minY * Level.TileSize, Math.Abs(rectW) * Level.TileSize, Math.Abs(rectH) * Level.TileSize),
                1f / window.ViewZoom,
                Color.White
            );

            if (!isToolActive)
            {
                // place tiles
                if (rectMode == RectMode.Place)
                {
                    for (int x = 0; x < Math.Abs(rectW); x += tileWidth)
                    {
                        for (int y = 0; y < Math.Abs(rectH); y += tileHeight)
                        {
                            var tileRootX = (int)minX + x + selectedTile.CenterX;
                            var tileRootY = (int)minY + y + selectedTile.CenterY;
                            if (!level.IsInBounds(tileRootX, tileRootY)) continue;

                            var status = level.ValidateTilePlacement(
                                selectedTile,
                                tileRootX - selectedTile.CenterX, tileRootY - selectedTile.CenterY, window.WorkLayer,
                                modifyGeometry || forcePlace
                            );

                            if (status == TilePlacementStatus.Success)
                            {
                                level.PlaceTile(selectedTile, window.WorkLayer, tileRootX, tileRootY, modifyGeometry);
                            }
                        }
                    }
                }

                // remove tiles
                else
                {
                    for (int x = 0; x < Math.Abs(rectW); x++)
                    {
                        for (int y = 0; y < Math.Abs(rectH); y++)
                        {
                            var gx = (int)minX + x;
                            var gy = (int)minY + y;
                            if (!level.IsInBounds(gx, gy)) continue;

                            if (level.Layers[window.WorkLayer, gx, gy].HasTile())
                                level.RemoveTileCell(window.WorkLayer, gx, gy, modifyGeometry);
                            
                            if (!disallowMatOverwrite)
                                level.Layers[window.WorkLayer, gx, gy].Material = 0;
                        }
                    }
                }

                rectMode = RectMode.Inactive;
            }

            // if the force geo button is tapped, will geoify tiles
            // instead of placing new ones
            else if (KeyShortcuts.Deactivated(KeyShortcut.TileForceGeometry) && editor.WasModifierTapped)
            {
                for (int x = 0; x < Math.Abs(rectW); x++)
                {
                    for (int y = 0; y < Math.Abs(rectH); y++)
                    {
                        var gx = (int)minX + x;
                        var gy = (int)minY + y;
                        if (!level.IsInBounds(gx, gy)) continue;
                        GeoifyTile(gx, gy, window.WorkLayer);
                    }
                }

                rectMode = RectMode.Inactive;
            }
        }

        else if (isToolActive && !wasToolActive)
        {
            // check if rect place mode will start
            if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
            {
                int rectOffsetX = 0;
                int rectOffsetY = 0;

                if (LeftMouseDown)
                {
                    rectMode = RectMode.Place;
                    rectOffsetX = -selectedTile.CenterX;
                    rectOffsetY = -selectedTile.CenterY;
                }
                else if (RightMouseDown)
                {
                    rectMode = RectMode.Remove;
                }

                if (rectMode != RectMode.Inactive)
                    rectStart = new CellPosition(window.MouseCx + rectOffsetX, window.MouseCy + rectOffsetY, window.WorkLayer);
            }

            else // start tile place mode
            {
                if (placeTiles = LeftMouseDown)
                    placeTilesJustStarted = true;
            }
        }

        // single-tile place mode
        else
        {
            // draw tile requirements
            TileRenderer.DrawTileSpecs(selectedTile, tileOriginX, tileOriginY);

            // check if requirements are satisfied
            TilePlacementStatus validationStatus;

            if (level.IsInBounds(window.MouseCx, window.MouseCy))
                validationStatus = level.ValidateTilePlacement(
                    selectedTile,
                    tileOriginX, tileOriginY, window.WorkLayer,
                    modifyGeometry || forcePlace
                );
            else
                validationStatus = TilePlacementStatus.OutOfBounds;
            
            // draw tile preview
            Rectangle srcRect, dstRect;
            dstRect = new Rectangle(
                new Vector2(tileOriginX, tileOriginY) * Level.TileSize - new Vector2(2, 2),
                new Vector2(selectedTile.Width, selectedTile.Height) * Level.TileSize
            );

            var tileTexFound = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(selectedTile, out var previewTexture, out var previewRect);
            if (tileTexFound)
            {
                //srcRect = new Rectangle(Vector2.Zero, new Vector2(selectedTile.Width * 16, selectedTile.Height * 16));
                srcRect = previewRect!.Value;
                Raylib.EndShaderMode();
            }
            else
            {
                srcRect = new Rectangle(Vector2.Zero, new Vector2(selectedTile.Width * 2, selectedTile.Height * 2));
                Raylib.BeginShaderMode(Shaders.UvRepeatShader);
            }

            // draw tile preview
            Raylib.DrawTexturePro(
                previewTexture ?? RainEd.Instance.PlaceholderTexture,
                srcRect, dstRect,
                Vector2.Zero, 0f,
                validationStatus == TilePlacementStatus.Success ? new Color(255, 255, 255, 200) : new Color(255, 0, 0, 200)
            );

            Raylib.EndShaderMode();

            // place tile on click
            if (placeTiles)
            {
                if (!LeftMouseDown) placeTiles = false;

                if (validationStatus == TilePlacementStatus.Success)
                {
                    // extra if statement to prevent overwriting the already placed tile
                    // when holding down LMB
                    if (lastPlaceX == -1 || !(modifyGeometry || forcePlace) || !level.IsIntersectingTile(
                        selectedTile,
                        tileOriginX, tileOriginY, window.WorkLayer,
                        lastPlaceX, lastPlaceY, lastPlaceL
                    ))
                    {
                        level.PlaceTile(
                            selectedTile,
                            window.WorkLayer, window.MouseCx, window.MouseCy,
                            modifyGeometry
                        );

                        lastPlaceX = window.MouseCx;
                        lastPlaceY = window.MouseCy;
                        lastPlaceL = window.WorkLayer;

                        if (selectedTile.Tags.Contains("Chain Holder"))
                        {
                            editor.BeginChainAttach(lastPlaceX, lastPlaceY, lastPlaceL);
                            placeTiles = false;
                            placeChainholder = true;
                        }
                    }
                }
                else if (placeTilesJustStarted)
                {
                    string errStr = validationStatus switch {
                        TilePlacementStatus.OutOfBounds => "Tile root is out of bounds",
                        TilePlacementStatus.Overlap => "Tile is overlapping another",
                        TilePlacementStatus.Geometry => "Tile geometry requirements not met",
                        _ => "Unknown tile placement error"
                    };

                    EditorWindow.ShowNotification(errStr);
                }

                placeTilesJustStarted = false;
            }

            // remove material under mouse cursor
            if (!removedOnSameCell && RightMouseDown && level.IsInBounds(window.MouseCx, window.MouseCy))
            {
                ref var cell = ref level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy];
                if (!cell.HasTile() && !disallowMatOverwrite)
                {
                    cell.Material = 0;
                    removedOnSameCell = true;
                }
            }
        }

        if (!isToolActive && wasToolActive)
        {
            window.CellChangeRecorder.TryPushChange();
            lastPlaceX = -1;
            lastPlaceY = -1;
            lastPlaceL = -1;
            removedOnSameCell = false;
        }

        wasToolActive = isToolActive;

        if (placeChainholder)
        {
            wasToolActive = false;
        }
    }

    public override void IdleProcess()
    {
        base.IdleProcess();

        var tileDb = RainEd.Instance.TileDatabase;

        // A/D to change selected group
        if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
        {
            selectedTileGroup = Util.Mod(selectedTileGroup - 1, tileDb.Categories.Count);
            selectedTile = tileDb.Categories[selectedTileGroup].Tiles[0];
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavRight))
        {
            selectedTileGroup = Util.Mod(selectedTileGroup + 1, tileDb.Categories.Count);
            selectedTile = tileDb.Categories[selectedTileGroup].Tiles[0];
        }

        // W/S to change selected tile in group
        if (KeyShortcuts.Activated(KeyShortcut.NavUp))
        {
            var tileList = selectedTile.Category.Tiles;
            selectedTile = tileList[Util.Mod(tileList.IndexOf(selectedTile) - 1, tileList.Count)];
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavDown))
        {
            var tileList = selectedTile.Category.Tiles;
            selectedTile = tileList[Util.Mod(tileList.IndexOf(selectedTile) + 1, tileList.Count)];
        }
    }

    public override void DrawToolbar()
    {
        catalogWidget.Draw();
    }

    private void GeoifyTile(int x, int y, int layer)
    {
        var level = RainEd.Instance.Level;
        var window = RainEd.Instance.LevelView;

        if (!level.Layers[layer, x, y].HasTile()) return;

        Tile? tile = level.GetTile(layer, x, y);
        if (tile is null)
        {
            Log.Error("GeoifyTile: Tile not found?");
            return;
        }

        var tileHeadPos = level.GetTileHead(layer, x, y);
        var localX = x - tileHeadPos.X + tile.CenterX;
        var localY = y - tileHeadPos.Y + tile.CenterY;
        var localZ = layer - tileHeadPos.Layer;
        
        sbyte[,] requirements;
        if (localZ == 0)
            requirements = tile.Requirements;
        else if (localZ == 1)
            requirements = tile.Requirements2;
        else
        {
            Log.Error("GeoifyTile: localZ is not 0 or 1");
            return;
        }

        level.Layers[layer, x, y].Geo = (GeoType) requirements[localX, localY];
        window.InvalidateGeo(x, y, layer);
    }
}