using ImGuiNET;
using System.Numerics;
using Raylib_cs;
using Rained.Assets;
using Rained.LevelData;
using Rained.EditorGui.AssetPreviews;

namespace Rained.EditorGui.Editors;

partial class PropEditor : IEditorMode
{
    private readonly string[] PropRenderTimeNames = ["Pre Effects", "Post Effects"];
    private readonly string[] RopeReleaseModeNames = ["None", "Left", "Right"];

    enum SelectionMode
    {
        Props,
        Tiles
    };
    
    private SelectionMode selectionMode = SelectionMode.Props;
    private SelectionMode? forceSelection = null;
    private readonly PropPreview propPreview = new();

    private int zTranslateValue = 0;
    private bool zTranslateActive = false;
    private bool zTranslateWrap = false;
    private Dictionary<Prop, int> zTranslateDepths = [];

    private PropInit? selectedInit = null;

    private void PropItemPostRender(int propIndex, bool selected, bool pressed)
    {
        if (ImGui.BeginItemTooltip())
        {
            var prop = RainEd.Instance.PropDatabase.Categories[propCatalogWidget.SelectedGroup].Props[propIndex];
            propPreview.UpdatePreview(prop);
            ImGuiExt.ImageRenderTextureScaled(propPreview.Texture!, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
            ImGui.EndTooltip();
        }
    }

    private void TileItemPostRender(int propIndex, bool selected, bool pressed)
    {
        if (ImGui.BeginItemTooltip())
        {
            var prop = RainEd.Instance.PropDatabase.TileCategories[tileCatalogWidget.SelectedGroup].Props[propIndex];
            propPreview.UpdatePreview(prop);
            ImGuiExt.ImageRenderTextureScaled(propPreview.Texture!, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
            ImGui.EndTooltip();
        }
    }
    
    private GenericDualCatalogWidget propCatalogWidget;
    private GenericDualCatalogWidget tileCatalogWidget;

    private bool isRopeSimulationActive = false;
    private bool wasRopeSimulationActive = false;

#region Multiselect Inputs
    // what a reflective mess...

    private void MultiselectDragInt<T>(T[] items, string label, string fieldName, float v_speed = 1f, int v_min = int.MinValue, int v_max = int.MaxValue)
    {
        var field = typeof(T).GetField(fieldName)!;
        var targetV = (int)field.GetValue(items[0])!;

        bool isSame = true;
        for (int i = 1; i < items.Length; i++)
        {
            if ((int)field.GetValue(items[i])! != targetV)
            {
                isSame = false;
                break;
            }
        }

        if (isSame)
        {
            int v = (int) field.GetValue(items[0])!;
            if (ImGui.DragInt(label, ref v, v_speed, v_min, v_max))
            {
                foreach (var prop in items)
                    field.SetValue(prop, v);
            }
        }
        else
        {
            int v = 0;
            if (ImGui.DragInt(label, ref v, v_speed, v_min, v_max, string.Empty))
            {
                foreach (var prop in items)
                    field.SetValue(prop, v);
            }
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            changeRecorder.PushSettingsChanges();
        }
    }

    private void MultiselectSliderInt<T>(T[] items, string label, string fieldName, int v_min, int v_max, string format = "%i", ImGuiSliderFlags flags = 0)
    {
        var field = typeof(T).GetField(fieldName)!;
        var targetV = (int)field.GetValue(items[0])!;
        var style = ImGui.GetStyle();

        bool isSame = true;
        for (int i = 1; i < items.Length; i++)
        {
            if ((int)field.GetValue(items[i])! != targetV)
            {
                isSame = false;
                break;
            }
        }

        bool depthOffsetInput = fieldName == nameof(Prop.DepthOffset);
        if (depthOffsetInput)
        {
            var w = ImGui.CalcItemWidth() - ImGui.GetFrameHeight() * 2 - style.ItemInnerSpacing.X * 2;
            ImGui.PushItemWidth(w);
        }
        
        if (isSame)
        {
            int v = (int) field.GetValue(items[0])!;
            if (ImGui.SliderInt(depthOffsetInput ? "##"+label : label, ref v, v_min, v_max, format, flags))
            {
                foreach (var prop in items)
                    field.SetValue(prop, v);
            }
        }
        else
        {
            int v = int.MinValue;
            if (ImGui.SliderInt(depthOffsetInput ? "##"+label : label, ref v, v_min, v_max, string.Empty, flags))
            {
                foreach (var prop in items)
                    field.SetValue(prop, v);
            }
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
            changeRecorder.PushSettingsChanges();

        if (depthOffsetInput)
        {
            // decrement/increment input
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, style.ItemInnerSpacing);

            ImGui.PushButtonRepeat(true);
            bool fast = EditorWindow.IsKeyDown(ImGuiKey.ModShift);
            var delta = fast ? 10 : 1;

            ImGui.SameLine();
            if (ImGui.Button(fast ? "<" : "-", Vector2.One * ImGui.GetFrameHeight()))
            {
                foreach (var prop in items)
                    field.SetValue(prop, Math.Max(0, (int)field.GetValue(prop)! - delta));
            }

            if (ImGui.IsItemDeactivated())
                changeRecorder.PushSettingsChanges();

            ImGui.SameLine();
            if (ImGui.Button(fast ? ">" : "+", Vector2.One * ImGui.GetFrameHeight()))
            {
                foreach (var prop in items)
                    field.SetValue(prop, Math.Min(Level.LayerCount*10-1, (int)field.GetValue(prop)! + delta));
            }

            if (ImGui.IsItemDeactivated())
                changeRecorder.PushSettingsChanges();

            ImGui.PopButtonRepeat();

            ImGui.SameLine();
            ImGui.Text(label);

            ImGui.PopStyleVar();
            ImGui.PopItemWidth();
        }
    }

    // this, specifically, is generic for both the items list and the field type,
    // because i use this for both prop properties and rope-type rope properties
    private void MultiselectEnumInput<T, E>(T[] items, string label, string fieldName, string[] enumNames) where E : Enum
    {
        var field = typeof(T).GetField(fieldName)!;
        E targetV = (E)field.GetValue(items[0])!;

        bool isSame = true;
        for (int i = 1; i < items.Length; i++)
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

    private static void MultiselectSwitchInput<T, E>(T[] items, string label, string fieldName, ReadOnlySpan<string> values) where E : Enum
    {
        var field = typeof(T).GetField(fieldName)!;
        object targetV = field.GetValue(items[0])!;

        bool isSame = true;
        for (int i = 1; i < items.Length; i++)
        {
            if (!field.GetValue(items[i])!.Equals(targetV))
            {
                isSame = false;
                break;
            }
        }

        int selected = isSame ? (int)targetV : -1;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);

        if (ImGuiExt.ButtonSwitch(label, values, ref selected))
        {
            E e = (E) Convert.ChangeType(selected, ((E)targetV).GetTypeCode());
            foreach (var item in items)
                field.SetValue(item, e);
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.PopStyleVar();
        ImGui.Text(label);
    }

    private void MultiselectListInput<T, L>(T[] items, string label, string fieldName, List<L> list)
    {
        var field = typeof(T).GetField(fieldName)!;
        int targetV = (int) field.GetValue(items[0])!;

        bool isSame = true;
        for (int i = 1; i < items.Length; i++)
        {
            if (!field.GetValue(items[i])!.Equals(targetV))
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
                    foreach (var prop in items)
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

    public void DrawToolbar()
    {
        // rope-type props are only simulated while the "Simulate" button is held down
        // in their prop options
        wasRopeSimulationActive = isRopeSimulationActive;
        isRopeSimulationActive = false;

        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.Rope is not null) prop.Rope.SimulationSpeed = 0f;
        }

        SelectorToolbar();
        OptionsToolbar();

        if (KeyShortcuts.Activated(KeyShortcut.ToggleVertexMode))
        {
            isWarpMode = !isWarpMode;
        }

        // shift+tab to switch between Tiles/Materials tabs
        if (KeyShortcuts.Activated(KeyShortcut.SwitchTab))
        {
            forceSelection = (SelectionMode)(((int)selectionMode + 1) % 2);
        }

        // tab to change view layer
        if (KeyShortcuts.Activated(KeyShortcut.SwitchLayer))
        {
            window.WorkLayer = (window.WorkLayer + 1) % 3;
        }

        if (isWarpMode)
            RainEd.Instance.LevelView.WriteStatus("Vertex Mode");
        
        // transform mode hints
        if (transformMode is ScaleTransformMode)
        {
            RainEd.Instance.LevelView.WriteStatus("Shift - Constrain proportion");
            RainEd.Instance.LevelView.WriteStatus("Ctrl - Scale by center");
        }
        else if (transformMode is RotateTransformMode)
        {
            RainEd.Instance.LevelView.WriteStatus("Shift - Snap rotation");
        }
        else if (transformMode is WarpTransformMode)
        {
            RainEd.Instance.LevelView.WriteStatus("Shift - Vertex snap");
        }

        if (isRopeSimulationActive)
        {
            RainEd.Instance.NeedScreenRefresh();
        }
        else if (wasRopeSimulationActive)
        {
            // push rope transform if simulation had just ended
            Log.Information("End rope simulation");
            changeRecorder.PushChanges();
        }
    }

    private void SelectorToolbar()
    {
        var propDb = RainEd.Instance.PropDatabase;

        if (KeyShortcuts.Activated(KeyShortcut.ChangePropSnapping))
        {
            // cycle through the four prop snap modes
            snappingMode = (PropSnapMode) (((int)snappingMode + 1) % 4);
        }

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
            {
                int snapModeInt = (int) snappingMode;
                if (ImGui.Combo("Snap", ref snapModeInt, "Off\00.25x\00.5x\01x\0"))
                    snappingMode = (PropSnapMode) snapModeInt;
            }
            
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
                        propCatalogWidget.SearchQuery = tileCatalogWidget.SearchQuery;
                        propCatalogWidget.ProcessSearch();
                    }
                    
                    propCatalogWidget.Draw();

                    ImGui.EndTabItem();
                }

                // Tiles as props tab
                if (ImGuiExt.BeginTabItem("Tiles", tilesFlags))
                {
                    // if tab changed, reset selected group back to 0
                    if (selectionMode != SelectionMode.Tiles)
                    {
                        selectionMode = SelectionMode.Tiles;
                        tileCatalogWidget.SearchQuery = propCatalogWidget.SearchQuery;
                        tileCatalogWidget.ProcessSearch();
                    }

                    tileCatalogWidget.Draw();

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            
            forceSelection = null;

        } ImGui.End();

        // A/D to change selected group
        if (KeyShortcuts.Activated(KeyShortcut.NavLeft))
        {
            if (selectionMode == SelectionMode.Props)
            {
                while (true) // must skip over the hidden Tiles as props categories (i now doubt the goodiness of this idea)
                {
                    propCatalogWidget.SelectedGroup = propCatalogWidget.PreviousGroup(propCatalogWidget.SelectedGroup);
                    var group = propDb.Categories[propCatalogWidget.SelectedGroup];
                    if (!group.IsTileCategory && group.Props.Count > 0) break;
                }
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                tileCatalogWidget.SelectedGroup = Mod(tileCatalogWidget.SelectedGroup - 1, propDb.TileCategories.Count);
            }
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavRight))
        {
             if (selectionMode == SelectionMode.Props)
            {
                while (true) // must skip over the hidden Tiles as props categories (i now doubt the goodiness of this idea)
                {
                    propCatalogWidget.SelectedGroup = propCatalogWidget.NextGroup(propCatalogWidget.SelectedGroup);
                    var group = propDb.Categories[propCatalogWidget.SelectedGroup];
                    if (!group.IsTileCategory && group.Props.Count > 0) break;
                }
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                tileCatalogWidget.SelectedGroup = tileCatalogWidget.NextGroup(tileCatalogWidget.SelectedGroup);
            }
        }

        // W/S to change selected tile in group
        if (KeyShortcuts.Activated(KeyShortcut.NavUp))
        {
            if (selectionMode == SelectionMode.Props)
            {
                propCatalogWidget.SelectedItem = propCatalogWidget.PreviousItem(propCatalogWidget.SelectedGroup, propCatalogWidget.SelectedItem);
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                tileCatalogWidget.SelectedItem = tileCatalogWidget.PreviousItem(tileCatalogWidget.SelectedGroup, tileCatalogWidget.SelectedItem);
            }
        }

        if (KeyShortcuts.Activated(KeyShortcut.NavDown))
        {
            if (selectionMode == SelectionMode.Props)
            {
                propCatalogWidget.SelectedItem = propCatalogWidget.NextItem(propCatalogWidget.SelectedGroup, propCatalogWidget.SelectedItem);
            }
            else if (selectionMode == SelectionMode.Tiles)
            {
                tileCatalogWidget.SelectedItem = tileCatalogWidget.NextItem(tileCatalogWidget.SelectedGroup, tileCatalogWidget.SelectedItem);
            }
        }

        // update selected init
        if (selectionMode == SelectionMode.Props)
        {
            selectedInit = propDb.Categories[propCatalogWidget.SelectedGroup].Props[propCatalogWidget.SelectedItem];
        }
        else if (selectionMode == SelectionMode.Tiles)
        {
            selectedInit = propDb.TileCategories[tileCatalogWidget.SelectedGroup].Props[tileCatalogWidget.SelectedItem];
        }
        else
        {
            throw new Exception("Prop Editor selectionMode is not Props or Tiles");
        }
    }

