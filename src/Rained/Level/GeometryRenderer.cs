using System.Numerics;
using Raylib_cs;
namespace RainEd.Rendering;

// interface to unify immediate-mode drawing and drawing to a mesh
interface IRenderOutput
{
    void Begin();
    void End();

    void DrawRect(float x, float y, float w, float h, float alpha);
    void DrawRectLines(float x, float y, float w, float h, float alpha);
    void DrawTri(Vector2 v1, Vector2 v2, Vector2 v3, float alpha);
}

class MeshRenderOutput : IRenderOutput
{
    public Glib.StandardMesh? Mesh = null;
    private readonly List<Vector3> verticesBuf = new();
    private readonly List<Glib.Color> colorsBuf = new();
    private readonly List<int> indicesBuf = new();

    private int meshIndex = 0;

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
        indicesBuf.Clear();
        meshIndex = 0;
    }

    public void End()
    {
        Mesh!.SetVertices(verticesBuf.ToArray());
        Mesh.SetColors(colorsBuf.ToArray());
        Mesh.SetIndexBufferData(indicesBuf.ToArray());
        Mesh.Upload();
    }

    public void DrawRect(float x, float y, float w, float h, float alpha)
    {
        verticesBuf.Add(new Vector3(x, y, 0));
        verticesBuf.Add(new Vector3(x, y+h, 0));
        verticesBuf.Add(new Vector3(x+w, y+h, 0));
        verticesBuf.Add(new Vector3(x+w, y, 0));

        var color = new Glib.Color(1f, 1f, 1f, alpha);
        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);

        indicesBuf.Add(meshIndex + 0);
        indicesBuf.Add(meshIndex + 1);
        indicesBuf.Add(meshIndex + 2);

        indicesBuf.Add(meshIndex + 2);
        indicesBuf.Add(meshIndex + 3);
        indicesBuf.Add(meshIndex + 0);
        
        meshIndex += 4;
    }

    public void DrawRectLines(float x, float y, float w, float h, float alpha)
    {
        DrawRect(x, y, 1, h, alpha);
        DrawRect(x, y+h-1, w, 1, alpha);
        DrawRect(x+w-1, y, 1, h, alpha);
        DrawRect(x, y, w, 1, alpha);
    }

    public void DrawTri(Vector2 v1, Vector2 v2, Vector2 v3, float alpha)
    {
        verticesBuf.Add(new Vector3(v1.X, v1.Y, 0));
        verticesBuf.Add(new Vector3(v2.X, v2.Y, 0));
        verticesBuf.Add(new Vector3(v3.X, v3.Y, 0));

        var color = new Glib.Color(1f, 1f, 1f, alpha);
        colorsBuf.Add(color);
        colorsBuf.Add(color);
        colorsBuf.Add(color);

        indicesBuf.Add(meshIndex + 0);
        indicesBuf.Add(meshIndex + 1);
        indicesBuf.Add(meshIndex + 2);
        meshIndex += 3;
    }
}

struct ImmediateRenderOutput : IRenderOutput
{
    public Color DrawColor;

    public ImmediateRenderOutput()
    {}

    public readonly void Begin()
    {}

    public readonly void End()
    {}

    public readonly void DrawRect(float x, float y, float w, float h, float alpha)
    {
        Raylib.DrawRectangleRec(new Rectangle(x, y, w, h), new Color(DrawColor.R, DrawColor.G, DrawColor.B, (int)(DrawColor.A * alpha)));
    }

    public readonly void DrawRectLines(float x, float y, float w, float h, float alpha)
    {
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, w, h), 1f, new Color(DrawColor.R, DrawColor.G, DrawColor.B, (int)(DrawColor.A * alpha)));
    }

    public readonly void DrawTri(Vector2 v1, Vector2 v2, Vector2 v3, float alpha)
    {
        Raylib.DrawTriangle(v1, v2, v3, new Color(DrawColor.R, DrawColor.G, DrawColor.B, (int)(DrawColor.A * alpha)));
    }
}

/// <summary>
/// A geometry renderer that builds chunk meshes
/// for the GPU to render.
/// </summary>
class EditorGeometryRenderer
{
    private readonly LevelEditRender renderInfo;

    private const int ChunkWidth = 32;
    private const int ChunkHeight = 32;

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
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
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

