using System.Numerics;
using Raylib_cs;
using ImGuiNET;

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

        if (selectedTile is not null)
            Raylib.DrawTexture(selectedTile.PreviewTexture, window.MouseCx * Level.TileSize, window.MouseCy * Level.TileSize, Color.White);
    }
}