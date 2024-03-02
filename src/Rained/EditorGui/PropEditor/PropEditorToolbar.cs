using ImGuiNET;
using System.Numerics;
using rlImGui_cs;
using RainEd.Props;

namespace RainEd;

partial class PropEditor : IEditorMode
{
    private int selectedGroup = 0;
    private int currentSelectorMode = 0;
    private PropInit? selectedInit = null;
    private readonly string[] PropRenderTimeNames = new string[] { "Pre Effects", "Post Effects" };
    private readonly string[] RopeReleaseModeNames = new string[] { "None", "Left", "Right" };

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
        var propDb = RainEd.Instance.PropDatabase;

        // rope-type props are only simulated while the "Simulate" button is held down
        // in their prop options
        foreach (var prop in RainEd.Instance.Level.Props)
        {
            if (prop.Rope is not null) prop.Rope.Simulate = false;
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
            ImGui.Combo("Snap", ref snappingMode, "Off\00.5x\01x");
            
            if (ImGui.BeginTabBar("PropSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetContentRegionAvail().Y;

                // Props tab
                if (ImGui.BeginTabItem("Props"))
                {
                    // if tab changed, reset selected group back to 0
                    if (currentSelectorMode != 0)
                    {
                        currentSelectorMode = 0;
                        selectedGroup = 0;
                    }

                    // group list box
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        for (int i = 0; i < propDb.Categories.Count; i++)
                        {
                            var group = propDb.Categories[i];
                            if (group.IsTileCategory) continue; // skip Tiles as props categories

                            if (ImGui.Selectable(group.Name, selectedGroup == i))
                                selectedGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.Categories[selectedGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            if (ImGui.Selectable(prop.Name, prop == selectedInit))
                            {
                                selectedInit = prop;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                var previewRect = prop.GetPreviewRectangle(0, prop.LayerCount / 2);
                                rlImGui.ImageRect(
                                    prop.Texture,
                                    (int)previewRect.Width, (int)previewRect.Height,
                                    previewRect
                                );
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                // Tiles as props tab
                if (ImGui.BeginTabItem("Tiles"))
                {
                    // if tab changed, reset selected group back to 0
                    if (currentSelectorMode != 1)
                    {
                        currentSelectorMode = 1;
                        selectedGroup = 0;
                    }

                    // group list box
                    if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
                    {
                        for (int i = 0; i < propDb.TileCategories.Count; i++)
                        {
                            if (ImGui.Selectable(propDb.TileCategories[i].Name, selectedGroup == i))
                                selectedGroup = i;
                        }
                        
                        ImGui.EndListBox();
                    }
                    
                    // group listing (effects) list box
                    ImGui.SameLine();
                    if (ImGui.BeginListBox("##Props", new Vector2(halfWidth, boxHeight)))
                    {
                        var propList = propDb.TileCategories[selectedGroup].Props;

                        for (int i = 0; i < propList.Count; i++)
                        {
                            var prop = propList[i];

                            if (ImGui.Selectable(prop.Name, prop == selectedInit))
                            {
                                selectedInit = prop;
                            }

                            if (ImGui.BeginItemTooltip())
                            {
                                var previewRect = prop.GetPreviewRectangle(0, prop.LayerCount / 2);
                                rlImGui.ImageRect(
                                    prop.Texture,
                                    (int)previewRect.Width, (int)previewRect.Height,
                                    previewRect
                                );
                                ImGui.EndTooltip();
                            }
                        }
                        
                        ImGui.EndListBox();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
            
        } ImGui.End();

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
                    foreach (var prop in selectedProps)
                        prop.ResetTransform();
                }

                ImGui.SameLine();
                if (ImGui.Button("Flip X"))
                    foreach (var prop in selectedProps)
                        prop.FlipX();

                ImGui.SameLine();
                if (ImGui.Button("Flip Y"))
                    foreach (var prop in selectedProps)
                        prop.FlipY();
                
                ImGui.PushItemWidth(ImGui.GetTextLineHeightWithSpacing() * 10f);
                MultiselectDragInt("Render Order", "RenderOrder", 0.02f);
                MultiselectSliderInt("Depth Offset", "DepthOffset", 0, 29, "%i", ImGuiSliderFlags.AlwaysClamp);
                MultiselectSliderInt("Seed", "Seed", 0, 999);
                MultiselectEnumInput<Prop, Prop.PropRenderTime>(selectedProps, "Render Time", "RenderTime", PropRenderTimeNames);

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
                {
                    bool ropeProps = true;
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

                        List<PropRope> ropes = new()
                        {
                            Capacity = selectedProps.Count
                        };
                        foreach (var p in selectedProps)
                            ropes.Add(p.Rope!);
                        
                        MultiselectEnumInput<PropRope, RopeReleaseMode>(ropes, "Release", "ReleaseMode", RopeReleaseModeNames);

                        if (ImGui.Button("Reset Simulation"))
                        {
                            foreach (var prop in selectedProps)
                                prop.Rope!.ResetSimulation();
                        }

                        ImGui.SameLine();
                        ImGui.Button("Simulate");
                        if (ImGui.IsItemActive())
                        {
                            foreach (var prop in selectedProps)
                                prop.Rope!.Simulate = true;
                        }
                    }
                }

                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

                    // normal prop
                    if (prop.Rope is null)
                    {
                        // prop variation
                        if (prop.PropInit.VariationCount > 1)
                        {
                            var varV = prop.Variation + 1;
                            ImGui.SliderInt(
                                label: "Variation",
                                v: ref varV,
                                v_min: prop.PropInit.PropFlags.HasFlag(PropFlags.RandomVariation) ? 0 : 1, // in Prop, a Variation of -1 means random variation
                                v_max: prop.PropInit.VariationCount,
                                format: varV == 0 ? "Random" : "%i",
                                flags: ImGuiSliderFlags.AlwaysClamp
                            );
                            prop.Variation = Math.Clamp(varV, 0, prop.PropInit.VariationCount) - 1;
                        }


                        ImGui.BeginDisabled();
                            bool selfShaded = prop.PropInit.PropFlags.HasFlag(PropFlags.ProcedurallyShaded);
                            ImGui.Checkbox("Procedurally Shaded", ref selfShaded);
                        ImGui.EndDisabled();
                    }

                    // notes
                    ImGui.SeparatorText("Notes");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.Tile))
                        ImGui.BulletText("Tile as Prop");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.PostEffectsWhenColorized))
                    {
                        ImGui.Bullet(); ImGui.SameLine();
                        ImGui.TextWrapped("It's recommended to render this prop after the effects if the color is activated, as the effects won't affect the color layers.");
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

        if (EditorWindow.IsKeyPressed(ImGuiKey.F))
        {
            isWarpMode = !isWarpMode;
        }

        if (EditorWindow.IsTabPressed())
        {
            window.WorkLayer = (window.WorkLayer + 1) % 3;
        }

        if (isWarpMode)
            RainEd.Instance.Window.StatusText = "Freeform Warp";
    }
}