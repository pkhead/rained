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
    void DrawViewport();
}

public class EditorWindow
{
    public bool IsWindowOpen = true;

    public readonly RainEd Editor;

    private Vector2 viewOffset = new();
    private float viewZoom = 1f;
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
    public bool IsMouseInLevel() => mouseCx >= 0 && mouseCy >= 0 && mouseCx < Editor.Level.Width && mouseCy < Editor.Level.Height;

    private readonly UICanvasWidget canvasWidget;
    public bool IsViewportHovered { get => canvasWidget.IsHovered; }

    private List<IEditorMode> editorModes = new();
    private int selectedMode = 0;

    public EditorWindow(RainEd editor)
    {
        Editor = editor;
        canvasWidget = new(1, 1);

        editorModes.Add(new GeometryEditor(this));
        editorModes.Add(new TileEditor(this));
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

            // canvas widget
            {
                var regionMax = ImGui.GetWindowContentRegionMax();
                var regionMin = ImGui.GetCursorPos();

                canvasWidget.Resize((int)(regionMax.X - regionMin.X), (int)(regionMax.Y - regionMin.Y));
                
                if (canvasWidget.RenderTexture is not null)
                {
                    Raylib.BeginTextureMode(canvasWidget.RenderTexture);
                    DrawCanvas();
                    Raylib.EndTextureMode();
                }

                canvasWidget.Draw();
            }
        }
        
        editorModes[selectedMode].DrawToolbar();
    }

    private void Zoom(float factor, Vector2 mpos)
    {
        Console.WriteLine(viewOffset);

        viewZoom *= factor;
        viewOffset = -(mpos - viewOffset) / factor + mpos;

        Console.WriteLine(viewOffset);
    }

    private void DrawCanvas()
    {
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        Rlgl.PushMatrix();
        Rlgl.Scalef(viewZoom, viewZoom, 1f);
        Rlgl.Translatef(-viewOffset.X, -viewOffset.Y, 0);
        
        // obtain mouse coordinates
        mouseCellFloat.X = (canvasWidget.MouseX / viewZoom + viewOffset.X) / Level.TileSize;
        mouseCellFloat.Y = (canvasWidget.MouseY / viewZoom + viewOffset.Y) / Level.TileSize;
        mouseCx = (int) mouseCellFloat.X;
        mouseCy = (int) mouseCellFloat.Y;

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
            if (wheelMove > 0f)
            {
                Zoom(1.5f, mouseCellFloat * Level.TileSize);
            }
            else if (wheelMove < 0f)
            {
                Zoom(1f / 1.5f, mouseCellFloat * Level.TileSize);
            }
        }

        editorModes[selectedMode].DrawViewport();

        // keybind to switch layer
        if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            workLayer = (workLayer + 1) % 3;
        }

        Rlgl.PopMatrix();
    }
}