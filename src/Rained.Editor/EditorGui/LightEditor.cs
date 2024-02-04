using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace RainEd;

public class LightEditor : IEditorMode
{
    private struct LightBrush
    {
        public string Name;
        public RlManaged.Texture2D Texture;
    }

    public string Name { get => "Light"; }
    private readonly EditorWindow window;

    private Vector2 brushSize = new(50f, 70f);
    private float brushRotation = 0f;
    private List<LightBrush> lightBrushes;
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

        lightBrushes = new List<LightBrush>();

        foreach (var fileName in File.ReadLines("data/light/init.txt"))
        {
            // if this line is empty, skip
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            // this line is a comment, skip
            if (fileName[0] == '#') continue;
            
            // load light texture
            var tex = new RlManaged.Texture2D($"data/light/{fileName.Trim()}");
            lightBrushes.Add(new LightBrush()
            {
                Name = fileName.Trim(),
                Texture = tex
            });
        }

        Console.WriteLine("Done!");
    }

    public void Load()
    {
        lightmapRt?.Dispose();

        // get light map as a texture
        var lightmapImg = window.Editor.Level.LightMap;
        var lightmapTex = new RlManaged.Texture2D(lightmapImg);

        // put into a render texture
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
        {
            Raylib.ShowCursor();
            Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            isCursorEnabled = true;
        }
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
            foreach (var brush in lightBrushes)
            {
                var texture = brush.Texture;
                
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

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(brush.Name);

                ImGui.PopStyleColor();

                ImGui.PopID();

                i++;

                if (!(i % 3 == 0)) ImGui.SameLine();
            }

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }
    }

    private void DrawOcclusionPlane()
    {
        if (lightmapRt is null) return;
        var level = window.Editor.Level;
        
        // render light plane
        Raylib.DrawTextureRec(
            lightmapRt.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(-300, -300),
            new Color(255, 0, 0, 100)
        );
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

        // Render mouse cursor
        if (window.IsViewportHovered)
        {
            var tex = lightBrushes[selectedBrush].Texture;
            var mpos = window.MouseCellFloat;
            if (!wasCursorEnabled) mpos = savedMouseGp;

            var screenSize = brushSize / window.ViewZoom;

            // render brush preview
            // if drawing, draw on light texture instead of screen
            var lmb = Raylib.IsMouseButtonDown(MouseButton.Left);
            var rmb = Raylib.IsMouseButtonDown(MouseButton.Right);
            if (lmb || rmb)
            {
                isDrawing = true;

                DrawOcclusionPlane();

                Rlgl.LoadIdentity(); // why the hell do i have to call this
                Raylib.BeginTextureMode(lightmapRt);

                // draw on brush plane
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
                // cast of brush preview
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    new Rectangle(
                        mpos.X * Level.TileSize + castOffset.X,
                        mpos.Y * Level.TileSize + castOffset.Y,
                        screenSize.X, screenSize.Y
                    ),
                    screenSize / 2f,
                    brushRotation,
                    new Color(0, 0, 0, 80)
                );
                
                DrawOcclusionPlane();

                // draw preview on on occlusion plane
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
                    new Color(255, 0, 0, 100)
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
        else
        {
            DrawOcclusionPlane();
        }

        Raylib.EndShaderMode();

        // handle cursor lock when transforming brush
        if (!isCursorEnabled)
        {
            Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);    
        }
        
        if (wasCursorEnabled != isCursorEnabled)
        {
            if (isCursorEnabled)
            {
                Raylib.ShowCursor();
                Raylib.SetMousePosition((int)savedMousePos.X, (int)savedMousePos.Y);
            }
            else
            {
                Raylib.HideCursor();
            }
        }
    }

    private void UpdateLightMap()
    {
        if (lightmapRt is null) return;
        Console.WriteLine("Read pixels from GPU");

        var level = window.Editor.Level;
        var lightMapImage = new RlManaged.Image(Raylib.LoadImageFromTexture(lightmapRt.Texture));
        Raylib.ImageFlipVertical(ref lightMapImage.Ref());
        Raylib.ImageFormat(ref lightMapImage.Ref(), PixelFormat.UncompressedGrayscale);
        level.LightMap = lightMapImage;
    }
}