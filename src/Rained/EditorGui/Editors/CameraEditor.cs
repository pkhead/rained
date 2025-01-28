using Raylib_cs;
using ImGuiNET;
using System.Numerics;

using Rained.LevelData;
using Rained.Rendering;
namespace Rained.EditorGui.Editors;

class CameraEditor : IEditorMode
{
    public string Name { get => "Cameras"; }
    public bool SupportsCellSelection => false;
    
    private readonly LevelWindow window;

    private List<Camera> selectedCameras; // all selected cameras

    // index of item is the same as the index of the associated camera in the selectedCameras list 
    private readonly List<Vector2> cameraOffsetsToDragPosition;
    private Camera? activeCamera; // selected camera that was last clicked

    // the selected camera whose corner is currently being hovered over.
    Camera? cornerHoverCamera = null;

    private int selectedCorner = -1;
    private bool isDraggingCamera = false;
    private Vector2 dragTargetPos = new(); // unaffected by camera snapping
    
    private Vector2 lastMousePos = new();
    private Vector2 dragBegin = new();

    private ChangeHistory.CameraChangeRecorder changeRecorder;

    public CameraEditor(LevelWindow window)
    {
        this.window = window;
        selectedCameras = [];
        cameraOffsetsToDragPosition = [];
        activeCamera = null;
        changeRecorder = new ChangeHistory.CameraChangeRecorder();

        RainEd.Instance.ChangeHistory.Cleared += () =>
        {
            changeRecorder = new ChangeHistory.CameraChangeRecorder();
        };
    }

    public void Unload()
    {
        selectedCameras.Clear();
        activeCamera = null;
        isDraggingCamera = false;
        selectedCorner = -1;
        cornerHoverCamera = null;

        changeRecorder.TryPushChange();
    }

    public void ShowEditMenu()
    {
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.RemoveObject, "Delete Selected Camera");
        KeyShortcuts.ImGuiMenuItem(KeyShortcut.Duplicate, "Duplicate Camera");
        if (ImGui.MenuItem("Reset Camera Corners") && selectedCameras.Count > 0)
        {
            changeRecorder.BeginChange();

            foreach (var camera in selectedCameras)
            {
                for (int i = 0; i < 4; i++)
                {
                    camera.CornerAngles[i] = 0f;
                    camera.CornerOffsets[i] = 0f;
                }
            }

            changeRecorder.PushChange();
        }

        ImGui.Separator();

        ImGui.BeginDisabled(selectedCameras.Count != 1);
        if (ImGui.MenuItem("Prioritize Camera"))
        {
            RainEd.Instance.Level.PrioritizedCamera = selectedCameras[0];
        }
        ImGui.EndDisabled();

