using System.Numerics;
using System.Runtime.InteropServices;
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
    private readonly VoxelCell[] voxels;

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

    public VoxelCell this[int x, int y, int z]
    {
        get => voxels[z * (width * height) + y * width + x];
        set => voxels[z * (width * height) + y * width + x] = value;
    }

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
        voxels = new VoxelCell[w*h*d];
        Array.Clear(voxels);

        meshConfiguration = new Glib.MeshConfiguration()
            .SetIndexed(true)
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
        return this[x,y,z];
    }

    private bool IsInBounds(int x, int y, int z)
    {
        return !(x < 0 || y < 0 || z < 0 || x >= width || y >= height || z >= depth);
    }

    public Glib.Mesh Voxelize()
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var colors = new List<Glib.Color>();
        var indices = new List<int>();

        var voxelSize = new Vector3(VoxelWidth, VoxelHeight, VoxelDepth);
        var meshIndex = 0;

        void FinishQuad()
        {
            indices.Add(meshIndex + 0);
            indices.Add(meshIndex + 1);
            indices.Add(meshIndex + 2);
            indices.Add(meshIndex + 2);
            indices.Add(meshIndex + 3);
            indices.Add(meshIndex + 0);
            meshIndex += 4;
        }

        void FinishTriangle()
        {
            indices.Add(meshIndex + 0);
            indices.Add(meshIndex + 1);
            indices.Add(meshIndex + 2);
            meshIndex += 3;
        }

        void LeftRect(float x, float y, float z, float w, float h, float d)
        {
            vertices.Add(new Vector3(x, y, z) * voxelSize);
            vertices.Add(new Vector3(x, y, z+d) * voxelSize);
            vertices.Add(new Vector3(x, y+h, z+d) * voxelSize);
            vertices.Add(new Vector3(x, y+h, z) * voxelSize);
            
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));

            normals.Add(new Vector3(-1f, 0f, 0f));
            normals.Add(new Vector3(-1f, 0f, 0f));
            normals.Add(new Vector3(-1f, 0f, 0f));
            normals.Add(new Vector3(-1f, 0f, 0f));

            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);

            FinishQuad();
        }

        void RightRect(float x, float y, float z, float w, float h, float d)
        {
            vertices.Add(new Vector3(x+w, y, z) * voxelSize); 
            vertices.Add(new Vector3(x+w, y+h, z) * voxelSize); 
            vertices.Add(new Vector3(x+w, y+h, z+d) * voxelSize);
            vertices.Add(new Vector3(x+w, y, z+d) * voxelSize);
            
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));

            normals.Add(new Vector3(1f, 0f, 0f));
            normals.Add(new Vector3(1f, 0f, 0f));
            normals.Add(new Vector3(1f, 0f, 0f));
            normals.Add(new Vector3(1f, 0f, 0f));

            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            
            FinishQuad();
        }

        void TopRect(float x, float y, float z, float w, float h, float d)
        {
            vertices.Add(new Vector3(x, y, z) * voxelSize);
            vertices.Add(new Vector3(x+w, y, z) * voxelSize);
            vertices.Add(new Vector3(x+w, y, z+d) * voxelSize);
            vertices.Add(new Vector3(x, y, z+d) * voxelSize);
            
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));

            normals.Add(new Vector3(0f, -1f, 0f));
            normals.Add(new Vector3(0f, -1f, 0f));
            normals.Add(new Vector3(0f, -1f, 0f));
            normals.Add(new Vector3(0f, -1f, 0f));

            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            
            FinishQuad();
        }

        void BottomRect(float x, float y, float z, float w, float h, float d)
        {
            vertices.Add(new Vector3(x, y+h, z) * voxelSize);
            vertices.Add(new Vector3(x, y+h, z+d) * voxelSize);
            vertices.Add(new Vector3(x+w, y+h, z+d) * voxelSize);
            vertices.Add(new Vector3(x+w, y+h, z) * voxelSize);
            
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));

            normals.Add(new Vector3(0f, 1f, 0f));
            normals.Add(new Vector3(0f, 1f, 0f));
            normals.Add(new Vector3(0f, 1f, 0f));
            normals.Add(new Vector3(0f, 1f, 0f));

            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            colors.Add(Glib.Color.White);
            
            FinishQuad();
        }

        // cache render info
        VoxelRenderInfo[,,] renderInfos = new VoxelRenderInfo[width, height, depth];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    renderInfos[x,y,z] = GetRenderInfo(x,y,z);
                }
            }
        }

        // greedy mesh the front face of solid tiles
        bool[,] visited = new bool[width, height];

        for (int z = 0; z < depth; z++)
        {
            Array.Clear(visited);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (visited[x,y]) continue;
                    if (this[x,y,z] != VoxelCell.Solid) continue;

                    // find rectangle
                    // (startX, startY) = (x, y) at this point
                    // (endX, endY) is the closest cell to the bottom-right of the bottom-right corner of the rectangle
                    //              thus, the cell at (endX, endY) is not part of the rectangle
                    int endX = x;
                    int endY = y;
                    
                    for (int y0 = y; y0 <= height; y0++)
                    {
                        // find the width of this row
                        for (int x0 = x; x0 <= width; x0++)
                        {
                            var cell = VoxelCell.Air;
                            if (IsInBounds(x0, y0, z) && !visited[x0, y0] && renderInfos[x0,y0,z].DoRender)
                            {
                                cell = this[x0, y0, z];
                            }

                            if (cell != VoxelCell.Solid)
                            {
                                // exit the search if the width at this row is not the same as the width
                                // of the above row
                                // (if this is the first row, this check is not performed)
                                if (endX != x0 && y != y0)
                                {
                                    goto endSearch;
                                }
                                else
                                {
                                    for (int i = x; i < x0; i++)
                                        visited[i, y0] = true;
                                    
                                    endX = x0;
                                    break;
                                }
                            }
                        }

                        endY++;
                    }
                    endSearch:;

                    // create the box
                    // TODO: uvs
                    if (x != endX && y != endY)
                    {
                        var w = endX - x;
                        var h = endY - y;

                        // front face
                        vertices.Add(new Vector3(x, y, z) * voxelSize);
                        vertices.Add(new Vector3(x, y+h, z) * voxelSize);
                        vertices.Add(new Vector3(x+w, y+h, z) * voxelSize);
                        vertices.Add(new Vector3(x+w, y, z) * voxelSize);
                        
                        uvs.Add(new Vector2(0f, 0f));
                        uvs.Add(new Vector2(0f, 1f));
                        uvs.Add(new Vector2(1f, 1f));
                        uvs.Add(new Vector2(1f, 0f));

                        normals.Add(new Vector3(0f, 0f, 1f));
                        normals.Add(new Vector3(0f, 0f, 1f));
                        normals.Add(new Vector3(0f, 0f, 1f));
                        normals.Add(new Vector3(0f, 0f, 1f));

                        colors.Add(Glib.Color.White);
                        colors.Add(Glib.Color.White);
                        colors.Add(Glib.Color.White);
                        colors.Add(Glib.Color.White);
                        
                        FinishQuad();

                        // side faces
                        RightRect(x, y, z, w, h, 1);
                        LeftRect(x, y, z, w, h, 1);
                        TopRect(x, y, z, w, h, 1);
                        BottomRect(x, y, z, w, h, 1);
                    }
                }
            }
        }

        // slopes
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var cell = this[x,y,z];
                    if (cell == VoxelCell.Air) continue;

                    var renderInfo = GetRenderInfo(x, y, z);
                    if (!renderInfo.DoRender) continue;

                    const float sqrt2over2 = 0.70710678118f;

                    switch (cell)
                    {
                        case VoxelCell.SlopeRightUp:
                        {
                            // front face
                            vertices.Add(new Vector3(x, y, z) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                            
                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));

                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            
                            FinishTriangle();

                            // sloped quad
                            vertices.Add(new Vector3(x, y, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);
                            vertices.Add(new Vector3(x, y, z+1) * voxelSize);

                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            var n = new Vector3(sqrt2over2, sqrt2over2, 0f);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            FinishQuad();

                            // left and bottom faces
                            LeftRect(x, y, z, 1, 1, 1);
                            BottomRect(x, y, z, 1, 1, 1);
                            break;
                        }

                        case VoxelCell.SlopeLeftUp:
                        {
                            // front face
                            vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                            
                            uvs.Add(new Vector2(1f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));

                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            indices.Add(meshIndex);
                            indices.Add(meshIndex+1);
                            indices.Add(meshIndex+2);
                            meshIndex += 3;

                            // sloped quad
                            vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y, z+1) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z+1) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z) * voxelSize);

                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            var n = new Vector3(-sqrt2over2, sqrt2over2, 0f);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            FinishQuad();

                            // right and bottom faces
                            RightRect(x, y, z, 1, 1, 1);
                            BottomRect(x, y, z, 1, 1, 1);
                            break;
                        }
                        
                        case VoxelCell.SlopeLeftDown:
                        {
                            // front face
                            vertices.Add(new Vector3(x, y, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                            
                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(1f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            indices.Add(meshIndex);
                            indices.Add(meshIndex+1);
                            indices.Add(meshIndex+2);
                            meshIndex += 3;

                            // sloped quad
                            vertices.Add(new Vector3(x, y, z) * voxelSize);
                            vertices.Add(new Vector3(x, y, z+1) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z+1) * voxelSize);
                            vertices.Add(new Vector3(x+1, y+1, z) * voxelSize);

                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            var n = new Vector3(-sqrt2over2, -sqrt2over2, 0f);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            FinishQuad();

                            // right and top faces
                            RightRect(x, y, z, 1, 1, 1);
                            TopRect(x, y, z, 1, 1, 1);
                            break;
                        }

                        case VoxelCell.SlopeRightDown:
                        {
                            // front face
                            vertices.Add(new Vector3(x, y, z) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                            
                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));
                            normals.Add(new Vector3(0f, 0f, 1f));

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            indices.Add(meshIndex);
                            indices.Add(meshIndex+1);
                            indices.Add(meshIndex+2);
                            meshIndex += 3;

                            // sloped quad
                            vertices.Add(new Vector3(x+1, y, z) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z) * voxelSize);
                            vertices.Add(new Vector3(x, y+1, z+1) * voxelSize);
                            vertices.Add(new Vector3(x+1, y, z+1) * voxelSize);

                            uvs.Add(new Vector2(0f, 0f));
                            uvs.Add(new Vector2(0f, 1f));
                            uvs.Add(new Vector2(1f, 1f));
                            uvs.Add(new Vector2(1f, 0f));

                            var n = new Vector3(sqrt2over2, -sqrt2over2, 0f);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);
                            normals.Add(n);

                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);
                            colors.Add(Glib.Color.White);

                            FinishQuad();

                            // left and top faces
                            LeftRect(x, y, z, 1, 1, 1);
                            TopRect(x, y, z, 1, 1, 1);
                            break;
                        }
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
        mesh.SetIndexBufferData([..indices]);
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
                        this[x,y,l] = VoxelCell.Air;
                    }
                    else
                    {
                        this[x,y,l] = cell.Geo switch
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
                        this[x,y,l] = VoxelCell.Solid;
                    }
                    else
                    {
                        this[x,y,l] = VoxelCell.Air;
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