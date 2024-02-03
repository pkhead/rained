using Raylib_cs;
using ImGuiNET;
using System.Numerics;

namespace RainEd;

public class CameraEditor : IEditorMode
{
    public string Name { get => "Cameras"; }
    private readonly EditorWindow window;
    private Camera? activeCamera = null;
    private int activeCorner = -1;
    private Vector2 mouseOffset = new(); // mouse offset from camera position when drag starts

    public CameraEditor(EditorWindow window) {
        this.window = window;
    }
    
    public void DrawToolbar() {
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

        // levelRender.RenderGrid(0.5f / window.ViewZoom);
        levelRender.RenderBorder();

        // mouse-pick cameras
        Camera? cameraHoveredOver = null;
        int cornerHover = -1;

        if (window.IsViewportHovered)
        {
            // drag active camera
            if (activeCamera is not null)
            {
                cameraHoveredOver = activeCamera;
                cornerHover = activeCorner;

                // drag camera rect
                if (activeCorner == -1)
                    activeCamera.Position = window.MouseCellFloat - mouseOffset;
                else // drag camera corner
                {
                    var vecDiff = window.MouseCellFloat - activeCamera.GetCornerPosition(activeCorner, false);

                    var angle = MathF.Atan2(vecDiff.X, -vecDiff.Y);
                    var offset = Math.Clamp(vecDiff.Length(), 0f, 4f);
                    activeCamera.CornerAngles[activeCorner] = angle;
                    activeCamera.CornerOffsets[activeCorner] = offset / 4f;
                }

                if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                {
                    activeCamera = null;
                    window.Editor.EndChange();
                }
            }

            // no active cameras, so mouse-pick cameras
            else
            {
                var mpos = window.MouseCellFloat;
                foreach (Camera camera in level.Cameras)
                {
                    // determine if mouse is close enough to one of its corners
                    var foundCorner = false;

                    for (int c = 0; c < 4; c++)
                    {
                        var cpos = camera.GetCornerPosition(c, true);
                        if ((cpos - mpos).Length() < 0.5f / window.ViewZoom)
                        {
                            cornerHover = c;
                            cameraHoveredOver = camera;
                            foundCorner = true;
                        }
                    }

                    if (foundCorner) continue;

                    // else, determine if mouse is within camera bounds
                    var cameraA = camera.Position;
                    var cameraB = camera.Position + Camera.WidescreenSize;

                    // if so, mark this camera as hovered-over
                    if (mpos.X > cameraA.X && mpos.Y > cameraA.Y &&
                        mpos.X < cameraB.X && mpos.Y < cameraB.Y
                    )
                    {
                        cornerHover = -1;
                        cameraHoveredOver = camera;
                    }
                }

                if (cameraHoveredOver is not null && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    mouseOffset = window.MouseCellFloat - cameraHoveredOver.Position;
                    activeCamera = cameraHoveredOver;
                    activeCorner = cornerHover;
                    window.Editor.BeginChange();
                }
            }
        }

        // keybinds
        if (!ImGui.GetIO().WantCaptureKeyboard && activeCamera is null)
        {
            // N to create new camera
            if (Raylib.IsKeyPressed(KeyboardKey.N) && level.Cameras.Count < Level.MaxCameraCount)
            {
                window.Editor.BeginChange();
                var cam = new Camera(window.MouseCellFloat - Camera.WidescreenSize / 2f);
                level.Cameras.Add(cam);
                window.Editor.EndChange();
            }

            // Right-Click, Delete, or Backspace to delete camera
            // that is being hovered over
            // if hovering over a corner, it instead resets the corner transform
            if (cameraHoveredOver is not null)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Delete)
                    || Raylib.IsKeyPressed(KeyboardKey.Backspace)
                    || Raylib.IsMouseButtonPressed(MouseButton.Right)
                )
                {
                    window.Editor.BeginChange();
                    if (cornerHover == -1 && level.Cameras.Count > 1)
                    {
                        level.Cameras.Remove(cameraHoveredOver);
                        cameraHoveredOver = null;
                        activeCamera = null;
                    } else if (cornerHover >= 0)
                    {
                        cameraHoveredOver.CornerAngles[cornerHover] = 0f;
                        cameraHoveredOver.CornerOffsets[cornerHover] = 0f;
                    }
                    window.Editor.EndChange();
                }
            }
        }

        // render cameras
        foreach (Camera camera in level.Cameras)
        {
            RenderCamera(camera, cameraHoveredOver == camera, cornerHover);
        }
    }

    private void RenderCamera(Camera camera, bool isHovered, int corner)
    {
        var camCenter = camera.Position + Camera.WidescreenSize / 2f;

        // draw full camera quad
        var p0 = camera.GetCornerPosition(0, true) * Level.TileSize;
        var p1 = camera.GetCornerPosition(1, true) * Level.TileSize;
        var p2 = camera.GetCornerPosition(2, true) * Level.TileSize;
        var p3 = camera.GetCornerPosition(3, true) * Level.TileSize;
        var quadColor = isHovered ? new Color(50, 255, 50, 60) : new Color(50, 255, 50, 30);

        Raylib.DrawTriangle(p0, p3, p1, quadColor);
        Raylib.DrawTriangle(p3, p2, p1, quadColor);
        
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

        // draw corner if highlighted
        if (isHovered && corner >= 0)
        {
            var cornerOrigin = camera.GetCornerPosition(corner, false);
            var cornerPos = camera.GetCornerPosition(corner, true);

            // outer circle
            Raylib.DrawCircleLines(
                (int)(cornerOrigin.X * Level.TileSize),
                (int)(cornerOrigin.Y * Level.TileSize),
                4f * Level.TileSize,
                new Color(0, 255, 0, 255)
            );

            // inner circle
            Raylib.DrawCircleLines(
                (int)(cornerOrigin.X * Level.TileSize),
                (int)(cornerOrigin.Y * Level.TileSize),
                camera.CornerOffsets[corner] * 4f * Level.TileSize,
                new Color(0, 255, 0, 255)
            );

            // point at corner
            Raylib.DrawCircle(
                (int)(cornerPos.X * Level.TileSize),
                (int)(cornerPos.Y * Level.TileSize),
                5f / window.ViewZoom,
                new Color(0, 255, 0, 255)
            );

            // point at corner origin
            Raylib.DrawCircle(
                (int)(cornerOrigin.X * Level.TileSize),
                (int)(cornerOrigin.Y * Level.TileSize),
                2f / window.ViewZoom,
                new Color(0, 255, 0, 255)
            );

            // line from corner origin to corner
            Raylib.DrawLine(
                (int)(cornerOrigin.X * Level.TileSize),
                (int)(cornerOrigin.Y * Level.TileSize),
                (int)(cornerPos.X * Level.TileSize),
                (int)(cornerPos.Y * Level.TileSize),
                new Color(0, 255, 0, 255)
            );
        }
    }
}