        if (ImGui.MenuItem("Clear Priority"))
        {
            RainEd.Instance.Level.PrioritizedCamera = null;
        }
    }
    
    public void DrawToolbar() {}

    private Camera? PickCameraAt(Vector2 mpos)
    {
        Camera? selectedCamera = null;

        float minDist = float.PositiveInfinity;

        foreach (Camera camera in RainEd.Instance.Level.Cameras)
        {
            // determine if mouse is within camera bounds
            var cameraA = camera.Position;
            var cameraB = camera.Position + Camera.Size;

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

        return selectedCamera;
    }

    public void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames)
    {
        var level = RainEd.Instance.Level;
        var levelRender = window.Renderer;

        // draw level background (solid white)
        levelRender.RenderLevel(new Rendering.LevelRenderConfig()
        {
            Fade = 30f / 255f
        });
        levelRender.RenderBorder();

        bool doubleClick = false;
        bool horizSnap = KeyShortcuts.Active(KeyShortcut.NavUp) || KeyShortcuts.Active(KeyShortcut.NavDown) || KeyShortcuts.Active(KeyShortcut.CameraSnapX);
        bool vertSnap = KeyShortcuts.Active(KeyShortcut.NavRight) || KeyShortcuts.Active(KeyShortcut.NavLeft) || KeyShortcuts.Active(KeyShortcut.CameraSnapY);

        if (horizSnap && vertSnap)
        {
            window.WriteStatus("X and Y Snap");
        }
        else if (horizSnap)
        {
            window.WriteStatus("X snap");
        }
        else if (vertSnap)
        {
            window.WriteStatus("Y snap");
        }

        if (window.IsViewportHovered)
        {
            doubleClick = EditorWindow.IsMouseDoubleClicked(ImGuiMouseButton.Left);

            var mpos = window.MouseCellFloat;

            if (EditorWindow.IsMouseClicked(ImGuiMouseButton.Left))
            {
                dragBegin = mpos;
            }

            if (!isDraggingCamera)
            {
                // determine if mouse is close enough to one of its corners
                selectedCorner = -1;
                cornerHoverCamera = null;

                float minCornerDistSq = 0.5f / window.ViewZoom;
                minCornerDistSq *= minCornerDistSq;
                foreach (var cam in selectedCameras)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var cpos = cam.GetCornerPosition(c, true);
                        float distSq = (cpos - mpos).LengthSquared();
                        if (distSq < minCornerDistSq)
                        {
                            minCornerDistSq = distSq;
                            selectedCorner = c;
                            cornerHoverCamera = cam;
                            break;
                        }
                    }
                }

                // drag begin
                if (EditorWindow.IsMouseDragging(ImGuiMouseButton.Left) || (selectedCorner >= 0 && EditorWindow.IsMouseDown(ImGuiMouseButton.Left)))
                {
                    var pickedCam = PickCameraAt(dragBegin);

                    if (selectedCorner == -1 && pickedCam is not null)
                    {
                        activeCamera = pickedCam;

                        if (!selectedCameras.Contains(pickedCam))
                        {
                            selectedCameras = [pickedCam];
                        }
                        
                        changeRecorder.BeginChange();
                        isDraggingCamera = true;

                        dragTargetPos = activeCamera.Position;
                        cameraOffsetsToDragPosition.Clear();
                        for (int i = 0; i < selectedCameras.Count; i++)
                        {
                            cameraOffsetsToDragPosition.Add(selectedCameras[i].Position - dragTargetPos);
                        }
                    }
                    else if (selectedCorner >= 0)
                    {
                        changeRecorder.BeginChange();
                        isDraggingCamera = true;
                    }
                }

                // right-click to reset corner
                if (selectedCorner >= 0 && EditorWindow.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    changeRecorder.BeginChange();
                    cornerHoverCamera!.CornerAngles[selectedCorner] = 0f;
                    cornerHoverCamera!.CornerOffsets[selectedCorner] = 0f;
                    changeRecorder.PushChange();
                }

                // mouse-pick select when lmb pressed
                if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    var cam = PickCameraAt(dragBegin);

                    if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                    {
                        if (cam is not null)
                        {
                            activeCamera = cam;

                            // add camera when not already selected,
                            // remove camera otherwise
                            int camIndex = selectedCameras.IndexOf(cam);
                            if (camIndex >= 0)
                                selectedCameras.RemoveAt(camIndex);
                            else
                                selectedCameras.Add(cam);
                        }
                    }
                    else
                    {
                        activeCamera = cam;
                        selectedCameras.Clear();
                        if (cam is not null)
                            selectedCameras.Add(cam);
                    }
                }
            }
        }

        // camera drag mode
        if (isDraggingCamera)
        {
            if (EditorWindow.IsMouseReleased(ImGuiMouseButton.Left))
            {
                // stop dragging camera
                isDraggingCamera = false;
                changeRecorder.PushChange();
            }

            // corner drag
            if (selectedCorner >= 0)
            {
                var limitAmp = RainEd.Instance.Preferences.RemoveCameraAngleLimit
                    ? EditorWindow.IsKeyDown(ImGuiKey.ModShift)
                    : !EditorWindow.IsKeyDown(ImGuiKey.ModShift);
                
                var vecDiff = window.MouseCellFloat - cornerHoverCamera!.GetCornerPosition(selectedCorner, false);

                var angle = MathF.Atan2(vecDiff.X, -vecDiff.Y);
                var offset = Math.Max(vecDiff.Length(), 0f);
                if (limitAmp && offset >= 4f)
                    offset = 4f;
                cornerHoverCamera.CornerAngles[selectedCorner] = angle;
                cornerHoverCamera.CornerOffsets[selectedCorner] = offset / 4f;
            }

            // camera drag
            else
            {
                dragTargetPos += window.MouseCellFloat - lastMousePos;

                // camera snap for the active camera
                var thisCamCenter = dragTargetPos + Camera.Size / 2f;
                var snapThreshold = 1.5f / window.ViewZoom;

                float minDistX = float.PositiveInfinity;
                float minDistY = float.PositiveInfinity;

                activeCamera!.Position = dragTargetPos;
                foreach (var camera in level.Cameras)
                {
                    if (selectedCameras.Contains(camera)) continue;

                    var camCenter = camera.Position + Camera.Size / 2f;
                    var distX = MathF.Abs(camCenter.X - thisCamCenter.X);
                    var distY = MathF.Abs(camCenter.Y - thisCamCenter.Y);

                    if (horizSnap && distX < snapThreshold && distX < minDistX)
                    {
                        minDistX = distX;
                        activeCamera.Position.X = camera.Position.X;
                    }

                    if (vertSnap && distY < snapThreshold && distY < minDistY)
                    {
                        minDistY = distY;
                        activeCamera.Position.Y = camera.Position.Y;
                    }
                }

                // maintain that all selected cameras have the same relative offset
                // to the active camera, because the active camera is the one that
                // gets snapped
                for (int i = 0; i < selectedCameras.Count; i++)
                {
                    selectedCameras[i].Position = activeCamera.Position + cameraOffsetsToDragPosition[i];
                }
            }
        }

        // keybinds
        if (!isDraggingCamera)
        {
            // N or double-click to create new camera
            bool wantCreate = KeyShortcuts.Activated(KeyShortcut.NewObject) || doubleClick;
            if (wantCreate)
            {
                changeRecorder.BeginChange();
                var cam = new Camera(window.MouseCellFloat - Camera.Size / 2f);
                level.Cameras.Add(cam);
                selectedCameras = [cam];
                activeCamera = cam;
                selectedCorner =- 1;
                changeRecorder.PushChange();
            }

            if (selectedCameras.Count > 0)
            {
                // Ctrl+D to duplicate selected cameras (duplicating camera corners) 
                if (EditorWindow.IsKeyDown(ImGuiKey.ModCtrl) && EditorWindow.IsKeyPressed(ImGuiKey.D))
                {
                    changeRecorder.BeginChange();
                    List<Camera> newList = [];
                    foreach (var srcCam in selectedCameras)
                    {
                        var newCam = new Camera(window.MouseCellFloat - Camera.Size / 2f);
                        level.Cameras.Add(newCam);

                        for (int i = 0; i < 4; i++)
                        {
                            newCam.CornerAngles[i] = srcCam.CornerAngles[i];
                            newCam.CornerOffsets[i] = srcCam.CornerOffsets[i];
                        }

                        newList.Add(newCam);

                        if (srcCam == activeCamera)
                            activeCamera = newCam;
                    }
                    selectedCorner = -1;
                    selectedCameras = newList;
                    changeRecorder.PushChange();
                }
                
                // Delete, or Backspace to delete the selected camera
                if (KeyShortcuts.Activated(KeyShortcut.RemoveObject))
                {
                    if (level.Cameras.Count > 1)
                    {
                        changeRecorder.BeginChange();
                        foreach (var cam in selectedCameras)
                        {
                            level.Cameras.Remove(cam);
                            if (level.PrioritizedCamera == cam)
                                level.PrioritizedCamera = null;
                        }
                        changeRecorder.PushChange();
                        selectedCameras.Clear();
                        activeCamera = null;
                    }
                    else
                    {
                        EditorWindow.ShowNotification("Cannot delete only camera");
                    }
                }
            }
        }

        // render cameras
        foreach (Camera camera in level.Cameras)
        {
            RenderCamera(camera, selectedCameras.Contains(camera), camera == cornerHoverCamera ? selectedCorner : -1);
        }

        lastMousePos = window.MouseCellFloat;
    }

    private void RenderCamera(Camera camera, bool isHovered, int hoveredCorner)
    {
        var camCenter = camera.Position + Camera.Size / 2f;

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
                Camera.Size * Level.TileSize
            ),
            2f / window.ViewZoom,
            new Color(0, 0, 0, 255)       
        );

        // 16:9 outline
        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                (camCenter - Camera.WidescreenSize / 2) * Level.TileSize,
                Camera.WidescreenSize * Level.TileSize
            ),
            2f / window.ViewZoom,
            new Color(9, 0, 0, 255)
        );

        // 4:3 outline
        Color cameraColor = RainEd.Instance.Level.PrioritizedCamera == camera ? new Color(255, 0, 0, 255) : new Color(0, 255, 0, 255);
        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                (camCenter - Camera.StandardSize / 2) * Level.TileSize,
                Camera.StandardSize * Level.TileSize
            ),
            2f / window.ViewZoom,
            cameraColor
        );

        // draw center circle
        Raylib.DrawCircleLinesV(camCenter * Level.TileSize, 50f, Color.Black);

        Raylib.DrawLineV(
            new Vector2(
                camCenter.X * Level.TileSize,
                camera.Position.Y * Level.TileSize
            ),
            new Vector2(
                camCenter.X * Level.TileSize,
                (camera.Position.Y + Camera.StandardSize.Y) * Level.TileSize
            ),
            Color.Black
        );

        Raylib.DrawLineV(
            new Vector2(
                (camCenter.X - 5f) * Level.TileSize,
                camCenter.Y * Level.TileSize
            ),
            new Vector2(
                (camCenter.X + 5f) * Level.TileSize,
                camCenter.Y * Level.TileSize
            ),
            Color.Black
        );

        // draw camera number
        if (RainEd.Instance.Preferences.ShowCameraNumbers)
        {
            RainEd.RenderContext.DrawColor = Glib.Color.White;
            var text = (RainEd.Instance.Level.Cameras.IndexOf(camera) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            
            var txtSize = TextRendering.CalcOutlinedTextSize(text);
            var scale = 2f / window.ViewZoom * Boot.PixelIconScale;
            TextRendering.DrawTextOutlined(
                text: text,
                offset: camCenter * Level.TileSize - txtSize / 2f * scale,
                scale: new Vector2(scale, scale)
            );
        }

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
                Raylib.DrawCircleLinesV(
                    cornerOrigin * Level.TileSize,
                    4f * Level.TileSize,
                    color
                );

                // inner circle
                Raylib.DrawCircleLinesV(
                    cornerOrigin * Level.TileSize,
                    camera.CornerOffsets[corner] * 4f * Level.TileSize,
                    color
                );

                // point at corner
                Raylib.DrawCircleV(
                    cornerPos * Level.TileSize,
                    5f / window.ViewZoom * Boot.WindowScale,
                    color
                );

                // point at corner origin
                Raylib.DrawCircleV(
                    cornerOrigin * Level.TileSize,
                    2f / window.ViewZoom * Boot.WindowScale,
                    color
                );

                // line from corner origin to corner
                Raylib.DrawLineV(
                    cornerOrigin * Level.TileSize,
                    cornerPos * Level.TileSize,
                    color
                );
            }
        }
    }
}