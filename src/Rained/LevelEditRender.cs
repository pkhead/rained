using Raylib_cs;
using System.Numerics;

namespace RainEd;

class LevelEditRender
{
    // Grid offsets into the level graphics texture
    // used to render object images in the level
    private static readonly Dictionary<LevelObject, Vector2> ObjectTextureOffsets = new()
    {
        { LevelObject.Rock,             new(0, 0) },
        { LevelObject.Spear,            new(1, 0) },
        { LevelObject.Shortcut,         new(2, 1) },
        { LevelObject.CreatureDen,      new(3, 1) },
        { LevelObject.Entrance,         new(4, 1) },
        { LevelObject.Hive,             new(0, 2) },
        { LevelObject.ForbidFlyChain,   new(1, 2) },
        { LevelObject.Waterfall,        new(2, 2) },
        { LevelObject.WhackAMoleHole,   new(3, 2) },
        { LevelObject.ScavengerHole,    new(4, 2) },
        { LevelObject.GarbageWorm,      new(0, 3) },
        { LevelObject.WormGrass,        new(1, 3) },
    };

    // all objects associated with shortcuts
    // (these are tracked because they will be rendered separately from other objects
    // (i.e. without transparency regardless of the user's work layer)
    private static readonly LevelObject[] ShortcutObjects = new[] {
        LevelObject.Shortcut, LevelObject.CreatureDen, LevelObject.Entrance,
        LevelObject.WhackAMoleHole, LevelObject.ScavengerHole, LevelObject.GarbageWorm,
    };

    // shortcut objects that can be used to make a valid shortcut connection
    // seems to just be everything except a garbage worm spawn
    private static readonly LevelObject[] ConnectableShortcutObjects = new[] {
        LevelObject.Shortcut, LevelObject.CreatureDen, LevelObject.Entrance,
        LevelObject.WhackAMoleHole, LevelObject.ScavengerHole
    };

    // it seems transparent pixels can be interpreted as pure white or no alpha
    private readonly static string RWTransparencyShaderSrc = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(texture0, fragTexCoord);
            bool isTransparent = texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0;
            vec3 color = mix(texelColor.rgb, vec3(1.0), fragColor.y);

