using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace RainEd;

class BrowserLevelPreview : FileBrowserPreview
{
    record MeshData(int Width, int Height, Vector3[] Vertices, Color[] Colors);
    record ThreadData(string Path, CancellationToken CancelToken); 

    public override bool IsReady => isReady;

    private Task<MeshData?>? geoLoadTask;

    private bool isReady = false;
    private int levelWidth = -1;
    private int levelHeight = -1;
    private RlManaged.Mesh? geoMesh;
    private RlManaged.Material? geoMaterial;
    private RlManaged.RenderTexture2D? renderTexture;
    private CancellationTokenSource cancelTokenSrc;

    Vector2 lastViewSize = Vector2.Zero;
    float viewScale = 1f;
    Vector2 viewPan = Vector2.Zero;

    public BrowserLevelPreview(string path) : base(path)
    {
        cancelTokenSrc = new CancellationTokenSource();
        geoLoadTask = Task<MeshData?>.Factory.StartNew(BuildPreview, new ThreadData(path, cancelTokenSrc.Token));
    }

    private MeshData? BuildPreview(object? arg0)
    {
        var threadData = (ThreadData) arg0!;

        var path = threadData.Path;
        var cancelToken = threadData.CancelToken;

        string? geoData;

        {
            using var file = File.OpenText(path);
            geoData = file.ReadLine();
        }

        if (string.IsNullOrEmpty(geoData)) return null;

        var parser = new Lingo.LingoParser();
        if (parser.Read(geoData) is not Lingo.List geoList) return null;

        List<Vector3> vertices = [];
        List<Color> colors = []; 

        int levelWidth = 0;
        int levelHeight = 0;

        levelWidth = geoList.values.Count;
        for (int x = 0; x < levelWidth; x++)
        {
            var listX = (Lingo.List) geoList.values[x];
            levelHeight = listX.values.Count;

            for (int y = 0; y < levelHeight; y++)
            {
                var listY = (Lingo.List) listX.values[y];

                for (int l = 2; l >= 0; l--)
                {
                    var cellData = (Lingo.List) listY.values[l];
                    var geoValue = (GeoType) Lingo.LingoNumber.AsInt(cellData.values[0]);

                    LevelObject objects = 0;
                    foreach (var v in ((Lingo.List) cellData.values[1]).values.Cast<int>())
                    {
                        objects |= (LevelObject) (1 << (v-1));
                    }

                    var color = l switch
                    {
                        0 => Color.Black,
                        1 => new Color(50, 50, 50, 255),
                        2 => new Color(100, 100, 100, 255),
                        _ => Color.Blank // should be impossible
                    };

                    BuildShape(x, y, geoValue, objects, color, vertices, colors);
                }

                cancelToken.ThrowIfCancellationRequested();
            }
        }

        return new MeshData(levelWidth, levelHeight, [..vertices], [..colors]);
    }

