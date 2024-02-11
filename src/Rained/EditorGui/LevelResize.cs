using System.Numerics;
using ImGuiNET;
namespace RainEd;

class LevelResizeWindow
{
    private readonly RainEd editor;
    public bool IsWindowOpen = true;
    private int newWidth;
    private int newHeight;
    private int newBufL, newBufR, newBufT, newBufB;

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

            ImGui.Separator();

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
                //editor.ResizeLevel(newWidth, newHeight);
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                IsWindowOpen = false;
            }

        } ImGui.End();
    }
}