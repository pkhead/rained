using System.Numerics;
using Raylib_cs;

namespace RainEd.Rendering;

enum VoxelCell : byte
{
    Air,
    Solid,

    // the different directions are differentiated by the direction of its normal vector
    SlopeRightUp,
    SlopeRightDown,
    SlopeLeftDown,
    SlopeLeftUp
}

struct VoxelRenderInfo()
{
    // 0 = top left
    // 1 = bottom left
    // 2 = bottom right
    // 3 = top right
    public bool DoRender = true;

    public Color[] FrontColors = [
        Color.White,
        Color.White,
        Color.White,
        Color.White
    ];

    public Vector2[] FrontUVs = [
        new Vector2(0f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 1f),
        new Vector2(1f, 0f)
    ];
}

class Voxelizer
{
    private int width, height, depth;
    public readonly VoxelCell[,,] Voxels;

    /// <summary>
    /// The width of the voxel grid
    /// </summary>
    public int Width => width;

    /// <summary>
    /// The height of the voxel grid
    /// </summary>
    public int Height => height;

    /// <summary>
    /// The depth of the voxel grid
    /// </summary>
    public int Depth => depth;

    /// <summary>
    /// The width of an individual voxel
    /// </summary>
    public float VoxelWidth = 1f;

    /// <summary>
    /// The height of an individual voxel
    /// </summary>
    public float VoxelHeight = 1f;

    /// <summary>
    /// The depth of an individual voxel
    /// </summary>
    public float VoxelDepth = 1f;

    private Glib.MeshConfiguration meshConfiguration;
    private enum MeshBufferIndex : int
    {
        Positions = 0,
        TexCoords = 1,
        Colors = 2,
        Normals = 3,
    }

    public Voxelizer(int w, int h, int d)
    {
        width = w;
        height = h;
        depth = d;
        Voxels = new VoxelCell[w,h,d];

        meshConfiguration = new Glib.MeshConfiguration()
            .SetIndexed(false)
            .SetPrimitiveType(Glib.MeshPrimitiveType.Triangles)
            .SetupBuffer((int) MeshBufferIndex.Positions, Glib.DataType.Vector3) // vertices
            .SetupBuffer((int) MeshBufferIndex.TexCoords, Glib.DataType.Vector2) // tex coords
            .SetupBuffer((int) MeshBufferIndex.Colors, Glib.DataType.Vector4) // colors
            .SetupBuffer((int) MeshBufferIndex.Normals, Glib.DataType.Vector3); // normals
    }

    protected virtual VoxelRenderInfo GetRenderInfo(int x, int y, int z)
    {
        return new VoxelRenderInfo();
    }

