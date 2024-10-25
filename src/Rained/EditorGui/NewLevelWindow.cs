namespace Rained.EditorGui;

using System.Numerics;
using ImGuiNET;
using Rained.LevelData;
using Raylib_cs;

static class NewLevelWindow
{
    public const string WindowName = "New Level";
    public static bool IsWindowOpen = false;

    private static int levelWidth;
    private static int levelHeight;
    private static float levelScreenW;
    private static float levelScreenH;
    private static int levelBufL, levelBufR, levelBufT, levelBufB;
    private static bool fillLayer1;
    private static bool fillLayer2;
    private static bool fillLayer3;
    private static bool autoCameras;

    private static RlManaged.RenderTexture2D? previewFramebuffer = null;

    public static void OpenWindow()
    {
        levelWidth = 72;
        levelHeight = 43;
        levelBufL = 12;
        levelBufR = 12;
        levelBufT = 3;
        levelBufB = 5;
        fillLayer1 = true;
        fillLayer2 = true;
        fillLayer3 = false;
        autoCameras = true;

        IsWindowOpen = true;

        // using the formula from the modding wiki
        levelScreenW = (levelWidth - 20) / 52f;
        levelScreenH = (levelHeight - 3) / 40f;
    }

    public static void ShowWindow()
    {
        if (!ImGui.IsPopupOpen(WindowName) && IsWindowOpen)
        {
            ImGui.OpenPopup(WindowName);

            // center popup modal
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(WindowName, ref IsWindowOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            // options column
            var childFlags = ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY;
            int previewHeight;

            var cursorStart = ImGui.GetCursorPos();
            ImGui.BeginChild("Options", new Vector2(0f, 0f), childFlags);
            {
                ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

                ImGui.SeparatorText("Level Size");
                {
                    // tile size
                    if (ImGui.InputInt("Width", ref levelWidth))
                        levelScreenW = (levelWidth - 20) / 52f;
                    
                    levelWidth = Math.Max(levelWidth, 1); // minimum value is 1

                    if (ImGui.InputInt("Height", ref levelHeight))
                        levelScreenH = (levelHeight - 3) / 40f;
                    
                    levelHeight = Math.Max(levelHeight, 1); // minimum value is 1

                    // screen size, using the formula from the modding wiki
                    if (!RainEd.Instance.Preferences.HideScreenSize)
                    {
                        if (ImGui.InputFloat("Screen Width", ref levelScreenW, 0.5f, 0.125f))
                        {
                            levelWidth = (int)(levelScreenW * 52f + 20f);
                        }
                        levelScreenW = Math.Max(levelScreenW, 0);

                        if (ImGui.InputFloat("Screen Height", ref levelScreenH, 0.5f, 0.125f))
                        {
                            levelHeight = (int)(levelScreenH * 40f + 3f);
                        }
                        levelScreenH = Math.Max(levelScreenH, 0); // minimum value is 1
                    }
                }

                ImGui.SeparatorText("Border Tiles");
                {
                    ImGui.InputInt("Border Tiles Left", ref levelBufL);
                    ImGui.InputInt("Border Tiles Top", ref levelBufT);
                    ImGui.InputInt("Border Tiles Right", ref levelBufR);
                    ImGui.InputInt("Border Tiles Bottom", ref levelBufB);

                    levelBufL = Math.Max(levelBufL, 0);
                    levelBufR = Math.Max(levelBufR, 0);
                    levelBufT = Math.Max(levelBufT, 0);
                    levelBufB = Math.Max(levelBufB, 0);
                }

                ImGui.SeparatorText("Layers");
                {
                    ImGui.Checkbox("Fill Layer 1", ref fillLayer1);
                    ImGui.Checkbox("Fill Layer 2", ref fillLayer2);
                    ImGui.Checkbox("Fill Layer 3", ref fillLayer3);
                }

                ImGui.SeparatorText("Options");
                {
                    ImGui.Checkbox("Auto-place Cameras", ref autoCameras);
                }

                ImGui.PopItemWidth();
            }
            ImGui.EndChild();
            var cursorEnd = ImGui.GetCursorPos();
            previewHeight = (int)(cursorEnd.Y - cursorStart.Y);

            // preview column
            ImGui.SameLine();
            ImGui.BeginGroup();

            if (previewFramebuffer is null ||
                previewFramebuffer.Texture.Height != previewHeight
            )
            {
                previewFramebuffer?.Dispose();
                previewFramebuffer = RlManaged.RenderTexture2D.Load((int)(ImGui.GetFontSize() * 40f), previewHeight);
                UpdatePreview();
            }

            ImGuiExt.ImageRenderTexture(previewFramebuffer);

            ImGui.EndGroup();

            ImGui.Separator();

            if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btnPressed))
            {
                if (btnPressed == 0)
                {
                    RainEd.Instance.OpenLevel(CreateLevel());
                }

                IsWindowOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static List<Vector2> CalcCameraPositions()
        => CalcCameraPositions([]);

    private static List<Vector2> CalcCameraPositions(List<Vector2> cameraPositions)
    {
        var screenW = (int)MathF.Round( Math.Max(1f, (levelWidth - 20) / 52f) );
        var screenH = (int)MathF.Round( Math.Max(1f, (levelHeight - 3) / 40f) );
        var levelCenter = new Vector2(
            levelBufL + levelWidth - levelBufR,
            levelBufT + levelHeight - levelBufB
        ) / 2f;

        // uhh why do i have to do this
        // what does StandardSize mean, exactly ??
        var camInnerSize = Camera.StandardSize * ((Camera.WidescreenSize.X - 2) / Camera.WidescreenSize.X);;

        var camTotalSize = new Vector2(
            camInnerSize.X * screenW,
            camInnerSize.Y * screenH
        );
        var camTopLeft = levelCenter - camTotalSize / 2f;
        var camOffset = (Camera.WidescreenSize - camInnerSize) / 2f;

        cameraPositions.Clear();

        for (int row = 0; row < screenH; row++)
        {
            for (int col = 0; col < screenW; col++)
            {
                var camPos = camTopLeft + camInnerSize * new Vector2(col, row) - camOffset;
                cameraPositions.Add(camPos);
            }
        }

        return cameraPositions;
    }

    private static void UpdatePreview()
    {
        if (previewFramebuffer is null) return;

        const int TileSize = 2;
        
        Raylib.BeginTextureMode(previewFramebuffer);
        Raylib.ClearBackground(Color.White);
        Raylib.EndTextureMode();
    }

    private static Level CreateLevel()
    {
        var level = new Level(levelWidth, levelHeight)
        {
            BufferTilesLeft = levelBufL,
            BufferTilesTop = levelBufT,
            BufferTilesRight = levelBufR,
            BufferTilesBot = levelBufB
        };

        // fill options
        for (int y = 0; y < level.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                if (fillLayer1)
                    level.Layers[0,x,y].Geo = GeoType.Solid;

                if (fillLayer2)
                    level.Layers[1,x,y].Geo = GeoType.Solid;

                if (fillLayer3)
                    level.Layers[2,x,y].Geo = GeoType.Solid;
            }
        }

        // camera autoplace
        foreach (var camPos in CalcCameraPositions())
        {
            level.Cameras.Add(new Camera(camPos));
        }

        return level;
    }
}