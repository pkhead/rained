using ImGuiNET;
using Rained.Assets;
using Raylib_cs;
using System.Numerics;

namespace Rained.EditorGui.Editors;

partial class TileEditor : IEditorMode
{
    enum SelectionMode
    {
        Materials, Tiles, Autotiles
    }
    private string searchQuery = "";

    // available groups (available = passes search)
    private readonly List<int> matSearchResults = [];
    private readonly List<int> tileSearchResults = [];

    private RlManaged.Texture2D? _loadedMatPreview = null;
    private bool _previewReady = false;
    private string _activeMatPreview = "";

    private RlManaged.RenderTexture2D? _hoverPreview = null;
    private RlManaged.RenderTexture2D? _tileGfxRender = null;
    private RlManaged.RenderTexture2D? _tileSpecRender = null;
    
    private void ProcessSearch()
    {
        var tileDb = RainEd.Instance.TileDatabase;
        var matDb = RainEd.Instance.MaterialDatabase;

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

    public void ShowEditMenu()
    {
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.IncreaseBrushSize, "Increase Brush Size");
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.DecreaseBrushSize, "Decrease Brush Size");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.SetMaterial, "Set Selected Material as Default");
    }

    private void RenderTileLayers(Tile tile)
    {
        var tileTexture = RainEd.Instance.AssetGraphics.GetTileTexture(tile.Name)
            ?? throw new Exception("Could not load tile graphics");
        
        var totalTileWidth = tile.Width + tile.BfTiles * 2;
        var totalTileHeight = tile.Height + tile.BfTiles * 2;

        Raylib.BeginShaderMode(Shaders.TileShader);
        Color drawCol = tile.Category.Color;
        var dstRec = new Rectangle(0, 0, totalTileWidth * 20, totalTileHeight * 20);

        // draw front of box tile
        if (tile.Type == TileType.Box)
        {
            var height = tile.Height * 20;
            var srcRec = new Rectangle(
                0f,
                tile.ImageYOffset + height * tile.Width,
                totalTileWidth * 20, totalTileHeight * 20
            );

            Raylib.DrawTexturePro(tileTexture, srcRec, dstRec, Vector2.Zero, 0f, drawCol);
        }

        // draw voxel tile
        else
        {
            for (int l = tile.LayerCount - 1; l >= 0; l--)
            {
                var srcRec = Rendering.TileRenderer.GetGraphicSublayer(tile, l, 0);

                float lf = (float)l / tile.LayerCount;

                // fade to white as the layer is further away
                // from the front
                float a = lf;
                var col = new Color
                {
                    R = (byte)(drawCol.R * (1f - a) + (drawCol.R * 0.5) * a),
                    G = (byte)(drawCol.G * (1f - a) + (drawCol.G * 0.5) * a),
                    B = (byte)(drawCol.B * (1f - a) + (drawCol.B * 0.5) * a),
                    A = 255
                };

                Raylib.DrawTexturePro(tileTexture, srcRec, dstRec, Vector2.Zero, 0f, col);
            }
        }

        Raylib.EndShaderMode();
    }

    private void RenderTilePreview(Tile tile)
    {
        if (RainEd.Instance.Preferences.ViewPreviews)
        {
            var tileTexture = RainEd.Instance.AssetGraphics.GetTileTexture(tile.Name);
            if (tileTexture is null)
                goto renderPlaceholder;

            var totalTileWidth = tile.Width + tile.BfTiles * 2;
            var totalTileHeight = tile.Height + tile.BfTiles * 2;
            
            var previewWidth = totalTileWidth * 20;
            var previewHeight = totalTileHeight * 20;
            previewHeight += totalTileHeight * 20;
            
            previewWidth += 2;
            previewHeight += 5;

            if (_hoverPreview == null || _hoverPreview.Texture.Width != previewWidth || _hoverPreview.Texture.Height != previewHeight)
            {
                _hoverPreview?.Dispose();
                _hoverPreview = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(_hoverPreview);
            Raylib.ClearBackground(Color.Blank);
            Rlgl.PushMatrix();
            Rlgl.Translatef(1f, 1f, 0f);

            // first, draw tile graphics
            RenderTileLayers(tile);

            // show tile inner border
            Raylib.DrawRectangleLines(tile.BfTiles * 20, tile.BfTiles * 20, tile.Width * 20, tile.Height * 20,
                new Color(255, 255, 255, 200));

            // show tile outer border
            Raylib.DrawRectangleLines(0, 0, totalTileWidth * 20, totalTileHeight * 20,
                new Color(255, 255, 255, 150));
            
            // then, draw tile specs
            Rlgl.Translatef(0f, totalTileHeight * 20 + 3f, 0f);
            Rlgl.Translatef(tile.BfTiles * 20f, tile.BfTiles * 20f, 0f);
            DrawTileSpecs(tile, 0, 0,
                tileSize: 20
            );
            Rlgl.PopMatrix();

            Raylib.EndTextureMode();
            
            ImGuiExt.ImageRenderTextureScaled(_hoverPreview, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
        }
        else
        {
            var previewTexFound = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile, out var previewTexture, out var previewRect);
            if (!previewTexFound || previewTexture is null || previewRect is null)
                goto renderPlaceholder;
                
            var previewWidth = previewRect.Value.Width + 2;
            var previewHeight = previewRect.Value.Height * 2 + 5;
            
            if (_hoverPreview == null || _hoverPreview.Texture.Width != previewWidth || _hoverPreview.Texture.Height != previewHeight)
            {
                _hoverPreview?.Dispose();
                _hoverPreview = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(_hoverPreview);

            // image will have 1 pixel of padding to accomodate for
            // lines
            Raylib.ClearBackground(Color.Blank);
            Rlgl.PushMatrix();
            Rlgl.Translatef(1f, 1f, 0f);
            
            Raylib.DrawTextureRec(previewTexture, previewRect.Value, Vector2.Zero, tile.Category.Color);

            Rlgl.Translatef(0f, previewRect.Value.Height + 3f, 0f);
            DrawTileSpecs(tile, 0, 0,
                tileSize: 16
            );
            Rlgl.PopMatrix();

            Raylib.EndTextureMode();

            ImGuiExt.ImageRenderTexture(_hoverPreview);
            //ImGuiExt.ImageRect(previewTexture!, previewWidth, previewHeight, previewRect.Value, tile.Category.Color);
        }

        return;

        // fallback case
        renderPlaceholder:
        ImGuiExt.ImageSize(RainEd.Instance.PlaceholderTexture, 16, 16);
    }

    public void DrawToolbar()
    {
        var tileDb = RainEd.Instance.TileDatabase;
        var matDb = RainEd.Instance.MaterialDatabase;
        var prefs = RainEd.Instance.Preferences;

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
            int defaultMat = RainEd.Instance.Level.DefaultMaterial;
            ImGui.TextUnformatted($"Default Material: {matDb.GetMaterial(defaultMat).Name}");

            if (selectionMode != SelectionMode.Materials)
                ImGui.BeginDisabled();
            
            if ((ImGui.Button("Set Selected Material as Default") || KeyShortcuts.Activated(KeyShortcut.SetMaterial)) && selectionMode == SelectionMode.Materials)
            {
                var oldMat = RainEd.Instance.Level.DefaultMaterial;
                var newMat = selectedMaterial;
                RainEd.Instance.Level.DefaultMaterial = newMat;

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

            if (ImGui.BeginTabBar("ModeSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;

                ImGuiTabItemFlags materialsFlags = ImGuiTabItemFlags.None;
                ImGuiTabItemFlags tilesFlags = ImGuiTabItemFlags.None;
                ImGuiTabItemFlags autotilesFlags = ImGuiTabItemFlags.None;

                // apply force selection
                if (forceSelection == SelectionMode.Materials)
                    materialsFlags = ImGuiTabItemFlags.SetSelected;
                else if (forceSelection == SelectionMode.Tiles)
                    tilesFlags = ImGuiTabItemFlags.SetSelected;
                else if (forceSelection == SelectionMode.Autotiles)
                    autotilesFlags = ImGuiTabItemFlags.SetSelected;

                // Materials tab
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

                            // show material preview when hovered
                            if (prefs.MaterialSelectorPreview && ImGui.IsItemHovered())
                            {
                                if (_activeMatPreview != mat.Name)
                                {
                                    _activeMatPreview = mat.Name;
                                    _loadedMatPreview?.Dispose();
                                    _loadedMatPreview = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "mat-previews", mat.Name + ".png"));
                                }

                                if (_loadedMatPreview is not null && Raylib_cs.Raylib.IsTextureReady(_loadedMatPreview))
                                {
                                    ImGui.BeginTooltip();
                                    ImGuiExt.ImageSize(_loadedMatPreview, _loadedMatPreview.Width * Boot.PixelIconScale, _loadedMatPreview.Height * Boot.PixelIconScale);
                                    ImGui.EndTooltip();
                                }
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
                        var drawList = ImGui.GetWindowDrawList();
                        float textHeight = ImGui.GetTextLineHeight();

                        foreach (var i in tileSearchResults)
                        {
                            var group = tileDb.Categories[i];
                            var cursor = ImGui.GetCursorScreenPos();

                            if (ImGui.Selectable("  " + group.Name, selectedTileGroup == i) || tileSearchResults.Count == 1)
                                selectedTileGroup = i;
                            
                            drawList.AddRectFilled(
                                p_min: cursor,
                                p_max: cursor + new Vector2(10f, textHeight),
                                ImGui.ColorConvertFloat4ToU32(new Vector4(group.Color.R / 255f, group.Color.G / 255f, group.Color.B / 255f, 1f))
                            );
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
                                RenderTilePreview(tile);
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                // Autotiles tab
                if (ImGuiExt.BeginTabItem("Autotiles", autotilesFlags))
                {
                    if (selectionMode != SelectionMode.Autotiles)
                    {
                        selectionMode = SelectionMode.Autotiles;
                        //ProcessSearch();
                    }

                    var catalog = RainEd.Instance.Autotiles;
                    var autotileGroups = catalog.AutotileCategories;

                    // deselect autotile if it was removed
                    if (selectedAutotile is not null && !RainEd.Instance.Autotiles.HasAutotile(selectedAutotile))
                    {
                        selectedAutotile = null;
                    }

                    var boxWidth = ImGui.GetTextLineHeight() * 16f;

                    // create autotile button
                    ImGui.BeginGroup();
                    if (ImGui.Button("Create Autotile", new Vector2(boxWidth, 0f)))
                    {
                        RainEd.Instance.Autotiles.OpenCreatePopup();
                        ImGui.OpenPopup("Create Autotile");
                        ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    }

                    RainEd.Instance.Autotiles.RenderCreatePopup();

                    // autotile list
                    var boxHeight = ImGui.GetContentRegionAvail().Y;
                    if (ImGui.BeginListBox("##Autotiles", new Vector2(boxWidth, boxHeight)))
                    {
                        for (int i = 0; i < autotileGroups.Count; i++)
                        {
                            ImGui.PushID(i);
                            var group = catalog.GetAutotilesInCategory(i);

                            if (group.Count > 0 && ImGui.TreeNode(autotileGroups[i]))
                            {
                                foreach (var autotile in group)
                                {
                                    if (ImGui.Selectable(autotile.Name, selectedAutotile == autotile))
                                    {
                                        selectedAutotile = autotile;
                                    }
                                }

                                ImGui.TreePop();
                            }

                            ImGui.PopID();
                        }
                        
                        ImGui.EndListBox();
                    }
                    ImGui.EndGroup();

                    // selected autotile options
                    ImGui.SameLine();
                    ImGui.BeginGroup();
                        if (selectedAutotile is not null)
                        {
                            var autotile = selectedAutotile;

                            ImGui.SeparatorText(autotile.Name);
                            if (autotile.Type == Autotiles.AutotileType.Path)
                            {
                                ImGui.Text("Path Autotile");
                            }
                            else if (autotile.Type == Autotiles.AutotileType.Rect)
                            {
                                ImGui.Text("Rectangle Autotile");
                            }

                            ImGui.Separator();

                            if (!autotile.IsReady)
                            {
                                ImGui.TextWrapped("There was a problem loading this autotile.");
                            }
                            else
                            {
                                autotile.ConfigGui();
                            }
                        }
                        else
                        {
                            ImGui.TextDisabled("(no autotile selected)");
                        }
                    ImGui.EndGroup();

                    ImGui.EndTabItem();
                }

                forceSelection = null;
                ImGui.EndTabBar();
            }
        } ImGui.End();
        
        bool tileGfxPreview = prefs.ViewTileGraphicPreview;
        bool tileSpecPreview = prefs.ViewTileSpecPreview;

        // window for tile graphics preview
        var previewWindowFlags = ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        if (tileGfxPreview)
        {
            if (ImGui.Begin("Tile###TileGfxPreview", ref tileGfxPreview, previewWindowFlags))
            {
                if (selectedTile is not null &&
                    RainEd.Instance.AssetGraphics.GetTileTexture(selectedTile.Name) is not null)
                {
                    // render tile layers into framebuffer
                    var totalTileWidth = selectedTile.Width + selectedTile.BfTiles * 2;
                    var totalTileHeight = selectedTile.Height + selectedTile.BfTiles * 2;

                    var fbWidth = totalTileWidth * 20;
                    var fbHeight = totalTileHeight * 20;

                    if (_tileGfxRender is null ||
                        _tileGfxRender.Texture.Width != fbWidth ||
                        _tileGfxRender.Texture.Height != fbHeight)
                    {
                        _tileGfxRender?.Dispose();
                        _tileGfxRender = RlManaged.RenderTexture2D.Load(fbWidth, fbHeight);
                    }

                    Raylib.BeginTextureMode(_tileGfxRender);
                    Raylib.ClearBackground(Color.Blank);
                    RenderTileLayers(selectedTile);
                    Raylib.EndTextureMode();

                    // render framebuffer into imgui
                    ImGui.SetCursorPos((ImGui.GetWindowSize() + new Vector2(-fbWidth, -fbHeight + ImGui.GetFrameHeight())) / 2f);
                    ImGuiExt.ImageRenderTexture(_tileGfxRender);
                }
            }
            ImGui.End();
        }

        // window for tile spec preview
        if (tileSpecPreview)
        {
            if (ImGui.Begin("Specs###TileSpecPreview", ref tileSpecPreview, previewWindowFlags))
            {
                if (selectedTile is not null)
                {
                    // render tile specs into framebuffer
                    var fbWidth = selectedTile.Width * 20 + 8;
                    var fbHeight = selectedTile.Height * 20 + 8;

                    if (_tileSpecRender is null ||
                        _tileSpecRender.Texture.Width != fbWidth ||
                        _tileSpecRender.Texture.Height != fbHeight)
                    {
                        _tileSpecRender?.Dispose();
                        _tileSpecRender = RlManaged.RenderTexture2D.Load(fbWidth, fbHeight);
                    }

                    Raylib.BeginTextureMode(_tileSpecRender);
                    Raylib.ClearBackground(Color.Blank);
                    Rlgl.PushMatrix();
                    Rlgl.Translatef(4f, 4f, 0f);
                    DrawTileSpecs(selectedTile, 0, 0,
                        tileSize: 20
                    );
                    Rlgl.PopMatrix();
                    Raylib.EndTextureMode();

                    // render framebuffer into imgui
                    ImGui.SetCursorPos((ImGui.GetWindowSize() + new Vector2(-fbWidth, -fbHeight + ImGui.GetFrameHeight())) / 2f);
                    ImGuiExt.ImageRenderTexture(_tileSpecRender);
                }
            }
            ImGui.End();
        }

        prefs.ViewTileGraphicPreview = tileGfxPreview;
        prefs.ViewTileSpecPreview = tileSpecPreview;

        // shift+tab to switch between tabs
        if (KeyShortcuts.Activated(KeyShortcut.SwitchTab))
        {
            forceSelection = (SelectionMode)(((int)selectionMode + 1) % 3);
        }
        
        // tab to change work layer
        if (KeyShortcuts.Activated(KeyShortcut.SwitchLayer))
        {
            window.WorkLayer = (window.WorkLayer + 1) % 3;
        }

        // A/D to change selected group
        if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
        {
            if (selectionMode == SelectionMode.Tiles)
            {
                selectedTileGroup = Mod(selectedTileGroup - 1, tileDb.Categories.Count);
                selectedTile = tileDb.Categories[selectedTileGroup].Tiles[0];
            }
            else if (selectionMode == SelectionMode.Materials)
            {
                selectedMatGroup = Mod(selectedMatGroup - 1, matDb.Categories.Count);
                selectedMaterial = matDb.Categories[selectedMatGroup].Materials[0].ID;
            }
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavRight))
        {
            if (selectionMode == SelectionMode.Tiles)
            {
                selectedTileGroup = Mod(selectedTileGroup + 1, tileDb.Categories.Count);
                selectedTile = tileDb.Categories[selectedTileGroup].Tiles[0];
            }
            else if (selectionMode == SelectionMode.Materials)
            {
                selectedMatGroup = Mod(selectedMatGroup + 1, matDb.Categories.Count);
                selectedMaterial = matDb.Categories[selectedMatGroup].Materials[0].ID;
            }
        }

        // W/S to change selected tile in group
        if (KeyShortcuts.Activated(KeyShortcut.NavUp))
        {
            if (selectionMode == SelectionMode.Tiles)
            {
                var tileList = selectedTile.Category.Tiles;
                selectedTile = tileList[Mod(tileList.IndexOf(selectedTile) - 1, tileList.Count)];
            }
            else if (selectionMode == SelectionMode.Materials)
            {
                var mat = matDb.GetMaterial(selectedMaterial);
                var matList = mat.Category.Materials;
                selectedMaterial = matList[Mod(matList.IndexOf(mat) - 1, matList.Count)].ID;
            }
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavDown))
        {
            if (selectionMode == SelectionMode.Tiles)
            {
                var tileList = selectedTile.Category.Tiles;
                selectedTile = tileList[Mod(tileList.IndexOf(selectedTile) + 1, tileList.Count)];
            }
            else if (selectionMode == SelectionMode.Materials)
            {
                var mat = matDb.GetMaterial(selectedMaterial);
                var matList = mat.Category.Materials;
                selectedMaterial = matList[Mod(matList.IndexOf(mat) + 1, matList.Count)].ID; 
            }
        }
    }

    private static int Mod(int a, int b)
        => (a%b + b)%b;
}