    private void OptionsToolbarWithSelectedProps(Prop[] selectedProps)
    {
        var btnSize = new Vector2(ImGuiExt.ButtonGroup.CalcItemWidth(ImGui.GetContentRegionAvail().X, 4), 0);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);

        if (ImGui.Button("Reset", btnSize))
        {
            changeRecorder.BeginTransform();
                foreach (var prop in selectedProps)
                    prop.ResetTransform();
            changeRecorder.PushChanges();
        }

        ImGui.SameLine();
        if (ImGui.Button("Flip X", btnSize))
        {
            changeRecorder.BeginTransform();
                foreach (var prop in selectedProps)
                    prop.FlipX();
            changeRecorder.PushChanges();
        }

        ImGui.SameLine();
        if (ImGui.Button("Flip Y", btnSize))
        {
            changeRecorder.BeginTransform();
                foreach (var prop in selectedProps)
                    prop.FlipY();
            changeRecorder.PushChanges();
        }

        ImGui.SameLine();
        if (ImGui.Button("Depth Move", btnSize))
        {
            ImGui.OpenPopup("ZTranslate");
            zTranslateValue = 0;
            zTranslateDepths.Clear();
            foreach (var prop in selectedProps)
                zTranslateDepths.Add(prop, prop.DepthOffset);
        }