    VoxelCell GetCell(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= width || y >= height || z >= depth) return VoxelCell.Air;
        return Voxels[x,y,z];
    }

    public Glib.Mesh Voxelize()
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var colors = new List<Glib.Color>();

        var voxelSize = new Vector3(VoxelWidth, VoxelHeight, VoxelDepth);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var cell = Voxels[x,y,z];
                    if (cell == VoxelCell.Air) continue;

                    var renderInfo = GetRenderInfo(x, y, z);
                    if (!renderInfo.DoRender) continue;

                    switch (cell)
                    {
                        case VoxelCell.Solid:
                            var ocell = GetCell(x, y, z-1);
                            if (ocell != VoxelCell.Solid)
                            {
                                // front face
                                vertices.Add(new Vector3(x, y, z) * voxelSize); 
                                vertices.Add(new Vector3(x, y+1, z) * voxelSize); 
                                vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);

                                vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                                vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                                vertices.Add(new Vector3(x, y, z) * voxelSize);

                                uvs.Add(renderInfo.FrontUVs[0]);
                                uvs.Add(renderInfo.FrontUVs[1]);
                                uvs.Add(renderInfo.FrontUVs[2]);

                                uvs.Add(renderInfo.FrontUVs[2]);
                                uvs.Add(renderInfo.FrontUVs[3]);
                                uvs.Add(renderInfo.FrontUVs[0]);

                                normals.Add(new Vector3(0f, 0f, 1f));
                                normals.Add(new Vector3(0f, 0f, 1f));
                                normals.Add(new Vector3(0f, 0f, 1f));

                                normals.Add(new Vector3(0f, 0f, 1f));
                                normals.Add(new Vector3(0f, 0f, 1f));
                                normals.Add(new Vector3(0f, 0f, 1f));

                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                            }

                            ocell = GetCell(x+1, y, z);
                            if (ocell != VoxelCell.Solid && ocell != VoxelCell.SlopeRightUp && ocell != VoxelCell.SlopeRightDown)
                            {
                                // right face
                                vertices.Add(new Vector3(x+1, y, z) * voxelSize); 
                                vertices.Add(new Vector3(x+1, y+1, z) * voxelSize); 
                                vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);

                                vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);
                                vertices.Add(new Vector3(x+1, y, z+1) * voxelSize);
                                vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                                
                                uvs.Add(renderInfo.FrontUVs[0]);
                                uvs.Add(renderInfo.FrontUVs[1]);
                                uvs.Add(renderInfo.FrontUVs[2]);

                                uvs.Add(renderInfo.FrontUVs[2]);
                                uvs.Add(renderInfo.FrontUVs[3]);
                                uvs.Add(renderInfo.FrontUVs[0]);

                                normals.Add(new Vector3(1f, 0f, 0f));
                                normals.Add(new Vector3(1f, 0f, 0f));
                                normals.Add(new Vector3(1f, 0f, 0f));

                                normals.Add(new Vector3(1f, 0f, 0f));
                                normals.Add(new Vector3(1f, 0f, 0f));
                                normals.Add(new Vector3(1f, 0f, 0f));

                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                            }

                            ocell = GetCell(x-1, y, z);
                            if (ocell != VoxelCell.Solid && ocell != VoxelCell.SlopeLeftDown && ocell != VoxelCell.SlopeLeftUp)
                            {
                                // left face
                                vertices.Add(new Vector3(x, y, z) * voxelSize);
                                vertices.Add(new Vector3(x, y, z+1) * voxelSize);
                                vertices.Add(new Vector3(x, y+1, z+1) * voxelSize);

                                vertices.Add(new Vector3(x, y+1, z+1) * voxelSize);
                                vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                                vertices.Add(new Vector3(x, y, z) * voxelSize);
                                
                                uvs.Add(renderInfo.FrontUVs[0]);
                                uvs.Add(renderInfo.FrontUVs[1]);
                                uvs.Add(renderInfo.FrontUVs[2]);

                                uvs.Add(renderInfo.FrontUVs[2]);
                                uvs.Add(renderInfo.FrontUVs[3]);
                                uvs.Add(renderInfo.FrontUVs[0]);

                                normals.Add(new Vector3(-1f, 0f, 0f));
                                normals.Add(new Vector3(-1f, 0f, 0f));
                                normals.Add(new Vector3(-1f, 0f, 0f));

                                normals.Add(new Vector3(-1f, 0f, 0f));
                                normals.Add(new Vector3(-1f, 0f, 0f));
                                normals.Add(new Vector3(-1f, 0f, 0f));

                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                            }

                            ocell = GetCell(x, y-1, z);
                            if (ocell != VoxelCell.Solid && ocell != VoxelCell.SlopeRightUp && ocell != VoxelCell.SlopeLeftUp)
                            {
                                // top face
                                vertices.Add(new Vector3(x, y, z) * voxelSize);
                                vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                                vertices.Add(new Vector3(x+1, y, z+1) * voxelSize);

                                vertices.Add(new Vector3(x+1, y, z+1) * voxelSize);
                                vertices.Add(new Vector3(x, y, z+1) * voxelSize);
                                vertices.Add(new Vector3(x, y, z) * voxelSize);
                                
                                uvs.Add(renderInfo.FrontUVs[0]);
                                uvs.Add(renderInfo.FrontUVs[1]);
                                uvs.Add(renderInfo.FrontUVs[2]);

                                uvs.Add(renderInfo.FrontUVs[2]);
                                uvs.Add(renderInfo.FrontUVs[3]);
                                uvs.Add(renderInfo.FrontUVs[0]);

                                normals.Add(new Vector3(0f, -1f, 0f));
                                normals.Add(new Vector3(0f, -1f, 0f));
                                normals.Add(new Vector3(0f, -1f, 0f));

                                normals.Add(new Vector3(0f, -1f, 0f));
                                normals.Add(new Vector3(0f, -1f, 0f));
                                normals.Add(new Vector3(0f, -1f, 0f));

                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                            }

                            ocell = GetCell(x, y+1, z);
                            if (ocell != VoxelCell.Solid && ocell != VoxelCell.SlopeRightDown && ocell != VoxelCell.SlopeLeftDown)
                            {
                                // bottom face
                                vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                                vertices.Add(new Vector3(x, y+1, z+1) * voxelSize);
                                vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);

                                vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);
                                vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                                vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                                
                                uvs.Add(renderInfo.FrontUVs[0]);
                                uvs.Add(renderInfo.FrontUVs[1]);
                                uvs.Add(renderInfo.FrontUVs[2]);

                                uvs.Add(renderInfo.FrontUVs[2]);
                                uvs.Add(renderInfo.FrontUVs[3]);
                                uvs.Add(renderInfo.FrontUVs[0]);

                                normals.Add(new Vector3(0f, 1f, 0f));
                                normals.Add(new Vector3(0f, 1f, 0f));
                                normals.Add(new Vector3(0f, 1f, 0f));

                                normals.Add(new Vector3(0f, 1f, 0f));
                                normals.Add(new Vector3(0f, 1f, 0f));
                                normals.Add(new Vector3(0f, 1f, 0f));

                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                                colors.Add(Glib.Color.White);
                            }

                        break;
                    }
                }
            }
        }

        // create mesh
        var rctx = RainEd.RenderContext!;
        var mesh = rctx.CreateMesh(meshConfiguration);
        mesh.SetBufferData((int)MeshBufferIndex.Positions, [..vertices]);
        mesh.SetBufferData((int)MeshBufferIndex.TexCoords, [..uvs]);
        mesh.SetBufferData((int)MeshBufferIndex.Normals, [..normals]);
        mesh.SetBufferData((int)MeshBufferIndex.Colors, [..colors]);
        return mesh;
    }
}

