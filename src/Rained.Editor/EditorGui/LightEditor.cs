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

    private RlManaged.RenderTexture2D? lightmapRt;

    private static string levelLightShaderSrc = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(texture0, fragTexCoord);
            finalColor = vec4(1.0, 1.0, 1.0, 1.0 - texelColor.r) * fragColor * colDiffuse;
        }
    ";

    private readonly RlManaged.Shader levelLightShader;

    public LightEditor(EditorWindow window)
    {
        this.window = window;

        levelLightShader = RlManaged.Shader.LoadFromMemory(null, levelLightShaderSrc);

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

    public void Load()
    {
        lightmapRt?.Dispose();

        var lightmapImg = window.Editor.Level.LightMap;

        // get texture of lightmap image
        var lightmapTex = new RlManaged.Texture2D(lightmapImg);

        // get into a render texture
        lightmapRt = new RlManaged.RenderTexture2D(lightmapImg.Width, lightmapImg.Height);
        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(lightmapTex, 0, 0, Color.White);
        Raylib.EndTextureMode();

        lightmapTex.Dispose();
    }

    public void Unload()
    {
        lightmapRt?.Dispose();
        lightmapRt = null;
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
        if (lightmapRt is null) return;

        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        // draw light background
        Raylib.DrawRectangle(-300, -300, level.Width * Level.TileSize + 300, level.Height * Level.TileSize + 300, Color.White);
        
        // draw level background (solid white)
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize - 30, level.Height * Level.TileSize - 30, new Color(127, 127, 127, 255));
        
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

        Raylib.BeginShaderMode(levelLightShader);

        // render cast
        Vector2 castOffset = new(
            MathF.Sin(level.LightAngle) * level.LightDistance * Level.TileSize,
            -MathF.Cos(level.LightAngle) * level.LightDistance * Level.TileSize
        );

        Raylib.DrawTextureRec(
            lightmapRt.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(-300, -300) + castOffset,
            new Color(0, 0, 0, 80)
        );

        // render light plane
        Raylib.DrawTextureRec(
            lightmapRt.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(-300, -300),
            new Color(255, 0, 0, 100)
        );

        Raylib.EndShaderMode();
    }
}