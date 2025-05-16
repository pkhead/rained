using ImGuiNET;
using Raylib_cs;
using System.Numerics;
using Rained.Assets;
using Rained.LevelData;
namespace Rained.EditorGui.Editors;

class EffectsEditor : IEditorMode
{
    public string Name { get => "Effects"; }
    public bool SupportsCellSelection => false;
    
    private readonly LevelWindow window;

    private int selectedGroup = 0;
    private int selectedEffect = -1;
    private string searchQuery = string.Empty;

    public int SelectedEffect { get => selectedEffect; set => selectedEffect = value; }

    private RlManaged.Texture2D matrixTexture;
    private RlManaged.Image matrixImage;

    private int brushSize = 4;
    private float userBrushStrength = 1f;
    private const float BrushStrengthMin = 0.1f;
    private const float BrushStrengthMax = 10.0f;
    private Vector2i lastBrushPos = new();
    private bool isToolActive = false;

    private ChangeHistory.EffectsChangeRecorder changeRecorder;

    public EffectsEditor(LevelWindow window)
    {
        this.window = window;

        // create matrix texture
        var level = RainEd.Instance.Level;
        matrixImage = RlManaged.Image.GenColor(level.Width, level.Height, Color.Black);
        matrixTexture = RlManaged.Texture2D.LoadFromImage(matrixImage);

        // create change recorder
        changeRecorder = new();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder = new();
        };

