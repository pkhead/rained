using Raylib_cs;
using System.Numerics;

namespace RainEd.Rendering;

// interface to unify immediate-mode drawing and drawing to a mesh
interface IRenderOutput
{
    void Begin();
    void End();

    void DrawRect(float x, float y, float w, float h, Color color);
    void DrawRectLines(float x, float y, float w, float h, Color color);
    void DrawTri(Vector2 v1, Vector2 v2, Vector2 v3, Color color);
}

class MeshRenderOutput : IRenderOutput
{
    public RlManaged.Mesh? Mesh = null;
    private readonly List<Vector3> verticesBuf = new();
    private readonly List<Color> colorsBuf = new();

    public MeshRenderOutput()
    {}

    public void Begin()
    {
        if (Mesh is null)
        {
            throw new NullReferenceException("MeshRenderOutput.Begin() called without a mesh!");
        }
        
        verticesBuf.Clear();
        colorsBuf.Clear();
    }

    public void End()
    {        
        Mesh!.SetVertices(verticesBuf.ToArray());
        Mesh.SetColors(colorsBuf.ToArray());
        Mesh.UploadMesh(true);
    }

    public void DrawRect(float x, float y, float w, float h, Color color)
    {
        verticesBuf.Add(new Vector3(x, y, 0));
        verticesBuf.Add(new Vector3(x, y+h, 0));
        verticesBuf.Add(new Vector3(x+w, y+h, 0));

        verticesBuf.Add(new Vector3(x+w, y+h, 0));
        verticesBuf.Add(new Vector3(x+w, y, 0));
        verticesBuf.Add(new Vector3(x, y, 0));

        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);

        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);
    }

    public void DrawRectLines(float x, float y, float w, float h, Color color)
    {
        DrawRect(x, y, 1, h, color);
        DrawRect(x, y+h-1, w, 1, color);
        DrawRect(x+w-1, y, 1, h, color);
        DrawRect(x, y, w, 1, color);
    }

    public void DrawTri(Vector2 v1, Vector2 v2, Vector2 v3, Color color)
    {
        verticesBuf.Add(new Vector3(v1.X, v1.Y, 0));
        verticesBuf.Add(new Vector3(v2.X, v2.Y, 0));
        verticesBuf.Add(new Vector3(v3.X, v3.Y, 0));

        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);
    }
}

struct ChunkPos
{
    public int X;
    public int Y;
    public int Layer;

    public ChunkPos(int x, int y, int layer)
    {
        X = x;
        Y = y;
        Layer = layer;
    }

    public readonly override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        
        ChunkPos other = (ChunkPos) obj;
        return X == other.X && Y == other.Y && Layer == other.Layer;
    }
    
    // override object.GetHashCode
    public readonly override int GetHashCode()
    {
        return HashCode.Combine(X.GetHashCode(), Y.GetHashCode(), Layer.GetHashCode());
    }
}

class ChunkedGeoRenderer
{
    private const int ChunkWidth = 32;
    private const int ChunkHeight = 32;
    public bool ViewObscuredBeams = false;

    private readonly RlManaged.Material geoMaterial;
    private readonly MeshRenderOutput meshRenderOutput = new();
    private RlManaged.Mesh?[,,] chunkLayers;
    private int chunkRowCount; // Y
    private int chunkColCount; // X
    private List<ChunkPos> dirtyChunks;

    public ChunkedGeoRenderer()
    {
        geoMaterial = RlManaged.Material.LoadMaterialDefault();
        chunkLayers = null!;
        dirtyChunks = null!;
    }

    public void ReloadLevel()
    {
        var level = RainEd.Instance.Level;

        // TODO: dispose old chunk layers
        chunkColCount = (level.Width-1) / ChunkWidth + 1;
        chunkRowCount = (level.Height-1) / ChunkHeight + 1;
        chunkLayers = new RlManaged.Mesh?[chunkColCount, chunkRowCount, 3];
        dirtyChunks = new List<ChunkPos>();

        for (int x = 0; x < chunkColCount; x++)
        {
            for (int y = 0; y < chunkRowCount; y++)
            {
                chunkLayers[x,y,0] = null;
                chunkLayers[x,y,1] = null;
                chunkLayers[x,y,2] = null;
            }
        }
        
        MarkNeedsRedraw(0);
        MarkNeedsRedraw(1);
        MarkNeedsRedraw(2);
    }

