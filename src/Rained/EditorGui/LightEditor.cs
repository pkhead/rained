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

    private struct StrokeAtom
    {
        public float rotation;
        public Rectangle rect;
        public int brush;
        public bool mode;
    }

    private class Stroke : IChangeRecord
    {
        public LightEditor lightEditor;

        public StrokeAtom[] atoms;
        public Stroke? previous = null;

        public Stroke(StrokeAtom[] atoms, LightEditor lightEditor)
        {
            this.lightEditor = lightEditor;
            this.atoms = atoms;
            previous = lightEditor.lastStroke;
        }

        public bool HasChange() => true;
        public void Apply(RainEd editor, bool useNew)
        {
            if (lightEditor is null) throw new NullReferenceException();

            // redo call
            if (useNew)
            {
                lightEditor.lastStroke = this;
                lightEditor.Retrace();
            }

            // undo call
            else
            {
                lightEditor.lastStroke = previous;
                lightEditor.Retrace();
            }
        }
    }

    public string Name { get => "Light"; }
    private readonly EditorWindow window;

    private Vector2 brushSize = new(50f, 70f);
    private float brushRotation = 0f;
    private readonly List<LightBrush> lightBrushes;
    private RlManaged.RenderTexture2D? lightmapRt;
    private RlManaged.Texture2D origLightmap; // for change history
    private int selectedBrush = 0;

    private int thisEditMode = -1;

    private List<StrokeAtom> currentStrokeData = new();
    private Stroke? lastStroke = null;

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
            var tex = RlManaged.Texture2D.Load($"data/light/{fileName.Trim()}");
            lightBrushes.Add(new LightBrush()
            {
                Name = fileName.Trim(),
                Texture = tex
            });
        }

        Console.WriteLine("Done!");

        ReloadLevel();
        if (origLightmap == null) throw new Exception();
    }

    public void ReloadLevel()
    {
        lastStroke = null;
        lightmapRt?.Dispose();
        origLightmap?.Dispose();

        // get light map as a texture
        var lightmapImg = window.Editor.Level.LightMap;
        var lightmapTex = RlManaged.Texture2D.LoadFromImage(lightmapImg);
        origLightmap = lightmapTex;

        // put into a render texture
        lightmapRt = RlManaged.RenderTexture2D.Load(lightmapImg.Width, lightmapImg.Height);
        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(lightmapTex, 0, 0, Color.White);
        Raylib.EndTextureMode();
    }

    public void Load()
    {
        currentStrokeData = new();
        thisEditMode = window.EditMode;
    }

    public void Unload()
    {
        UpdateLightMap();

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
            ImGui.SliderFloat("Shadow Dist", ref level.LightDistance, 1f, Level.MaxLightDistance, "%.3f", ImGuiSliderFlags.AlwaysClamp);
            ImGui.PopItemWidth();

            level.LightAngle = lightDeg / 180f * MathF.PI;

            if (ImGui.Button("Reset Brush") || (!ImGui.GetIO().WantTextInput && ImGui.IsKeyPressed(ImGuiKey.R)))
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
        var levelBoundsW = level.Width * 20;
        var levelBoundsH = level.Height * 20;
        Raylib.DrawTextureRec(
            lightmapRt.Texture,
            new Rectangle(0, level.LightMap.Height, level.LightMap.Width, -level.LightMap.Height),
            new Vector2(levelBoundsW - level.LightMap.Width, levelBoundsH - level.LightMap.Height),
            new Color(255, 0, 0, 100)
        );
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame)
    {
        if (lightmapRt is null) return;
        var level = window.Editor.Level;
        var levelRender = window.LevelRenderer;

        var levelBoundsW = level.Width * 20;
        var levelBoundsH = level.Height * 20;
        var lightMapOffset = new Vector2(
            levelBoundsW - level.LightMap.Width,
            levelBoundsH - level.LightMap.Height
        );

        var wasCursorEnabled = isCursorEnabled;
        var wasDrawing = isDrawing;
        isCursorEnabled = true;
        isDrawing = false;


        // draw light background
        Raylib.DrawRectangle(
            (int)lightMapOffset.X,
            (int)lightMapOffset.Y,
            level.Width * Level.TileSize - (int)lightMapOffset.X,
            level.Height * Level.TileSize - (int)lightMapOffset.Y,
            Color.White
        );
        
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
            lightMapOffset + castOffset,
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
                StrokeAtom atom = new()
                {
                    rect = new Rectangle(
                        mpos.X * Level.TileSize - lightMapOffset.X,
                        mpos.Y * Level.TileSize - lightMapOffset.Y,
                        screenSize.X, screenSize.Y
                    ),
                    rotation = brushRotation,
                    mode = lmb,
                    brush = selectedBrush
                };

                // record brush "atom" if last atom is different
                if (currentStrokeData.Count == 0 || !atom.Equals(currentStrokeData[^1]))
                    currentStrokeData.Add(atom);

                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    atom.rect,
                    screenSize / 2f,
                    atom.rotation,
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

            var doScale = Raylib.IsKeyDown(KeyboardKey.Q);
            var doRotate = Raylib.IsKeyDown(KeyboardKey.E);

            if (doScale || doRotate)
            {
                if (wasCursorEnabled)
                {
                    savedMouseGp = mpos;
                    savedMousePos = Raylib.GetMousePosition();
                }
                isCursorEnabled = false;

                if (doScale)
                    brushSize += Raylib.GetMouseDelta();
                if (doRotate)
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
        
        // record stroke data at the end of the stroke
        if (wasDrawing && !isDrawing)
        {
            EndStroke();
        }

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

    private void EndStroke()
    {
        if (currentStrokeData.Count == 0) return;

        var stroke = new Stroke(currentStrokeData.ToArray(), this);
        currentStrokeData.Clear();

        window.Editor.ChangeHistory.PushCustom(stroke);
        lastStroke = stroke;
    }

    private void UpdateLightMap()
    {
        if (lightmapRt is null) return;
        Console.WriteLine("Update light map");
        var level = window.Editor.Level;

        var lightMapImage = RlManaged.Image.LoadFromTexture(lightmapRt.Texture);
        Raylib.ImageFlipVertical(ref lightMapImage.Ref());
        Raylib.ImageFormat(ref lightMapImage.Ref(), PixelFormat.UncompressedGrayscale);
        level.LightMap = lightMapImage;
    }

    private void Retrace()
    {
        if (lightmapRt is null) throw new Exception();
        window.EditMode = thisEditMode;

        void recurse(Stroke? thisStroke)
        {
            if (thisStroke is null) return;
            recurse(thisStroke.previous);

            foreach (var atom in thisStroke.atoms)
            {
                var tex = lightBrushes[atom.brush].Texture;
                Raylib.DrawTexturePro(
                    tex,
                    new Rectangle(0, 0, tex.Width, tex.Height),
                    atom.rect,
                    new Vector2(atom.rect.Width, atom.rect.Height) / 2f,
                    atom.rotation,
                    atom.mode ? Color.Black : Color.White
                );
            }
        }

        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(origLightmap, 0, 0, Color.White);
        Raylib.BeginShaderMode(levelLightShader);
        recurse(lastStroke);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }
}