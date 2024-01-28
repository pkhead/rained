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

    private bool placeGeometry = false;

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

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Place Geometry");
            ImGui.SameLine();
            ImGui.Checkbox("##PlaceGeometry", ref placeGeometry);
            
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
            var tileOriginX = window.MouseCx - (int)MathF.Ceiling((float)selectedTile.Width / 2) + 1;
            int tileOriginY = window.MouseCy - (int)MathF.Ceiling((float)selectedTile.Height / 2) + 1;

            // draw tile requirements
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
            bool isPlacementValid = level.IsInBounds(window.MouseCx, window.MouseCy);

            // if placeGeometry is true, the editor will place tile geometry
            // that is needed, instead of having to rely on the user doing
            // that themselves. thus, a validation check isn't needed
            if (isPlacementValid && !placeGeometry)
            {
                for (int x = 0; x < selectedTile.Width; x++)
                {
                    for (int y = 0; y < selectedTile.Height; y++)
                    {
                        int gx = tileOriginX + x;
                        int gy = tileOriginY + y;

                        // check first layer
                        var specInt = selectedTile.Requirements[x,y];
                        if (specInt == -1) continue;
                        if (level.GetClamped(window.WorkLayer, gx, gy).Cell != (CellType) specInt)
                        {
                            isPlacementValid = false;
                            goto exitRequirementLoop;
                        }

                        // check second layer
                        // if we are on layer 3, there is no second layer
                        // all checks pass
                        if (window.WorkLayer == 2) continue;
                        
                        var spec2Int = selectedTile.Requirements2[x,y];
                        if (spec2Int == -1) continue;
                        if (level.GetClamped(window.WorkLayer+1, gx, gy).Cell != (CellType) spec2Int)
                        {
                            isPlacementValid = false;
                            goto exitRequirementLoop;
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
        }
    }
}