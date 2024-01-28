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
            var tileOrigin = window.MouseCellFloat + new Vector2(0.5f, 0.5f) - new Vector2(selectedTile.Width, selectedTile.Height) / 2f;

            // draw tile requirements
            for (int x = 0; x < selectedTile.Width; x++)
            {
                for (int y = 0; y < selectedTile.Height; y++)
                {
                    Rlgl.PushMatrix();
                    Rlgl.Translatef((int)tileOrigin.X * Level.TileSize + 2, (int)tileOrigin.Y * Level.TileSize + 2, 0);

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
                    Rlgl.Translatef((int)tileOrigin.X * Level.TileSize, (int)tileOrigin.Y * Level.TileSize, 0);

                    sbyte tileInt = selectedTile.Requirements[x,y];
                    drawTile(tileInt, x, y, 1f / window.ViewZoom, new Color(0, 0, 0, 255));
                    Rlgl.PopMatrix();
                }
            }

            // draw tile preview
            Raylib.DrawTextureEx(
                selectedTile.PreviewTexture,
                new Vector2(MathF.Floor(tileOrigin.X), MathF.Floor(tileOrigin.Y)) * Level.TileSize - new Vector2(2, 2),
                0,
                (float)Level.TileSize / 16,
                new Color(255, 255, 255, 200)
            );
        }
    }
}