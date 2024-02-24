using ImGuiNET;
using Raylib_cs;
using System.Numerics;
namespace RainEd;

class PropEditor : IEditorMode
{
    public string Name { get => "Props"; }
    private readonly EditorWindow window;

    public PropEditor(EditorWindow window)
    {
        this.window = window;
    }

    public void DrawToolbar()
    {
        var level = RainEd.Instance.Level;

        if (ImGui.Begin("Props", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.SeparatorText("Prop Selector");

            if (ImGui.BeginTabBar("PropSelector"))
            {
                if (ImGui.BeginTabItem("Tiles"))
                {
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Props"))
                {
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.SeparatorText("Options");

            // work layer
            {
                int workLayerV = window.WorkLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("Work Layer", ref workLayerV);
                window.WorkLayer = Math.Clamp(workLayerV, 1, 3) - 1;
            }

            // sublayer
            // prop settings
            
            ImGui.End();
        }
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        window.BeginLevelScissorMode();

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, new Color(127, 127, 127, 255));

        // draw layers
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            Raylib.BeginTextureMode(layerFrame);

            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            levelRender.RenderGeometry(l, new Color(0, 0, 0, 255));
            levelRender.RenderTiles(l, 255);
            
            // draw alpha-blended result into main frame
            Raylib.BeginTextureMode(mainFrame);
            Rlgl.PushMatrix();
            Rlgl.LoadIdentity();

            int offset = l * 2;
            var alpha = l == window.WorkLayer ? 255 : 50;
            Raylib.DrawTextureRec(
                layerFrame.Texture,
                new Rectangle(0f, layerFrame.Texture.Height, layerFrame.Texture.Width, -layerFrame.Texture.Height),
                Vector2.One * offset,
                new Color(255, 255, 255, alpha)
            );
            Rlgl.PopMatrix();
        }

        levelRender.RenderGrid();
        levelRender.RenderBorder();
    }
}