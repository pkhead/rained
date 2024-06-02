using Raylib_cs;
using System.Numerics;
namespace RainEd.Rendering;

class VoxelRenderer
{
    private readonly Level level;
    private RlManaged.RenderTexture2D renderTexture;
    public RlManaged.RenderTexture2D RenderTexture { get => renderTexture; }

    private RlManaged.Mesh? levelMesh;
    private List<TileInstance> tileInstances;
    private RlManaged.Material? material;
    private RlManaged.Shader shader3D;
    private readonly RlManaged.Texture2D testTexture;

    private readonly Dictionary<string, RlManaged.Mesh> tileMeshes = [];

    struct TileInstance
    {
        public Model model;
        public Vector3 pos;
    }

    private const string Shader3DVSrc = @"
        #version 330

        attribute vec3 vertexPosition;
        attribute vec2 vertexTexCoord;
        attribute vec4 vertexColor;
        attribute vec3 vertexNormal;

        varying vec2 fragTexCoord;
        varying vec4 fragColor;
        varying vec3 fragNormal;

        uniform mat4 mvp;
        uniform mat4 matNormal;

        void main()
        {
            fragTexCoord = vertexTexCoord;
            fragColor = vertexColor;
            fragNormal = normalize(vec3(matNormal * vec4(vertexNormal, 1.0)));
            gl_Position = mvp*vec4(vertexPosition, 1.0);
        }
    ";

    private const string Shader3DFSrc = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;
        in vec3 fragNormal;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        uniform vec3 ambientLightColor;

        out vec4 finalColor;

        void main()
        {
            vec3 lightDir = normalize(vec3(1.0, 1.0, 0.0));
            float diff = max(dot(fragNormal, lightDir), 0.0);

            vec4 baseColor = texture(texture0, fragTexCoord) * fragColor;
            //finalColor = vec4((vec3(1.0, 1.0, 1.0) * diff + ambientLightColor) * baseColor.rgb, baseColor.a);
            finalColor = vec4(fragNormal, baseColor.a);
        }    
    ";

    public VoxelRenderer(Level level)
    {
        this.level = level;
        tileInstances = [];
        renderTexture = RlManaged.RenderTexture2D.Load(1400, 800);
        //testTexture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "internal", "Internal_331_bricksTexture.png"));
        testTexture = RlManaged.Texture2D.LoadFromImage(RlManaged.Image.GenColor(1, 1, new Color(255, 0, 0, 255)));
        shader3D = RlManaged.Shader.LoadFromMemory(Shader3DVSrc, Shader3DFSrc);
    }

    public bool IsTransparent(int x, int y, int layer)
    {
        if (!level.IsInBounds(x, y)) return true;
        if (layer < 0 || layer > 2) return true;
        
        var cell = level.Layers[layer, x, y];
        return cell.Geo == GeoType.Air || cell.HasTile();
    }

    RlManaged.Mesh GetTileMesh(Tiles.Tile init)
    {
        if (tileMeshes.TryGetValue(init.Name, out var mesh))
            return mesh;

        var voxelizer = new TileVoxelizer(init);
        mesh = voxelizer.Voxelize();
        mesh.UploadMesh(false);
        tileMeshes[init.Name] = mesh;
        return mesh;
    }

    public void UpdateLevel()
    {
        var voxelizer = new GeometryVoxelizer(level);
        var mesh = voxelizer.Voxelize();

        levelMesh?.Dispose();
        levelMesh = mesh;
        levelMesh.UploadMesh(false);

        material?.Dispose();
        material = RlManaged.Material.LoadMaterialDefault();

        unsafe
        {
            var matRaw = (Material) material;
            matRaw.Maps[(int) MaterialMapIndex.Diffuse].Color = new Color(255, 255, 255, 255);
            matRaw.Maps[(int) MaterialMapIndex.Diffuse].Texture = testTexture;
            matRaw.Shader = shader3D;
        }

        // create tile models
        tileInstances.Clear();

        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                for (int l = 0; l < 3; l++)
                {
                    var cell = level.Layers[l,x,y];
                    var init = cell.TileHead;
                    if (init is not null)
                    {
                        var tileMesh = GetTileMesh(init);
                        var model = Raylib.LoadModelFromMesh(tileMesh);
                        
                        unsafe
                        {
                            model.Materials[0] = material;
                        }

                        tileInstances.Add(new TileInstance()
                        {
                            model = model,
                            pos = new Vector3(x - init.CenterX - init.BfTiles, y - init.CenterY - init.BfTiles, l) * 10f
                        });
                    }
                }
            }
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
        {
            Raylib.BeginShaderMode(shader3D);

            Raylib.SetShaderValue(shader3D, Raylib.GetShaderLocation(shader3D, "ambientLightColor"), [0.2f, 0.2f, 0.2f], ShaderUniformDataType.Vec3);

            // draw level geometry
            Raylib.DrawMesh(levelMesh!, material!, Matrix4x4.Identity);

            // draw tiles
            foreach (var inst in tileInstances)
            {
                Raylib.DrawModel(inst.model, inst.pos, 1f, Color.White);
            }

            Raylib.EndShaderMode();
        }
        Raylib.EndMode3D();

        Raylib.EndTextureMode();
    }
}