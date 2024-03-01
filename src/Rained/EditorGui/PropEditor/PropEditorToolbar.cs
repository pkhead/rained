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
    private readonly string[] PropRenderTimeNames = new string[] { "Pre Effects", "Post Effects "};

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

    // what a reflective mess
    private void MultiselectEnumInput<T>(string label, string fieldName, string[] enumNames) where T : Enum
    {
        var field = typeof(Prop).GetField(fieldName)!;
        T targetV = (T)field.GetValue(selectedProps[0])!;

        bool isSame = true;
        for (int i = 1; i < selectedProps.Count; i++)
        {
            if (!((T)field.GetValue(selectedProps[i])!).Equals(targetV))
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
                T e = (T) Convert.ChangeType(i, targetV.GetTypeCode());
                bool sel = isSame && e.Equals(targetV);
                if (ImGui.Selectable(enumNames[i], sel))
                {
                    foreach (var prop in selectedProps)
                        field.SetValue(prop, e);
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

    public void DrawToolbar()
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
            
            if (ImGui.BeginTabBar("PropSelector"))
            {
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetContentRegionAvail().Y;

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
                MultiselectSliderInt("Depth Offset", "DepthOffset", 0, 29, "%i", ImGuiSliderFlags.AlwaysClamp);
                MultiselectSliderInt("Seed", "Seed", 0, 999);
                MultiselectEnumInput<Prop.PropRenderTime>("Render Time", "RenderTime", PropRenderTimeNames);

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

                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

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