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

        if (selectedTile is not null)
        {
            var tileOrigin = window.MouseCellFloat + new Vector2(0.5f, 0.5f) - new Vector2(selectedTile.Width, selectedTile.Height) / 2f;

            Raylib.DrawTextureEx(
                selectedTile.PreviewTexture,
                new Vector2(MathF.Floor(tileOrigin.X), MathF.Floor(tileOrigin.Y)) * Level.TileSize,
                0,
                (float)Level.TileSize / 16,
                Color.White
            );
        }
    }
}