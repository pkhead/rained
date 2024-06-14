using Raylib_cs;
using System.Numerics;

namespace RainEd;
using CameraBorderModeOption = UserPreferences.CameraBorderModeOption;

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

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            vec3 color = mix(texelColor.rgb, vec3(1.0), glib_color.y);

            finalColor = vec4(color, (1.0 - float(isTransparent)) * glib_color.x) * glib_uColor;
        }
    ";

    private readonly RainEd editor;
    private Level Level { get => editor.Level; }

    public bool ViewGrid = true;
    public bool ViewObscuredBeams = false;
    public bool ViewTileHeads = false;
    public bool ViewCameras = false;

    public Vector2 ViewTopLeft;
    public Vector2 ViewBottomRight;
    public float ViewZoom = 1f;
    
    private readonly EditorGeometryRenderer geoRenderer;
    private readonly RlManaged.Shader propPreviewShader;
    public RlManaged.Shader PropPreviewShader { get => propPreviewShader; }

    private readonly RlManaged.Texture2D bigChainSegment;

    public LevelEditRender()
    {
        editor = RainEd.Instance;
        //ReloadGridTexture();
        
        propPreviewShader = RlManaged.Shader.LoadFromMemory(null, RWTransparencyShaderSrc);
        geoRenderer = new EditorGeometryRenderer(this);

        // TODO: this is actually unused for now
        using var chainSegmentImg = RlManaged.Image.Load(Path.Combine(Boot.AppDataPath, "assets", "internal", "Internal_144_bigChainSegment.png"));
        
        for (int x = 0; x < chainSegmentImg.Width; x++)
        {
            for (int y = 0; y < chainSegmentImg.Height; y++)
            {
                var col = Raylib.GetImageColor(chainSegmentImg, x, y);

                if (col.Equals(new Color(0, 0, 0, 255)))
                {
                    chainSegmentImg.DrawPixel(x, y, Color.White);
                }
                else
                {
                    chainSegmentImg.DrawPixel(x, y, new Color(255, 255, 255, 0));
                }
            }
        }

        bigChainSegment = RlManaged.Texture2D.LoadFromImage(chainSegmentImg);
    }

    // re-render the grid texture for the new zoom level
    // i have determined that this causes slowdown on higher zoom levels on the gpu side.
    // not sure why. i switched to rendering it in real-time using GL_LINES
    // (a.k.a. Raylib.DrawLine)
    /*
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
    */

    private bool IsInBorder(int x, int y)
    {
        return
            x >= Level.BufferTilesLeft && y >= Level.BufferTilesTop &&
            x < Level.Width - Level.BufferTilesRight && y < Level.Height - Level.BufferTilesBot;
    }

    // mark entire layer as dirty
    public void MarkNeedsRedraw(int layer)
    {
        geoRenderer.MarkNeedsRedraw(layer);
    }

    public void MarkNeedsRedraw(int x, int y, int layer)
    {
        geoRenderer.MarkNeedsRedraw(x, y, layer);
    }

    public void ReloadLevel()
    {
        geoRenderer.ReloadLevel();
    }

    public void RenderGeometry(int layer, Color color)
    {
        geoRenderer.Render(layer, color);
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

                // detached tile body
                // probably caused from comms move level tool,
                // which does not correct tile pointers
                if (tile == null)
                {
                    Raylib.DrawRectangleV(new Vector2(x, y) * Level.TileSize, Vector2.One * Level.TileSize, Color.Red);
                    Raylib.DrawRectangleV(new Vector2(x + 0.5f, y) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                    Raylib.DrawRectangleV(new Vector2(x, y + 0.5f) * Level.TileSize, Vector2.One * Level.TileSize / 2f, Color.Black);
                    continue;
                }

                var tileLeft = tx - tile.CenterX;
                var tileTop = ty - tile.CenterY;
                var previewTexture = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile);
                var col = previewTexture is null ? Color.White : tile.Category.Color;

                var srcRect = previewTexture is not null
                    ? new Rectangle((x - tileLeft) * 16, (y - tileTop) * 16, 16, 16)
                    : new Rectangle((x - tileLeft) * 2, (y - tileTop) * 2, 2, 2); 

                Raylib.DrawTexturePro(
                    previewTexture ?? RainEd.Instance.PlaceholderTexture,
                    srcRect,
                    new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize),
                    Vector2.Zero,
                    0f,
                    new Color(col.R, col.G, col.B, alpha)
                );

                // highlight tile head
                if (cell.TileHead is not null && ViewTileHeads)
                {
                    Raylib.DrawRectangle(
                        x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize,
                        new Color(col.R, col.G, col.B, (int)(alpha * 0.2f))  
                    );

                    Raylib.DrawLineV(
                        new Vector2(x, y) * Level.TileSize,
                        new Vector2(x+1, y+1) * Level.TileSize,
                        col
                    );

                    Raylib.DrawLineV(
                        new Vector2(x+1, y) * Level.TileSize,
                        new Vector2(x, y+1) * Level.TileSize,
                        col
                    );
                }
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

        // draw chains from chain holders
        foreach (var (k, v) in RainEd.Instance.Level.ChainData)
        {
            if (k.Item1 != layer) break;
            var cellPos = new Vector2(k.Item2, k.Item3);
            var chainEnd = new Vector2(v.X, v.Y);

            var tile = Level.Layers[layer, k.Item2, k.Item3].TileHead!;
            Raylib.DrawLineEx(
                (cellPos + Vector2.One) * Level.TileSize,
                (chainEnd + Vector2.One) * Level.TileSize,
                4f / ViewZoom,
                tile.Category.Color
            );
        }
    }

    public void RenderProps(int srcLayer, int alpha)
    {
        int srcDepth = srcLayer * 10;

        var rctx = RainEd.RenderContext;

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
            var propTexture = RainEd.Instance.AssetGraphics.GetPropTexture(prop.PropInit);
            var displayTexture = propTexture ?? RainEd.Instance.PlaceholderTexture;

            rctx.SetEnabled(Glib.Feature.CullFace, false);
            Raylib.BeginShaderMode(propPreviewShader);

            var variation = prop.Variation == -1 ? 0 : prop.Variation;

            var depthOffset = Math.Max(0, prop.DepthOffset - srcDepth);
        
            for (int depth = prop.PropInit.LayerCount - 1; depth >= 0; depth--)
            {
                float startFade =
                    (prop.PropInit.Type == Props.PropType.SimpleDecal || prop.PropInit.Type == Props.PropType.VariedDecal)
                    ? 0.364f : 0f;
                
                float whiteFade = Math.Clamp((1f - startFade) * ((depthOffset + depth / 2f) / 10f) + startFade, 0f, 1f);

                var srcRect = propTexture is null
                    ? new Rectangle(Vector2.Zero, 2.0f * Vector2.One)
                    : prop.PropInit.GetPreviewRectangle(variation, depth);

                rctx.DrawColor = new Glib.Color(alpha / 255f, whiteFade, 0f, 0f);

                {
                    using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, displayTexture.GlibTexture);
                        
                    // top-left
                    batch.TexCoord(srcRect.X / displayTexture.Width, srcRect.Y / displayTexture.Height);
                    batch.Vertex(quad[0].X * Level.TileSize, quad[0].Y * Level.TileSize);

                    // bottom-left
                    batch.TexCoord(srcRect.X / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                    batch.Vertex(quad[3].X * Level.TileSize, quad[3].Y * Level.TileSize);

                    // bottom-right
                    batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                    batch.Vertex(quad[2].X * Level.TileSize, quad[2].Y * Level.TileSize);

                    batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, srcRect.Y / displayTexture.Height);
                    batch.Vertex(quad[1].X * Level.TileSize, quad[1].Y * Level.TileSize);
                }
            }

            Raylib.EndShaderMode();
            rctx.SetEnabled(Glib.Feature.CullFace, true);

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

        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);
        
        var col = new Color(255, 255, 255, 50);
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                var cellRect = new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize, Level.TileSize);
                Raylib.DrawLineV(cellRect.Position, cellRect.Position + new Vector2(cellRect.Size.X, 0f), col);
                Raylib.DrawLineV(cellRect.Position, cellRect.Position + new Vector2(0f, cellRect.Size.Y), col);
            }
        }

        // draw bigger grid squares
        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x += 2)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y += 2)
            {
                var cellRect = new Rectangle(x * Level.TileSize, y * Level.TileSize, Level.TileSize * 2, Level.TileSize * 2);
                Raylib.DrawLineV(cellRect.Position, cellRect.Position + new Vector2(cellRect.Size.X, 0f), col);
                Raylib.DrawLineV(cellRect.Position, cellRect.Position + new Vector2(0f, cellRect.Size.Y), col);
            }
        }
    }

    public void RenderCameraBorders()
    {
        if (!ViewCameras) return;

        var camBorderMode = RainEd.Instance.Preferences.CameraBorderMode;
        bool both = camBorderMode == CameraBorderModeOption.Both;
        bool showWidescreen = camBorderMode == CameraBorderModeOption.Widescreen || both;
        bool showStandard = camBorderMode == CameraBorderModeOption.Standard || both;

        foreach (var camera in Level.Cameras)
        {
            var camCenter = camera.Position + Camera.WidescreenSize / 2f;

            // draw full rect ouline
            if (showWidescreen)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        camera.Position * Level.TileSize,
                        Camera.WidescreenSize * Level.TileSize
                    ),
                    2f / ViewZoom,
                    new Color(0, 255, 0, 255)       
                );
            }

            // 4:3 outline
            if (showStandard)
            {
                var standardResOutlineSize = Camera.StandardSize * ((Camera.WidescreenSize.X - 2) / Camera.WidescreenSize.X);
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        (camCenter - standardResOutlineSize / 2) * Level.TileSize,
                        standardResOutlineSize * Level.TileSize
                    ),
                    (both ? 1f : 2f) / ViewZoom,
                    new Color(0, 255, 0, 255)
                );
            }
        }
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