    // build the mesh for the sub-rectangle of a layer
    private void MeshGeometry(IRenderOutput renderOutput, int layer, int subL, int subT, int subR, int subB)
    {
        var level = RainEd.Instance.Level;
        renderOutput.Begin();

        bool crackCanConnect(int x, int y, int layer)
        {
            if (!level.IsInBounds(x, y)) return false;
            ref var cell = ref level.Layers[layer, x, y];
            
            return cell.Geo != GeoType.Solid || cell.Has(LevelObject.Crack);
        }

        for (int x = subL; x < subR; x++)
        {
            for (int y = subT; y < subB; y++)
            {
                ref LevelCell c = ref level.Layers[layer,x,y];

                var hasHBeam = (c.Objects & LevelObject.HorizontalBeam) != 0;
                var hasVBeam = (c.Objects & LevelObject.VerticalBeam) != 0;
                var hasCrack = (c.Objects & LevelObject.Crack) != 0;

                bool crackH = false;
                bool crackV = false;
                if ((c.Objects & LevelObject.Crack) != 0)
                {
                    bool nUp = crackCanConnect(x, y-1, layer);
                    bool nDown = crackCanConnect(x, y+1, layer);
                    bool nLeft = crackCanConnect(x-1, y, layer);
                    bool nRight = crackCanConnect(x+1, y, layer);
                    crackH = nLeft || nRight;
                    crackV = nUp || nDown;
                }

                switch (c.Geo)
                {
                    case GeoType.Solid:
                        if (hasCrack)
                        {
                            if (crackH && crackV)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 4, 4, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, 4, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 16, 4, 4, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize + 16, 4, 4, Color.White);
                            }
                            else if (crackH)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 4, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 16, Level.TileSize, 4, Color.White);
                            }
                            else if (crackV)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 4, Level.TileSize, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, Level.TileSize, Color.White);
                            }
                            else
                            {
                                // draw negative diagonal line
                                renderOutput.DrawTri(
                                    new Vector2(x, y) * Level.TileSize,
                                    new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                                    new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                                    Color.White
                                );
                                renderOutput.DrawTri(
                                    new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                                    new Vector2(x * Level.TileSize + 2f, (y+1) * Level.TileSize),
                                    new Vector2(x+1, y+1) * Level.TileSize,
                                    Color.White
                                );
                            }
                        }
                        else if (ViewObscuredBeams)
                        {
                            // extra logic to signify that there is a beam here
                            // when beam is completely covered
                            // this is done by not drawing on the space where there is a beam
                            if (hasHBeam && hasVBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 8, 8, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, 8, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 12, 8, 8, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize + 12, 8, 8, Color.White);
                            }
                            else if (hasHBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 8, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 12, Level.TileSize, 8, Color.White);
                            }
                            else if (hasVBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 8, Level.TileSize, Color.White);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, Level.TileSize, Color.White);
                            }
                            else
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                            }
                        }
                        else
                        {
                            // view obscured beams is off, draw as normal
                            renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                        }

                        break;
                        
                    case GeoType.Platform:
                        renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10, Color.White);
                        break;
                    
                    case GeoType.Glass:
                        renderOutput.DrawRectLines(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                        break;

                    case GeoType.ShortcutEntrance:
                        // draw a lighter square
                        renderOutput.DrawRect(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            new Color(255, 255, 255, 127)
                        );
                        break;

                    case GeoType.SlopeLeftDown:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeLeftUp:
                        renderOutput.DrawTri(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeRightDown:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeRightUp:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            Color.White
                        );
                        break;
                }

                if (c.Geo != GeoType.Solid)
                {
                    // draw horizontal beam
                    if (hasHBeam)
                    {
                        renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, Color.White);
                    }

                    // draw vertical beam
                    if (hasVBeam)
                    {
                        renderOutput.DrawRect(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, Color.White);
                    }
                }
            }
        }

        renderOutput.End();
    }

    public void ReloadGeometryMesh()
    {
        var level = RainEd.Instance.Level;

        if (dirtyChunks.Count == 0) return;
        RainEd.Logger.Debug("Remesh geometry chunks");

        foreach (var chunkPos in dirtyChunks)
        {
            ref RlManaged.Mesh? chunk = ref chunkLayers[chunkPos.X, chunkPos.Y, chunkPos.Layer];

            chunk?.Dispose();
            chunk = new RlManaged.Mesh();

            meshRenderOutput.Mesh = chunk;
            MeshGeometry(
                meshRenderOutput,
                layer: chunkPos.Layer,
                subL: chunkPos.X * ChunkWidth,
                subT: chunkPos.Y * ChunkHeight,
                subR: Math.Min(level.Width, (chunkPos.X + 1) * ChunkWidth),
                subB: Math.Min(level.Height, (chunkPos.Y + 1) * ChunkHeight)
            );
        }
        dirtyChunks.Clear();
    }

    public void RenderGeometry(int layer, Vector2 viewTopLeft, Vector2 viewBottomRight, Color color)
    {
        ReloadGeometryMesh();

        unsafe
        {
            geoMaterial.Maps[(int) MaterialMapIndex.Diffuse].Color = color;
        }

        var mat = Matrix4x4.Identity;

        // should raylib not do this automatically??
        // i suppose raylib isn't designed for raw meshes to drawn in Drawing/2D mode
        Rlgl.DrawRenderBatchActive();

        int viewL = (int) Math.Floor(viewTopLeft.X / ChunkWidth);
        int viewT = (int) Math.Floor(viewTopLeft.Y / ChunkHeight);
        int viewR = (int) Math.Ceiling(viewBottomRight.X / ChunkWidth);
        int viewB = (int) Math.Ceiling(viewBottomRight.Y / ChunkHeight);

        for (int x = Math.Max(viewL, 0); x < Math.Min(viewR, chunkColCount); x++)
        {
            for (int y = Math.Max(viewT, 0); y < Math.Min(viewB, chunkRowCount); y++)
            {
                var mesh = chunkLayers[x,y,layer];
                if (mesh is not null)
                    Raylib.DrawMesh(mesh, geoMaterial, mat);
            }
        }
    }

    private void MarkNeedsRedraw(ChunkPos cpos)
    {
        if (!dirtyChunks.Contains(cpos))
            dirtyChunks.Add(cpos);
    }

    // mark entire layer as dirty
    public void MarkNeedsRedraw(int layer)
    {
        for (int x = 0; x < chunkColCount; x++)
        {
            for (int y = 0; y < chunkRowCount; y++)
            {
                MarkNeedsRedraw(new ChunkPos(x, y, layer));
            }
        }
    }

    public void MarkNeedsRedraw(int x, int y, int layer)
    {
        var cpos = new ChunkPos(x / ChunkWidth, y / ChunkHeight, layer);
        MarkNeedsRedraw(cpos);

        // if on edge, mark neighboring chunks (because of terrain crack)
        if (x % ChunkWidth == 0)
            MarkNeedsRedraw(new ChunkPos((x-1) / ChunkWidth, y / ChunkHeight, layer));
        if (y % ChunkHeight == 0)
            MarkNeedsRedraw(new ChunkPos(x / ChunkWidth, (y-1) / ChunkHeight, layer));
        if ((x+1) % ChunkWidth == 0)
            MarkNeedsRedraw(new ChunkPos((x+1) / ChunkWidth, y / ChunkHeight, layer));
        if ((y+1) % ChunkHeight == 0)
            MarkNeedsRedraw(new ChunkPos(x / ChunkWidth, (y+1) / ChunkHeight, layer));
    }
}