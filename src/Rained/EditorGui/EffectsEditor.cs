using ImGuiNET;
using Raylib_cs;
using System.Numerics;

namespace RainEd;

class EffectsEditor : IEditorMode
{
    public string Name { get => "Effects"; }
    private readonly EditorWindow window;

    private int selectedGroup = 0;
    private int selectedEffect = -1;
    private string searchQuery = string.Empty;

    private RlManaged.Texture2D matrixTexture;
    private RlManaged.Image matrixImage;

    private readonly static string MatrixTextureShaderSource = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(texture0, fragTexCoord);
            finalColor = mix(vec4(1.0, 0.0, 1.0, 1.0), vec4(0.0, 1.0, 0.0, 1.0), texelColor.r) * fragColor * colDiffuse;
        }
    ";
    private readonly RlManaged.Shader matrixTexShader;

    public EffectsEditor(EditorWindow window)
    {
        this.window = window;

        // create shader
        matrixTexShader = RlManaged.Shader.LoadFromMemory(null, MatrixTextureShaderSource);

        // create matrix texture
        var level = window.Editor.Level;
        matrixImage = RlManaged.Image.GenColor(level.Width, level.Height, Color.Black);
        matrixTexture = RlManaged.Texture2D.LoadFromImage(matrixImage);
    }

    public void ReloadLevel()
    {
        var level = window.Editor.Level;
        matrixImage.Dispose();
        matrixTexture.Dispose();
        
        matrixImage = RlManaged.Image.GenColor(level.Width, level.Height, Color.Black);
        matrixTexture = RlManaged.Texture2D.LoadFromImage(matrixImage);
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

            // search bar
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, ImGuiInputTextFlags.AlwaysOverwrite);

            var groupsPassingSearch = new List<int>();
            var queryLower = searchQuery.ToLower();

            // find groups that have any entries that pass the search query
            for (int i = 0; i < fxDatabase.Groups.Count; i++)
            {
                if (searchQuery == "")
                {
                    groupsPassingSearch.Add(i);
                    continue;
                }

                for (int j = 0; j < fxDatabase.Groups[i].effects.Count; j++)
                {
                    if (fxDatabase.Groups[i].effects[j].name.ToLower().Contains(queryLower))
                    {
                        groupsPassingSearch.Add(i);
                        break;
                    }
                }
            }

            // group list box
            if (ImGui.BeginListBox("##Groups", new Vector2(halfWidth, boxHeight)))
            {
                foreach (int i in groupsPassingSearch)
                {
                    if (ImGui.Selectable(fxDatabase.Groups[i].name, i == selectedGroup) || groupsPassingSearch.Count == 1)
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
                    if (searchQuery != "" && !effectData.name.ToLower().Contains(queryLower)) continue;

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
                    int deleteRequest = -1;

                    for (int i = 0; i < level.Effects.Count; i++)
                    {
                        var effect = level.Effects[i];
                        
                        ImGui.PushID(effect.GetHashCode());
                        
                        if (ImGui.Selectable(effect.Data.name, selectedEffect == i))
                            selectedEffect = i;

                        // drag to reorder items
                        if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                        {
                            var inext = i + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                            if (inext >= 0 && inext < level.Effects.Count)
                            {
                                level.Effects[i] = level.Effects[inext];
                                level.Effects[inext] = effect;
                                ImGui.ResetMouseDragDelta();

                                if (selectedEffect == i) selectedEffect = inext;
                                else if (selectedEffect == inext) selectedEffect = i;
                            }   
                        }

                        // right-click to delete
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            deleteRequest = i;

                        ImGui.PopID();
                    }

                    if (deleteRequest >= 0)
                    {
                        level.Effects.RemoveAt(deleteRequest);
                        selectedEffect = -1;
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

                // seed
                ImGui.SliderInt("Seed", ref effect.Seed, 0, 500);

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

        // update and draw matrix
        if (selectedEffect >= 0)
        {
            var effect = level.Effects[selectedEffect];

            // update image and send to gpu
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    int v = (int)(effect.Matrix[x,y] / 100f * 255f);
                    Raylib.ImageDrawPixel(ref matrixImage.Ref(), x, y, new Color(v, v, v, 255));
                }
            }
            matrixImage.UpdateTexture(matrixTexture);

            // draw matrix texture
            Raylib.BeginShaderMode(matrixTexShader);
            Raylib.DrawTextureEx(
                matrixTexture,
                Vector2.Zero,
                0f,
                Level.TileSize,
                new Color(255, 255, 255, 100)
            );
            Raylib.EndShaderMode();
        }
    }

    private void AddEffect(EffectInit init)
    {
        var level = window.Editor.Level;
        selectedEffect = level.Effects.Count;
        level.Effects.Add(new Effect(level, init));
    }
}