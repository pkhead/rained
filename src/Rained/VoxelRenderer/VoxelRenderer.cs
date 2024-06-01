using Raylib_cs;
using System.Numerics;
namespace RainEd.Rendering;

class VoxelRenderer
{
    private readonly Level level;
    private RlManaged.RenderTexture2D renderTexture;
    public RlManaged.RenderTexture2D RenderTexture { get => renderTexture; }

    private RlManaged.Mesh levelMesh;
    private RlManaged.Material material;
    private RlManaged.Texture2D testTexture;

    public VoxelRenderer(Level level)
    {
        this.level = level;
        renderTexture = RlManaged.RenderTexture2D.Load(1400, 800);
        testTexture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "internal", "Internal_331_bricksTexture.png"));
    }

    public bool IsTransparent(int x, int y, int layer)
    {
        if (!level.IsInBounds(x, y)) return true;
        if (layer < 0 || layer > 2) return true;
        
        var cell = level.Layers[layer, x, y];
        return cell.Geo == GeoType.Air || cell.HasTile();
    }

    public void UpdateLevel()
    {
        List<Vector3> vertices = [];
        List<Vector2> uvs = [];

        void GenerateSolid(int x, int y, int z)
        {
            if (IsTransparent(x, y, z-1))
            {
                // front face
                vertices.Add(new Vector3(x, y, z) * 10f); 
                vertices.Add(new Vector3(x, y+1, z) * 10f); 
                vertices.Add(new Vector3(x+1, y+1, z) * 10f);

                vertices.Add(new Vector3(x+1, y+1, z) * 10f);
                vertices.Add(new Vector3(x+1, y, z) * 10f);
                vertices.Add(new Vector3(x, y, z) * 10f);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));

                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 0f));
            }

            if (IsTransparent(x+1, y, z))
            {
                // right face
                vertices.Add(new Vector3(x+1, y, z) * 10f); 
                vertices.Add(new Vector3(x+1, y+1, z) * 10f); 
                vertices.Add(new Vector3(x+1, y+1, z+1) * 10f);

                vertices.Add(new Vector3(x+1, y+1, z+1) * 10f);
                vertices.Add(new Vector3(x+1, y, z+1) * 10f);
                vertices.Add(new Vector3(x+1, y, z) * 10f);

                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));

                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }

            if (IsTransparent(x-1, y, z))
            {
                // left face
                vertices.Add(new Vector3(x, y, z) * 10f);
                vertices.Add(new Vector3(x, y, z+1) * 10f);
                vertices.Add(new Vector3(x, y+1, z+1) * 10f);

                vertices.Add(new Vector3(x, y+1, z+1) * 10f);
                vertices.Add(new Vector3(x, y+1, z) * 10f);
                vertices.Add(new Vector3(x, y, z) * 10f);

                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));

                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }

            if (IsTransparent(x, y-1, z))
            {
                // top face
                vertices.Add(new Vector3(x, y, z) * 10f);
                vertices.Add(new Vector3(x+1, y, z) * 10f);
                vertices.Add(new Vector3(x+1, y, z+1) * 10f);

                vertices.Add(new Vector3(x+1, y, z+1) * 10f);
                vertices.Add(new Vector3(x, y, z+1) * 10f);
                vertices.Add(new Vector3(x, y, z) * 10f);

                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));

                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }

            if (IsTransparent(x, y+1, z))
            {
                // bottom face
                vertices.Add(new Vector3(x, y+1, z) * 10f);
                vertices.Add(new Vector3(x, y+1, z+1) * 10f);
                vertices.Add(new Vector3(x+1, y+1, z+1) * 10f);

                vertices.Add(new Vector3(x+1, y+1, z+1) * 10f);
                vertices.Add(new Vector3(x+1, y+1, z) * 10f);
                vertices.Add(new Vector3(x, y+1, z) * 10f);

                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));

                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
            }
        }

        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    var cell = level.Layers[z,x,y];
                    if (cell.HasTile()) continue; 
                    if (cell.Geo == GeoType.Air) continue;

                    if (cell.Geo == GeoType.Solid)
                    {
                        GenerateSolid(x, y, z);
                    }
                }
            }
        }

        levelMesh?.Dispose();
        levelMesh = new RlManaged.Mesh();
        levelMesh.SetVertices([..vertices]);
        levelMesh.SetTexCoords([..uvs]);
        levelMesh.UploadMesh(false);

        material?.Dispose();
        material = RlManaged.Material.LoadMaterialDefault();

        unsafe
        {
            var matRaw = (Material) material;
            matRaw.Maps[(int) MaterialMapIndex.Diffuse].Color = new Color(255, 255, 255, 255);
            matRaw.Maps[(int) MaterialMapIndex.Diffuse].Texture = testTexture;
        }
    }

    public void Render(float camX, float camY, float camZoom)
    {
        Raylib.BeginTextureMode(renderTexture);
        Raylib.ClearBackground(Color.White);

        Rlgl.DrawRenderBatchActive();

        var camPos = new Vector3(camX / Level.TileSize * 10f, camY / Level.TileSize * 10f, -200f * camZoom);
        var cam = new Camera3D(camPos, camPos + Vector3.UnitZ, -Vector3.UnitY, 70f, CameraProjection.Perspective);

        Raylib.BeginMode3D(cam);
            Raylib.DrawMesh(levelMesh, material, Matrix4x4.Identity);
        Raylib.EndMode3D();

        Raylib.EndTextureMode();
    }
}