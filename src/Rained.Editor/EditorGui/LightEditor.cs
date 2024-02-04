using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace RainEd;

public class LightEditor : IEditorMode
{
    public string Name { get => "Light"; }
    private readonly EditorWindow window;

    private Vector2 brushSize = new(50f, 70f);
    private float brushRotation = 0f;
    public List<RlManaged.Texture2D> lightTextures;
    private RlManaged.RenderTexture2D? lightmapRt;
    private int selectedBrush = 0;

    private bool isCursorEnabled = true;
    private bool isDrawing = false;
    private Vector2 savedMouseGp = new();
    private Vector2 savedMousePos = new();

    private readonly static string levelLightShaderSrc = @"
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
        UpdateLightMap();

        lightmapRt?.Dispose();
        lightmapRt = null;

        if (!isCursorEnabled)
            Raylib.EnableCursor();
    }

    public void DrawToolbar()
    {
        var level = window.Editor.Level;

        if (ImGui.Begin("Light###Light Catalog", ImGuiWindowFlags.NoFocusOnAppearing))
        {
            float lightDeg = level.LightAngle / MathF.PI * 180f;

            ImGui.PushItemWidth(ImGui.GetTextLineHeight() * 8.0f);
            ImGui.SliderFloat("Light Angle", ref lightDeg, 0f, 360f, "%.1f deg");
            ImGui.SliderFloat("Light Dist", ref level.LightDistance, 1f, Level.MaxLightDistance, "%.3f", ImGuiSliderFlags.AlwaysClamp);
            ImGui.PopItemWidth();

            level.LightAngle = lightDeg / 180f * MathF.PI;

            if (ImGui.Button("Reset Brush Transform") || (!ImGui.GetIO().WantTextInput && ImGui.IsKeyPressed(ImGuiKey.R)))
            {
                brushSize = new(50f, 70f);
                brushRotation = 0f;
            }

            // paint brushes
            ImGui.Text("Brushes");

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

            int i = 0;
            foreach (var texture in lightTextures)
            {
                // highlight selected brush
                if (i == selectedBrush)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered]);
                
                // buttons will have a more transparent hover color
                } else {
                    Vector4 col = ImGui.GetStyle().Colors[(int) ImGuiCol.ButtonHovered];
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                        new Vector4(col.X, col.Y, col.Z, col.W / 4f));
                }

                ImGui.PushID(i);
                if (rlImGui.ImageButtonRect("##Texture", texture, 64, 64, new Rectangle(0, 0, texture.Width, texture.Height)))
                {
                    selectedBrush = i;
                }

                ImGui.PopStyleColor();

                ImGui.PopID();

                i++;

                if (!(i % 3 == 0)) ImGui.SameLine();
            }

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        if (lightmapRt is null) return;

        var wasCursorEnabled = isCursorEnabled;
        var wasDrawing = isDrawing;
        isCursorEnabled = true;
        isDrawing = false;

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

        // Render mouse cursor
        if (window.IsViewportHovered)
        {
            var tex = lightTextures[selectedBrush];
            var mpos = window.MouseCellFloat;
            if (!wasCursorEnabled) mpos = savedMouseGp;

            var screenSize = brushSize / window.ViewZoom;

            // if drawing, draw on light texture instead of screen
            var lmb = Raylib.IsMouseButtonDown(MouseButton.Left);
            var rmb = Raylib.IsMouseButtonDown(MouseButton.Right);
            if (lmb || rmb)
            {
                isDrawing = true;

                Rlgl.LoadIdentity(); // why the hell do i have to call this
                Raylib.BeginTextureMode(lightmapRt);

                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    new Rectangle(
                        mpos.X * Level.TileSize + 300f,
                        mpos.Y * Level.TileSize + 300f,
                        screenSize.X, screenSize.Y
                    ),
                    screenSize / 2f,
                    brushRotation,
                    lmb ? Color.Black : Color.White
                );
                
                Raylib.BeginTextureMode(mainFrame);
            }
            else
            {
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    new Rectangle(
                        mpos.X * Level.TileSize,
                        mpos.Y * Level.TileSize,
                        screenSize.X, screenSize.Y
                    ),
                    screenSize / 2f,
                    brushRotation,
                    Color.White
                );
            }

            var isShiftDown = Raylib.IsKeyDown(KeyboardKey.LeftShift);
            var isCtrlDown = Raylib.IsKeyDown(KeyboardKey.LeftControl);

            if (isShiftDown || isCtrlDown)
            {
                if (wasCursorEnabled)
                {
                    savedMouseGp = mpos;
                    savedMousePos = Raylib.GetMousePosition();
                }
                isCursorEnabled = false;

                if (isShiftDown)
                    brushSize += Raylib.GetMouseDelta();
                if (isCtrlDown)
                    brushRotation -= Raylib.GetMouseDelta().Y / 2f;
            }

            brushSize.X = MathF.Max(0f, brushSize.X);
            brushSize.Y  = MathF.Max(0f, brushSize.Y);
        }

        Raylib.EndShaderMode();

        // write light map change from gpu to ram
        // when drawing ends
        if (wasDrawing && !isDrawing)
            UpdateLightMap();

        // handle cursor lock when transforming brush
        if (!isCursorEnabled) Raylib.DisableCursor();
        if (wasCursorEnabled != isCursorEnabled)
        {
            if (isCursorEnabled)
            {
                Raylib.EnableCursor();
                Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            }
        }
    }

    private void UpdateLightMap()
    {
        if (lightmapRt is null) return;
        Console.WriteLine("Read pixels from GPU");

        var level = window.Editor.Level;
        var oldLightMap = level.LightMap;
        var newLightMap = new RlManaged.Image(Raylib.LoadImageFromTexture(lightmapRt.Texture));
        Raylib.ImageFlipVertical(ref newLightMap.Ref());
        level.LightMap = newLightMap;

        oldLightMap.Dispose();
    }
}