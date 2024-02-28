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
                    ImGui.TextUnformatted($"Selected {selectedProps[0].PropInit.Name}");
                }
                else
                {
                    ImGui.Text("Selected multiple props");
                }

                // convert to/from freeform prop
                bool canConvert = false;

                foreach (var prop in selectedProps)
                {
                    if (prop.IsAffine)
                    {
                        canConvert = true;
                        break;
                    }
                }

                if (!canConvert)
                    ImGui.BeginDisabled();
                
                if (ImGui.Button("Convert to Warpable Prop"))
                {
                    foreach (var prop in selectedProps)
                        prop.ConvertToFreeform();
                }

                if (!canConvert)
                    ImGui.EndDisabled();

                ImGui.PushItemWidth(ImGui.GetTextLineHeightWithSpacing() * 10f);
                MultiselectSliderInt("Depth", "Depth", 0, 29, "%i", ImGuiSliderFlags.AlwaysClamp);
                MultiselectSliderInt("Seed", "Seed", 0, 999);
                MultiselectEnumInput<Prop.PropRenderTime>("Render Time", "RenderTime", PropRenderTimeNames);

                if (selectedProps.Count == 1)
                {
                    var prop = selectedProps[0];

                    // prop settings
                    if (prop.PropInit.VariationCount > 1)
                    {
                        var varV = prop.Variation + 1;
                        ImGui.SliderInt(
                            label: "Variation",
                            v: ref varV,
                            v_min: prop.PropInit.HasRandomVariation ? 0 : 1, // in Prop, a Variation of -1 means random variation
                            v_max: prop.PropInit.VariationCount,
                            format: varV == 0 ? "Random" : "%i",
                            flags: ImGuiSliderFlags.AlwaysClamp
                        );
                        prop.Variation = Math.Clamp(varV, 0, prop.PropInit.VariationCount) - 1;
                    }

                    // notes + synopses
                    ImGui.SeparatorText("Notes");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.Tile))
                        ImGui.BulletText("Tile as Prop");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.ProcedurallyShaded))
                        ImGui.BulletText("Procedurally Shaded");
                    else
                        ImGui.BulletText("Shadows will not rotate with this prop");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.RandomVariations))
                        ImGui.BulletText("Random Variation");
                    
                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.HasVariations))
                        ImGui.BulletText("Variation Selectable");
                    
                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.PostEffectsWhenColorized))
                        ImGui.BulletText("Post Effects Recommended When Colored");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.CanColorTube))
                        ImGui.BulletText("Can Color Tube");
                    
                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.CanSetThickness))
                        ImGui.BulletText("Can Set Thickness");

                    if (prop.PropInit.PropFlags.HasFlag(PropFlags.CustomColorAvailable))
                        ImGui.BulletText("Custom Color Available");
                }

                ImGui.PopItemWidth();
            }
            else
            {
                ImGui.Text("No props selected");
            }

        } ImGui.End();
    }
}