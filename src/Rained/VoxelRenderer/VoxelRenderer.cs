using System.Numerics;
namespace RainEd.Rendering;

class VoxelRenderer
{
    private Level level = null!;
    private readonly Glib.Framebuffer renderTexture;
    public Glib.Framebuffer Framebuffer => renderTexture;

    private Glib.Mesh? levelMesh;
    private readonly List<TileInstance> tileInstances;
    private readonly Glib.Shader shader3D;
    private readonly Glib.Texture testTexture;

    private readonly Dictionary<string, Glib.Mesh> tileMeshes = [];

    public bool Wireframe = false;
    public float LightAngle = 0f;

    struct TileInstance
    {
        public Glib.Mesh mesh;
        public Vector3 pos;
    }

    private const string Shader3DVSrc = @"
        #version 330 core

        layout (location = 0) in vec3 vertexPosition;
        layout (location = 1) in vec2 vertexTexCoord;
        layout (location = 2) in vec4 vertexColor;
        layout (location = 3) in vec3 vertexNormal;

        varying vec2 fragTexCoord;
        varying vec4 fragColor;
        varying vec3 fragNormal;

        uniform mat4 uModelMatrix;
        uniform mat4 uViewMatrix;
        uniform mat4 uProjectionMatrix;
        uniform mat4 uNormalMatrix;

        void main()
        {
            fragTexCoord = vertexTexCoord;
            fragColor = vertexColor;
            fragNormal = normalize(vec3(uNormalMatrix * vec4(vertexNormal, 1.0)));
            gl_Position = uProjectionMatrix * uViewMatrix * uModelMatrix * vec4(vertexPosition, 1.0);
        }
    ";

    private const string Shader3DFSrc = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;
        in vec3 fragNormal;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        uniform vec3 ambientLightColor;
        uniform vec3 lightDirection;

        out vec4 finalColor;

        void main()
        {
            float diff = max(dot(fragNormal, lightDirection), 0.0);

            vec4 baseColor = texture(glib_uTexture, fragTexCoord) * fragColor;
            finalColor = vec4((vec3(0.6, 0.6, 0.6) * diff + ambientLightColor) * baseColor.rgb, baseColor.a);
            
            //finalColor = vec4((fragNormal + vec3(1.0, 1.0, 1.0)) / 2.0, baseColor.a);
        }    
    ";

    public VoxelRenderer()
    {
        var rctx = RainEd.RenderContext!;

        tileInstances = [];

        renderTexture = Glib.FramebufferConfiguration.Standard(1400, 800)
            .Create(rctx);
        //testTexture = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "internal", "Internal_331_bricksTexture.png"));
        testTexture = rctx.CreateTexture(Glib.Image.FromColor(1, 1, Glib.Color.FromRGBA(255, 0, 0)));
        shader3D = rctx.CreateShader(Shader3DVSrc, Shader3DFSrc);
    }

    public bool IsTransparent(int x, int y, int layer)
    {
        if (!level.IsInBounds(x, y)) return true;
        if (layer < 0 || layer > 2) return true;
        
        var cell = level.Layers[layer, x, y];
        return cell.Geo == GeoType.Air || cell.HasTile();
    }

    Glib.Mesh GetTileMesh(Tiles.Tile init)
    {
        if (tileMeshes.TryGetValue(init.Name, out var mesh))
            return mesh;

        var voxelizer = new TileVoxelizer(init);
        mesh = voxelizer.Voxelize();
        mesh.Upload();
        tileMeshes[init.Name] = mesh;
        return mesh;
    }

    public void UpdateLevel(Level level)
    {
        this.level = level;

        var voxelizer = new GeometryVoxelizer(level);
        var mesh = voxelizer.Voxelize();

        levelMesh?.Dispose();
        levelMesh = mesh;
        levelMesh.Upload();

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
                    if (init is not null && init.Type == Tiles.TileType.VoxelStruct)
                    {
                        var tileMesh = GetTileMesh(init);
                        tileInstances.Add(new TileInstance()
                        {
                            mesh = tileMesh,
                            pos = new Vector3(x - init.CenterX - init.BfTiles, y - init.CenterY - init.BfTiles, l) * 10f
                        });
                    }
                }
            }
        }
    }

    private static Matrix4x4 InvertMatrix(Matrix4x4 mat)
    {
        if (!Matrix4x4.Invert(mat, out var result))
            throw new InvalidOperationException("Matrix determinant is non-zero!");
        return result;
    }

    private static Matrix4x4 CreateNormalMatrix(Matrix4x4 model)
    {
        var normalMatrix4 = Matrix4x4.Transpose(InvertMatrix(model));
        
        // take only the upper-left 3x3 part
        normalMatrix4.M41 = 0f;
        normalMatrix4.M42 = 0f;
        normalMatrix4.M43 = 0f;
        normalMatrix4.M44 = 1f;
        normalMatrix4.M14 = 0f;
        normalMatrix4.M24 = 0f;
        normalMatrix4.M34 = 0f;

        return normalMatrix4;
    }

    public void Render(float camX, float camY, float camZoom)
    {
        var rctx = RainEd.RenderContext!;

        rctx.PushFramebuffer(renderTexture);
        rctx.Clear(Glib.Color.White, Glib.ClearFlags.Color | Glib.ClearFlags.Depth);
        rctx.SetEnabled(Glib.Feature.DepthTest, true);
        rctx.SetEnabled(Glib.Feature.CullFace, true);
        rctx.SetEnabled(Glib.Feature.WireframeRendering, Wireframe);
        rctx.Shader = shader3D;

        rctx.PushTransform();
        rctx.ResetTransform();

        var camPos = new Vector3(camX / Level.TileSize * 10f, camY / Level.TileSize * 10f, -200f * camZoom);
        var projectionMatrix = Glib.GlibMath.CreatePerspective(70f / 180f * MathF.PI, (float)renderTexture.Width / renderTexture.Height, 0.1f, 1000f);
        var viewMatrix = Matrix4x4.CreateTranslation(-camPos) * Matrix4x4.CreateScale(1f, -1f, -1f);

        var lightDirection = Vector3.Normalize(new Vector3(-1f, -1f, 0.6f));
        lightDirection = Vector3.Transform(lightDirection, Matrix4x4.CreateRotationZ(LightAngle));

        shader3D.SetUniform("uProjectionMatrix", projectionMatrix);
        shader3D.SetUniform("uViewMatrix", viewMatrix);
        shader3D.SetUniform("ambientLightColor", new Vector3(0.2f, 0.2f, 0.2f));
        shader3D.SetUniform("lightDirection", lightDirection);

        // draw level geometry
        shader3D.SetUniform("uNormalMatrix", Matrix4x4.Identity);
        shader3D.SetUniform("uModelMatrix", Matrix4x4.Identity);
        rctx.Draw(levelMesh!);

        // draw tiles
        foreach (var inst in tileInstances)
        {
            var modelMatrix = Matrix4x4.CreateTranslation(inst.pos);
            shader3D.SetUniform("uModelMatrix", modelMatrix);
            shader3D.SetUniform("uNormalMatrix", CreateNormalMatrix(modelMatrix));
            rctx.Draw(inst.mesh);
        }

        rctx.SetEnabled(Glib.Feature.DepthTest, false);
        rctx.SetEnabled(Glib.Feature.WireframeRendering, false);
        rctx.PopFramebuffer();
        rctx.Shader = null;
    }
}