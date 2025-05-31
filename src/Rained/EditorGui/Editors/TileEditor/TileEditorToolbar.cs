namespace Rained.EditorGui.Editors;
using ImGuiNET;
using Rained.Assets;
using Rained.EditorGui.AssetPreviews;
using Rained.Rendering;
using Raylib_cs;
using System.Numerics;
using CellSelection = CellEditing.CellSelection;


partial class TileEditor : IEditorMode
{
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
                        TilePreview.RenderTileLayers(selectedTile);
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