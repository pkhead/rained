namespace Rained.EditorGui.Editors;
using Rained.Autotiles;
using ImGuiNET;
using System.Numerics;

class AutotileEditMode : TileEditorMode
{
    public override string TabName => "Autotiles";
    
    private Autotile? selectedAutotile = null;
    private IAutotileInputBuilder? activePathBuilder = null;

    public AutotileEditMode(TileEditor editor) : base(editor)
    {}

    public override void Unfocus()
    {
        base.Unfocus();
        activePathBuilder = null;
    }

    public override void Process()
    {
        base.Process();
        
        var forcePlace = editor.PlacementFlags.HasFlag(TilePlacementFlags.Force);
        var modifyGeometry = editor.PlacementFlags.HasFlag(TilePlacementFlags.Geometry);
        var window = RainEd.Instance.LevelView;

        bool deactivate = false;
        bool endOnClick = RainEd.Instance.Preferences.AutotileMouseMode == UserPreferences.AutotileMouseModeOptions.Click;

        // if mouse was pressed
        if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if (activePathBuilder is null)
            {
                if (
                    selectedAutotile is not null &&
                    selectedAutotile.IsReady &&
                    selectedAutotile.CanActivate
                )
                {
                    activePathBuilder = selectedAutotile.Type switch {
                        AutotileType.Path => new AutotilePathBuilder(selectedAutotile),
                        AutotileType.Rect => new AutotileRectBuilder(selectedAutotile, new Vector2i(window.MouseCx, window.MouseCy)),
                        _ => null
                    };

                    RainEd.Instance.LevelView.CellChangeRecorder.BeginChange();
                }
            }
            else if (endOnClick)
            {
                deactivate = true;
            }
        }

        // if mouse was released
        if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left) && !endOnClick)
        {
            deactivate = true;
        }

        if (activePathBuilder is not null)
        {
            activePathBuilder.Update();

            // press escape to cancel path building
            if (EditorWindow.IsKeyPressed(ImGuiKey.Escape))
            {
                activePathBuilder = null;
            }
            else if (deactivate)
            {
                activePathBuilder.Finish(window.WorkLayer, forcePlace, modifyGeometry);
                activePathBuilder = null;
                RainEd.Instance.LevelView.CellChangeRecorder.TryPushChange();
            }
        }
    }

    public override void DrawToolbar()
    {
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
    }
}