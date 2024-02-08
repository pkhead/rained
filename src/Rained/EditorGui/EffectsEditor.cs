using ImGuiNET;
using Raylib_cs;

namespace RainEd;

public class EffectsEditor : IEditorMode
{
    public string Name { get => "Effects"; }
    private readonly EditorWindow window;

    public EffectsEditor(EditorWindow window)
    {
        this.window = window;
    }

    public void DrawToolbar()
    {
        var level = window.Editor.Level;

        if (ImGui.Begin("Effects", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.Text("Effect Stack");
            ImGui.TextDisabled("(none)");
            ImGui.Separator();
            ImGui.Text("Add Effect");
        }
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

        levelRender.RenderGrid();
        levelRender.RenderBorder();
    }
}