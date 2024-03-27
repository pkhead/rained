using RainEd.Tiles;
using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using ImGuiNET;

namespace RainEd;

partial class TileEditor : IEditorMode
{
    public string Name { get => "Tiles"; }

    private readonly EditorWindow window;
    private Tile selectedTile;
    private int selectedMaterial = 1;
    private bool isToolActive = false;

    private SelectionMode selectionMode = SelectionMode.Materials;
    private SelectionMode? forceSelection = null;
    private int selectedTileGroup = 0;
    private int selectedMatGroup = 0;
    private int selectedAutotile = 0;

    private int materialBrushSize = 1;

    private bool isAutotileActive = false;
    private List<Vector2i> autotilePath = [];
    
    // this is used to fix force placement when
    // holding down lmb
    private int lastPlaceX = -1;
    private int lastPlaceY = -1;
    private int lastPlaceL = -1;
    
    public TileEditor(EditorWindow window) {
        this.window = window;
        selectedTile = RainEd.Instance.TileDatabase.Categories[0].Tiles[0];
        selectedMaterial = 1;
    }

    public void Load()
    {
        isToolActive = false;
        ProcessSearch(); // defined in TileEditorToolbar.cs
    }

    public void Unload()
    {
        window.CellChangeRecorder.TryPushChange();
        lastPlaceX = -1;
        lastPlaceY = -1;
        lastPlaceL = -1;
    }

    private static void DrawTile(int tileInt, int x, int y, float lineWidth, Color color)
    {
        if (tileInt == 0)
        {
            // air is represented by a cross (OMG ASCEND WITH GORB???)
            // an empty cell (-1) would mean any tile is accepted
            Raylib.DrawLineEx(
                startPos: new Vector2(x * Level.TileSize + 5, y * Level.TileSize + 5),
                endPos: new Vector2((x+1) * Level.TileSize - 5, (y+1) * Level.TileSize - 5),
                lineWidth,
                color
            );

            Raylib.DrawLineEx(
                startPos: new Vector2((x+1) * Level.TileSize - 5, y * Level.TileSize + 5),
                endPos: new Vector2(x * Level.TileSize + 5, (y+1) * Level.TileSize - 5),
                lineWidth,
                color
            );
        }
        else if (tileInt > 0)
        {
            var cellType = (GeoType) tileInt;
            switch (cellType)
            {
                case GeoType.Solid:
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                        lineWidth,
                        color
                    );
                    break;
                
                case GeoType.Platform:
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10),
                        lineWidth,
                        color
                    );
                    break;
                
