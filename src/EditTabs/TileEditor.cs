using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;

namespace RainEd;

public class TileEditor : IEditorMode
{
    public string Name { get => "Tiles"; }

    private readonly EditorWindow window;
    private Tiles.TileData? selectedTile;
    private int selectedMaterialIdx = 0;
    private bool isToolActive = false;

    private string searchQuery = "";

    public TileEditor(EditorWindow window) {
        this.window = window;
        selectedTile = null;
    }

    public void DrawToolbar() {
        if (ImGui.Begin("Tile Selector", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // default material dropdown
            ImGui.Text("Default Material");
            int defaultMat = (int) window.Editor.Level.DefaultMaterial - 1;
            ImGui.Combo("##DefaultMaterial", ref defaultMat, Level.MaterialNames, Level.MaterialNames.Length, 999999);
            window.Editor.Level.DefaultMaterial = (Material) defaultMat + 1;
            
            bool? headerOpenState = null;

            if (ImGui.Button("Collapse All"))
                headerOpenState = false;
            
            ImGui.SameLine();
            if (ImGui.Button("Expand All"))
                headerOpenState = true;

            ImGui.SameLine();
            var right = ImGui.GetCursorPosX();
            ImGui.NewLine();

            ImGui.SetNextItemWidth(right - ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputTextWithHint("##search", "Search...", ref searchQuery, 64, ImGuiInputTextFlags.AlwaysOverwrite);
            var searchQueryL = searchQuery.ToLower();

            // the tiles in the group that pass search test
            var tilesInGroup = new List<Tiles.TileData>();
            var materialsInGroup = new List<int>();
            
            if (ImGui.BeginChild("List", ImGui.GetContentRegionAvail()))
            {
                // get list of materials that match search
                for (int i = 0; i < Level.MaterialNames.Length; i++)
                {
                    var name = Level.MaterialNames[i];
                    if (searchQuery.Length == 0 || name.ToLower().Contains(searchQueryL))
                        materialsInGroup.Add(i);
                }

                // materials section
                if (materialsInGroup.Count > 0 && ImGui.CollapsingHeader("Materials"))
                {
                    foreach (int i in materialsInGroup)
                    {
                        var name = Level.MaterialNames[i];
                        var isSelected = selectedTile == null && selectedMaterialIdx == i;
                        if (ImGui.Selectable(name, isSelected))
                        {
                            selectedTile = null;
                            selectedMaterialIdx = i;
                        }
                    }
                }

                foreach (var group in window.Editor.TileDatabase.Categories)
                {
                    bool groupNameInQuery = searchQuery.Length == 0 || group.Name.ToLower().Contains(searchQueryL);

                    // get a list of the tiles that are in query
                    tilesInGroup.Clear();
                    foreach (Tiles.TileData tile in group.Tiles)
                    {
                        if (searchQuery.Length == 0 || tile.Name.ToLower().Contains(searchQueryL))
                        {
                            tilesInGroup.Add(tile);
                        }
                    }

                    if (headerOpenState is not null) ImGui.SetNextItemOpen(headerOpenState.GetValueOrDefault());
                    if ((groupNameInQuery || tilesInGroup.Count > 0) && ImGui.CollapsingHeader(group.Name))
                    {
                        foreach (var tile in tilesInGroup)
                        {
                            if (ImGui.Selectable(tile.Name, selectedTile is not null && selectedTile == tile))
                            {
                                selectedTile = tile;
                            }

                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                rlImGui.Image(tile.PreviewTexture);
                                ImGui.EndTooltip();
                            }
                        }
                    }
                }
            } ImGui.EndChild();
        }
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame) {
        var wasToolActive = isToolActive;
        isToolActive = false;

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));

        // draw layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrame);

            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            levelRender.RenderGeometry(l, new Color(0, 0, 0, 255));
            levelRender.RenderTiles(l, 255);
            
            // draw alpha-blended result into main frame
            Raylib.BeginTextureMode(mainFrame);
            Rlgl.PushMatrix();
            Rlgl.LoadIdentity();

