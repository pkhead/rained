using Raylib_cs;
using ImGuiNET;

namespace RainEd;

static class EditorWindow
{
    // input overrides for lmb because of alt + left click drag
    private static bool isLmbPanning = false;
    private static bool isLmbDown = false;
    private static bool isLmbClicked = false;
    private static bool isLmbReleased = false;
    private static bool isLmbDragging = false;

    public static bool IsPanning { get => isLmbPanning; set => isLmbPanning = value; }

    public static bool IsKeyDown(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyDown(key);
    }

    public static bool IsKeyPressed(ImGuiKey key)
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return ImGui.IsKeyPressed(key);
    }

    // need to use Raylib.IsKeyPressed instead of EditorWindow.IsKeyPressed
    // because i specifically disabled the Tab key in ImGui input handling
    public static bool IsTabPressed()
    {
        if (ImGui.GetIO().WantTextInput) return false;
        return Raylib.IsKeyPressed(KeyboardKey.Tab);
    }

    public static bool IsMouseClicked(ImGuiMouseButton button, bool repeat = false)
    {
        if (button == ImGuiMouseButton.Left) return isLmbClicked;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Activated(KeyShortcut.RightMouse);
        return ImGui.IsMouseClicked(button, repeat);
    }

    public static bool IsMouseDown(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDown;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Active(KeyShortcut.RightMouse);
        return ImGui.IsMouseDown(button);
    }

    public static bool IsMouseDoubleClicked(ImGuiMouseButton button)
    {
        if (isLmbPanning) return false;
        return ImGui.IsMouseDoubleClicked(button);
    }

    public static bool IsMouseReleased(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbReleased;
        if (button == ImGuiMouseButton.Right) return KeyShortcuts.Deactivated(KeyShortcut.RightMouse);
        return ImGui.IsMouseReleased(button);
    }

    public static bool IsMouseDragging(ImGuiMouseButton button)
    {
        if (button == ImGuiMouseButton.Left) return isLmbDragging;
        return ImGui.IsMouseDragging(button);
    }

    public static void Render()
    {
        //UpdateMouseState();
    }

    public static void UpdateMouseState()
    {
        bool wasLmbDown = isLmbDown;
        isLmbClicked = false;
        isLmbDown = false;
        isLmbReleased = false;
        isLmbDragging = false;

        if (!isLmbPanning)
        {
            isLmbDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            isLmbDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);

            // manually set Clicked or Released bools based on lmbdown state changes
            // this is so it registers that the mouse was released when ther user alt+tabs out of the window
            if (!wasLmbDown && isLmbDown)
                isLmbClicked = true;

            if (wasLmbDown && !isLmbDown)
                isLmbReleased = true;
        }
    }
}