            finalColor = vec4(color, (1.0 - float(isTransparent)) * fragColor.x) * colDiffuse;
        }
    ";

    private readonly RainEd editor;
    private Level Level { get => editor.Level; }

    public bool ViewGrid = true;
    public bool ViewObscuredBeams = false;

    public Vector2 ViewTopLeft;
    public Vector2 ViewBottomRight;
    public float ViewZoom = 1f;
    private float lastViewZoom = 0f;

    private RlManaged.Texture2D gridTexture = null!;
    private readonly RlManaged.Shader propPreviewShader;
    public RlManaged.Shader PropPreviewShader { get => propPreviewShader; }
    
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

    private Raylib_cs.Material geoMaterial;
    private RlManaged.Mesh?[,,] chunkLayers;
    private int chunkRowCount; // Y
    private int chunkColCount; // X
    private List<ChunkPos> dirtyChunks;
    
    private readonly List<Vector3> verticesBuf = new();
    private readonly List<Color> colorsBuf = new();

    public LevelEditRender()
    {
        editor = RainEd.Instance;
        ReloadGridTexture();

        geoMaterial = Raylib.LoadMaterialDefault();
        chunkLayers = null!;
        dirtyChunks = null!;
        ReloadLevel();
        
        propPreviewShader = RlManaged.Shader.LoadFromMemory(null, RWTransparencyShaderSrc);
    }

    public void ReloadLevel()
    {
        // TODO: dispose old chunk layers
        chunkColCount = (Level.Width-1) / ChunkWidth + 1;
        chunkRowCount = (Level.Height-1) / ChunkHeight + 1;
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

    // re-render the grid texture for the new zoom level
    public void ReloadGridTexture()
    {
        if (ViewZoom == lastViewZoom) return;
        lastViewZoom = ViewZoom;

        var imageW = (int)(Level.TileSize * ViewZoom * 2); 
        var imageH = (int)(Level.TileSize * ViewZoom * 2);
        using var image = RlManaged.Image.GenColor(imageW, imageH, new Color(0, 0, 0, 0));

        var majorLineCol = new Color(255, 255, 255, 100);
        var minorLineCol = new Color(255, 255, 255, 60);
        //var majorLineCol = new Color(255, 255, 255, 255);

        // minor grid lines
        Raylib.ImageDrawLine(ref image.Ref(), imageW / 2, 0, imageW / 2, imageH, minorLineCol);
        Raylib.ImageDrawLine(ref image.Ref(), 0, imageH / 2, imageW, imageH / 2, minorLineCol);

        // major lines
        Raylib.ImageDrawLine(ref image.Ref(), 0, 0, imageW, 0, majorLineCol);
        Raylib.ImageDrawLine(ref image.Ref(), 0, 0, 0, imageH, majorLineCol);

        gridTexture?.Dispose();
        gridTexture = RlManaged.Texture2D.LoadFromImage(image);
    }

    // build the mesh for the sub-rectangle of a layer
    private void MeshGeometry(RlManaged.Mesh geoMesh, int layer, int subL, int subT, int subR, int subB)
    {
        var vertices = verticesBuf;
        var colors = colorsBuf;
        vertices.Clear();
        colors.Clear();

        void drawRect(float x, float y, float w, float h, Color color)
        {
            vertices.Add(new Vector3(x, y, 0));
            vertices.Add(new Vector3(x, y+h, 0));
            vertices.Add(new Vector3(x+w, y+h, 0));

            vertices.Add(new Vector3(x+w, y+h, 0));
            vertices.Add(new Vector3(x+w, y, 0));
            vertices.Add(new Vector3(x, y, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }

        void drawRectLines(float x, float y, float w, float h, Color color)
        {
            drawRect(x, y, 1, h, color);
            drawRect(x, y+h, w, 1, color);
            drawRect(x+w-1, y, 1, h, color);
            drawRect(x, y, w, 1, color);
        }

        void drawTri(Vector2 v1, Vector2 v2, Vector2 v3, Color color)
        {
            vertices.Add(new Vector3(v1.X, v1.Y, 0));
            vertices.Add(new Vector3(v2.X, v2.Y, 0));
            vertices.Add(new Vector3(v3.X, v3.Y, 0));

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }

        bool crackCanConnect(int x, int y, int layer)
        {
            if (!Level.IsInBounds(x, y)) return false;
            ref var cell = ref Level.Layers[layer, x, y];
            
            return cell.Geo != GeoType.Solid || cell.Has(LevelObject.Crack);
        }

        for (int x = subL; x < subR; x++)
        {
            for (int y = subT; y < subB; y++)
            {
                ref LevelCell c = ref Level.Layers[layer,x,y];

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
                                drawRect(x * Level.TileSize, y * Level.TileSize, 4, 4, Color.White);
                                drawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, 4, Color.White);
                                drawRect(x * Level.TileSize, y * Level.TileSize + 16, 4, 4, Color.White);
                                drawRect(x * Level.TileSize + 16, y * Level.TileSize + 16, 4, 4, Color.White);
                            }
                            else if (crackH)
                            {
                                drawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 4, Color.White);
                                drawRect(x * Level.TileSize, y * Level.TileSize + 16, Level.TileSize, 4, Color.White);
                            }
                            else if (crackV)
                            {
                                drawRect(x * Level.TileSize, y * Level.TileSize, 4, Level.TileSize, Color.White);
                                drawRect(x * Level.TileSize + 16, y * Level.TileSize, 4, Level.TileSize, Color.White);
                            }
                            else
                            {
                                // draw negative diagonal line
                                drawTri(
                                    new Vector2(x, y) * Level.TileSize,
                                    new Vector2(x * Level.TileSize, (y+1) * Level.TileSize - 2f),
                                    new Vector2((x+1) * Level.TileSize - 2f, y * Level.TileSize),
                                    Color.White
                                );
                                drawTri(
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
                                drawRect(x * Level.TileSize, y * Level.TileSize, 8, 8, Color.White);
                                drawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, 8, Color.White);
                                drawRect(x * Level.TileSize, y * Level.TileSize + 12, 8, 8, Color.White);
                                drawRect(x * Level.TileSize + 12, y * Level.TileSize + 12, 8, 8, Color.White);
                            }
                            else if (hasHBeam)
                            {
                                drawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 8, Color.White);
                                drawRect(x * Level.TileSize, y * Level.TileSize + 12, Level.TileSize, 8, Color.White);
                            }
                            else if (hasVBeam)
                            {
                                drawRect(x * Level.TileSize, y * Level.TileSize, 8, Level.TileSize, Color.White);
                                drawRect(x * Level.TileSize + 12, y * Level.TileSize, 8, Level.TileSize, Color.White);
                            }
                            else
                            {
                                drawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                            }
                        }
                        else
                        {
                            // view obscured beams is off, draw as normal
                            drawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                        }

                        break;
                        
                    case GeoType.Platform:
                        drawRect(x * Level.TileSize, y * Level.TileSize, Level.TileSize, 10, Color.White);
                        break;
                    
                    case GeoType.Glass:
                        drawRectLines(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize, Color.White);
                        break;

                    case GeoType.ShortcutEntrance:
                        // draw a lighter square
                        drawRect(
                            x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                            new Color(255, 255, 255, 127)
                        );
                        break;

                    case GeoType.SlopeLeftDown:
                        drawTri(
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeLeftUp:
                        drawTri(
                            new Vector2(x, y+1) * Level.TileSize,
                            new Vector2(x+1, y+1) * Level.TileSize,
                            new Vector2(x+1, y) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeRightDown:
                        drawTri(
                            new Vector2(x+1, y) * Level.TileSize,
                            new Vector2(x, y) * Level.TileSize,
                            new Vector2(x, y+1) * Level.TileSize,
                            Color.White
                        );
                        break;

                    case GeoType.SlopeRightUp:
                        drawTri(
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
                        drawRect(x * Level.TileSize, y * Level.TileSize + 8, Level.TileSize, 4, Color.White);
                    }

                    // draw vertical beam
                    if (hasVBeam)
                    {
                        drawRect(x * Level.TileSize + 8, y * Level.TileSize, 4, Level.TileSize, Color.White);
                    }
                }
            }
        }

        geoMesh.SetVertices(vertices.ToArray());
        geoMesh.SetColors(colors.ToArray());
        geoMesh.UploadMesh(true);
    }

    public void ReloadGeometryMesh()
    {
        if (dirtyChunks.Count == 0) return;
        RainEd.Logger.Debug("Remesh geometry chunks");

        foreach (var chunkPos in dirtyChunks)
        {
            ref RlManaged.Mesh? chunk = ref chunkLayers[chunkPos.X, chunkPos.Y, chunkPos.Layer];

            chunk?.Dispose();
            chunk = new RlManaged.Mesh();

            MeshGeometry(
                geoMesh: chunk,
                layer: chunkPos.Layer,
                subL: chunkPos.X * ChunkWidth,
                subT: chunkPos.Y * ChunkHeight,
                subR: Math.Min(Level.Width, (chunkPos.X + 1) * ChunkWidth),
                subB: Math.Min(Level.Height, (chunkPos.Y + 1) * ChunkHeight)
            );
        }
        dirtyChunks.Clear();
    }

    private bool IsInBorder(int x, int y)
    {
        return
            x >= Level.BufferTilesLeft && y >= Level.BufferTilesTop &&
            x < Level.Width - Level.BufferTilesRight && y < Level.Height - Level.BufferTilesBot;
    }

    public void RenderGeometry(int layer, Color color)
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

        int viewL = (int) Math.Floor(ViewTopLeft.X / ChunkWidth);
        int viewT = (int) Math.Floor(ViewTopLeft.Y / ChunkHeight);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X / ChunkWidth);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y / ChunkHeight);

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

    public void RenderObjects(Color color)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[0, x, y];

                // draw object graphics
                for (int i = 1; i < 32; i++)
                {
                    LevelObject objType = (LevelObject) (1 << (i-1));
                    if (cell.Has(objType) && !ShortcutObjects.Contains(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
                    {
                        Raylib.DrawTextureRec(
                            editor.LevelGraphicsTexture,
                            new Rectangle(offset.X * 20, offset.Y * 20, 20, 20),
                            new Vector2(x, y) * Level.TileSize,
                            color
                        );
                    }
                }
            }
        }
    }

    private enum ShortcutDirection
    {
        Invalid,
        Left,
        Up,
        Right,
        Down
    };

    // assumes the cell at (x, y) is a shortcut entrance
    private ShortcutDirection CalcShortcutEntranceDirection(int x, int y)
    {
        static bool shortcutCanConnect(Level Level, int x, int y)
        {
            if (!Level.IsInBounds(x, y)) return false;
            
            foreach (var objType in ConnectableShortcutObjects)
                if (Level.Layers[0,x,y].Has(objType)) return true;

            return false;
        }

        static bool isWallBlock(Level level, int x, int y)
        {
            if (!level.IsInBounds(x, y)) return false;
            return level.Layers[0,x,y].Geo == GeoType.Solid;
        }

        int neighborCount = 0;
        int dx = 0;
        int dy = 0;
        ShortcutDirection dir = ShortcutDirection.Invalid;

        // left
        if (shortcutCanConnect(Level, x-1, y))
        {
            dir = ShortcutDirection.Left;
            dx = -1;
            dy = 0;
            neighborCount++;
        }

        // right
        if (shortcutCanConnect(Level, x+1, y))
        {
            dir = ShortcutDirection.Right;
            dx = 1;
            dy = 0;
            neighborCount++;
        }

        // up
        if (shortcutCanConnect(Level, x, y-1))
        {
            dir = ShortcutDirection.Up;
            dx = 0;
            dy = -1;
            neighborCount++;
        }

        // down
        if (shortcutCanConnect(Level, x, y+1))
        {
            dir = ShortcutDirection.Down;
            dx = 0;
            dy = 1;
            neighborCount++;
        }

        // can only be one shortcut-connectable neighbor
        if (neighborCount != 1)
        {
            return ShortcutDirection.Invalid;
        }
        else
        {
            ref var fromCell = ref Level.Layers[0,x-dx,y-dy];
            ref var toCell = ref Level.Layers[0,x+dx,y+dy];
            
            // the shortcut it's facing toward has to be over a solid block (wall, glass, slope(?))
            if (
                toCell.Geo != GeoType.Solid &&
                toCell.Geo != GeoType.Glass &&
                !(toCell.Geo >= GeoType.SlopeRightUp && toCell.Geo <= GeoType.SlopeLeftDown) // check if any slope?
            )
            {
                return ShortcutDirection.Invalid;
            }
            
            // the tile opposite to the shortcut direction must be an air or platform block
            else if (fromCell.Geo != GeoType.Air && fromCell.Geo != GeoType.Platform)
            {
                return ShortcutDirection.Invalid;
            }

            // the 3 blocks to the side must be wall blocks
            for (int i = -1; i <= 1; i++)
            {
                if (!isWallBlock(Level, x+dy + dx*i, y+dx + dy*i))
                    return ShortcutDirection.Invalid;
                
                if (!isWallBlock(Level, x-dy + dx*i, y-dx + dy*i))
                    return ShortcutDirection.Invalid;
            }
        }

        return dir;
    } 
    
    public void RenderShortcuts(Color color)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[0, x, y];

                // shortcut entrance changes appearance
                // based on neighbor Shortcuts
                if (cell.Geo == GeoType.ShortcutEntrance)
                {
                    int texX = 0;
                    int texY = 0;
                        
                    switch (CalcShortcutEntranceDirection(x, y))
                    {
                        case ShortcutDirection.Left:
                            texX = 1;
                            texY = 1;
                            break;
                            
                        case ShortcutDirection.Right:
                            texX = 4;
                            texY = 0;
                            break;
                    
                        case ShortcutDirection.Up:
                            texX = 3;
                            texY = 0;
                            break;
                            
                        case ShortcutDirection.Down:
                            texX = 0;
                            texY = 1;
                            break;
                        
                        case ShortcutDirection.Invalid:
                            texX = 2;
                            texY = 0;
                            break;
                    }
                    
                    Raylib.DrawTextureRec(
                        editor.LevelGraphicsTexture,
                        new Rectangle(texX*20, texY*20, 20, 20),
                        new Vector2(x, y) * Level.TileSize,
                        color
                    );
                }

                // draw other objects
                foreach (LevelObject objType in ShortcutObjects)
                {
                    if (cell.Has(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
                    {
                        Raylib.DrawTextureRec(
                            editor.LevelGraphicsTexture,
                            new Rectangle(offset.X * 20, offset.Y * 20, 20, 20),
                            new Vector2(x, y) * Level.TileSize,
                            IsInBorder(x, y) ? color : new Color(255, 0, 0, 255)
                        );
                    }
                }
            }
        }
    }

    public void RenderTiles(int layer, int alpha)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        // draw tile previews
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[layer, x, y];
                if (!cell.HasTile()) continue;

                Tiles.Tile? tile;
                int tx;
                int ty;

                if (cell.TileHead is not null)
                {
                    tile = cell.TileHead;
                    tx = x;
                    ty = y;
                }
                else
                {
                    tile = Level.Layers[cell.TileLayer, cell.TileRootX, cell.TileRootY].TileHead;
                    tx = cell.TileRootX;
                    ty = cell.TileRootY;
                }

                // TODO: why can this happen?
                if (tile == null) continue;

                var tileLeft = tx - tile.CenterX;
                var tileTop = ty - tile.CenterY;
                var col = tile.Category.Color;

                Raylib.DrawTexturePro(
                    tile.PreviewTexture,
                    new Rectangle((x - tileLeft) * 16, (y - tileTop) * 16, 16, 16),
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                    Vector2.Zero,
                    0f,
                    new Color(col.R, col.G, col.B, alpha)
                );
            }
        }

        // draw material color squares
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[layer, x, y];

                if (!cell.HasTile() && cell.Material != 0 && cell.Geo != GeoType.Air)
                {
                    var col = RainEd.Instance.MaterialDatabase.GetMaterial(cell.Material).Color;
                    Raylib.DrawRectangle(
                        x * Level.TileSize + 7, y * Level.TileSize + 7,
                        Level.TileSize - 14, Level.TileSize - 14,
                        new Color(col.R, col.G, col.B, alpha)
                    );
                }
            }
        }
    }

    

    public void RenderProps(int srcLayer, int alpha)
    {
        int srcDepth = srcLayer * 10;

        foreach (var prop in Level.Props)
        {
            // cull prop if it is outside of the view bounds
            if (prop.Rope is null)
            {
                var aabb = prop.CalcAABB();
                var aabbMin = aabb.Position;
                var aabbMax = aabb.Position + aabb.Size;
                if (aabbMax.X < ViewTopLeft.X || aabbMax.Y < ViewTopLeft.Y || aabbMin.X > ViewBottomRight.X || aabbMin.Y > ViewBottomRight.Y)
                {
                    continue;
                }
            }

            if (prop.DepthOffset < srcDepth || prop.DepthOffset >= srcDepth + 10)
                continue;
            
            var quad = prop.QuadPoints;
            var texture = prop.PropInit.Texture;

            Rlgl.DisableBackfaceCulling();
            Raylib.BeginShaderMode(propPreviewShader);
            Rlgl.SetTexture(texture.Id);

            var variation = prop.Variation == -1 ? 0 : prop.Variation;

            var depthOffset = Math.Max(0, prop.DepthOffset - srcDepth);
        
            for (int depth = prop.PropInit.LayerCount - 1; depth >= 0; depth--)
            {
                float startFade =
                    (prop.PropInit.Type == Props.PropType.SimpleDecal || prop.PropInit.Type == Props.PropType.VariedDecal)
                    ? 0.364f : 0f;
                
                float whiteFade = Math.Clamp((1f - startFade) * ((depthOffset + depth / 2f) / 10f) + startFade, 0f, 1f);

                var srcRect = prop.PropInit.GetPreviewRectangle(variation, depth);

                Rlgl.Begin(DrawMode.Quads);
                {
                    Rlgl.Color4f(alpha / 255f, whiteFade, 0f, 0f);

                    // top-left
                    Rlgl.TexCoord2f(srcRect.X / texture.Width, srcRect.Y / texture.Height);
                    Rlgl.Vertex2f(quad[0].X * Level.TileSize, quad[0].Y * Level.TileSize);

                    // bottom-left
                    Rlgl.TexCoord2f(srcRect.X / texture.Width, (srcRect.Y + srcRect.Height) / texture.Height);
                    Rlgl.Vertex2f(quad[3].X * Level.TileSize, quad[3].Y * Level.TileSize);

                    // bottom-right
                    Rlgl.TexCoord2f((srcRect.X + srcRect.Width) / texture.Width, (srcRect.Y + srcRect.Height) / texture.Height);
                    Rlgl.Vertex2f(quad[2].X * Level.TileSize, quad[2].Y * Level.TileSize);

                    // top-right
                    Rlgl.TexCoord2f((srcRect.X + srcRect.Width) / texture.Width, srcRect.Y / texture.Height);
                    Rlgl.Vertex2f(quad[1].X * Level.TileSize, quad[1].Y * Level.TileSize);
                }
                Rlgl.End();
            }

            Rlgl.SetTexture(0);
            Raylib.EndShaderMode();
            Rlgl.EnableBackfaceCulling();

            // render segments of rope-type props
            if (prop.Rope is not null)
            {
                var rope = prop.Rope.Model;

                if (rope is not null)
                {
                    for (int i = 0; i < rope.SegmentCount; i++)
                    {
                        var newPos = rope.GetSmoothSegmentPos(i);
                        var oldPos = rope.GetSmoothLastSegmentPos(i);
                        var lerpPos = (newPos - oldPos) * prop.Rope.SimulationTimeRemainder + oldPos;

                        Raylib.DrawCircleV(lerpPos * Level.TileSize, 2f, prop.PropInit.Rope!.PreviewColor);
                    }
                }
            }
        }
    }

    public void RenderGrid()
    {
        if (!ViewGrid) return;

        ReloadGridTexture();
        
        var levelW = editor.Level.Width;
        var levelH = editor.Level.Height;

        Raylib.DrawTexturePro(
            texture:    gridTexture,
            source:     new Rectangle(0, 0, gridTexture.Width * levelW / 2f, gridTexture.Height * levelH / 2f),
            dest:       new Rectangle(0, 0, Level.TileSize * levelW, Level.TileSize * levelH),
            origin:     Vector2.Zero,
            rotation:   0f,
            tint:       Color.White
        );
        
        /*var lineWidth = 0.5f / ViewZoom;
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);
        
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                var cellRect = new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize);
                Raylib.DrawRectangleLinesEx(
                    cellRect,
                    lineWidth,
                    new Color(255, 255, 255, 60)
                );
            }
        }

        // draw bigger grid squares
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x += 2)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y += 2)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize * 2, Level.TileSize * 2),
                    lineWidth,
                    new Color(255, 255, 255, 100)
                );
            }
        }*/
    }

    public void RenderBorder()
    {
        var lineWidth = 1f / ViewZoom;

        int borderRight = Level.Width - Level.BufferTilesRight;
        int borderBottom = Level.Height - Level.BufferTilesBot;
        int borderW = borderRight - Level.BufferTilesLeft;
        int borderH = borderBottom - Level.BufferTilesTop;

        Raylib.DrawRectangleLinesEx(
            new Rectangle(
                Level.BufferTilesLeft * Level.TileSize, Level.BufferTilesTop * Level.TileSize,
                borderW * Level.TileSize, borderH * Level.TileSize
            ),
            lineWidth,
            Color.White
        );

        // draw water height
        if (Level.HasWater)
        {
            float waterHeight = Level.WaterLevel + Level.BufferTilesBot + 0.5f;
            int waterDrawY = (int)((Level.Height - waterHeight) * Level.TileSize);
            Raylib.DrawLine(
                0, waterDrawY,
                Level.Width * Level.TileSize, waterDrawY,
                Color.Blue
            );
        }
    }
}