            int offset = l * 2;
            var alpha = l == window.WorkLayer ? 255 : 50;
            Raylib.DrawTextureRec(
                layerFrame.Texture,
                new Rectangle(0f, layerFrame.Texture.Height, layerFrame.Texture.Width, -layerFrame.Texture.Height),
                Vector2.One * offset,
                new Color(255, 255, 255, alpha)
            );
            Rlgl.PopMatrix();
        }

        levelRender.RenderGrid(0.5f / window.ViewZoom);
        levelRender.RenderBorder(1.0f / window.ViewZoom);

        static void drawTile(int tileInt, int x, int y, float lineWidth, Color color)
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
                var cellType = (CellType) tileInt;
                switch (cellType)
                {
                    case CellType.Solid:
                        Raylib.DrawRectangleLinesEx(
                            new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                            lineWidth,
                            color
                        );
                        break;
                    
                    case CellType.Platform:
                        Raylib.DrawRectangleLinesEx(
                            new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10),
                            lineWidth,
                            color
                        );
                        break;
                    
                    case CellType.Glass:
                        Raylib.DrawRectangleLinesEx(
                            new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                            lineWidth,
                            color
                        );
                        break;

                    case CellType.ShortcutEntrance:
                        Raylib.DrawRectangleLinesEx(
                            new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                            lineWidth,
                            Color.Red
                        );
                        break;

                    case CellType.SlopeLeftDown:
                        Raylib.DrawTriangleLines(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeLeftUp:
                        Raylib.DrawTriangleLines(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightDown:
                        Raylib.DrawTriangleLines(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            color
                        );
                        break;

                    case CellType.SlopeRightUp:
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

        if (window.IsViewportHovered)
        {
            var modifyGeometry = Raylib.IsKeyDown(KeyboardKey.G);
            var forcePlace = Raylib.IsKeyDown(KeyboardKey.F);

            // render selected tile
            if (selectedTile is not null)
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
                            drawTile(tileInt, x, y, 1f / window.ViewZoom, new Color(0, 255, 0, 255));
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
                        drawTile(tileInt, x, y, 1f / window.ViewZoom, new Color(255, 0, 0, 255));
                        Rlgl.PopMatrix();
                    }
                }

                // check if requirements are satisfied
                TilePlacementStatus validationStatus = TilePlacementStatus.Success;

                if (level.IsInBounds(window.MouseCx, window.MouseCy))
                    validationStatus = ValidateTilePlacement(selectedTile, tileOriginX, tileOriginY, modifyGeometry || forcePlace);
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

                if (modifyGeometry)
                    ImGui.SetTooltip("Force Geometry");
                else if (forcePlace)
                    ImGui.SetTooltip("Force Placement");

                // place tile on click
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (validationStatus == TilePlacementStatus.Success)
                    {
                        window.Editor.BeginChange();

                        PlaceTile(
                            selectedTile,
                            tileOriginX, tileOriginY,
                            window.WorkLayer, window.MouseCx, window.MouseCy,
                            modifyGeometry
                        );

                        window.Editor.EndChange();
                    }
                    else
                    {
                        string errStr = validationStatus switch {
                            TilePlacementStatus.OutOfBounds => "Tile is out of bounds",
                            TilePlacementStatus.Overlap => "Tile is overlapping another",
                            TilePlacementStatus.Geometry => "Tile geometry requirements not met",
                            _ => "Unknown tile placement error"
                        };

                        window.Editor.ShowError(errStr);
                    }
                }
            }

            // render selected material
            else if (window.IsMouseInLevel())
            {
                // draw grid cursor
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(window.MouseCx * Level.TileSize, window.MouseCy * Level.TileSize, Level.TileSize, Level.TileSize),
                    1f / window.ViewZoom,
                    LevelRenderer.MaterialColors[selectedMaterialIdx]
                );

                // place material
                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    if (!wasToolActive) window.Editor.BeginChange();
                    isToolActive = true;
                    level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy].Material = (Material) selectedMaterialIdx + 1;
                }

                // remove material
                if (Raylib.IsMouseButtonDown(MouseButton.Right) &&
                    !level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy].HasTile()
                )
                {
                    if (!wasToolActive) window.Editor.BeginChange();
                    isToolActive = true;
                    level.Layers[window.WorkLayer, window.MouseCx, window.MouseCy].Material = Material.None;
                }
            }

            // remove tile on right click
            if (window.IsMouseInLevel() && Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                int tileLayer = window.WorkLayer;
                int tileX = window.MouseCx;
                int tileY = window.MouseCy;
                
                var mouseCell = level.Layers[tileLayer, tileX, tileY];
                if (mouseCell.HasTile())
                {
                    // if this is a tile body, go to referenced tile head
                    if (mouseCell.TileHead is null)
                    {
                        tileLayer = mouseCell.TileLayer;
                        tileX = mouseCell.TileRootX;
                        tileY = mouseCell.TileRootY;
                    }

                    window.Editor.BeginChange();
                    RemoveTile(tileLayer, tileX, tileY, modifyGeometry);
                    window.Editor.EndChange();
                }
            }
        }

        if (wasToolActive && !isToolActive)
            window.Editor.EndChange();
    }

    private enum TilePlacementStatus
    {
        Success,
        OutOfBounds,
        Overlap,
        Geometry
    };

    private TilePlacementStatus ValidateTilePlacement(Tiles.TileData tile, int tileLeft, int tileTop, bool force)
    {
        var level = window.Editor.Level;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                var specInt = tile.Requirements[x,y];
                var spec2Int = tile.Requirements2[x,y];

                // check that there is not already a tile here
                if (level.IsInBounds(gx, gy))
                {
                    // check on first layer
                    var isHead = x == tile.CenterX && y == tile.CenterY;

                    if ((isHead || specInt >= 0) && level.Layers[window.WorkLayer, gx, gy].HasTile())
                        return TilePlacementStatus.Overlap;

                    // check on second layer
                    if (window.WorkLayer < 2)
                    {
                        if ((isHead || spec2Int >= 0) && level.Layers[window.WorkLayer+1, gx, gy].HasTile())
                            return TilePlacementStatus.Overlap;
                    }
                }

                
                if (!force)
                {
                    // check first layer geometry
                    if (specInt == -1) continue;
                    if (level.GetClamped(window.WorkLayer, gx, gy).Cell != (CellType) specInt)
                        return TilePlacementStatus.Geometry;

                    // check second layer geometry
                    // if we are on layer 3, there is no second layer
                    // all checks pass
                    if (window.WorkLayer == 2) continue;
                    
                    if (spec2Int == -1) continue;
                    if (level.GetClamped(window.WorkLayer+1, gx, gy).Cell != (CellType) spec2Int)
                        return TilePlacementStatus.Geometry;
                }
            }
        }
        
        return TilePlacementStatus.Success;
    }

    private void PlaceTile(
        Tiles.TileData tile,
        int tileLeft, int tileTop,
        int layer, int tileRootX, int tileRootY,
        bool placeGeometry
    )
    {
        var level = window.Editor.Level;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!level.IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];

                if (placeGeometry)
                {
                    // place first layer    
                    if (specInt >= 0)
                    {
                        level.Layers[layer, gx, gy].Cell = (CellType) specInt;
                    }

                    // place second layer
                    if (layer < 2 && spec2Int >= 0)
                    {
                        level.Layers[layer+1, gx, gy].Cell = (CellType) spec2Int;
                    }
                }

                // tile first 
                if (specInt >= 0)
                {
                    level.Layers[layer, gx, gy].TileRootX = tileRootX;
                    level.Layers[layer, gx, gy].TileRootY = tileRootY;
                    level.Layers[layer, gx, gy].TileLayer = layer;
                }

                // tile second layer
                if (spec2Int >= 0 && layer < 2)
                {
                    level.Layers[layer+1, gx, gy].TileRootX = tileRootX;
                    level.Layers[layer+1, gx, gy].TileRootY = tileRootY;
                    level.Layers[layer+1, gx, gy].TileLayer = layer;
                }
            }
        }

        // place tile root
        level.Layers[layer, tileRootX, tileRootY].TileHead = tile;
    }

    private void RemoveTile(int layer, int tileRootX, int tileRootY, bool removeGeometry)
    {
        var level = window.Editor.Level;
        var tile = level.Layers[layer, tileRootX, tileRootY].TileHead;
        if (tile == null) throw new Exception("Attempt to remove unknown tile");

        int tileLeft = tileRootX - tile.CenterX;
        int tileTop = tileRootY - tile.CenterY;

        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                int gx = tileLeft + x;
                int gy = tileTop + y;
                if (!level.IsInBounds(gx, gy)) continue;

                int specInt = tile.Requirements[x,y];
                int spec2Int = tile.Requirements2[x,y];
                
                // remove tile bodies
                if (specInt >= 0)
                {
                    level.Layers[layer, gx, gy].TileRootX = -1;
                    level.Layers[layer, gx, gy].TileRootY = -1;
                    level.Layers[layer, gx, gy].TileLayer = -1;
                }

                if (spec2Int >= 0 && layer < 2)
                {
                    level.Layers[layer+1, gx, gy].TileRootX = -1;
                    level.Layers[layer+1, gx, gy].TileRootY = -1;
                    level.Layers[layer+1, gx, gy].TileLayer = -1;
                }

                // remove geometry
                if (removeGeometry)
                {
                    if (specInt >= 0)
                        level.Layers[layer, gx, gy].Cell = CellType.Air;

                    if (spec2Int >= 0 && layer < 2)
                        level.Layers[layer+1, gx, gy].Cell = CellType.Air;
                }
            }
        }

        // remove tile root
        level.Layers[layer, tileRootX, tileRootY].TileHead = null;
    }
}