using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
namespace RainEd;

public class LightEditor : IEditorMode
{
    public string Name { get => "Light"; }
    private readonly EditorWindow window;

    public List<RlManaged.Texture2D> lightTextures;

    public LightEditor(EditorWindow window)
    {
        this.window = window;

        // load light textures
        Console.WriteLine("Initializing light cast catalog...");

        lightTextures = new List<RlManaged.Texture2D>();

        foreach (var fileName in File.ReadLines("data/light/init.txt"))
        {
            // if this line is empty, skip
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            // this line is a comment, skip
            if (fileName[0] == '#') continue;
            
            // load light texture
            var tex = new RlManaged.Texture2D($"data/light/{fileName.Trim()}");
            lightTextures.Add(tex);
        }

        Console.WriteLine("Done!");
    }

    public void DrawToolbar()
    {
        if (ImGui.Begin("Catalog###Light Catalog", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, 0f);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0f, 0f, 0f, 0f));

            int i = 0;
            foreach (var texture in lightTextures)
            {
                ImGui.PushID(i);
                rlImGui.ImageButtonRect("##Texture", texture, 64, 64, new Rectangle(0, 0, texture.Width, texture.Height));
                ImGui.PopID();

                i++;
            }

            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
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

        levelRender.RenderBorder();
    }
}