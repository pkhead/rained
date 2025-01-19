using System.Numerics;
using Rained.LevelData;
namespace Rained.Rendering;

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

    interface IGeometryOutput
    {
        void DrawRectangle(float x, float y, float w, float h, Glib.Color color);
        void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Glib.Color color);
        void DrawRectangleLines(float x, float y, float w, float h, Glib.Color color)
        {
            DrawRectangle(x, y, 1, h, color);
            DrawRectangle(x, y+h-1, w, 1, color);
            DrawRectangle(x+w-1, y, 1, h, color);
            DrawRectangle(x, y, w, 1, color);
        }
    }

    /// <summary>
    /// Outputs geometry to a mesh. Only one MeshGeometryOutput can be active at a time.
    /// </summary>
    class MeshGeometryOutput : IGeometryOutput
    {
        private static readonly List<Vector3> vertices = [];
        private static readonly List<Glib.Color> colors = [];
        private static readonly List<uint> indices = [];
        private uint meshIndex = 0;

        public MeshGeometryOutput()
        {
            vertices.Clear();
            colors.Clear();
            indices.Clear();
        }

        public void DrawRectangle(float x, float y, float w, float h, Glib.Color color)
        {
            vertices.Add(new Vector3(x, y, 0));
            vertices.Add(new Vector3(x, y+h, 0));
            //vertices.Add(new Vector3(x+w, y+h, 0));

            vertices.Add(new Vector3(x+w, y+h, 0));
            vertices.Add(new Vector3(x+w, y, 0));
            //vertices.Add(new Vector3(x, y, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            indices.Add(meshIndex + 0);
            indices.Add(meshIndex + 1);
            indices.Add(meshIndex + 2);

            indices.Add(meshIndex + 2);
            indices.Add(meshIndex + 3);
            indices.Add(meshIndex + 0);

            meshIndex += 4;
        }

        public void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Glib.Color color)
        {
            vertices.Add(new Vector3(v1.X, v1.Y, 0));
            vertices.Add(new Vector3(v2.X, v2.Y, 0));
            vertices.Add(new Vector3(v3.X, v3.Y, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            indices.Add(meshIndex++);
            indices.Add(meshIndex++);
            indices.Add(meshIndex++);
        }

        public Glib.StandardMesh? CreateMesh()
        {
            if (indices.Count == 0)
            {
                return null;
            }
            else
            {
                var mesh = Glib.StandardMesh.CreateIndexed32([..indices], vertices.Count);
                mesh.SetVertexData([..vertices]);
                mesh.SetColorData([..colors]);
                mesh.Upload();
                return mesh;
            }
        }
    }

    /// <summary>
    /// Immediately draws pushed shapes.
    /// </summary>
    class ImmediateGeometryOutput : IGeometryOutput
    {
        private readonly Glib.RenderContext rctx;
        private readonly Glib.Color baseColor;

        public ImmediateGeometryOutput(Glib.Color baseColor)
        {
            rctx = RainEd.RenderContext;
            this.baseColor = baseColor;
        }

        private static Glib.Color Multiply(Glib.Color a, Glib.Color b)
        {
            return new Glib.Color(
                a.R * b.R,
                a.G * b.G,
                a.B * b.B,
                a.A * b.A
            );
        }

        public void DrawRectangle(float x, float y, float w, float h, Glib.Color color)
        {
            rctx.DrawColor = Multiply(baseColor, color);
            rctx.DrawRectangle(x, y, w, h);
        }

        public void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Glib.Color color)
        {
            rctx.DrawColor = Multiply(baseColor, color);
            rctx.DrawTriangle(v1, v2, v3);
        }
        
        public void Dispose()
        {}
    }

    private Glib.StandardMesh?[,,] chunkLayers;
    private int chunkRowCount; // Y
    private int chunkColCount; // X
    private List<ChunkPos> dirtyChunks;

    public EditorGeometryRenderer(LevelEditRender renderer)
    {
        this.renderInfo = renderer;
        chunkLayers = null!;
        dirtyChunks = null!;
        ReloadLevel();
    }

    public void ReloadLevel()
    {
        // dispose old chunk layers
        for (int x = 0; x < chunkColCount; x++)
        {
            for (int y = 0; y < chunkRowCount; y++)
            {
                chunkLayers[x,y,0]?.Dispose();
                chunkLayers[x,y,1]?.Dispose();
                chunkLayers[x,y,2]?.Dispose();
            }
        }

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

    // build the mesh for the sub-rectangle of a layer
    private void MeshGeometry(IGeometryOutput output, int layer, int subL, int subT, int subR, int subB, bool respectOverlay)
    {
        var viewBeams = RainEd.Instance.Preferences.ViewObscuredBeams;

        static bool crackCanConnect(int x, int y, int layer)
        {
            if (!RainEd.Instance.Level.IsInBounds(x, y)) return false;
            ref var cell = ref RainEd.Instance.Level.Layers[layer, x, y];
            
            return cell.Geo != GeoType.Solid || cell.Has(LevelObject.Crack);
        }

        var overlayX = renderInfo.OverlayX;
        var overlayY = renderInfo.OverlayY;
        var overlayW = renderInfo.OverlayWidth;
        var overlayH = renderInfo.OverlayHeight;
        var overlayGeo = renderInfo.OverlayGeometry;
        if (!renderInfo.IsOverlayActive) respectOverlay = false;

        var level = RainEd.Instance.Level;
        for (int x = subL; x < subR; x++)
        {
            for (int y = subT; y < subB; y++)
            {
                LevelCell c;
                if (respectOverlay &&
                    x >= overlayX && y >= overlayY && x < overlayX + overlayW && y < overlayY + overlayH &&
                    overlayGeo![layer, x-overlayX, y-overlayY].mask
                )
                {
                    c = overlayGeo[layer, x-overlayX, y-overlayY].cell;
                }
                else
                {
                    c = level.Layers[layer,x,y];
                }

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
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 4, 4, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 16, y * Level.TileSize, 4, 4, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 16, 4, 4, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 16, y * Level.TileSize + 16, 4, 4, Glib.Color.White);
                            }
                            else if (crackH)
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 4, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 16, Level.TileSize, 4, Glib.Color.White);
                            }
                            else if (crackV)
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 4, Level.TileSize, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 16, y * Level.TileSize, 4, Level.TileSize, Glib.Color.White);
                            }
                            else
                            {
                                // draw negative diagonal line
                                output.DrawTriangle(
                                    new Vector2(x, y) * Level.TileSize,
                                    new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                                    new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                                    Glib.Color.White
                                );
                                output.DrawTriangle(
                                    new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                                    new Vector2(x * Level.TileSize + 2f, (y+1) * Level.TileSize),
                                    new Vector2(x+1, y+1) * Level.TileSize,
                                    Glib.Color.White
                                );
                            }
                        }
                        else if (viewBeams)
                        {
                            // extra logic to signify that there is a beam here
                            // when beam is completely covered
                            // this is done by not drawing on the space where there is a beam
                            if (hasHBeam && hasVBeam)
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 8, 8, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize, 8, 8, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 12, 8, 8, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize + 12, 8, 8, Glib.Color.White);
                            }
                            else if (hasHBeam)
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 8, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 12, Level.TileSize, 8, Glib.Color.White);
                            }
                            else if (hasVBeam)
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, 8, Level.TileSize, Glib.Color.White);
                                output.DrawRectangle(x * Level.TileSize + 12, y * Level.TileSize, 8, Level.TileSize, Glib.Color.White);
                            }
                            else
                            {
                                output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Glib.Color.White);
                            }
                        }
                        else
                        {
                            // view obscured beams is off, draw as normal
                            output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Glib.Color.White);
                        }

                        break;
                        
                    case GeoType.Platform:
                        output.DrawRectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10, Glib.Color.White);
                        break;
                    
                    case GeoType.Glass:
                        output.DrawRectangleLines(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Glib.Color.White);
                        break;

                    case GeoType.ShortcutEntrance:
                        // draw a lighter square
                        output.DrawRectangle(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            Glib.Color.FromRGBA(255, 255, 255, 127)
                        );
                        break;

                    case GeoType.SlopeLeftDown:
                        output.DrawTriangle(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            Glib.Color.White
                        );
                        break;

                    case GeoType.SlopeLeftUp:
                        output.DrawTriangle(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            Glib.Color.White
                        );
                        break;

                    case GeoType.SlopeRightDown:
                        output.DrawTriangle(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            Glib.Color.White
                        );
                        break;

                    case GeoType.SlopeRightUp:
                        output.DrawTriangle(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            Glib.Color.White
                        );
                        break;
                }

                if (c.Geo != GeoType.Solid)
                {
                    // draw horizontal beam
                    if (hasHBeam)
                    {
                        output.DrawRectangle(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, Glib.Color.White);
                    }

                    // draw vertical beam
                    if (hasVBeam)
                    {
                        output.DrawRectangle(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, Glib.Color.White);
                    }

                    // draw crack
                    if (hasCrack)
                    {
                        // top-right triangle
                        output.DrawTriangle(
                            new Vector2((x+1) * Level.TileSize, y * Level.TileSize),
                            new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                            new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                            Glib.Color.White
                        );

                        // bottom-left triangle
                        output.DrawTriangle(
                            new Vector2(x * Level.TileSize, (y+1) * Level.TileSize),
                            new Vector2(x * Level.TileSize + 2f, (y+1) * Level.TileSize),
                            new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                            Glib.Color.White
                        );

                        // long quad
                        output.DrawTriangle(
                            new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                            new Vector2(x * Level.TileSize + 2f, (y+1) * Level.TileSize),
                            new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                            Glib.Color.White
                        );

                        output.DrawTriangle(
                            new Vector2((x+1) * Level.TileSize, y * Level.TileSize + 2f),
                            new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                            new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                            Glib.Color.White
                        );
                    }
                }
            }
        }
    }

    public void ReloadGeometryMesh()
    {
        if (dirtyChunks.Count == 0) return;

        foreach (var chunkPos in dirtyChunks)
        {
            ref Glib.StandardMesh? chunk = ref chunkLayers[chunkPos.X, chunkPos.Y, chunkPos.Layer];
            chunk?.Dispose();

            var geoOutput = new MeshGeometryOutput();
            MeshGeometry(
                output: geoOutput,
                layer: chunkPos.Layer,
                subL: chunkPos.X * ChunkWidth,
                subT: chunkPos.Y * ChunkHeight,
                subR: Math.Min(RainEd.Instance.Level.Width, (chunkPos.X + 1) * ChunkWidth),
                subB: Math.Min(RainEd.Instance.Level.Height, (chunkPos.Y + 1) * ChunkHeight),
                respectOverlay: false
            );
            chunk = geoOutput.CreateMesh();
        }
        dirtyChunks.Clear();
    }

    public void Render(int layer, Raylib_cs.Color color)
    {
        ReloadGeometryMesh();
        var baseColor = Raylib_cs.Raylib.ToGlibColor(color);

        int viewL = (int) Math.Floor(renderInfo.ViewTopLeft.X / ChunkWidth);
        int viewT = (int) Math.Floor(renderInfo.ViewTopLeft.Y / ChunkHeight);
        int viewR = (int) Math.Ceiling(renderInfo.ViewBottomRight.X / ChunkWidth);
        int viewB = (int) Math.Ceiling(renderInfo.ViewBottomRight.Y / ChunkHeight);

        bool isOverlayActive = renderInfo.IsOverlayActive;
        var overlayL = renderInfo.OverlayX / ChunkWidth;
        var overlayT = renderInfo.OverlayY / ChunkHeight;
        var overlayR = (renderInfo.OverlayX + renderInfo.OverlayWidth - 1) / ChunkWidth;
        var overlayB = (renderInfo.OverlayY + renderInfo.OverlayHeight - 1) / ChunkHeight;
        var imOutput = new ImmediateGeometryOutput(baseColor);

        for (int x = Math.Max(viewL, 0); x < Math.Min(viewR, chunkColCount); x++)
        {
            for (int y = Math.Max(viewT, 0); y < Math.Min(viewB, chunkRowCount); y++)
            {
                // if this chunk overlaps with the overlay, do immediate-mode rendering of the chunk
                // so that the overlay updates properly according to user changes.
                if (isOverlayActive && x >= overlayL && y >= overlayT && x <= overlayR && y <= overlayB)
                {
                    MeshGeometry(
                        output: imOutput,
                        layer: layer,
                        subL: x * ChunkWidth,
                        subT: y * ChunkHeight,
                        subR: Math.Min(RainEd.Instance.Level.Width, (x + 1) * ChunkWidth),
                        subB: Math.Min(RainEd.Instance.Level.Height, (y + 1) * ChunkHeight),
                        respectOverlay: true
                    );
                }
                else
                {
                    var mesh = chunkLayers[x,y,layer];
                    if (mesh is not null && mesh.GetIndexVertexCount() > 0)
                    {
                        RainEd.RenderContext.DrawColor = baseColor;
                        RainEd.RenderContext.Draw(mesh);
                    }
                }
            }
        }
    }

    private void MarkNeedsRedraw(ChunkPos cpos)
    {
        // out of bounds check
        if ((cpos.X + 1) * ChunkWidth <= 0) return;
        if ((cpos.X + 1) * ChunkHeight <= 0) return;
        if (cpos.X * ChunkWidth >= RainEd.Instance.Level.Width) return;
        if (cpos.Y * ChunkHeight >= RainEd.Instance.Level.Height) return;

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