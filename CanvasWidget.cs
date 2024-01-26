using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using RlManaged;

namespace RainEd;

abstract public class UICanvasWidget
{
    protected string id;
    private float mouseX;
    private float mouseY;
    private bool hovered;

    public float MouseX { get => mouseX; }
    public float MouseY { get => mouseY; }
    public bool IsHovered { get => hovered; }

    private RlManaged.RenderTexture2D? renderTexture;
    int curWidth, curHeight;

    public delegate void ActivationHandler(object sender);
    public delegate void ClickHandler(object sender, MouseButton button);
    public event ActivationHandler? Activated;
    public event ActivationHandler? Dectivated;
    public event ClickHandler? Clicked;

    public UICanvasWidget(int width, int height)
    {
        id = "##canvas_widget";

        if (width > 0 && height > 0)
        {
            renderTexture = new(width, height);
        }
        else
        {
            renderTexture = null;
        }

        hovered = false;
        mouseX = 0;

        curWidth = width;
        curHeight = height;
        Resize(width, height);
    }

    public void Resize(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0)
        {
            if (renderTexture is not null)
            {
                renderTexture.Dispose();
                renderTexture = null;
            }
        }
        else if (renderTexture is null || curWidth != newWidth || curHeight != newHeight)
        {
            curWidth = newWidth;
            curHeight = newHeight;

            renderTexture?.Dispose();
            renderTexture = new(curWidth, curHeight);
        }
    }

    protected abstract void Draw();

    public void ImguiRender()
    {
        if (renderTexture is null) return;

        Raylib.BeginTextureMode(renderTexture);
        Draw();
        Raylib.EndTextureMode();

        var windowOrigin = ImGui.GetCursorPos();
        var screenOrigin = ImGui.GetCursorScreenPos();

        rlImGui.ImageRenderTexture(renderTexture);
        ImGui.SetCursorPos(windowOrigin);
        ImGui.InvisibleButton(id, new Vector2(curWidth, curHeight));

        hovered = ImGui.IsItemHovered() || ImGui.IsItemActive();
        if (hovered)
        {
            mouseX = Raylib.GetMouseX() - screenOrigin.X;
            mouseY = Raylib.GetMouseY() - screenOrigin.Y;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) Clicked?.Invoke(this, MouseButton.Left);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Middle)) Clicked?.Invoke(this, MouseButton.Middle);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) Clicked?.Invoke(this, MouseButton.Right);
        if (ImGui.IsItemActivated()) Activated?.Invoke(this);
        if (ImGui.IsItemDeactivated()) Dectivated?.Invoke(this);
    }
}