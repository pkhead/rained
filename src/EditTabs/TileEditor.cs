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

    public TileEditor(EditorWindow window) {
        this.window = window;
        selectedTile = null;
    }

    public void DrawToolbar() {
        if (ImGui.Begin("Tile Selector"))
        {
            bool? headerOpenState = null;

            if (ImGui.Button("Collapse All"))
                headerOpenState = false;
            
            ImGui.SameLine();
            if (ImGui.Button("Expand All"))
                headerOpenState = true;
            
            ImGui.Text("Shift+Click to modify geometry");
            
            if (ImGui.BeginChild("List", ImGui.GetContentRegionAvail()))
            {
                foreach (var group in window.Editor.TileDatabase.Categories)
                {
                    if (headerOpenState is not null) ImGui.SetNextItemOpen(headerOpenState.GetValueOrDefault());
                    if (ImGui.CollapsingHeader(group.Name))
                    {
                        foreach (Tiles.TileData tile in group.Tiles)
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

    public void DrawViewport() {
        var level = window.Editor.Level;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));

        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == window.WorkLayer ? 255 : 50;
            var color = new Color(0, 0, 0, alpha);
            int offset = l * 2;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            level.RenderLayer(l, color);
            level.RenderTiles(l, alpha);
            Rlgl.PopMatrix();
        }

        level.RenderGrid(1f / window.ViewZoom);
        level.RenderBorder(1f / window.ViewZoom);

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
            // first of all, placement is impossible if tile center is out of bounds
            bool isPlacementValid = level.IsInBounds(window.MouseCx, window.MouseCy);
            var placeGeometry = Raylib.IsKeyDown(KeyboardKey.LeftShift);
            if (isPlacementValid)
            {
                for (int x = 0; x < selectedTile.Width; x++)
                {
                    for (int y = 0; y < selectedTile.Height; y++)
                    {
                        int gx = tileOriginX + x;
                        int gy = tileOriginY + y;
                        var specInt = selectedTile.Requirements[x,y];
                        var spec2Int = selectedTile.Requirements2[x,y];

                        // check that there is not already a tile here
                        if (level.IsInBounds(gx, gy))
                        {
                            // check on first layer
                            if (specInt >= 0 && level.Layers[window.WorkLayer, gx, gy].HasTile())
                            {
                                isPlacementValid = false;
                                goto exitRequirementLoop;
                            }

                            // check on second layer
                            if (window.WorkLayer < 2 && spec2Int >= 0 && level.Layers[window.WorkLayer+1, gx, gy].HasTile())
                            {
                                isPlacementValid = false;
                                goto exitRequirementLoop;
                            }
                        }

                        // if placeGeometry is true, the editor will place tile geometry
                        // that is needed, instead of having to rely on the user doing
                        // that themselves. thus, a geometry check isn't needed
                        if (!placeGeometry)
                        {
                            // check first layer geometry
                            if (specInt == -1) continue;
                            if (level.GetClamped(window.WorkLayer, gx, gy).Cell != (CellType) specInt)
                            {
                                isPlacementValid = false;
                                goto exitRequirementLoop;
                            }

                            // check second layer geometry
                            // if we are on layer 3, there is no second layer
                            // all checks pass
                            if (window.WorkLayer == 2) continue;
                            
                            if (spec2Int == -1) continue;
                            if (level.GetClamped(window.WorkLayer+1, gx, gy).Cell != (CellType) spec2Int)
                            {
                                isPlacementValid = false;
                                goto exitRequirementLoop;
                            }
                        }
                    }
                }
                exitRequirementLoop:;
            }

            // draw tile preview
            Raylib.DrawTextureEx(
                selectedTile.PreviewTexture,
                new Vector2(tileOriginX, tileOriginY) * Level.TileSize - new Vector2(2, 2),
                0,
                (float)Level.TileSize / 16,
                isPlacementValid ? new Color(255, 255, 255, 200) : new Color(255, 0, 0, 200)
            );

            if (window.IsViewportHovered)
            {
                // place tile on click
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && isPlacementValid)
                {
                    PlaceTile(
                        selectedTile,
                        tileOriginX, tileOriginY,
                        window.WorkLayer, window.MouseCx, window.MouseCy,
                        placeGeometry
                    );
                }

                // remove tile
                if (Raylib.IsMouseButtonPressed(MouseButton.Right))
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

                        RemoveTile(tileLayer, tileX, tileY, placeGeometry);
                    }
                }
            }
        }
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