class GeometryVoxelizer : Voxelizer
{
    private readonly Level level;

    public GeometryVoxelizer(Level level) : base(level.Width, level.Height, 3)
    {
        this.level = level;

        VoxelWidth = 10;
        VoxelHeight = 10;
        VoxelDepth = 10;

        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                for (int l = 0; l < 3; l++)
                {
                    var cell = level.Layers[l,x,y];
                    if (cell.HasTile())
                    {
                        Voxels[x,y,l] = VoxelCell.Air;
                    }
                    else
                    {
                        Voxels[x,y,l] = cell.Geo switch
                        {
                            GeoType.Air => VoxelCell.Air,
                            GeoType.Solid => VoxelCell.Solid,
                            GeoType.SlopeLeftDown => VoxelCell.SlopeLeftDown,
                            GeoType.SlopeLeftUp => VoxelCell.SlopeLeftUp,
                            GeoType.SlopeRightDown => VoxelCell.SlopeRightDown,
                            GeoType.SlopeRightUp => VoxelCell.SlopeRightUp,
                            _ => VoxelCell.Air
                        };
                    }
                }
            }
        }
    }
}

class TileVoxelizer : Voxelizer
{
    public TileVoxelizer(Tiles.Tile init) : base((init.Width + init.BfTiles * 2) * 20, (init.Height + init.BfTiles * 2) * 20, init.LayerCount)
    {
        var texPath = Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", init.Name + ".png");
        using var img = RlManaged.Image.Load(texPath);

        VoxelWidth = 0.5f;
        VoxelHeight = 0.5f;
        VoxelDepth = 10f / init.LayerCount * (init.HasSecondLayer ? 2f : 1f);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int l = 0; l < init.LayerCount; l++)
                {
                    var yOffset = init.ImageYOffset + Height * l;
                    var col = Raylib.GetImageColor(img, x, y + yOffset);
                    
                    if (col.R != 255 || col.G != 255 || col.B != 255)
                    {
                        Voxels[x,y,l] = VoxelCell.Solid;
                    }
                    else
                    {
                        Voxels[x,y,l] = VoxelCell.Air;
                    }
                }
            }
        }
    }

    protected override VoxelRenderInfo GetRenderInfo(int x, int y, int z)
    {
        var renderInfo = base.GetRenderInfo(x, y, z);

        // top-left
        renderInfo.FrontUVs[0] = new Vector2(x / Width, y / Height);

        // bottom-left
        renderInfo.FrontUVs[1] = new Vector2(x / Width, (y+1) / Height);

        // bottom-right
        renderInfo.FrontUVs[2] = new Vector2((x+1) / Width, (y+1) / Height);

        // top-right
        renderInfo.FrontUVs[3] = new Vector2((x+1) / Width, y / Height);

        return renderInfo;
    }
}