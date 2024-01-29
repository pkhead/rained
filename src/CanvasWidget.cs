using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
namespace RainEd;

public class UICanvasWidget
{
    public string ID;
    private float mouseX;
    private float mouseY;
    private bool hovered;

    public float MouseX { get => mouseX; }
    public float MouseY { get => mouseY; }
    public bool IsHovered { get => hovered; }

    private RlManaged.RenderTexture2D renderTexture;
    public RlManaged.RenderTexture2D RenderTexture { get => renderTexture; }

    int curWidth, curHeight;

    public delegate void ActivationHandler(object sender);
    public delegate void ClickHandler(object sender, MouseButton button);
    public event ActivationHandler? Activated;
    public event ActivationHandler? Dectivated;
    public event ClickHandler? Clicked;

    public UICanvasWidget(int width, int height)
    {
        ID = "##canvas_widget";

        if (width > 0 && height > 0)
        {
            renderTexture = new(width, height);
        }
        else
        {
            renderTexture = new(1, 1);
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
            newWidth = 1;
            newHeight = 1;
        }
        
        if (curWidth != newWidth || curHeight != newHeight)
        {
            curWidth = newWidth;
            curHeight = newHeight;

            renderTexture?.Dispose();
            renderTexture = new(curWidth, curHeight);
        }
    }
    
    public void Draw()
    {
        var windowOrigin = ImGui.GetCursorPos();
        var screenOrigin = ImGui.GetCursorScreenPos();

        rlImGui.ImageRenderTexture(renderTexture);
        ImGui.SetCursorPos(windowOrigin);
        ImGui.InvisibleButton(ID, new Vector2(curWidth, curHeight));

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