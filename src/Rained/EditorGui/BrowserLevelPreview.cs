using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace RainEd;

class BrowserLevelPreview : FileBrowserPreview
{
    record MeshData(Vector3[] Vertices, Color[] Colors);
    record ThreadData(string Path, CancellationToken CancelToken); 

    public override bool IsReady => isReady;

    private Task<MeshData?>? geoLoadTask;

    private bool isReady = false;
    private RlManaged.Mesh? geoMesh;
    private RlManaged.Material? geoMaterial;
    private RlManaged.RenderTexture2D? renderTexture;
    private CancellationTokenSource cancelTokenSrc;

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

        for (int x = 0; x < geoList.values.Count; x++)
        {
            var listX = (Lingo.List) geoList.values[x];
            for (int y = 0; y < listX.values.Count; y++)
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
                        1 => new Color(100, 100, 100, 255),
                        2 => new Color(200, 200, 200, 255),
                        _ => Color.Blank // should be impossible
                    };

                    BuildShape(x, y, geoValue, objects, color, vertices, colors);
                }

                cancelToken.ThrowIfCancellationRequested();
            }
        }

        return new MeshData([..vertices], [..colors]);
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
                vertices.Add(new Vector3(x+1, y+1, 0));

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
            vertices.Add(new Vector3(x, y+0.3f, 0));
            vertices.Add(new Vector3(x, y+0.7f, 0));
            vertices.Add(new Vector3(x+1, y+0.7f, 0));

            vertices.Add(new Vector3(x+1, y+0.7f, 0));
            vertices.Add(new Vector3(x+1, y+0.3f, 0));
            vertices.Add(new Vector3(x, y+0.3f, 0));

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
            vertices.Add(new Vector3(x+0.3f, y, 0));
            vertices.Add(new Vector3(x+0.3f, y+1, 0));
            vertices.Add(new Vector3(x+0.7f, y+1, 0));

            vertices.Add(new Vector3(x+0.7f, y+1, 0));
            vertices.Add(new Vector3(x+0.7f, y, 0));
            vertices.Add(new Vector3(x, y, 0));

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
        if (!isReady)
        {
            if (geoLoadTask is not null && geoLoadTask.IsCompleted)
            {
                if (geoLoadTask.IsCompletedSuccessfully && geoLoadTask.Result is MeshData meshData)
                {
                    var width = ImGui.GetContentRegionAvail().X;

                    renderTexture = RlManaged.RenderTexture2D.Load((int)width, (int)(width / (4.0f / 3.0f)));
                    
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
            if (geoMesh is null || geoMaterial is null || renderTexture is null)
            {
                var text = "Could not display preview";
                ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2f);
                ImGui.Text(text);
            }
            else
            {
                Raylib.BeginTextureMode(renderTexture);
                Raylib.ClearBackground(new Color(127, 127, 127, 255));
                Raylib.DrawMesh(geoMesh, geoMaterial, Matrix4x4.Identity);
                Raylib.EndTextureMode();
                rlImGui_cs.rlImGui.ImageRenderTexture(renderTexture);
            }
        }
    }

    public override void Dispose()
    {
        if (geoLoadTask is not null && !geoLoadTask.IsCompleted)
        {
            cancelTokenSrc.Cancel();
            geoLoadTask.Wait();
            geoLoadTask.Dispose();
        }

        geoMesh?.Dispose();
        geoMaterial?.Dispose();
        renderTexture?.Dispose();
    }
}