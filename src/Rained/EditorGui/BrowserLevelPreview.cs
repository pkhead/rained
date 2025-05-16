using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using Rained.LevelData;
namespace Rained.EditorGui;

class BrowserLevelPreview : FileBrowserPreview
{
    record MeshData(int Width, int Height, Vector3[] Vertices, Glib.Color[] Colors);
    record ThreadData(string Path, CancellationToken CancelToken); 

    public override bool IsReady => isReady;

    private Task<MeshData?>? geoLoadTask;

    private bool isReady = false;
    private int levelWidth = -1;
    private int levelHeight = -1;
    private Glib.StandardMesh? geoMesh;
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
        if (parser.Read(geoData) is not Lingo.LinearList geoList) return null;

        List<Vector3> vertices = [];
        List<Glib.Color> colors = []; 

        int levelWidth = 0;
        int levelHeight = 0;

        levelWidth = geoList.Count;
        for (int x = 0; x < levelWidth; x++)
        {
            var listX = (Lingo.LinearList) geoList[x];
            levelHeight = listX.Count;

            for (int y = 0; y < levelHeight; y++)
            {
                var listY = (Lingo.LinearList) listX[y];

                for (int l = 2; l >= 0; l--)
                {
                    var cellData = (Lingo.LinearList) listY[l];
                    var geoValue = (GeoType) Lingo.LingoNumber.AsInt(cellData[0]);

                    LevelObject objects = 0;
                    foreach (var v in ((Lingo.LinearList) cellData[1]).Cast<int>())
                    {
                        objects |= (LevelObject) (1 << (v-1));
                    }

                    var color = l switch
                    {
                        0 => Glib.Color.Black,
                        1 => Glib.Color.FromRGBA(50, 50, 50, 255),
                        2 => Glib.Color.FromRGBA(100, 100, 100, 255),
                        _ => Glib.Color.Transparent // should be impossible
                    };

                    BuildShape(x, y, geoValue, objects, color, vertices, colors);
                }

                cancelToken.ThrowIfCancellationRequested();
            }
        }

        RainEd.Instance.NeedScreenRefresh();
        return new MeshData(levelWidth, levelHeight, [..vertices], [..colors]);
    }

    private static void BuildShape(int x, int y, GeoType v, LevelObject objects, Glib.Color color, List<Vector3> vertices, List<Glib.Color> colors)
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

                    geoMesh = Glib.StandardMesh.Create(meshData.Vertices.Length);
                    geoMesh.SetVertexData(meshData.Vertices);
                    geoMesh.SetColorData(meshData.Colors);

                    if (geoMesh.GetBufferVertexCount(0) > 0)
                        geoMesh.Upload();
                }
                else if (geoLoadTask.IsFaulted)
                {
                    if (RainEd.Instance is not null) Log.Error("Could not build level preview:{ErrorMessage}", geoLoadTask.Exception);
                }

                geoLoadTask.Dispose();
                geoLoadTask = null;
                isReady = true;
            }
        }

        if (isReady)
        {
            if (geoMesh is null)
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

                    Rlgl.PushMatrix();
                    Rlgl.Scalef(viewScale, viewScale, 1f);
                    Rlgl.Translatef(-viewPan.X, -viewPan.Y, 0f);

                    Raylib.DrawRectangle(0, 0, levelWidth, levelHeight, new Color(127, 127, 127, 255));

                    if (geoMesh.GetBufferVertexCount(0) > 0)
                    {
                        RainEd.RenderContext.DrawColor = Glib.Color.White;
                        RainEd.RenderContext!.Draw(geoMesh);
                    }

                    Rlgl.PopMatrix();
                }
                Raylib.EndTextureMode();

                var cursorPos = ImGui.GetCursorPos();
                var cursorScreenPos = ImGui.GetCursorScreenPos();
                ImGuiExt.ImageRenderTexture(renderTexture);
                ImGui.SetCursorPos(cursorPos);

                ImGui.InvisibleButton("##interactArea", new Vector2(renderTexture.Texture.Width, renderTexture.Texture.Height));

                if (ImGui.IsItemActive())
                {
                    viewPan -= Raylib.GetMouseDelta() / viewScale;
                }

                var wheelMove = Raylib.GetMouseWheelMove();
                if (ImGui.IsItemHovered() && wheelMove != 0f)
                {
                    var mpos = (ImGui.GetMousePos() - cursorScreenPos) / viewScale + viewPan;
                    var factor = MathF.Pow(zoomSpeed, -Math.Sign(wheelMove));

                    viewScale *= factor;
                    viewPan = -(mpos - viewPan) / factor + mpos;
                }
            }
        }
    }

    public override void Dispose()
    {
        Log.Information("Unload preview");

        if (geoLoadTask is not null && !geoLoadTask.IsCompleted)
        {
            cancelTokenSrc.Cancel();
        }

        geoMesh?.Dispose();

        if (renderTexture is not null)
            RainEd.Instance.DeferToNextFrame(renderTexture.Dispose);
    }
}