    private static void BuildShape(int x, int y, GeoType v, LevelObject objects, Color color, List<Vector3> vertices, List<Color> colors)
    {
        switch (v)
        {
            case GeoType.Air:
                break;
            
            case GeoType.Solid:
                vertices.Add(new Vector3(x, y, 0));
                vertices.Add(new Vector3(x, y+1, 0));
                vertices.Add(new Vector3(x+1, y+1, 0));

                vertices.Add(new Vector3(x+1, y+1, 0));
                vertices.Add(new Vector3(x+1, y, 0));
                vertices.Add(new Vector3(x, y, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;
            
            case GeoType.Platform:
                vertices.Add(new Vector3(x, y, 0));
                vertices.Add(new Vector3(x, y+0.4f, 0));
                vertices.Add(new Vector3(x+1, y+0.4f, 0));

                vertices.Add(new Vector3(x+1, y+0.4f, 0));
                vertices.Add(new Vector3(x+1, y, 0));
                vertices.Add(new Vector3(x, y, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;
            
            case GeoType.SlopeLeftDown:
                vertices.Add(new Vector3(x, y, 0));
                vertices.Add(new Vector3(x+1f, y+1f, 0));
                vertices.Add(new Vector3(x+1f, y, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;
            
            case GeoType.SlopeLeftUp:
                vertices.Add(new Vector3(x+1, y, 0));
                vertices.Add(new Vector3(x, y+1, 0));
                vertices.Add(new Vector3(x+1, y+1, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;

            case GeoType.SlopeRightDown:
                vertices.Add(new Vector3(x+1, y, 0));
                vertices.Add(new Vector3(x, y, 0));
                vertices.Add(new Vector3(x, y+1, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;

            case GeoType.SlopeRightUp:
                vertices.Add(new Vector3(x, y, 0));
                vertices.Add(new Vector3(x, y+1, 0));
                vertices.Add(new Vector3(x+1, y+1, 0));

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                break;
        }

        // create horizontal poles
        if (objects.HasFlag(LevelObject.HorizontalBeam))
        {
            vertices.Add(new Vector3(x, y+0.4f, 0));
            vertices.Add(new Vector3(x, y+0.6f, 0));
            vertices.Add(new Vector3(x+1, y+0.6f, 0));

            vertices.Add(new Vector3(x+1, y+0.6f, 0));
            vertices.Add(new Vector3(x+1, y+0.4f, 0));
            vertices.Add(new Vector3(x, y+0.4f, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }

        // create vertical poles
        if (objects.HasFlag(LevelObject.VerticalBeam))
        {
            vertices.Add(new Vector3(x+0.4f, y, 0));
            vertices.Add(new Vector3(x+0.4f, y+1, 0));
            vertices.Add(new Vector3(x+0.6f, y+1, 0));

            vertices.Add(new Vector3(x+0.6f, y+1, 0));
            vertices.Add(new Vector3(x+0.6f, y, 0));
            vertices.Add(new Vector3(x+0.4f, y, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }
    }

    public override void Render()
    {
        var viewSize = ImGui.GetContentRegionAvail();
        float zoomSpeed = 0.8f;    

        if (!isReady)
        {
            if (geoLoadTask is not null && geoLoadTask.IsCompleted)
            {
                if (geoLoadTask.IsCompletedSuccessfully && geoLoadTask.Result is MeshData meshData)
                {
                    viewScale = viewSize.X / meshData.Width;

                    viewPan.X = (viewSize.X - meshData.Width * viewScale) / 2f;
                    viewPan.Y = 0f;
                    //viewPan = (viewSize - new Vector2(meshData.Width * viewScale, meshData.Height)) / 2f;
                    
                    levelWidth = meshData.Width;
                    levelHeight = meshData.Height;

                    geoMesh = new RlManaged.Mesh();
                    geoMesh.SetVertices(meshData.Vertices);
                    geoMesh.SetColors(meshData.Colors);
                    geoMesh.UploadMesh(false);

                    geoMaterial = RlManaged.Material.LoadMaterialDefault();
                }
                else if (geoLoadTask.IsFaulted)
                {
                    if (RainEd.Instance is not null) RainEd.Logger.Error("Could not build level preview:{ErrorMessage}", geoLoadTask.Exception);
                }

                geoLoadTask.Dispose();
                geoLoadTask = null;
                isReady = true;
            }
        }

        if (isReady)
        {
            if (geoMesh is null || geoMaterial is null)
            {
                var text = "Could not display preview";
                ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2f);
                ImGui.Text(text);
            }
            else
            {
                if (renderTexture is null || viewSize != lastViewSize)
                {
                    lastViewSize = viewSize;

                    renderTexture?.Dispose();
                    renderTexture = RlManaged.RenderTexture2D.Load((int)viewSize.X, (int)viewSize.Y);
                }

                Raylib.BeginTextureMode(renderTexture);
                {
                    Raylib.ClearBackground(Color.Blank);

                    Rlgl.DrawRenderBatchActive();

                    Rlgl.PushMatrix();
                    Rlgl.Scalef(viewScale, viewScale, 1f);
                    Rlgl.Translatef(-viewPan.X, -viewPan.Y, 0f);

                    Raylib.DrawRectangle(0, 0, levelWidth, levelHeight, new Color(127, 127, 127, 255));
                    Rlgl.DrawRenderBatchActive();
                    Raylib.DrawMesh(geoMesh, geoMaterial, Matrix4x4.Identity);

                    Rlgl.PopMatrix();
                }
                Raylib.EndTextureMode();

                var cursorPos = ImGui.GetCursorPos();
                rlImGui_cs.rlImGui.ImageRenderTexture(renderTexture);
                ImGui.SetCursorPos(cursorPos);

                ImGui.InvisibleButton("##interactArea", new Vector2(renderTexture.Texture.Width, renderTexture.Texture.Height));

                if (ImGui.IsItemActive())
                {
                    viewPan -= Raylib.GetMouseDelta() / viewScale;
                }

                var mouseMove = Raylib.GetMouseWheelMove();
                if (ImGui.IsItemHovered() && mouseMove != 0f)
                {
                    if (mouseMove > 0)
                    {
                        viewScale /= Math.Sign(mouseMove) * zoomSpeed;
                    }
                    else
                    {
                        viewScale *= -Math.Sign(mouseMove) * zoomSpeed;
                    }
                }
            }
        }
    }

    public override void Dispose()
    {
        RainEd.Logger.Information("Unload preview");

        if (geoLoadTask is not null && !geoLoadTask.IsCompleted)
        {
            cancelTokenSrc.Cancel();
        }

        geoMesh?.Dispose();
        geoMaterial?.Dispose();
        renderTexture?.Dispose();
    }
}