        zTranslateActive = false;
        if (ImGui.BeginPopup("ZTranslate"))
        {
            zTranslateActive = true;
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 20f);
            ImGui.SliderInt("##depth", ref zTranslateValue, -29, 29);
            ImGui.PopItemWidth();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            ImGui.Checkbox("Wrap around", ref zTranslateWrap);
            ImGui.PopStyleVar();

            if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out var btn))
            {
                zTranslateActive = false;

                if (btn == 0)
                {
                    changeRecorder.BeginTransform();
                    foreach (var prop in selectedProps)
                    {
                        prop.DepthOffset += zTranslateValue;
                        if (zTranslateWrap)
                            prop.DepthOffset = Util.Mod(prop.DepthOffset, 30);
                        else
                            prop.DepthOffset = Math.Clamp(prop.DepthOffset, 0, 29);
                    }
                    changeRecorder.PushChanges();
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();

        ImGui.PushItemWidth(Math.Max(
            ImGui.GetTextLineHeightWithSpacing() * 12f,
            ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeightWithSpacing() * 8f
        ));
        MultiselectDragInt(selectedProps, "Render Order", "RenderOrder", 0.02f);
        MultiselectSliderInt(selectedProps, "Depth Offset", "DepthOffset", 0, 29, "%i", ImGuiSliderFlags.AlwaysClamp);
        MultiselectSliderInt(selectedProps, "Seed", "Seed", 0, 999);
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
                MultiselectSliderInt(selectedProps, "Custom Depth", "CustomDepth", 0, 30, "%i", ImGuiSliderFlags.AlwaysClamp);
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
                MultiselectListInput(selectedProps, "Custom Color", "CustomColor", propColorNames);
        }

        // rope properties, if all selected props are ropes
        bool longProps = true;
        bool ropeProps = true;
        bool affineProps = true;
        {
            // check if they're all affine
            foreach (var prop in selectedProps)
            {
                if (!prop.IsAffine)
                {
                    affineProps = false;
                    break;
                }
            }

            // check if they're all long props
            foreach (var prop in selectedProps)
            {
                if (!prop.IsLong)
                {
                    longProps = false;
                    break;
                }
            }

            // check if they're all rope props
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

                    for (int i = 1; i < selectedProps.Length; i++)
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
                    Capacity = selectedProps.Length
                };
                foreach (var p in selectedProps)
                    ropes.Add(p.Rope!);
                
                MultiselectSwitchInput<PropRope, RopeReleaseMode>([..ropes], "Release", "ReleaseMode", ["None", "Left", "Right"]);

                if (selectedProps.Length == 1)
                {
                    var prop = selectedProps[0];

                    // thickness
                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.CanSetThickness))
                    {
                        ImGui.SliderFloat("Thickness", ref prop.Rope!.Thickness, 1f, 5f, "%.2f", ImGuiSliderFlags.AlwaysClamp);
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
                    if (ImGui.Button("Reset Simulation") || KeyShortcuts.Activated(KeyShortcut.ResetSimulation))
                    {
                        changeRecorder.BeginTransform();

                        foreach (var prop in selectedProps)
                            prop.Rope!.ResetModel();
                        
                        changeRecorder.PushChanges();
                    }

                    var simSpeed = 0f;

                    ImGui.SameLine();
                    ImGui.Button("Simulate");

                    if ((ImGui.IsItemActive() || KeyShortcuts.Active(KeyShortcut.RopeSimulation)) && transformMode is null)
                    {
                        simSpeed = 1f;
                    }

                    ImGui.SameLine();
                    ImGui.Button("Fast");
                    if ((ImGui.IsItemActive() || KeyShortcuts.Active(KeyShortcut.RopeSimulationFast)) && transformMode is null)
                    {
                        simSpeed = RainEd.Instance.Preferences.FastSimulationSpeed;
                    }

                    if (simSpeed > 0f)
                    {
                        isRopeSimulationActive = true;

                        if (!wasRopeSimulationActive)
                        {
                            changeRecorder.BeginTransform();
                            Log.Information("Begin rope simulation");
                        }

                        foreach (var prop in selectedProps)
                            prop.Rope!.SimulationSpeed = simSpeed;
                    }
                }
            }
        }

        if (selectedProps.Length == 1)
        {
            var prop = selectedProps[0];

            // if is a normal prop
            if (!prop.IsLong)
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

        if (longProps && !affineProps)
        {
            ImGui.Bullet(); ImGui.SameLine();
            ImGui.TextWrapped("One or more selected rope or long props did not load as a rectangle, so editing is limited. Reset its transformation to edit it again.");
        }

        if (selectedProps.Length == 1)
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

    private void OptionsToolbar()
    {
        if (ImGui.Begin("Prop Options", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // prop transformation mode
            if (selectedObjects.Count > 0)
            {
                if (selectedObjects.Count == 1)
                {
                    var obj = selectedObjects[0];

                    ImGui.TextUnformatted($"Selected {obj.DisplayName}");

                    if (obj.Type == PropEditorObjectType.Prop)
                        ImGui.TextUnformatted($"Depth: {obj.DepthOffset} - {obj.DepthOffset + obj.Prop.PropInit.Depth}");
                    else
                        ImGui.TextUnformatted($"Depth: {obj.DepthOffset};");
                }
                else
                {
                    ImGui.Text("Selected multiple props");
                }

                Prop[] selectedProps = [..SelectedProps];
                if (selectedProps.Length > 0)
                {
                    OptionsToolbarWithSelectedProps(selectedProps);
                }
            }
            else
            {
                ImGui.Text("No props selected");
            }

        } ImGui.End();
    }

    public void ShowEditMenu()
    {
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Copy, "Copy");

        // TODO: grey this out if prop clipboard data is not available
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Paste, "Paste");

        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Duplicate, "Duplicate Selected Prop(s)");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.RemoveObject, "Delete Selected Prop(s)");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.ToggleVertexMode, "Toggle Vertex Edit");
    }

    private static int Mod(int a, int b)
        => (a%b + b)%b;
}