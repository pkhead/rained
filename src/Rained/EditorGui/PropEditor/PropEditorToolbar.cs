using ImGuiNET;
using System.Numerics;
using rlImGui_cs;
using RainEd.Props;
using Raylib_cs;

namespace RainEd;

partial class PropEditor : IEditorMode
{
    private readonly string[] PropRenderTimeNames = ["Pre Effects", "Post Effects"];
    private readonly string[] RopeReleaseModeNames = ["None", "Left", "Right"];

    enum SelectionMode
    {
        Props,
        Tiles
    };

    private int selectedPropGroup = 0;
    private int selectedTileGroup = 0;
    private int selectedPropIdx = 0;
    private int selectedTileIdx = 0;
    private SelectionMode selectionMode = SelectionMode.Props;
    private SelectionMode? forceSelection = null;
    private PropInit selectedInit;
    private RlManaged.RenderTexture2D previewTexture = null!;
    private PropInit? curPropPreview = null;

    // search results only process groups because i'm too lazy to have
    // it also process the resulting props
    // plus, i don't think it's much of an optimization concern because then
    // it'd only need to filter props per one category, and there's not
    // that many props per category
    private string searchQuery = "";
    private readonly List<(int, PropCategory)> searchResults = new();
    private readonly List<(int, PropTileCategory)> tileSearchResults = new();

    private bool isRopeSimulationActive = false;
    private bool wasRopeSimulationActive = false;

#region Multiselect Inputs
    // what a reflective mess...

    private void MultiselectDragInt(string label, string fieldName, float v_speed = 1f, int v_min = int.MinValue, int v_max = int.MaxValue)
    {
        var field = typeof(Prop).GetField(fieldName)!;
        var targetV = (int)field.GetValue(selectedProps[0])!;

        bool isSame = true;
        for (int i = 1; i < selectedProps.Count; i++)
        {
            if ((int)field.GetValue(selectedProps[i])! != targetV)
            {
                isSame = false;
                break;
            }
        }

        if (isSame)
        {
            int v = (int) field.GetValue(selectedProps[0])!;
            if (ImGui.DragInt(label, ref v, v_speed, v_min, v_max))
            {
                foreach (var prop in selectedProps)
                    field.SetValue(prop, v);
            }
        }
        else
        {
            int v = 0;
            if (ImGui.DragInt(label, ref v, v_speed, v_min, v_max, string.Empty))
            {
                foreach (var prop in selectedProps)
                    field.SetValue(prop, v);
            }
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            changeRecorder.PushSettingsChanges();
        }
    }

    private void MultiselectSliderInt(string label, string fieldName, int v_min, int v_max, string format = "%i", ImGuiSliderFlags flags = 0)
    {
        var field = typeof(Prop).GetField(fieldName)!;
        var targetV = (int)field.GetValue(selectedProps[0])!;

        bool isSame = true;
        for (int i = 1; i < selectedProps.Count; i++)
        {
            if ((int)field.GetValue(selectedProps[i])! != targetV)
            {
                isSame = false;
                break;
            }
        }

        if (isSame)
        {
            int v = (int) field.GetValue(selectedProps[0])!;
            if (ImGui.SliderInt(label, ref v, v_min, v_max, format, flags))
            {
                foreach (var prop in selectedProps)
                    field.SetValue(prop, v);
            }
        }
        else
        {
            int v = int.MinValue;
            if (ImGui.SliderInt(label, ref v, v_min, v_max, string.Empty, flags))
            {
                foreach (var prop in selectedProps)
                    field.SetValue(prop, v);
            }
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            changeRecorder.PushSettingsChanges();
        }
    }