        RainEd.Instance.ChangeHistory.UndidOrRedid += () =>
        {
            changeRecorder.UpdateConfigSnapshot();
        };
    }

    public void ReloadLevel()
    {
        selectedEffect = -1;

        var level = RainEd.Instance.Level;
        matrixImage.Dispose();
        matrixTexture.Dispose();
        
        matrixImage = RlManaged.Image.GenColor(level.Width, level.Height, Color.Black);
        matrixTexture = RlManaged.Texture2D.LoadFromImage(matrixImage);
    }

    public void Unload()
    {
        changeRecorder.TryPushListChange();
        changeRecorder.TryPushMatrixChange();
        isToolActive = false;
    }

    private static readonly string[] layerModeNames =
    [
        "All", "1", "2", "3", "1+2", "2+3"
    ];

    private static readonly string[] plantColorNames =
    [
        "Color1", "Color2", "Dead"
    ];

    private bool doDeleteCurrent = false;
    private bool doMoveCurrentUp = false;
    private bool doMoveCurrentDown = false;
    
    public void ShowEditMenu()
    {
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.IncreaseBrushSize, "Increase Brush Size");
        //KeyShortcuts.ImGuiMenuItem(KeyShortcut.DecreaseBrushSize, "Decrease Brush Size");

        if (ImGui.MenuItem("Delete Effect", selectedEffect >= 0))
            doDeleteCurrent = true;

        if (ImGui.MenuItem("Move Effect Up", selectedEffect >= 0))
            doMoveCurrentUp = true;

        if (ImGui.MenuItem("Move Effect Down", selectedEffect >= 0))
            doMoveCurrentDown = true;

        // TODO: clear effect menu item
    }

    public void DrawToolbar()
    {
        var level = RainEd.Instance.Level;
        var fxDatabase = RainEd.Instance.EffectsDatabase;

        if (ImGui.Begin("Add Effect", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // work layer
            {
                int workLayerV = window.WorkLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("View Layer", ref workLayerV);
                window.WorkLayer = Math.Clamp(workLayerV, 1, 3) - 1;
            }

            // search bar
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll);

            var groupsPassingSearch = new List<int>();

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
                    var effect = fxDatabase.Groups[i].effects[j];
                    if (effect.deprecated) continue; // ignore deprecated effects

                    if (effect.name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                    {
                        groupsPassingSearch.Add(i);
                        break;
                    }
                }
            }

            // calculate list box dimensions
            var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
            var boxHeight = ImGui.GetContentRegionAvail().Y;

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
                    if (effectData.deprecated) continue; // ignore deprecated effects

                    if (searchQuery != "" && !effectData.name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase)) continue;

                    if (ImGui.Selectable(effectData.name))
                    {
                        AddEffect(effectData);
                    }
                }
                
                ImGui.EndListBox();
            }
        } ImGui.End();

        int deleteRequest = -1;

        if (ImGui.Begin("Active Effects", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            if (ImGui.BeginListBox("##EffectStack", ImGui.GetContentRegionAvail()))
            {
                if (level.Effects.Count == 0)
                {
                    ImGui.TextDisabled("(no effects)");
                }
                else
                {
                    for (int i = 0; i < level.Effects.Count; i++)
                    {
                        var effect = level.Effects[i];
                        
                        ImGui.PushID(effect.GetHashCode());
                        
                        if (ImGui.Selectable(effect.Data.name, selectedEffect == i))
                            selectedEffect = i;

                        // drag to reorder items
                        if (ImGui.IsItemActivated())
                            changeRecorder.BeginListChange();
                        
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

                        if (ImGui.IsItemDeactivated())
                            changeRecorder.PushListChange();

                        // right-click to delete
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            deleteRequest = i;

                        ImGui.PopID();
                    }
                }

                ImGui.EndListBox();
            }
        } ImGui.End();

        // delete/backspace to delete selected effect
        if (KeyShortcuts.Activated(KeyShortcut.RemoveObject) || doDeleteCurrent)
        {
            doDeleteCurrent = false;
            deleteRequest = selectedEffect;
        }

        if (deleteRequest >= 0)
        {
            changeRecorder.BeginListChange();
            level.Effects.RemoveAt(deleteRequest);
            selectedEffect = -1;
            changeRecorder.PushListChange();
        }

        if (ImGui.Begin("Effect Options", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            // effect properties
            if (selectedEffect >= 0)
            {
                var effect = level.Effects[selectedEffect];

                // on delete action, only delete effect after UI has been processed
                bool doDelete = false;
                if (ImGui.Button("Delete"))
                    doDelete = true;
                
                ImGui.SameLine();
                if ((ImGui.Button("Move Up") || doMoveCurrentUp) && selectedEffect > 0)
                {
                    doMoveCurrentUp = false;

                    // swap this effect with up
                    changeRecorder.BeginListChange();
                    level.Effects[selectedEffect] = level.Effects[selectedEffect - 1];
                    level.Effects[selectedEffect - 1] = effect;
                    selectedEffect--;
                    changeRecorder.PushListChange();
                }

                ImGui.SameLine();
                if ((ImGui.Button("Move Down") || doMoveCurrentDown) && selectedEffect < level.Effects.Count - 1)
                {
                    doMoveCurrentDown = false;

                    // swap this effect with down
                    changeRecorder.BeginListChange();
                    level.Effects[selectedEffect] = level.Effects[selectedEffect + 1];
                    level.Effects[selectedEffect + 1] = effect;
                    selectedEffect++;
                    changeRecorder.PushListChange();
                }

                {
                    ImGui.SameLine();

                    var sliderRight = ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X;
                    ImGui.NewLine();

                    ImGui.SetNextItemWidth(sliderRight - ImGui.GetCursorPosX());
                    ImGui.SliderFloat("Brush Strength", ref userBrushStrength, BrushStrengthMin, BrushStrengthMax, "%.1fx", ImGuiSliderFlags.AlwaysClamp);

                    // middle- or right-click to reset brush strength
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Middle) || ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        userBrushStrength = 1f;
                }

                ImGui.Separator();

                if (effect.Data.deprecated)
                    ImGui.TextDisabled("This effect is deprecated!");

                ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

                changeRecorder.SetCurrentConfig(effect);
                bool hadChanged = false;

                // layers property
                if (effect.Data.useLayers)
                {
                    if (ImGui.BeginCombo("Layers", layerModeNames[(int) effect.Layer]))
                    {
                        foreach (int i in effect.Data.availableLayers.Select(v => (int)v))
                        {
                            bool isSelected = i == (int) effect.Layer;
                            if (ImGui.Selectable(layerModeNames[i], isSelected))
                            {
                                effect.Layer = (Effect.LayerMode) i;
                                hadChanged = true;
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

                    if (ImGui.IsItemEdited())
                        hadChanged = true;
                }

                // 3d property
                if (effect.Data.use3D)
                {
                    ImGui.Checkbox("3D", ref effect.Is3D);

                    if (ImGui.IsItemDeactivatedAfterEdit())
                        hadChanged = true;
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
                            {
                                effect.PlantColor = i;
                                hadChanged = true;
                            }
                            
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

                    if (ImGui.IsItemEdited())
                        hadChanged = true;
                }

                // affect colors and gradients
                if (effect.Data.useDecalAffect)
                {
                    if (ImGui.Checkbox("Affect Gradients and Decals", ref effect.AffectGradientsAndDecals))
                        hadChanged = true;
                }

                // custom properties
                for (int configIndex = 0; configIndex < effect.Data.customConfigs.Count; configIndex++)
                {
                    CustomEffectConfig configInfo = effect.Data.customConfigs[configIndex];
                    ref int configValue = ref effect.CustomValues[configIndex];

                    // string config
                    if (configInfo is CustomEffectString strConfig)
                    {
                        if (ImGui.BeginCombo(strConfig.Name, strConfig.Options[configValue]))
                        {
                            for (int i = 0; i < strConfig.Options.Length; i++)
                            {
                                bool isSelected = i == configValue;
                                if (ImGui.Selectable(strConfig.Options[i], isSelected))
                                {
                                    configValue = i;
                                    hadChanged = true;
                                }

                                if (isSelected)
                                    ImGui.SetItemDefaultFocus();
                            }

                            ImGui.EndCombo();
                        }
                    }

                    // int config
                    else if (configInfo is CustomEffectInteger intConfig)
                    {
                        ImGui.SliderInt(intConfig.Name, ref configValue, intConfig.MinInclusive, intConfig.MaxInclusive);
                        if (ImGui.IsItemDeactivatedAfterEdit())
                            hadChanged = true;
                    }
                }

                if (effect.Data.optionalInBounds)
                {
                    if (ImGui.Checkbox("Require In-Bounds", ref effect.RequireInBounds))
                        hadChanged = true;
                }

                // seed
                ImGui.SliderInt("Seed", ref effect.Seed, 0, 500);
                if (ImGui.IsItemDeactivatedAfterEdit())
                        hadChanged = true;

                ImGui.PopItemWidth();

                if (hadChanged)
                {
                    changeRecorder.PushConfigChange();
                }

                // if user requested delete, do it here
                if (doDelete)
                {
                    changeRecorder.BeginListChange();
                    level.Effects.RemoveAt(selectedEffect);
                    selectedEffect = -1;
                    changeRecorder.PushListChange();
                }
            }
            else
            {
                ImGui.TextDisabled("No effect selected");
            }
        } ImGui.End();

        // tab to change work layer
        if (KeyShortcuts.Activated(KeyShortcut.SwitchLayer))
        {
            window.WorkLayer = (window.WorkLayer + 1) % 3;
        }
    }

    private static float GetBrushPower(int cx, int cy, int bsize, int x, int y)
    {
        var dx = x - cx;
        var dy = y - cy;
        return 1.0f - (MathF.Sqrt(dx*dx + dy*dy) / bsize);
    }

    private float timeStacker = 0f;
    private void BrushUpdate(bool isFirstTick, int bcx, int bcy, int bsize, float brushFac)
    {
        const int BrushTickRate = 60;

        var level = RainEd.Instance.Level;
        var effect = level.Effects[selectedEffect];
        var bLeft = bcx - bsize;
        var bTop = bcy - bsize;
        var bRight = bcx + bsize;
        var bBot = bcy + bsize;

        var brushStrength = EditorWindow.IsKeyDown(ImGuiKey.ModShift) ? 100f : 10f;
        if (effect.Data.binary) brushStrength = 100000000f;
        
        if (isFirstTick) timeStacker += 1f;
        timeStacker += Raylib.GetFrameTime() * BrushTickRate;

        if (timeStacker >= 1f)
        {
            if (isFirstTick || new Vector2i(bcx, bcy) != lastBrushPos)
            {
                var origX = bcx;
                var origY = bcy;

                for (int x = bLeft; x <= bRight; x++)
                {
                    for (int y = bTop; y <= bBot; y++)
                    {
                        if (!level.IsInBounds(x, y)) continue;
                        var brushP = GetBrushPower(origX, origY, bsize, x, y) * userBrushStrength;

                        if (brushP > 0f)
                        {
                            effect.Matrix[x,y] = Math.Clamp(effect.Matrix[x,y] + brushStrength * brushP * brushFac, 0f, 100f);                            
                        }
                    }
                }
            }

            lastBrushPos.X = bcx;
            lastBrushPos.Y = bcy;
            timeStacker %= 1f;
        }
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        window.BeginLevelScissorMode();

        bool wasToolActive = isToolActive;
        isToolActive = false;

        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;
        
        levelRender.RenderLevelComposite(mainFrame, layerFrames, new Rendering.LevelRenderConfig()
        {
            Fade = 30f / 255f,
            ActiveLayer = window.WorkLayer
        });

        if (selectedEffect >= level.Effects.Count)
            selectedEffect = -1;

        if (selectedEffect >= 0)
        {
            var effect = level.Effects[selectedEffect];

            var bsize = brushSize;
            if (effect.Data.single) bsize = 1;

            float brushFac = 0.0f;
            int bcx = window.MouseCx;
            int bcy = window.MouseCy;

            var bLeft = bcx - bsize;
            var bTop = bcy - bsize;
            var bRight = bcx + bsize;
            var bBot = bcy + bsize;

            // user painting
            if (window.IsViewportHovered)
            {
                // shift + scroll to change brush size
                bool brushSizeKey =
                    KeyShortcuts.Activated(KeyShortcut.IncreaseBrushSize) || KeyShortcuts.Activated(KeyShortcut.DecreaseBrushSize);
                if (EditorWindow.IsKeyDown(ImGuiKey.ModShift) || brushSizeKey)
                {
                    window.OverrideMouseWheel = true;

                    if (Raylib.GetMouseWheelMove() > 0.0f || KeyShortcuts.Activated(KeyShortcut.IncreaseBrushSize))
                        brushSize += 1;
                    else if (Raylib.GetMouseWheelMove() < 0.0f || KeyShortcuts.Activated(KeyShortcut.DecreaseBrushSize))
                        brushSize -= 1;
                    
                    brushSize = Math.Clamp(brushSize, 1, 10);
                }

                if (EditorWindow.IsKeyDown(ImGuiKey.ModCtrl))
                {
                    window.OverrideMouseWheel = true;
                    userBrushStrength -= Raylib.GetMouseWheelMove();

                    userBrushStrength = Math.Clamp(userBrushStrength, BrushStrengthMin, BrushStrengthMax);
                }

                bool strokeStart = EditorWindow.IsMouseClicked(ImGuiMouseButton.Left) || EditorWindow.IsMouseClicked(ImGuiMouseButton.Right);
                if (strokeStart)
                    lastBrushPos = new(bcx, bcy);
                
                // paint when user's mouse is down and moving
                if (EditorWindow.IsMouseDown(ImGuiMouseButton.Left))
                    brushFac = 1.0f;
                else if (EditorWindow.IsMouseDown(ImGuiMouseButton.Right))
                    brushFac = -1.0f;
                
                if (brushFac != 0.0f)
                {
                    if (!wasToolActive) changeRecorder.BeginMatrixChange(effect);
                    isToolActive = true;

                    BrushUpdate(strokeStart, bcx, bcy, bsize, brushFac); 
                }
            }

            // update and draw matrix
            // first, update data on the gpu
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    int v = (int)(effect.Matrix[x,y] / 100f * 255f);
                    Raylib.ImageDrawPixel(matrixImage, x, y, new Color(v, v, v, 255));
                }
            }
            matrixImage.UpdateTexture(matrixTexture);

            // then, draw the matrix texture
            Raylib.BeginShaderMode(Shaders.EffectsMatrixShader);
            Raylib.DrawTextureEx(
                matrixTexture,
                Vector2.Zero,
                0f,
                Level.TileSize,
                new Color(255, 255, 255, 100)
            );
            Raylib.EndShaderMode();

            // draw brush outline
            if (window.IsViewportHovered && brushFac == 0.0f)
            {
                // draw brush outline
                for (int x = bLeft; x <= bRight; x++)
                {
                    for (int y = bTop; y <= bBot; y++)
                    {
                        if (!level.IsInBounds(x, y)) continue;
                        if (GetBrushPower(bcx, bcy, bsize, x, y) <= 0f) continue;
                        
                        // left
                        if (GetBrushPower(bcx, bcy, bsize, x - 1, y) <= 0f)
                            Raylib.DrawLine(
                                x * Level.TileSize, y * Level.TileSize,
                                x * Level.TileSize, (y+1) * Level.TileSize,
                                Color.White
                            );

                        // top
                        if (GetBrushPower(bcx, bcy, bsize, x, y - 1) <= 0f)
                            Raylib.DrawLine(
                                x * Level.TileSize, y * Level.TileSize,
                                (x+1) * Level.TileSize, y * Level.TileSize,
                                Color.White
                            );
                        
                        // right
                        if (GetBrushPower(bcx, bcy, bsize, x + 1, y) <= 0f)
                            Raylib.DrawLine(
                                (x+1) * Level.TileSize, y * Level.TileSize,
                                (x+1) * Level.TileSize, (y+1) * Level.TileSize,
                                Color.White
                            );

                        // bottom
                        if (GetBrushPower(bcx, bcy, bsize, x, y + 1) <= 0f)
                            Raylib.DrawLine(
                                x * Level.TileSize, (y+1) * Level.TileSize,
                                (x+1) * Level.TileSize, (y+1) * Level.TileSize,
                                Color.White
                            );
                    }
                }
            }
        }

        levelRender.RenderBorder();
        levelRender.RenderCameraBorders();
        Raylib.EndScissorMode();

        if (!isToolActive && wasToolActive)
            changeRecorder.PushMatrixChange();
    }

    private void AddEffect(EffectInit init)
    {
        changeRecorder.BeginListChange();
        var level = RainEd.Instance.Level;
        selectedEffect = level.Effects.Count;
        level.Effects.Add(new Effect(level, init));
        changeRecorder.PushListChange();
    }
}