                case GeoType.Glass:
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                        lineWidth,
                        color
                    );
                    break;

                case GeoType.ShortcutEntrance:
                    Raylib.DrawRectangleLinesEx(
                        new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                        lineWidth,
                        Color.Red
                    );
                    break;

                case GeoType.SlopeLeftDown:
                    Raylib.DrawTriangleLines(
                        new Vector2(x+1, y+1) * Level.TileSize,
                        new Vector2(x+1, y) * Level.TileSize,
                        new Vector2(x, y) * Level.TileSize,
                        color
                    );
                    break;

                case GeoType.SlopeLeftUp:
                    Raylib.DrawTriangleLines(
                        new Vector2(x, y+1) * Level.TileSize,
                        new Vector2(x+1, y+1) * Level.TileSize,
                        new Vector2(x+1, y) * Level.TileSize,
                        color
                    );
                    break;

                case GeoType.SlopeRightDown:
                    Raylib.DrawTriangleLines(
                        new Vector2(x+1, y) * Level.TileSize,
                        new Vector2(x, y) * Level.TileSize,
                        new Vector2(x, y+1) * Level.TileSize,
                        color
                    );
                    break;

                case GeoType.SlopeRightUp:
                    Raylib.DrawTriangleLines(
                        new Vector2(x+1, y+1) * Level.TileSize,
                        new Vector2(x, y) * Level.TileSize,
                        new Vector2(x, y+1) * Level.TileSize,
                        color
                    );
                    break;
            }
        }
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        window.BeginLevelScissorMode();

        var wasToolActive = isToolActive;
        isToolActive = false;

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;
        var tileDb = RainEd.Instance.TileDatabase;
        var matDb = RainEd.Instance.MaterialDatabase;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, EditorWindow.BackgroundColor);

        // draw layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrames[l]);

            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Rlgl.PushMatrix();
                levelRender.RenderGeometry(l, EditorWindow.GeoColor(255));
                levelRender.RenderTiles(l, 255);
            Rlgl.PopMatrix();
        }

        // draw alpha-blended result into main frame
        Raylib.BeginTextureMode(mainFrame);
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            Rlgl.PushMatrix();
            Rlgl.LoadIdentity();

            var alpha = l == window.WorkLayer ? 255 : 50;
            Raylib.DrawTextureRec(
                layerFrames[l].Texture,
                new Rectangle(0f, layerFrames[l].Texture.Height, layerFrames[l].Texture.Width, -layerFrames[l].Texture.Height),
                Vector2.Zero,
                new Color(255, 255, 255, alpha)
            );
            Rlgl.PopMatrix();
        }

        levelRender.RenderGrid();
        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();
        
        var modifyGeometry = KeyShortcuts.Active(KeyShortcut.TileForceGeometry);
        var forcePlace = KeyShortcuts.Active(KeyShortcut.TileForcePlacement);
        var disallowMatOverwrite = KeyShortcuts.Active(KeyShortcut.TileIgnoreDifferent);

        if (selectionMode == SelectionMode.Tiles || selectionMode == SelectionMode.Autotiles)
        {
            if (modifyGeometry)
                window.StatusText = "Force Geometry";
            else if (forcePlace)
                window.StatusText = "Force Placement";
        }
        else if (selectionMode == SelectionMode.Materials)
        {
            if (disallowMatOverwrite)
                    window.StatusText = "Disallow Overwrite";
        }

        if (window.IsViewportHovered)
        {
            // begin change if left or right button is down
            // regardless of what it's doing
            if (window.IsMouseDown(ImGuiMouseButton.Left) || window.IsMouseDown(ImGuiMouseButton.Right))
            {
                if (!wasToolActive) window.CellChangeRecorder.BeginChange();
                isToolActive = true;
            }

            // render selected tile
            if (selectionMode == SelectionMode.Tiles)
            {
                // mouse position is at center of tile
                // tileOrigin is the top-left of the tile, so some math to adjust
                //var tileOriginFloat = window.MouseCellFloat + new Vector2(0.5f, 0.5f) - new Vector2(selectedTile.Width, selectedTile.Height) / 2f;
                var tileOriginX = window.MouseCx - selectedTile.CenterX;
                int tileOriginY = window.MouseCy - selectedTile.CenterY;

                // draw tile requirements
                // second layer
                if (selectedTile.HasSecondLayer)
                {
                    for (int x = 0; x < selectedTile.Width; x++)
                    {
                        for (int y = 0; y < selectedTile.Height; y++)
                        {
                            Rlgl.PushMatrix();
                            Rlgl.Translatef(tileOriginX * Level.TileSize + 2, tileOriginY * Level.TileSize + 2, 0);

                            sbyte tileInt = selectedTile.Requirements2[x,y];
                            DrawTile(tileInt, x, y, 1f / window.ViewZoom, new Color(0, 255, 0, 255));
                            Rlgl.PopMatrix();
                        }
                    }
                }

                // first layer
                for (int x = 0; x < selectedTile.Width; x++)
                {
                    for (int y = 0; y < selectedTile.Height; y++)
                    {
                        Rlgl.PushMatrix();
                        Rlgl.Translatef(tileOriginX * Level.TileSize, tileOriginY * Level.TileSize, 0);

                        sbyte tileInt = selectedTile.Requirements[x,y];
                        DrawTile(tileInt, x, y, 1f / window.ViewZoom, new Color(255, 0, 0, 255));
                        Rlgl.PopMatrix();
                    }
                }

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
                Raylib.DrawTextureEx(
                    selectedTile.PreviewTexture,
                    new Vector2(tileOriginX, tileOriginY) * Level.TileSize - new Vector2(2, 2),
                    0,
                    (float)Level.TileSize / 16,
                    validationStatus == TilePlacementStatus.Success ? new Color(255, 255, 255, 200) : new Color(255, 0, 0, 200)
                );

                // place tile on click
                if (window.IsMouseDown(ImGuiMouseButton.Left))
                {
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
                        }
                    }
                    else if (window.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        string errStr = validationStatus switch {
                            TilePlacementStatus.OutOfBounds => "Tile is out of bounds",
                            TilePlacementStatus.Overlap => "Tile is overlapping another",
                            TilePlacementStatus.Geometry => "Tile geometry requirements not met",
                            _ => "Unknown tile placement error"
                        };

                        window.Editor.ShowNotification(errStr);
                    }
                }
            }

            // render selected material
            else if (selectionMode == SelectionMode.Materials)
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
                    1f / window.ViewZoom,
                    RainEd.Instance.MaterialDatabase.GetMaterial(selectedMaterial).Color
                );

                // place material
                int placeMode = 0;
                if (window.IsMouseDown(ImGuiMouseButton.Left))
                    placeMode = 1;
                else if (window.IsMouseDown(ImGuiMouseButton.Right))
                    placeMode = 2;
                
                if (placeMode != 0)
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
                                    cell.Material = selectedMaterial;
                            }
                            else
                            {
                                if (!disallowMatOverwrite || cell.Material == selectedMaterial)
                                    cell.Material = 0;
                            }
                        }
                    }
                }
            }

            // render selected autotile
            else if (selectionMode == SelectionMode.Autotiles && RainEd.Instance.PluginInterface.Autotiles.Count > 0)
            {
                var autotile = RainEd.Instance.PluginInterface.Autotiles[selectedAutotile];

                // activated
                if (isToolActive && !wasToolActive)
                {
                    isAutotileActive = true;
                    autotilePath.Clear();
                }

                if (isAutotileActive)
                {
                    // add current position to autotile path
                    // only add the position if it is adjacent to the last
                    // placed position
                    var posTuple = new Vector2i(window.MouseCx, window.MouseCy);
                    
                    if (autotilePath.Count == 0)
                    {
                        autotilePath.Add(posTuple);
                    }
                    else
                    {
                        if (!autotilePath.Contains(posTuple))
                        {
                            var lastPos = autotilePath[^1];
                            if (MathF.Abs(lastPos.X - posTuple.X) + MathF.Abs(lastPos.Y - posTuple.Y) == 1)
                            {
                                autotilePath.Add(posTuple);
                            }
                        }
                    }

                    // draw autotile path
                    foreach (var pos in autotilePath)
                    {
                        Raylib.DrawRectangleLinesEx(
                            new Rectangle(pos.X * Level.TileSize, pos.Y * Level.TileSize, Level.TileSize, Level.TileSize),
                            1f / window.ViewZoom,
                            Color.White
                        );
                    }

                    // mouse released
                    if (wasToolActive && !isToolActive)
                    {
                        RainEd.Logger.Information("Run autotile {Name}", autotile.Name);
                        
                        //window.CellChangeRecorder.BeginChange();
                        RainEd.Instance.PluginInterface.RunAutotile(autotile, window.WorkLayer, autotilePath, forcePlace, modifyGeometry);
                        //window.CellChangeRecorder.PushChange();

                        isAutotileActive = false;
                        autotilePath.Clear();
                    }
                }
            }

            if (window.IsMouseInLevel())
            {
                int tileLayer = window.WorkLayer;
                int tileX = window.MouseCx;
                int tileY = window.MouseCy;

                var mouseCell = level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy];

                // if this is a tile body, find referenced tile head
                if (mouseCell.HasTile() && mouseCell.TileHead is null)
                {
                    tileLayer = mouseCell.TileLayer;
                    tileX = mouseCell.TileRootX;
                    tileY = mouseCell.TileRootY;
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
                            throw new Exception("Could not find tile head");
                        }
                        else
                        {
                            forceSelection = SelectionMode.Tiles;
                            selectedTile = tile;
                            selectedTileGroup = selectedTile.Category.Index;
                        }
                    }

                    // material eyedropper
                    else
                    {
                        if (mouseCell.Material > 0)
                        {
                            selectedMaterial = mouseCell.Material;
                            var matInfo = matDb.GetMaterial(selectedMaterial);

                            // select tile group that contains this material
                            var idx = matDb.Categories.IndexOf(matInfo.Category);
                            if (idx == -1)
                            {
                                RainEd.Instance.ShowNotification("Error");
                                RainEd.Logger.Error("Error eyedropping material '{MaterialName}' (ID {ID})", matInfo.Name, selectedMaterial);
                            }
                            else
                            {
                                selectedMatGroup = idx;
                                forceSelection = SelectionMode.Materials;
                            }
                        }
                    }
                }

                // remove tile on right click
                if (selectionMode == SelectionMode.Tiles && window.IsMouseDown(ImGuiMouseButton.Right) && mouseCell.HasTile())
                {
                    level.RemoveTile(tileLayer, tileX, tileY, modifyGeometry);
                }
            }
        }

        if (wasToolActive && !isToolActive)
        {
            window.CellChangeRecorder.PushChange();
            lastPlaceX = -1;
            lastPlaceY = -1;
            lastPlaceL = -1;
        }
        
        Raylib.EndScissorMode();
    }
}