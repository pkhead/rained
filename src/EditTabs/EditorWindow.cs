using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;

namespace RainEd;

public interface IEditorMode
{
    string Name { get; }

    void Load() {}
    void Unload() {}

    void DrawToolbar();
    void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D layerFrame);
}

public class EditorWindow
{
    public bool IsWindowOpen = true;

    public readonly RainEd Editor;

    private Vector2 viewOffset = new();
    private float viewZoom = 1f;
    private int zoomSteps = 0;
    private int workLayer = 0;

    public float ViewZoom { get => viewZoom; }
    public int WorkLayer { get => workLayer; set => workLayer = value; }

    public Vector2 ViewOffset { get => viewOffset; }

    private int mouseCx = 0;
    private int mouseCy = 0;
    private Vector2 mouseCellFloat = new();

    public int MouseCx { get => mouseCx; }
    public int MouseCy { get => mouseCy; }
    public Vector2 MouseCellFloat { get => mouseCellFloat; }
    public bool IsMouseInLevel() => Editor.Level.IsInBounds(mouseCx, mouseCy);

    private readonly UICanvasWidget canvasWidget;
    public bool IsViewportHovered { get => canvasWidget.IsHovered; }

    private readonly List<IEditorMode> editorModes = new();
    private int selectedMode = 0;

    // render texture given to each editor mode class
    private RlManaged.RenderTexture2D layerRenderTexture;

    public readonly LevelRenderer LevelRenderer;

    public EditorWindow(RainEd editor)
    {
        Editor = editor;
        canvasWidget = new(1, 1);
        layerRenderTexture = new(1, 1);

        LevelRenderer = new LevelRenderer(editor);
        editorModes.Add(new GeometryEditor(this));
        editorModes.Add(new TileEditor(this));
        editorModes.Add(new CameraEditor(this));
    }

    public void Render()
    {
        if (IsWindowOpen && ImGui.Begin("Level", ref IsWindowOpen))
        {
            // edit mode
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Edit Mode");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 8f);
            if (ImGui.BeginCombo("##EditMode", editorModes[selectedMode].Name))
            {
                for (int i = 0; i < editorModes.Count; i++)
                {
                    var isSelected = i == selectedMode;
                    if (ImGui.Selectable(editorModes[i].Name, isSelected))
                    {
                        editorModes[i].Unload();
                        selectedMode = i;
                        editorModes[i].Load();
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            // work layer
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Work Layer");
            ImGui.SameLine();
            {
                var workLayerV = workLayer + 1;
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeightWithSpacing() * 4f);
                ImGui.InputInt("##WorkLayer", ref workLayerV);
                workLayerV = Math.Clamp(workLayerV, 1, 3);    
                workLayer = workLayerV - 1;
            }

            ImGui.SameLine();
            if (ImGui.Button("Reset View"))
            {
                viewOffset = Vector2.Zero;
                viewZoom = 1f;
                zoomSteps = 0;
            }

            ImGui.SameLine();
            ImGui.TextUnformatted($"Zoom: {Math.Floor(viewZoom * 100f)}%");

            // canvas widget
            {
                var regionMax = ImGui.GetWindowContentRegionMax();
                var regionMin = ImGui.GetCursorPos();

                int canvasW = (int)(regionMax.X - regionMin.X);
                int canvasH = (int)(regionMax.Y - regionMin.Y);

                canvasWidget.Resize(canvasW, canvasH);
                if (layerRenderTexture.Texture.Width != canvasW || layerRenderTexture.Texture.Height != canvasH)
                {
                    layerRenderTexture.Dispose();
                    layerRenderTexture = new(canvasW, canvasH);
                }
                
                Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                DrawCanvas();
                Raylib.EndTextureMode();

                canvasWidget.Draw();
            }
        }
        
        editorModes[selectedMode].DrawToolbar();
    }

    private void Zoom(float factor, Vector2 mpos)
    {
        viewZoom *= factor;
        viewOffset = -(mpos - viewOffset) / factor + mpos;
    }

    private void DrawCanvas()
    {
        var level = Editor.Level;
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        Rlgl.PushMatrix();
        Rlgl.Scalef(viewZoom, viewZoom, 1f);
        Rlgl.Translatef(-viewOffset.X, -viewOffset.Y, 0);
        
        // obtain mouse coordinates
        mouseCellFloat.X = (canvasWidget.MouseX / viewZoom + viewOffset.X) / Level.TileSize;
        mouseCellFloat.Y = (canvasWidget.MouseY / viewZoom + viewOffset.Y) / Level.TileSize;
        mouseCx = (int) Math.Floor(mouseCellFloat.X);
        mouseCy = (int) Math.Floor(mouseCellFloat.Y);

        if (canvasWidget.IsHovered)
        {
            // middle click pan
            if (Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                var mouseDelta = Raylib.GetMouseDelta();
                viewOffset -= mouseDelta / viewZoom;
            }

            // scroll wheel zooming
            var wheelMove = Raylib.GetMouseWheelMove();
            var zoomFactor = 1.5;
            if (wheelMove > 0f && zoomSteps < 5)
            {
                var newZoom = Math.Round(viewZoom * zoomFactor * 1000.0) / 1000.0;
                Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                zoomSteps++;
            }
            else if (wheelMove < 0f && zoomSteps > -5)
            {
                var newZoom = Math.Round(viewZoom / zoomFactor * 1000.0) / 1000.0;
                Zoom((float)(newZoom / viewZoom), mouseCellFloat * Level.TileSize);
                zoomSteps--;
            }
        }

        // keep drawing in level bounds
        Raylib.BeginScissorMode(
            (int) (-viewOffset.X * viewZoom),
            (int) (-viewOffset.Y * viewZoom),
            (int) (level.Width * Level.TileSize * viewZoom),
            (int) (level.Height * Level.TileSize * viewZoom)
        );

        editorModes[selectedMode].DrawViewport(canvasWidget.RenderTexture, layerRenderTexture);
        Raylib.EndScissorMode();

        // keybind to switch layer
        if (!ImGui.GetIO().WantCaptureKeyboard && Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }

        Rlgl.PopMatrix();
    }
}