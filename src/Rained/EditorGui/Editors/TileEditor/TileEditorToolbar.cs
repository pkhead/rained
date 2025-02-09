namespace Rained.EditorGui.Editors;
using ImGuiNET;
using Rained.Assets;
using Rained.Rendering;
using Raylib_cs;
using System.Numerics;
using CellSelection = CellEditing.CellSelection;


partial class TileEditor : IEditorMode
{
    private RlManaged.RenderTexture2D? _hoverPreview = null;
    private RlManaged.RenderTexture2D? _tileGfxRender = null;
    private RlManaged.RenderTexture2D? _tileSpecRender = null;

    public void ShowEditMenu()
    {
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.IncreaseBrushSize, "Increase Brush Size");
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.DecreaseBrushSize, "Decrease Brush Size");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.SetMaterial, "Set Selected Material as Default");

        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Select, "Select");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Copy, "Copy", false, CellSelection.Instance is not null);
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Paste, "Paste", false);
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

            tileTexture.DrawRectangle(srcRec, dstRec, drawCol);
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

                tileTexture.DrawRectangle(srcRec, dstRec, col);
            }
        }

        Raylib.EndShaderMode();
    }

    private void RenderTilePreview(Tile tile)
    {
        var prefs = RainEd.Instance.Preferences;

        if (prefs.ViewPreviews)
        {
            var tileTexture = RainEd.Instance.AssetGraphics.GetTileTexture(tile.Name);
            if (tileTexture is null)
                goto renderPlaceholder;

            var totalTileWidth = tile.Width + tile.BfTiles * 2;
            var totalTileHeight = tile.Height + tile.BfTiles * 2;
            
            var previewWidth = totalTileWidth * 20;
            var previewHeight = totalTileHeight * 20;
            if (prefs.ViewTileSpecsOnTooltip)
            {
                previewHeight += totalTileHeight * 20;
                previewHeight += 3; // spacing inbetween graphics and geometry
            }
            
            previewWidth += 2;
            previewHeight += 2;

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
            if (prefs.ViewTileSpecsOnTooltip)
            {
                Rlgl.Translatef(0f, totalTileHeight * 20 + 3f, 0f);
                Rlgl.Translatef(tile.BfTiles * 20f, tile.BfTiles * 20f, 0f);
                TileRenderer.DrawTileSpecs(tile, 0, 0,
                    tileSize: 20
                );
            }

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
            float previewHeight;

            if (prefs.ViewTileSpecsOnTooltip)
            {
                previewHeight = previewRect.Value.Height * 2 + 5;
            }            
            else
            {
                previewHeight = previewRect.Value.Height + 2;
            }
            
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
            
            // draw preview texture
            Raylib.DrawTextureRec(previewTexture, previewRect.Value, Vector2.Zero, tile.Category.Color);

            // draw tile specs
            if (prefs.ViewTileSpecsOnTooltip)
            {
                Rlgl.Translatef(0f, previewRect.Value.Height + 3f, 0f);
                TileRenderer.DrawTileSpecs(tile, 0, 0,
                    tileSize: 16
                );
            }

            Rlgl.PopMatrix();
            Raylib.EndTextureMode();

            ImGuiExt.ImageRenderTextureScaled(_hoverPreview, Vector2.One * Boot.PixelIconScale);
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

            var matEdit = editModes[currentMode] as MaterialEditMode;
            if (matEdit is null)
                ImGui.BeginDisabled();
            
            if ((ImGui.Button("Set Selected Material as Default") || KeyShortcuts.Activated(KeyShortcut.SetMaterial)) && matEdit is not null)
            {
                var oldMat = RainEd.Instance.Level.DefaultMaterial;
                var newMat = matEdit.SelectedMaterial;
                RainEd.Instance.Level.DefaultMaterial = newMat;

                if (oldMat != newMat)
                    RainEd.Instance.ChangeHistory.Push(new ChangeHistory.DefaultMaterialChangeRecord(oldMat, newMat));
            }

            if (matEdit is null)
                ImGui.EndDisabled();

            if (ImGui.IsItemHovered() && matEdit is null)
            {
                ImGui.SetTooltip("A material is not selected");
            }

            // search bar
            var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

            if (ImGui.BeginTabBar("ModeSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;

                for (int i = 0; i < editModes.Length; i++)
                {
                    var editMode = editModes[i];
                    var flags = ImGuiTabItemFlags.None;

                    // apply force selection
                    if (forceSelection == i)
                        flags |= ImGuiTabItemFlags.SetSelected;
                    
                    if (ImGuiExt.BeginTabItem(editMode.TabName, flags))
                    {
                        if (currentMode != i)
                        {
                            editModes[currentMode].Unfocus();
                            currentMode = i;
                            editMode.Focus();
                        }

                        editMode.DrawToolbar();
                        ImGui.EndTabItem();
                    }
                }

                forceSelection = -1;
                ImGui.EndTabBar();
            }
        } ImGui.End();
        
        bool tileGfxPreview = prefs.ViewTileGraphicPreview;
        bool tileSpecPreview = prefs.ViewTileSpecPreview;

        static float GetFitScale(int fbWidth, int fbHeight)
        {
            var winSize = ImGui.GetWindowSize() - new Vector2(0f, ImGui.GetFrameHeight())
                - ImGui.GetStyle().WindowPadding * 2f;
            float scale = Math.Min(winSize.X / fbWidth, winSize.Y / fbHeight);
            scale = Math.Max(scale, 0.25f);

            return scale;
        }

        // window for tile graphics preview
        if (editModes[currentMode] is TileEditMode tileEdit)
        {
            var selectedTile = tileEdit.SelectedTile;

            var previewWindowFlags = ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            if (tileGfxPreview)
            {
                if (ImGui.Begin("Graphics###TileGfxPreview", ref tileGfxPreview, previewWindowFlags))
                {
                    if (selectedTile is not null &&
                        RainEd.Instance.AssetGraphics.GetTileTexture(selectedTile.Name) is not null)
                    {
                        // render tile layers into framebuffer
                        var totalTileWidth = selectedTile.Width + selectedTile.BfTiles * 2;
                        var totalTileHeight = selectedTile.Height + selectedTile.BfTiles * 2;

                        var fbWidth = totalTileWidth * 20;
                        var fbHeight = totalTileHeight * 20;
                        var scale = GetFitScale(fbWidth, fbHeight);

                        if (_tileGfxRender is null ||
                            _tileGfxRender.Texture.Width != fbWidth ||
                            _tileGfxRender.Texture.Height != fbHeight)
                        {
                            _tileGfxRender?.Dispose();
                            _tileGfxRender = RlManaged.RenderTexture2D.Load(fbWidth, fbHeight);
                        }

                        Raylib.BeginTextureMode(_tileGfxRender);
                        Raylib.ClearBackground(Color.Blank);
                        Rlgl.PushMatrix();
                        RenderTileLayers(selectedTile);
                        Rlgl.PopMatrix();
                        Raylib.EndTextureMode();

                        // render framebuffer into imgui
                        ImGui.SetCursorPos((ImGui.GetWindowSize() + new Vector2(-fbWidth*scale, -fbHeight*scale + ImGui.GetFrameHeight())) / 2f);
                        ImGuiExt.ImageRenderTextureScaled(_tileGfxRender, Vector2.One * scale);
                    }
                }
                ImGui.End();
            }

            // window for tile spec preview
            if (tileSpecPreview)
            {
                if (ImGui.Begin("Geometry###TileSpecPreview", ref tileSpecPreview, previewWindowFlags))
                {
                    if (selectedTile is not null)
                    {
                        // render tile specs into framebuffer
                        var fbWidth = selectedTile.Width * 20 + 8;
                        var fbHeight = selectedTile.Height * 20 + 8;
                        var scale = GetFitScale(fbWidth, fbHeight);
                        
                        if (_tileSpecRender is null ||
                            _tileSpecRender.Texture.Width != fbWidth * scale ||
                            _tileSpecRender.Texture.Height != fbHeight * scale)
                        {
                            _tileSpecRender?.Dispose();
                            _tileSpecRender = RlManaged.RenderTexture2D.Load((int)(fbWidth * scale), (int)(fbHeight * scale));
                        }

                        Raylib.BeginTextureMode(_tileSpecRender);
                        Raylib.ClearBackground(Color.Blank);
                        Rlgl.PushMatrix();
                        Rlgl.Scalef(scale, scale, 1f);
                        Rlgl.Translatef(4f, 4f, 0f);
                        TileRenderer.DrawTileSpecs(selectedTile, 0, 0,
                            tileSize: 20
                        );
                        Rlgl.PopMatrix();
                        Raylib.EndTextureMode();

                        // render framebuffer into imgui
                        ImGui.SetCursorPos((ImGui.GetWindowSize() + new Vector2(-fbWidth*scale, -fbHeight*scale + ImGui.GetFrameHeight())) / 2f);
                        ImGuiExt.ImageRenderTexture(_tileSpecRender);
                    }
                }
                ImGui.End();
            }
        }

        prefs.ViewTileGraphicPreview = tileGfxPreview;
        prefs.ViewTileSpecPreview = tileSpecPreview;

        // shift+tab to switch between tabs
        if (KeyShortcuts.Activated(KeyShortcut.SwitchTab))
        {
            forceSelection = (currentMode + 1) % editModes.Length;
        }
        
        // tab to change work layer
        if (KeyShortcuts.Activated(KeyShortcut.SwitchLayer))
        {
            window.WorkLayer = (window.WorkLayer + 1) % 3;
        }
    }
}