    private readonly MeshRenderOutput meshRenderOutput = new();
    private Glib.StandardMesh?[,,] chunkLayers;
    private int chunkRowCount; // Y
    private int chunkColCount; // X
    private List<ChunkPos> dirtyChunks;
    
    private readonly List<Vector3> verticesBuf = [];
    private readonly List<Glib.Color> colorsBuf = [];
    private readonly List<int> indicesBuf = [];

    private LevelCell[,,]? overlay = null;
    private bool[,,]? overlayMask = null;
    private int overlayWidth;
    private int overlayHeight;
    public int OverlayX;
    public int OverlayY;

    public LevelCell[,,]? Overlay { get => overlay; }
    public bool[,,]? OverlayMask { get => overlayMask; }
    public int OverlayWidth { get => overlayWidth; }
    public int OverlayHeight { get => overlayHeight; }


    public EditorGeometryRenderer(LevelEditRender renderer)
    {
        this.renderInfo = renderer;
        chunkLayers = null!;
        dirtyChunks = null!;
        ReloadLevel();
    }

    public void ReloadLevel()
    {
        // TODO: dispose old chunk layers
        chunkColCount = (RainEd.Instance.Level.Width-1) / ChunkWidth + 1;
        chunkRowCount = (RainEd.Instance.Level.Height-1) / ChunkHeight + 1;
        chunkLayers = new Glib.StandardMesh?[chunkColCount, chunkRowCount, 3];
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

    // get the cell at (l, x, y) accounting for the overlay
    public LevelCell GetDrawnCell(int layer, int x, int y)
    {
        if (overlay is not null && x >= OverlayX && y >= OverlayY && x < OverlayX + overlayWidth && y < OverlayY + overlayHeight)
        {
            int ox = x - OverlayX;
            int oy = y - OverlayY;

            if (overlayMask![layer, ox, oy])
            {
                return overlay[layer, ox, oy];
            }
        }

        return RainEd.Instance.Level.Layers[layer, x, y];
    }

    // build the mesh for the sub-rectangle of a layer
    private void MeshGeometry(IRenderOutput renderOutput, bool drawOverlay, int layer, int subL, int subT, int subR, int subB)
    {
        var level = RainEd.Instance.Level;
        renderOutput.Begin();

        static bool crackCanConnect(int x, int y, int layer)
        {
            if (!RainEd.Instance.Level.IsInBounds(x, y)) return false;
            ref var cell = ref RainEd.Instance.Level.Layers[layer, x, y];
            
            return cell.Geo != GeoType.Solid || cell.Has(LevelObject.Crack);
        }

        for (int x = subL; x < subR; x++)
        {
            for (int y = subT; y < subB; y++)
            {
                LevelCell c = drawOverlay ? GetDrawnCell(layer, x, y) : level.Layers[layer, x, y];

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
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 4, 4, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, 4, 1f);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 16, 4, 4, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize + 16, 4, 4, 1f);
                            }
                            else if (crackH)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 4, 1f);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 16, Level.TileSize, 4, 1f);
                            }
                            else if (crackV)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 4, Level.TileSize, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, Level.TileSize, 1f);
                            }
                            else
                            {
                                // draw negative diagonal line
                                renderOutput.DrawTri(
                                    new Vector2(x, y) * Level.TileSize,
                                    new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                                    new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                                    1f
                                );
                                renderOutput.DrawTri(
                                    new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                                    new Vector2(x * Level.TileSize + 2f, (y+1) * Level.TileSize),
                                    new Vector2(x+1, y+1) * Level.TileSize,
                                    1f
                                );
                            }
                        }
                        else if (renderInfo.ViewObscuredBeams)
                        {
                            // extra logic to signify that there is a beam here
                            // when beam is completely covered
                            // this is done by not drawing on the space where there is a beam
                            if (hasHBeam && hasVBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 8, 8, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, 8, 1f);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 12, 8, 8, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize + 12, 8, 8, 1f);
                            }
                            else if (hasHBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 8, 1f);
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 12, Level.TileSize, 8, 1f);
                            }
                            else if (hasVBeam)
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, 8, Level.TileSize, 1f);
                                renderOutput.DrawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, Level.TileSize, 1f);
                            }
                            else
                            {
                                renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, 1f);
                            }
                        }
                        else
                        {
                            // view obscured beams is off, draw as normal
                            renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, 1f);
                        }

                        break;
                        
                    case GeoType.Platform:
                        renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10, 1f);
                        break;
                    
                    case GeoType.Glass:
                        renderOutput.DrawRectLines(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, 1f);
                        break;

                    case GeoType.ShortcutEntrance:
                        // draw a lighter square
                        renderOutput.DrawRect(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            0.5f
                        );
                        break;

                    case GeoType.SlopeLeftDown:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            1f
                        );
                        break;

                    case GeoType.SlopeLeftUp:
                        renderOutput.DrawTri(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            1f
                        );
                        break;

                    case GeoType.SlopeRightDown:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            1f
                        );
                        break;

                    case GeoType.SlopeRightUp:
                        renderOutput.DrawTri(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            1f
                        );
                        break;
                }

                if (c.Geo != GeoType.Solid)
                {
                    // draw horizontal beam
                    if (hasHBeam)
                    {
                        renderOutput.DrawRect(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, 1f);
                    }

                    // draw vertical beam
                    if (hasVBeam)
                    {
                        renderOutput.DrawRect(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, 1f);
                    }
                }
            }
        }

        renderOutput.End();
    }

    public void ReloadGeometryMesh()
    {
        if (dirtyChunks.Count == 0) return;
        RainEd.Logger.Debug("Remesh geometry chunks");

        foreach (var chunkPos in dirtyChunks)
        {
            ref Glib.StandardMesh? chunk = ref chunkLayers[chunkPos.X, chunkPos.Y, chunkPos.Layer];

            chunk?.Dispose();
            chunk = RainEd.RenderContext.CreateMesh(true);

            meshRenderOutput.Mesh = chunk;
            MeshGeometry(
                meshRenderOutput,
                drawOverlay: false,
                layer: chunkPos.Layer,
                subL: chunkPos.X * ChunkWidth,
                subT: chunkPos.Y * ChunkHeight,
                subR: Math.Min(RainEd.Instance.Level.Width, (chunkPos.X + 1) * ChunkWidth),
                subB: Math.Min(RainEd.Instance.Level.Height, (chunkPos.Y + 1) * ChunkHeight)
            );
        }
        dirtyChunks.Clear();
    }

    public void Render(int layer, Raylib_cs.Color color)
    {
        var level = RainEd.Instance.Level;

        ReloadGeometryMesh();
        RainEd.RenderContext.DrawColor = Raylib_cs.Raylib.ToGlibColor(color);

        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X / ChunkWidth);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y / ChunkHeight);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X / ChunkWidth);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y / ChunkHeight);

        int overlayL = OverlayX / ChunkWidth;
        int overlayT = OverlayY / ChunkHeight;
        int overlayR = (OverlayX + overlayWidth) / ChunkWidth;
        int overlayB = (OverlayY + overlayHeight) / ChunkHeight;

        ImmediateRenderOutput immediateRender = new()
        {
            DrawColor = color
        };

        for (int x = Math.Max(viewL, 0); x < Math.Min(viewR, chunkColCount); x++)
        {
            for (int y = Math.Max(viewT, 0); y < Math.Min(viewB, chunkRowCount); y++)
            {
                // if this chunk intersects with the overlay rect,
                // draw the chunk to the glib immediate draw batch
                // rather than drawing its prebuilt mesh
                if (overlay is not null && x >= overlayL && y >= overlayT && x <= overlayR && y <= overlayB)
                {
                    MeshGeometry(
                        immediateRender,
                        drawOverlay: true,
                        layer: layer,
                        subL: x * ChunkWidth,
                        subT: y * ChunkHeight,
                        subR: Math.Min(level.Width, (x + 1) * ChunkWidth),
                        subB: Math.Min(level.Height, (y + 1) * ChunkHeight)
                    );
                }
                else
                {
                    var mesh = chunkLayers[x,y,layer];
                    if (mesh is not null)
                    {
                        RainEd.RenderContext.Draw(mesh);
                    }
                }
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

    public void ClearOverlay()
    {
        overlay = null;
        overlayMask = null;
    }

    public void SetOverlay(int x, int y, int width, int height, LevelCell[,,] overlay, bool[,,] mask)
    {
        OverlayX = x;
        OverlayY = y;

        overlayWidth = width;
        overlayHeight = height;
        this.overlay = overlay;
        overlayMask = mask;
    }
}