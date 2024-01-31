using Raylib_cs;
using ImGuiNET;
using System.Numerics;

namespace RainEd;

public class CameraEditor : IEditorMode
{
    public string Name { get => "Cameras"; }
    private EditorWindow window;

    public CameraEditor(EditorWindow window) {
        this.window = window;
    }
    
    public void DrawToolbar() {}

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

        // levelRender.RenderGrid(0.5f / window.ViewZoom);
        levelRender.RenderBorder(1.0f / window.ViewZoom);

        // render cameras
        foreach (Camera camera in level.Cameras)
        {
            var camCenter = camera.Position + Camera.WidescreenSize / 2f;

            // draw full camera rectangle
            Raylib.DrawRectangleRec(
                new Rectangle(
                    camera.Position * Level.TileSize,
                    Camera.WidescreenSize * Level.TileSize
                ),
                new Color(50, 255, 50, 30)       
            );

            // draw full rect ouline
            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    camera.Position * Level.TileSize,
                    Camera.WidescreenSize * Level.TileSize
                ),
                2f / window.ViewZoom,
                new Color(0, 0, 0, 255)       
            );

            // draw inner outline
            var innerOutlineSize = Camera.WidescreenSize * ((Camera.WidescreenSize.X - 2) / Camera.WidescreenSize.X);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    (camCenter - innerOutlineSize / 2) * Level.TileSize,
                    innerOutlineSize * Level.TileSize
                ),
                2f / window.ViewZoom,
                new Color(9, 0, 0, 255)
            );

            // 4:3 outline
            var standardResOutlineSize = Camera.StandardSize * ((Camera.WidescreenSize.X - 2) / Camera.WidescreenSize.X);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(
                    (camCenter - standardResOutlineSize / 2) * Level.TileSize,
                    standardResOutlineSize * Level.TileSize
                ),
                2f / window.ViewZoom,
                new Color(255, 0, 0, 255)
            );

            // draw center circle
            Raylib.DrawCircleLines((int)(camCenter.X * Level.TileSize), (int)(camCenter.Y * Level.TileSize), 50f, Color.Black);

            Raylib.DrawLine(
                (int)(camCenter.X * Level.TileSize),
                (int)(camera.Position.Y * Level.TileSize),
                (int)(camCenter.X * Level.TileSize),
                (int)((camera.Position.Y + Camera.StandardSize.Y) * Level.TileSize),
                Color.Black
            );

            Raylib.DrawLine(
                (int)((camCenter.X - 5f) * Level.TileSize),
                (int)(camCenter.Y * Level.TileSize),
                (int)((camCenter.X + 5f) * Level.TileSize),
                (int)(camCenter.Y * Level.TileSize),
                Color.Black
            );
        }
    }
}