    // this, specifically, is generic for both the items list and the field type,
    // because i use this for both prop properties and rope-type rope properties
    private void MultiselectEnumInput<T, E>(List<T> items, string label, string fieldName, string[] enumNames) where E : Enum
    {
        var field = typeof(T).GetField(fieldName)!;
        E targetV = (E)field.GetValue(items[0])!;

        bool isSame = true;
        for (int i = 1; i < items.Count; i++)
        {
            if (!((E)field.GetValue(items[i])!).Equals(targetV))
            {
                isSame = false;
                break;
            }
        }

        var previewText = isSame ? enumNames[(int) Convert.ChangeType(targetV, targetV.GetTypeCode())] : "";

        if (ImGui.BeginCombo(label, previewText))
        {
            for (int i = 0; i < enumNames.Length; i++)
            {
                E e = (E) Convert.ChangeType(i, targetV.GetTypeCode());
                bool sel = isSame && e.Equals(targetV);
                if (ImGui.Selectable(enumNames[i], sel))
                {
                    foreach (var item in items)
                        field.SetValue(item, e);
                    
                    changeRecorder.PushSettingsChanges();
                }

                if (sel)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
    }

    private void MultiselectListInput<T>(string label, string fieldName, List<T> list)
    {
        var field = typeof(Prop).GetField(fieldName)!;
        int targetV = (int) field.GetValue(selectedProps[0])!;

        bool isSame = true;
        for (int i = 1; i < selectedProps.Count; i++)
        {
            if (!field.GetValue(selectedProps[i])!.Equals(targetV))
            {
                isSame = false;
                break;
            }
        }

        var previewText = isSame ? list[targetV]!.ToString() : "";

        if (ImGui.BeginCombo(label, previewText))
        {
            for (int i = 0; i < list.Count; i++)
            {
                var txt = list[i]!.ToString();
                bool sel = isSame && targetV == i;
                if (ImGui.Selectable(txt, sel))
                {
                    foreach (var prop in selectedProps)
                        field.SetValue(prop, i);
                    
                    changeRecorder.PushSettingsChanges();
                }

                if (sel)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
    }
#endregion

    private void ProcessSearch()
    {
        searchResults.Clear();
        tileSearchResults.Clear();
        var propDb = RainEd.Instance.PropDatabase;

        // normal props
        if (selectionMode == SelectionMode.Props)
        {
            for (int i = 0; i < propDb.Categories.Count; i++)
            {
                var group = propDb.Categories[i];

                // skip "Tiles as props" categories
                if (group.IsTileCategory) continue;

                foreach (var prop in group.Props)
                {
                    if (prop.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                    {
                        searchResults.Add((i, group));
                        break;
                    }
                }
            }
        }

        // tile props
        else if (selectionMode == SelectionMode.Tiles)
        {
            for (int i = 0; i < propDb.TileCategories.Count; i++)
            {
                var group = propDb.TileCategories[i];

                foreach (var prop in group.Props)
                {
                    if (prop.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                    {
                        tileSearchResults.Add((i, group));
                        break;
                    }
                }
            }
        }
    }

    private void UpdatePreview(PropInit prop)
    {
        var texWidth = (int)(prop.Width * 20f);
        var texHeight = (int)(prop.Height * 20f);

        if (previewTexture is null || curPropPreview != prop)
        {
            curPropPreview = prop;

            previewTexture?.Dispose();
            previewTexture = RlManaged.RenderTexture2D.Load(texWidth, texHeight);
            
            Raylib.BeginTextureMode(previewTexture);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Raylib.BeginShaderMode(window.LevelRenderer.PropPreviewShader);
            {
                for (int depth = prop.LayerCount - 1; depth >= 0; depth--)
                {
                    float whiteFade = Math.Clamp(depth / 16f, 0f, 1f);
                    var srcRect = prop.GetPreviewRectangle(0, depth);
                    Raylib.DrawTextureRec(prop.Texture, srcRect, Vector2.Zero, new Color(255, (int)(whiteFade * 255f), 0, 0));
                }
            }
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();
        }
    }

    public void DrawToolbar()
    {
        // rope-type props are only simulated while the "Simulate" button is held down
        // in their prop options
        wasRopeSimulationActive = isRopeSimulationActive;
        isRopeSimulationActive = false;

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.Rope is not null) prop.Rope.Simulate = false;
        }

        SelectorToolbar();
        OptionsToolbar();

        if (EditorWindow.IsKeyPressed(ImGuiKey.F))
        {
            isWarpMode = !isWarpMode;
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
            // tab to change view layer
            if (EditorWindow.IsTabPressed())
            {
                window.WorkLayer = (window.WorkLayer + 1) % 3;
            }
        }

        if (isWarpMode)
            RainEd.Instance.Window.StatusText = "Freeform Warp";
        
        // transform mode hints
        if (transformMode is ScaleTransformMode)
        {
            RainEd.Instance.Window.StatusText = "Shift - Constrain proportion    Ctrl - Scale by center";
        }
        else if (transformMode is RotateTransformMode)
        {
            RainEd.Instance.Window.StatusText = "Shift - Snap rotation";
        }

        // push rope transform if simulation had just ended
        if (wasRopeSimulationActive && !isRopeSimulationActive)
        {
            RainEd.Logger.Information("End rope simulation");
            changeRecorder.PushTransform();
        }
    }

    private void SelectorToolbar()
    {
        var propDb = RainEd.Instance.PropDatabase;

        if (ImGui.Begin("Props", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // work layer
            {
                int workLayerV = window.WorkLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("View Layer", ref workLayerV);
                window.WorkLayer = Math.Clamp(workLayerV, 1, 3) - 1;
            }

            // snapping
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
            ImGui.Combo("Snap", ref snappingMode, "Off\00.5x\01x");
            
            // flags for search bar
            var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

            if (ImGui.BeginTabBar("PropSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;

                ImGuiTabItemFlags propsFlags = ImGuiTabItemFlags.None;
                ImGuiTabItemFlags tilesFlags = ImGuiTabItemFlags.None;

                // apply force selection
                if (forceSelection == SelectionMode.Props)
                    propsFlags = ImGuiTabItemFlags.SetSelected;
                else if (forceSelection == SelectionMode.Tiles)
                    tilesFlags = ImGuiTabItemFlags.SetSelected;

                // Props tab
                if (ImGuiExt.BeginTabItem("Props", propsFlags))
                {
                    if (selectionMode != SelectionMode.Props)
                    {
                        selectionMode = SelectionMode.Props;
                        ProcessSearch();
                    }

                    // search bar
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
                    {
                        ProcessSearch();
                    }

                    // group list box
                    var boxHeight = ImGui.GetContentRegionAvail().Y;
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        foreach ((var i, var group) in searchResults)
                        {
                            // redundant skip Tiles as props categories
                            if (group.IsTileCategory) continue; // skip Tiles as props categories

                            if (ImGui.Selectable(group.Name, selectedPropGroup == i) || searchResults.Count == 1)
                            {
                                if (i != selectedPropGroup)
                                {
                                    selectedPropGroup = i;
                                    selectedPropIdx = 0;
                                }
                            }
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.Categories[selectedPropGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            // don't show this prop if it doesn't pass search test
                            if (!prop.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                                continue;
                            
                            if (ImGui.Selectable(prop.Name, i == selectedPropIdx))
                            {
                                selectedPropIdx = i;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                UpdatePreview(prop);
                                rlImGui.ImageRenderTexture(previewTexture);
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                // Tiles as props tab
                if (ImGuiExt.BeginTabItem("Tiles", tilesFlags))
                {
                    // if tab changed, reset selected group back to 0
                    if (selectionMode != SelectionMode.Tiles)
                    {
                        selectionMode = SelectionMode.Tiles;
                        ProcessSearch();
                    }

                    // search bar
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
                    {
                        ProcessSearch();
                    }

                    // group list box
                    var boxHeight = ImGui.GetContentRegionAvail().Y;
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        foreach ((var i, var group) in tileSearchResults)
                        {
                            if (ImGui.Selectable(propDb.TileCategories[i].Name, selectedTileGroup == i) || tileSearchResults.Count == 1)
                            {
                                if (i != selectedTileGroup)
                                {
                                    selectedTileGroup = i;
                                    selectedTileIdx = 0;
                                }
                            }
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.TileCategories[selectedTileGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            // don't show this prop if it doesn't pass search test
                            if (!prop.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                                continue;

                            if (ImGui.Selectable(prop.Name, selectedTileIdx == i))
                            {
                                selectedTileIdx = i;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                UpdatePreview(prop);
                                rlImGui.ImageRenderTexture(previewTexture);
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            
            forceSelection = null;

        } ImGui.End();

        // A/D to change selected group
        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavLeft))
        {
            if (selectionMode == SelectionMode.Props)
            {
                while (true) // must skip over the hidden Tiles as props categories (i now doubt the goodiness of this idea)
                {
                    selectedPropGroup = Mod(selectedPropGroup - 1, propDb.Categories.Count);
                    var group = propDb.Categories[selectedPropGroup];
                    if (!group.IsTileCategory && group.Props.Count > 0) break;
                }
                
                selectedPropIdx = 0;
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                selectedTileGroup = Mod(selectedTileGroup - 1, propDb.TileCategories.Count);
                selectedTileIdx = 0;
            }
        }

        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavRight))
        {
             if (selectionMode == SelectionMode.Props)
            {
                while (true) // must skip over the hidden Tiles as props categories (i now doubt the goodiness of this idea)
                {
                    selectedPropGroup = Mod(selectedPropGroup + 1, propDb.Categories.Count);
                    var group = propDb.Categories[selectedPropGroup];
                    if (!group.IsTileCategory && group.Props.Count > 0) break;
                }

                selectedPropIdx = 0;
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                selectedTileGroup = Mod(selectedTileGroup + 1, propDb.TileCategories.Count);
                selectedTileIdx = 0;
            }
        }

        // W/S to change selected tile in group
        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavUp))
        {
            if (selectionMode == SelectionMode.Props)
            {
                var propList = propDb.Categories[selectedPropGroup].Props;
                selectedPropIdx = Mod(selectedPropIdx - 1, propList.Count);
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                var propList = propDb.TileCategories[selectedTileGroup].Props;
                selectedTileIdx = Mod(selectedTileIdx - 1, propList.Count);
            }
        }

        if (window.Editor.IsShortcutActivated(RainEd.ShortcutID.NavDown))
        {
            if (selectionMode == SelectionMode.Props)
            {
                var propList = propDb.Categories[selectedPropGroup].Props;
                selectedPropIdx = Mod(selectedPropIdx + 1, propList.Count);
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                var propList = propDb.TileCategories[selectedTileGroup].Props;
                selectedTileIdx = Mod(selectedTileIdx + 1, propList.Count);
            }
        }

        // update selected init
        if (selectionMode == SelectionMode.Props)
        {
            selectedInit = propDb.Categories[selectedPropGroup].Props[selectedPropIdx];
        }
        else if (selectionMode == SelectionMode.Tiles)
        {
            selectedInit = propDb.TileCategories[selectedTileGroup].Props[selectedTileIdx];
        }
        else
        {
            throw new Exception("Prop Editor selectionMode is not Props or Tiles");
        }
    }

    private void OptionsToolbar()
    {
        var propDb = RainEd.Instance.PropDatabase;

        if (ImGui.Begin("Prop Options", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // prop transformation mode
            if (selectedProps.Count > 0)
            {
                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

                    ImGui.TextUnformatted($"Selected {prop.PropInit.Name}");
                    ImGui.TextUnformatted($"Depth: {prop.DepthOffset} - {prop.DepthOffset + prop.PropInit.Depth}");
                }
                else
                {
                    ImGui.Text("Selected multiple props");
                }
                
                if (ImGui.Button("Reset Transform"))
                {
                    changeRecorder.BeginTransform();
                        foreach (var prop in selectedProps)
                            prop.ResetTransform();
                    changeRecorder.PushTransform();
                }

                ImGui.SameLine();
                if (ImGui.Button("Flip X"))
                {
                    changeRecorder.BeginTransform();
                        foreach (var prop in selectedProps)
                            prop.FlipX();
                    changeRecorder.PushTransform();
                }

                ImGui.SameLine();
                if (ImGui.Button("Flip Y"))
                {
                    changeRecorder.BeginTransform();
                        foreach (var prop in selectedProps)
                            prop.FlipY();
                    changeRecorder.PushTransform();
                }

                ImGui.PushItemWidth(ImGui.GetTextLineHeightWithSpacing() * 10f);
                MultiselectDragInt("Render Order", "RenderOrder", 0.02f);
                MultiselectSliderInt("Depth Offset", "DepthOffset", 0, 29, "%i", ImGuiSliderFlags.AlwaysClamp);
                MultiselectSliderInt("Seed", "Seed", 0, 999);
                MultiselectEnumInput<Prop, PropRenderTime>(selectedProps, "Render Time", "RenderTime", PropRenderTimeNames);

                // custom depth, if available
                {
                    bool hasCustomDepth = true;
                    foreach (var prop in selectedProps)
                    {
                        if (!prop.PropInit.PropFlags.HasFlag(PropFlags.CustomDepthAvailable))
                        {
                            hasCustomDepth = false;
                            break;
                        }
                    }

                    if (hasCustomDepth)
                        MultiselectSliderInt("Custom Depth", "CustomDepth", 1, 30, "%i", ImGuiSliderFlags.AlwaysClamp);
                }

                // custom color, if available
                {
                    bool hasCustomColor = true;
                    foreach (var prop in selectedProps)
                    {
                        if (!prop.PropInit.PropFlags.HasFlag(PropFlags.CustomColorAvailable))
                        {
                            hasCustomColor = false;
                            break;
                        }
                    }

                    if (hasCustomColor)
                        MultiselectListInput("Custom Color", "CustomColor", propColorNames);
                }

                // rope properties, if all selected props are ropes
                bool ropeProps = true;
                bool affineProps = true;
                {
                    foreach (var prop in selectedProps)
                    {
                        if (!prop.IsAffine)
                        {
                            affineProps = false;
                            break;
                        }
                    }

                    foreach (var prop in selectedProps)
                    {
                        if (prop.Rope is null)
                        {
                            ropeProps = false;
                            break;
                        }
                    }

                    if (ropeProps)
                    {
                        if (affineProps)
                        {
                            // flexibility drag float
                            // can't make a MultiselectDragFloat function for this,
                            // cus it doesn't directly control a value
                            bool sameFlexi = true;
                            float targetFlexi = selectedProps[0].Rect.Size.Y / selectedProps[0].PropInit.Height;
                            float minFlexi = 0.5f / selectedProps[0].PropInit.Height;

                            for (int i = 1; i < selectedProps.Count; i++)
                            {
                                var prop = selectedProps[i];
                                float flexi = prop.Rect.Size.Y / prop.PropInit.Height;

                                if (MathF.Abs(flexi - targetFlexi) > 0.01f)
                                {
                                    sameFlexi = false;
                                    
                                    // idk why i did this cus every rope-type prop
                                    // starts out at the same size anyway
                                    float min = 0.5f / prop.PropInit.Height;
                                    if (min > minFlexi)
                                        minFlexi = min;
                                    
                                    break;
                                }
                            }

                            if (!sameFlexi)
                            {
                                targetFlexi = 1f;
                            }

                            // if not all props have the same flexibility value, the display text will be empty
                            // and interacting it will set them all to the default
                            if (ImGui.DragFloat("Flexibility", ref targetFlexi, 0.02f, minFlexi, float.PositiveInfinity, sameFlexi ? "%.2f" : "", ImGuiSliderFlags.AlwaysClamp))
                            {
                                foreach (var prop in selectedProps)
                                {
                                    prop.Rect.Size.Y = targetFlexi * prop.PropInit.Height;
                                }
                            }

                            if (ImGui.IsItemDeactivatedAfterEdit())
                                changeRecorder.PushSettingsChanges();
                        }

                        List<PropRope> ropes = new()
                        {
                            Capacity = selectedProps.Count
                        };
                        foreach (var p in selectedProps)
                            ropes.Add(p.Rope!);
                        
                        MultiselectEnumInput<PropRope, RopeReleaseMode>(ropes, "Release", "ReleaseMode", RopeReleaseModeNames);

                        if (selectedProps.Count == 1)
                        {
                            var prop = selectedProps[0];

                            // thickness
                            if (prop.PropInit.PropFlags.HasFlag(PropFlags.CanSetThickness))
                            {
                                ImGui.SliderFloat("Thickness", ref prop.Rope!.Thickness, 2f, 5f, "%.3f", ImGuiSliderFlags.AlwaysClamp);
                                if (ImGui.IsItemDeactivatedAfterEdit())
                                    changeRecorder.PushSettingsChanges();
                            }

                            // color Zero-G Tube white
                            if (prop.PropInit.PropFlags.HasFlag(PropFlags.Colorize))
                            {
                                if (ImGui.Checkbox("Apply Color", ref prop.ApplyColor))
                                    changeRecorder.PushSettingsChanges();
                            }
                        }

                        // rope simulation controls
                        if (affineProps)
                        {
                            if (ImGui.Button("Reset Simulation"))
                            {
                                changeRecorder.BeginTransform();

                                foreach (var prop in selectedProps)
                                    prop.Rope!.ResetModel();
                                
                                changeRecorder.PushTransform();
                            }

                            ImGui.SameLine();
                            ImGui.Button("Simulate");

                            if (ImGui.IsItemActive())
                            {
                                isRopeSimulationActive = true;

                                if (!wasRopeSimulationActive)
                                {
                                    changeRecorder.BeginTransform();
                                    RainEd.Logger.Information("Begin rope simulation");
                                }

                                foreach (var prop in selectedProps)
                                    prop.Rope!.Simulate = true;
                            }
                        }
                    }
                }

                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

                    // if is a normal prop
                    if (prop.Rope is null)
                    {
                        // prop variation
                        if (prop.PropInit.VariationCount > 1)
                        {
                            var varV = prop.Variation + 1;
                            ImGui.SliderInt(
                                label: "Variation",
                                v: ref varV,
                                v_min: 1,
                                v_max: prop.PropInit.VariationCount,
                                format: varV == 0 ? "Random" : "%i",
                                flags: ImGuiSliderFlags.AlwaysClamp
                            );
                            prop.Variation = Math.Clamp(varV, 0, prop.PropInit.VariationCount) - 1;

                            if (ImGui.IsItemDeactivatedAfterEdit())
                                changeRecorder.PushSettingsChanges();
                        }

                        // apply color
                        if (prop.PropInit.PropFlags.HasFlag(PropFlags.Colorize))
                        {
                            if (ImGui.Checkbox("Apply Color", ref prop.ApplyColor))
                                changeRecorder.PushSettingsChanges();
                        }

                        //ImGui.BeginDisabled();
                        //    bool selfShaded = prop.PropInit.PropFlags.HasFlag(PropFlags.ProcedurallyShaded);
                        //    ImGui.Checkbox("Procedurally Shaded", ref selfShaded);
                        //ImGui.EndDisabled();
                    }
                    
                    // notes
                }

                ImGui.SeparatorText("Notes");

                if (ropeProps && !affineProps)
                {
                    ImGui.Bullet(); ImGui.SameLine();
                    ImGui.TextWrapped("One or more selected rope props did not load as a rectangle, so editing is limited.");
                }

                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

                    bool isDecal = prop.PropInit.Type == PropType.SimpleDecal || prop.PropInit.Type == PropType.VariedDecal;
                    if (!isDecal && prop.DepthOffset <= 5 && prop.DepthOffset + prop.CustomDepth >= 6)
                    {
                        ImGui.Bullet(); ImGui.SameLine();
                        ImGui.TextWrapped("Warning: This prop will intersect with the play layer (depth 5-6)!");
                    }

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.Tile))
                        ImGui.BulletText("Tile as Prop");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.Colorize))
                    {
                        ImGui.Bullet(); ImGui.SameLine();

                        if (prop.Rope is not null)
                        {
                            ImGui.TextWrapped("The tube can be colored white through settings.");
                        }
                        else
                        {
                            ImGui.TextWrapped("It's recommended to render this prop after the effects if the color is activated, as the effects won't affect the color layers.");
                        }
                    }

                    if (!prop.PropInit.PropFlags.HasFlag(PropFlags.ProcedurallyShaded))
                    {
                        ImGui.Bullet(); ImGui.SameLine();
                        ImGui.TextWrapped("Be aware that shadows and highlights will not rotate with the prop, so extreme rotations may cause incorrect shading.");
                    }
                    
                    // user notes
                    foreach (string note in prop.PropInit.Notes)
                    {
                        ImGui.Bullet(); ImGui.SameLine();
                        ImGui.TextWrapped(note);
                    }
                }

                ImGui.PopItemWidth();
            }
            else
            {
                ImGui.Text("No props selected");
            }

        } ImGui.End();
    }

    private static int Mod(int a, int b)
        => (a%b + b)%b;
}