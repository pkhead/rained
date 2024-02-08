using ImGuiNET;
using Raylib_cs;
using System.Numerics;

namespace RainEd;

public class EffectsEditor : IEditorMode
{
    public string Name { get => "Effects"; }
    private readonly EditorWindow window;

    private int selectedGroup = 0;
    private int selectedEffect = -1;

    public EffectsEditor(EditorWindow window)
    {
        this.window = window;
    }

    private static string[] layerModeNames = new string[]
    {
        "All", "1", "2", "3", "1+2", "2+3"
    };

    private static string[] plantColorNames = new string[]
    {
        "Color1", "Color2", "Dead"
    };

    public void DrawToolbar()
    {
        var level = window.Editor.Level;
        var fxDatabase = window.Editor.EffectsDatabase;

        if (ImGui.Begin("Effects", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.SeparatorText("Add Effect");

            var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
            var boxHeight = ImGui.GetTextLineHeight() * 20.0f;
            // group list box
            if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
            {
                for (int i = 0; i < fxDatabase.Groups.Count; i++)
                {
                    var group = fxDatabase.Groups[i];
                    if (ImGui.Selectable(group.name, i == selectedGroup))
                        selectedGroup = i;
                }
                
                ImGui.EndListBox();
            }
            
            // group listing (effects) list box
            ImGui.SameLine();
            if (ImGui.BeginListBox("##Effects", new Vector2(halfWidth, boxHeight)))
            {
                var effectsList = fxDatabase.Groups[selectedGroup].effects;

                for (int i = 0; i < effectsList.Count; i++)
                {
                    var effectData = effectsList[i];
                    if (ImGui.Selectable(effectData.name))
                    {
                        AddEffect(effectData);
                    }
                }
                
                ImGui.EndListBox();
            }

            ImGui.SeparatorText("Effect Stack");

            if (ImGui.BeginListBox("##EffectStack", new Vector2(-0.0001f, ImGui.GetTextLineHeight() * 10.0f)))
            {
                if (level.Effects.Count == 0)
                {
                    ImGui.TextDisabled("(no effects)");
                }
                else
                {
                    for (int i = 0; i < level.Effects.Count; i++)
                    {
                        ImGui.PushID(i);
                        
                        var effect = level.Effects[i];
                        if (ImGui.Selectable(effect.Data.name, selectedEffect == i))
                            selectedEffect = i;
                        
                        ImGui.PopID();
                    }
                }

                ImGui.EndListBox();
            }

            // effect properties
            if (selectedEffect >= 0)
            {
                var effect = level.Effects[selectedEffect];
                ImGui.SeparatorText("Effect Options");

                // on delete action, only delete effect after UI has been processed
                bool doDelete = false;
                if (ImGui.Button("Delete"))
                    doDelete = true;
                
                ImGui.SameLine();
                if (ImGui.Button("Move Up") && selectedEffect > 0)
                {
                    // swap this effect with up
                    level.Effects[selectedEffect] = level.Effects[selectedEffect - 1];
                    level.Effects[selectedEffect - 1] = effect;
                    selectedEffect--;
                }

                ImGui.SameLine();
                if (ImGui.Button("Move Down") && selectedEffect < level.Effects.Count - 1)
                {
                    // swap this effect with down
                    level.Effects[selectedEffect] = level.Effects[selectedEffect + 1];
                    level.Effects[selectedEffect + 1] = effect;
                    selectedEffect++;
                }

                ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

                // layers property
                if (effect.Data.useLayers)
                {
                    if (ImGui.BeginCombo("Layers", layerModeNames[(int) effect.Layer]))
                    {
                        for (int i = 0; i < layerModeNames.Length; i++)
                        {
                            bool isSelected = i == (int) effect.Layer;
                            if (ImGui.Selectable(layerModeNames[i], isSelected))
                                effect.Layer = (Effect.LayerMode) i;
                            
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }
                }

                // 3d property
                if (effect.Data.use3D)
                {
                    ImGui.Checkbox("3D", ref effect.Is3D);
                }

                // plant color property
                if (effect.Data.usePlantColors)
                {
                    if (ImGui.BeginCombo("Color", plantColorNames[effect.PlantColor]))
                    {
                        for (int i = 0; i < plantColorNames.Length; i++)
                        {
                            bool isSelected = i == effect.PlantColor;
                            if (ImGui.Selectable(plantColorNames[i], isSelected))
                                effect.PlantColor = i;
                            
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }
                }

                // custom property
                if (!string.IsNullOrEmpty(effect.Data.customSwitchName))
                {
                    if (ImGui.BeginCombo(effect.Data.customSwitchName, effect.Data.customSwitchOptions[effect.CustomValue]))
                    {
                        for (int i = 0; i < effect.Data.customSwitchOptions.Length; i++)
                        {
                            bool isSelected = i == effect.CustomValue;
                            if (ImGui.Selectable(effect.Data.customSwitchOptions[i], isSelected))
                                effect.CustomValue = i;
                            
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }
                }

                ImGui.PopItemWidth();

                // if user requested delete, do it here
                if (doDelete)
                {
                    level.Effects.RemoveAt(selectedEffect);
                    selectedEffect = -1;
                }
            }
            
        } ImGui.End();
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;
        
        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));
        
        // draw the layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == 0 ? 255 : 50;
            var color = new Color(30, 30, 30, alpha);
            int offset = l * 2;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            levelRender.RenderGeometry(l, color);
            Rlgl.PopMatrix();
        }

        levelRender.RenderBorder();
    }

    private void AddEffect(EffectInit init)
    {
        var level = window.Editor.Level;
        selectedEffect = level.Effects.Count;
        level.Effects.Add(new Effect(level, init));
    }
}