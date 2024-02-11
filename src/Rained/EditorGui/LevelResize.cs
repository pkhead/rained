using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
namespace RainEd;

class LevelResizeWindow
{
    private readonly RainEd editor;
    public bool IsWindowOpen = true;
    private int newWidth;
    private int newHeight;
    private int newBufL, newBufR, newBufT, newBufB;
    private int anchorX = 1;
    private int anchorY = 1;

    private float screenW, screenH;

    public int InputWidth { get => newWidth; }
    public int InputHeight { get => newWidth; }
    public int InputBufferLeft { get => newBufL; }
    public int InputBufferRight { get => newBufR; }
    public int InputBufferTop { get => newBufT; }
    public int InputBufferBottom { get => newBufB; }

    public LevelResizeWindow(RainEd rained)
    {
        editor = rained;
        var level = editor.Level;

        newWidth = level.Width;
        newHeight = level.Height;
        newBufL = level.BufferTilesLeft;
        newBufR = level.BufferTilesRight;
        newBufT = level.BufferTilesTop;
        newBufB = level.BufferTilesBot;

        // using the formula from the modding wiki
        screenW = (newWidth - 20) / 52f;
        screenH = (newHeight - 3) / 40f;
    }

    public void DrawWindow()
    {
        if (!IsWindowOpen) return;

        var winFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking;
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetWorkCenter(), ImGuiCond.Once, new Vector2(0.5f, 0.5f));
        if (ImGui.Begin("Resize Level", ref IsWindowOpen, winFlags))
        {
            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);

            ImGui.SeparatorText("Level Size");
            {
                // screen size, using the formula from the modding wiki
                ImGui.BeginGroup();
                if (ImGui.InputFloat("Screen Width", ref screenW))
                {
                    newWidth = (int)(screenW * 52f + 20f);
                }
                screenW = Math.Max(screenW, 0);

                if (ImGui.InputFloat("Screen Height", ref screenH))
                {
                    newHeight = (int)(screenH * 40f + 3f);
                }
                screenH = Math.Max(screenH, 0); // minimum value is 1
                ImGui.EndGroup();

                // tile size
                ImGui.SameLine();
                ImGui.BeginGroup();
                if (ImGui.InputInt("Width", ref newWidth))
                    screenW = (newWidth - 20) / 52f;
                
                newWidth = Math.Max(newWidth, 1); // minimum value is 1

                if (ImGui.InputInt("Height", ref newHeight))
                    screenH = (newHeight - 3) / 40f;
                
                newHeight = Math.Max(newHeight, 1); // minimum value is 1
                ImGui.EndGroup();
            }

            ImGui.SeparatorText("Anchors");
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2f, 2f));

                for (int y = 0; y < 3; y++)
                {
                    ImGui.PushID(y);
                    for (int x = 0; x < 3; x++)
                    {
                        ImGui.PushID(x);
                        if (x > 0) ImGui.SameLine();

                        int ox = x - anchorX;
                        int oy = y - anchorY;

                        // calculate the texture offset needed
                        // to show an arrow pointing a certain direction
                        // these default values are an empty part of the texture atlas
                        int textureX = 4;
                        int textureY = 3;

                        if (ox == 0)
                        {
                            if (oy == -1)
                            {
                                textureX = 3;
                                textureY = 0;
                            }
                            else if (oy == 1)
                            {
                                textureX = 0;
                                textureY = 1;
                            }
                            else if (oy == 0)
                            {
                                textureX = 2;
                                textureY = 1;
                            }
                        }
                        else if (oy == 0)
                        {
                            if (ox == -1)
                            {
                                textureX = 1;
                                textureY = 1;
                            }
                            else if (ox == 1)
                            {
                                textureX = 4;
                                textureY = 0;
                            }
                            // if (ox, oy) == (0, 0), it would have been
                            // evaluated in the ox == 0 branch, so there is no
                            // need to check it here
                        }

                        if (rlImGui.ImageButtonRect(
                            "##button",
                            editor.LevelGraphicsTexture, 20, 20,
                            new Rectangle(textureX * 20, textureY * 20, 20, 20)
                        ))
                        {
                            anchorX = x;
                            anchorY = y;
                        }

                        ImGui.PopID();
                    }
                    ImGui.PopID();
                }

                ImGui.PopStyleVar();
            }

            ImGui.SeparatorText("Border Tiles");
            {
                ImGui.InputInt("Border Tiles Left", ref newBufL);
                ImGui.InputInt("Border Tiles Top", ref newBufT);
                ImGui.InputInt("Border Tiles Right", ref newBufR);
                ImGui.InputInt("Border Tiles Bottom", ref newBufB);

                newBufL = Math.Max(newBufL, 0);
                newBufR = Math.Max(newBufR, 0);
                newBufT = Math.Max(newBufT, 0);
                newBufB = Math.Max(newBufB, 0);

                ImGui.PopItemWidth();

                if (ImGui.Button("OK"))
                {
                    Apply();
                    IsWindowOpen = false;
                }

                ImGui.SameLine();
                if (ImGui.Button("Apply"))
                {
                    Apply();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    IsWindowOpen = false;
                }
            }
        } ImGui.End();
    }

    private void Apply()
    {
        var level = editor.Level;

        // call resize through the editor class, so that the
        // edit window is reloaded
        editor.ResizeLevel(newWidth, newHeight, anchorX - 1, anchorY - 1);

        // don't need to do so with buffer tiles
        level.BufferTilesLeft = newBufL;
        level.BufferTilesTop = newBufT;
        level.BufferTilesRight = newBufR;
        level.BufferTilesBot = newBufB;
    }
}