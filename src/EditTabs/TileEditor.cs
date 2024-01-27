using System.Numerics;
using ImGuiNET;

namespace RainEd;

public class TileEditor
{
    public bool IsWindowOpen = true;
    private readonly RainEd editor;

    public TileEditor(RainEd editor) {
        this.editor = editor;
    }

    public void Render() {
        if (IsWindowOpen && ImGui.Begin("Tile Editor", ref IsWindowOpen))
        {
            bool? headerOpenState = null;

            if (ImGui.Button("Collapse All"))
                headerOpenState = false;
            
            ImGui.SameLine();
            if (ImGui.Button("Expand All"))
                headerOpenState = true;
            
            if (ImGui.BeginChild("List", ImGui.GetContentRegionAvail()))
            {
                foreach (var group in editor.TileDatabase.Categories)
                {
                    if (headerOpenState is not null) ImGui.SetNextItemOpen(headerOpenState.GetValueOrDefault());
                    if (ImGui.CollapsingHeader(group.Name))
                    {
                        foreach (var tile in group.Tiles)
                        {
                            ImGui.Selectable(tile.Name);
                        }
                    }
                }
            } ImGui.EndChild();
        }

        ImGui.End();
    }
}