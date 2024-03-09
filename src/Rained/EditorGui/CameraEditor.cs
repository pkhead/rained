using Raylib_cs;
using ImGuiNET;
using System.Numerics;

namespace RainEd;

class CameraEditor : IEditorMode
{
    public string Name { get => "Cameras"; }
    private readonly EditorWindow window;

    private Camera? selectedCamera = null;
    private int selectedCorner = -1;
    private bool isDraggingCamera = false;
    
    private Vector2 lastMousePos = new();
    private Vector2 dragBegin = new();

    private ChangeHistory.CameraChangeRecorder changeRecorder;

    public CameraEditor(EditorWindow window) {
        this.window = window;
        changeRecorder = new ChangeHistory.CameraChangeRecorder();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder = new ChangeHistory.CameraChangeRecorder();
        };
    }

    public void Unload()
    {
        selectedCamera = null;
        isDraggingCamera = false;
        selectedCorner = -1;

        changeRecorder.TryPushChange();
    }
    
    public void DrawToolbar() {
    }

    private void PickCameraAt(Vector2 mpos)
    {
        selectedCamera = null;
        selectedCorner = -1;

        float minDist = float.PositiveInfinity;

        foreach (Camera camera in RainEd.Instance.Level.Cameras)
        {
            // determine if mouse is within camera bounds
            var cameraA = camera.Position;
            var cameraB = camera.Position + Camera.WidescreenSize;

            // if so, select this camera
            if (mpos.X > cameraA.X && mpos.Y > cameraA.Y &&
                mpos.X < cameraB.X && mpos.Y < cameraB.Y
            )
            {
                // if there are multiple cameras at this point, pick the one
                // with the center that is closest to the user's cursor
                var center = (cameraA + cameraB) / 2f;
                var dist = Vector2.DistanceSquared(center, mpos);

                if (dist < minDist)
                {
                    selectedCamera = camera;
                    minDist = dist;
                }
            }
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

        // levelRender.RenderGrid(0.5f / window.ViewZoom);
        levelRender.RenderBorder();

        if (window.IsViewportHovered)
        {
            
            var mpos = window.MouseCellFloat;

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                dragBegin = mpos;
            
            }

            if (isDraggingCamera)
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    // stop dragging camera
                    isDraggingCamera = false;
                    changeRecorder.PushChange();
                }

                // corner drag
                if (selectedCorner >= 0)
                {
                    var vecDiff = window.MouseCellFloat - selectedCamera!.GetCornerPosition(selectedCorner, false);

                    var angle = MathF.Atan2(vecDiff.X, -vecDiff.Y);
                    var offset = Math.Clamp(vecDiff.Length(), 0f, 4f);
                    selectedCamera!.CornerAngles[selectedCorner] = angle;
                    selectedCamera!.CornerOffsets[selectedCorner] = offset / 4f;
                }

                // camera drag
                else
                {
                    selectedCamera!.Position += window.MouseCellFloat - lastMousePos;
                }
            }
            else
            {
                // determine if mouse is close enough to one of its corners
                selectedCorner = -1;

                if (selectedCamera is not null)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var cpos = selectedCamera.GetCornerPosition(c, true);
                        if ((cpos - mpos).Length() < 0.5f / window.ViewZoom)
                        {
                            selectedCorner = c;
                            break;
                        }
                    }
                }

                // drag begin
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) || (selectedCorner >= 0 && ImGui.IsMouseDown(ImGuiMouseButton.Left)))
                {
                    if (selectedCorner == -1)
                    {
                        PickCameraAt(dragBegin);
                    }

                    if (selectedCamera is not null)
                    {
                        changeRecorder.BeginChange();
                        isDraggingCamera = true;
                    }
                }

                // right-click to reset corner
                if (selectedCorner >= 0 && selectedCamera is not null && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    changeRecorder.BeginChange();
                    selectedCamera.CornerAngles[selectedCorner] = 0f;
                    selectedCamera.CornerOffsets[selectedCorner] = 0f;
                    changeRecorder.PushChange();
                }

                // mouse-pick select when lmb pressed
                if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                {
                    PickCameraAt(dragBegin);
                }
            }
        }

        // keybinds
        if (!isDraggingCamera)
        {
            // N or double-click to create new camera
            bool wantCreate = window.IsShortcutActivated(RainEd.ShortcutID.NewObject) || ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
            if (wantCreate && level.Cameras.Count < Level.MaxCameraCount)
            {
                changeRecorder.BeginChange();
                var cam = new Camera(window.MouseCellFloat - Camera.WidescreenSize / 2f);
                level.Cameras.Add(cam);
                selectedCamera = cam;
                selectedCorner =- 1;
                changeRecorder.PushChange();
            }


            if (selectedCamera is not null)
            {
                // Ctrl+D to duplicate selected camera (duplicating camera corners) 
                if (EditorWindow.IsKeyDown(ImGuiKey.ModCtrl) && EditorWindow.IsKeyPressed(ImGuiKey.D))
                {
                    changeRecorder.BeginChange();
                    {
                        var cam = new Camera(window.MouseCellFloat - Camera.WidescreenSize / 2f);
                        level.Cameras.Add(cam);

                        for (int i = 0; i < 4; i++)
                        {
                            cam.CornerAngles[i] = selectedCamera.CornerAngles[i];
                            cam.CornerOffsets[i] = selectedCamera.CornerOffsets[i];
                        }

                        selectedCamera = cam;
                        selectedCorner = -1;
                    }
                    changeRecorder.PushChange();
                }
                
                // Delete, or Backspace to delete the selected camera
                if (EditorWindow.IsKeyPressed(ImGuiKey.Delete)
                    || EditorWindow.IsKeyPressed(ImGuiKey.Backspace)
                )
                {
                    if (level.Cameras.Count > 1)
                    {
                        changeRecorder.BeginChange();
                        level.Cameras.Remove(selectedCamera);
                        selectedCamera = null;
                        changeRecorder.PushChange();
                    }
                    else
                    {
                        RainEd.Instance.ShowNotification("Cannot delete only camera");
                    }
                }
            }
        }

        // render cameras
        foreach (Camera camera in level.Cameras)
        {
            RenderCamera(camera, selectedCamera == camera, selectedCorner);
        }

        lastMousePos = window.MouseCellFloat;
    }

    private void RenderCamera(Camera camera, bool isHovered, int hoveredCorner)
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
        if (isHovered)
        {
            for (int corner = 0; corner < 4; corner++)
            {
                var color = new Color(0, 255, 0, 255);
                if (corner != hoveredCorner) // corner gizmo is transparent when not hovered over
                {
                    color.A = 127;
                }

                var cornerOrigin = camera.GetCornerPosition(corner, false);
                var cornerPos = camera.GetCornerPosition(corner, true);

                // outer circle
                Raylib.DrawCircleLines(
                    (int)(cornerOrigin.X * Level.TileSize),
                    (int)(cornerOrigin.Y * Level.TileSize),
                    4f * Level.TileSize,
                    color
                );

                // inner circle
                Raylib.DrawCircleLines(
                    (int)(cornerOrigin.X * Level.TileSize),
                    (int)(cornerOrigin.Y * Level.TileSize),
                    camera.CornerOffsets[corner] * 4f * Level.TileSize,
                    color
                );

                // point at corner
                Raylib.DrawCircle(
                    (int)(cornerPos.X * Level.TileSize),
                    (int)(cornerPos.Y * Level.TileSize),
                    5f / window.ViewZoom,
                    color
                );

                // point at corner origin
                Raylib.DrawCircle(
                    (int)(cornerOrigin.X * Level.TileSize),
                    (int)(cornerOrigin.Y * Level.TileSize),
                    2f / window.ViewZoom,
                    color
                );

                // line from corner origin to corner
                Raylib.DrawLine(
                    (int)(cornerOrigin.X * Level.TileSize),
                    (int)(cornerOrigin.Y * Level.TileSize),
                    (int)(cornerPos.X * Level.TileSize),
                    (int)(cornerPos.Y * Level.TileSize),
                    color
                );
            }
        }
    }
}