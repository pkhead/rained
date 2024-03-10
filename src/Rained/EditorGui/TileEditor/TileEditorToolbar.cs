using ImGuiNET;
using System.Numerics;
using rlImGui_cs;

namespace RainEd;

partial class TileEditor : IEditorMode
{
    enum SelectionMode
    {
        Materials, Tiles
    }
    private SelectionMode selectionMode = SelectionMode.Materials;
    private SelectionMode? forceSelection = null;
    private int selectedTileGroup = 0;
    private int selectedMatGroup = 0;
    private string searchQuery = "";

    // available groups (available = passes search)
    private readonly List<int> matSearchResults = [];
    private readonly List<int> tileSearchResults = [];
    private void ProcessSearch()
    {
        var tileDb = window.Editor.TileDatabase;
        var matDb = window.Editor.MaterialDatabase;

        tileSearchResults.Clear();
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

        // find groups that have any entries that pass the search query
        for (int i = 0; i < tileDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the search query
            if (searchQuery == "")
            {
                tileSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the tiles in this group
            // if there is one tile that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < tileDb.Categories[i].Tiles.Count; j++)
            {
                // this tile passes the search, so add this group to the search results
                if (tileDb.Categories[i].Tiles[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    tileSearchResults.Add(i);
                    break;
                }
            }
        }
    }

    public void DrawToolbar()
    {
        var tileDb = window.Editor.TileDatabase;
        var matDb = window.Editor.MaterialDatabase;

        if (ImGui.Begin("Tile Selector", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // work layer
            {
                int workLayerV = window.WorkLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("Work Layer", ref workLayerV);
                window.WorkLayer = Math.Clamp(workLayerV, 1, 3) - 1;
            }

            // default material button (or press E)
            int defaultMat = window.Editor.Level.DefaultMaterial;
            ImGui.TextUnformatted($"Default Material: {matDb.GetMaterial(defaultMat).Name}");

            if (selectionMode != SelectionMode.Materials)
                ImGui.BeginDisabled();
            
            if ((ImGui.Button("Set Selected Material as Default") || EditorWindow.IsKeyPressed(ImGuiKey.Q)) && selectionMode == SelectionMode.Materials)
            {
                var oldMat = window.Editor.Level.DefaultMaterial;
                var newMat = selectedMaterial;
                window.Editor.Level.DefaultMaterial = newMat;

                if (oldMat != newMat)
                    RainEd.Instance.ChangeHistory.Push(new ChangeHistory.DefaultMaterialChangeRecord(oldMat, newMat));
            }

            if (selectionMode != SelectionMode.Materials)
                ImGui.EndDisabled();

            if (ImGui.IsItemHovered() && selectionMode != SelectionMode.Materials)
            {
                ImGui.SetTooltip("A material is not selected");
            }

            // search bar
            var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

            if (ImGui.BeginTabBar("TileSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;

                ImGuiTabItemFlags materialsFlags = ImGuiTabItemFlags.None;
                ImGuiTabItemFlags tilesFlags = ImGuiTabItemFlags.None;

                // apply force selection
                if (forceSelection == SelectionMode.Materials)
                    materialsFlags = ImGuiTabItemFlags.SetSelected;
                else if (forceSelection == SelectionMode.Tiles)
                    tilesFlags = ImGuiTabItemFlags.SetSelected;

                if (ImGuiExt.BeginTabItem("Materials", materialsFlags))
                {
                    if (selectionMode != SelectionMode.Materials)
                    {
                        selectionMode = SelectionMode.Materials;
                        ProcessSearch();
                    }

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
                    {
                        ProcessSearch();
                    }

                    var boxHeight = ImGui.GetContentRegionAvail().Y;
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        foreach (var i in matSearchResults)
                        {
                            var group = matDb.Categories[i];

                            if (ImGui.Selectable(group.Name, selectedMatGroup == i) || matSearchResults.Count == 1)
                                selectedMatGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Materials", new Vector2(halfWidth, boxHeight)))
                    {
                        var matList = matDb.Categories[selectedMatGroup].Materials;

                        for (int i = 0; i < matList.Count; i++)
                        {
                            var mat = matList[i];

                            // don't show this prop if it doesn't pass search test
                            if (!mat.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                                continue;
                            
                            if (ImGui.Selectable(mat.Name, mat.ID == selectedMaterial))
                            {
                                selectedMaterial = mat.ID;
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }
                // Tiles tab
                if (ImGuiExt.BeginTabItem("Tiles", tilesFlags))
                {
                    if (selectionMode != SelectionMode.Tiles)
                    {
                        selectionMode = SelectionMode.Tiles;
                        ProcessSearch();
                    }

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
                    {
                        ProcessSearch();
                    }

                    var boxHeight = ImGui.GetContentRegionAvail().Y;
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        foreach (var i in tileSearchResults)
                        {
                            var group = tileDb.Categories[i];

                            if (ImGui.Selectable(group.Name, selectedTileGroup == i) || tileSearchResults.Count == 1)
                                selectedTileGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Tiles", new Vector2(halfWidth, boxHeight)))
                    {
                        var tileList = tileDb.Categories[selectedTileGroup].Tiles;

                        for (int i = 0; i < tileList.Count; i++)
                        {
                            var tile = tileList[i];

                            // don't show this prop if it doesn't pass search test
                            if (!tile.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                                continue;
                            
                            if (ImGui.Selectable(tile.Name, tile == selectedTile))
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
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                forceSelection = null;
            }
        }
        
        if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
        {
            // tab to switch between Tiles/Materials tabs
            if (EditorWindow.IsTabPressed())
            {
                forceSelection = (SelectionMode)(((int)selectionMode + 1) % 2);
            }
        }
        else
        {
            // tab to change work layer
            if (EditorWindow.IsTabPressed())
            {
                window.WorkLayer = (window.WorkLayer + 1) % 3;
            }
        }

        // A and D to change selected group
        /*
        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavLeft))
        {
            selectedGroup--;
            if (selectedGroup < -1)
                selectedGroup = tileDb.Categories.Count - 1;
            
            // select the first tile in this group
            if (selectedGroup == -1)
            {
                selectedTile = null;
                selectedMaterialIdx = 0;
            }
            else
            {
                selectedTile = tileDb.Categories[selectedGroup].Tiles[0];
            }
        }

        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavRight))
        {
            selectedGroup++;
            if (selectedGroup >= tileDb.Categories.Count)
                selectedGroup = -1;
            
            // select the first tile in this group
            if (selectedGroup == -1)
            {
                selectedTile = null;
                selectedMaterialIdx = 0;
            }
            else
            {
                selectedTile = tileDb.Categories[selectedGroup].Tiles[0];
            }
        }

        // W and S to change selected tile in group
        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavDown)) // S
        {
            if (selectedGroup == -1)
            {
                selectedMaterialIdx = Mod(selectedMaterialIdx + 1, Level.MaterialNames.Length);
            }
            else if (selectedTile != null)
            {
                // select the next tile, or wrap around if at end of the list
                if (selectedTile.Category.Index != selectedGroup)
                {
                    selectedTile = tileDb.Categories[selectedGroup].Tiles[0];
                }
                else
                {
                    var tileList = selectedTile.Category.Tiles;
                    selectedTile = tileList[Mod(tileList.IndexOf(selectedTile) + 1, tileList.Count)];
                }
            }
        }

        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavUp)) // W
        {
            if (selectedGroup == -1)
            {
                selectedMaterialIdx = Mod(selectedMaterialIdx - 1, Level.MaterialNames.Length);
            }
            else if (selectedTile != null)
            {
                // select the previous tile, or wrap around if at end of the list
                if (selectedTile.Category.Index != selectedGroup)
                {
                    selectedTile = tileDb.Categories[selectedGroup].Tiles[0];
                }
                else
                {
                    var tileList = selectedTile.Category.Tiles;
                    selectedTile = tileList[Mod(tileList.IndexOf(selectedTile) - 1, tileList.Count)];
                }
            }
        }
        */
    }
}