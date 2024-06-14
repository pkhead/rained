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

    // the shader used for prop rendering in the editor.
    // white pixels are transparent
    // the R color component controls transparency and the G color component controls white blend 
    private readonly static string PropShaderSrc = @"
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

    // the shader used for tile rendering in the editor.
    // while pixels are transparent.
    private readonly static string TileShaderSrc = @"
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
            bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
            bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
            bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
            bool isShaded = isLight || isShade || isNormal;

            float light = float(isLight) * 1.0 + float(isShade) * 0.4 + float(isNormal) * 0.8;
            vec3 shadedCol = glib_color.rgb * light;

            finalColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent)) * glib_color.a) * glib_uColor;
        }
    ";

    private readonly static string PaletteShaderSrc = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        uniform vec3[30] litColor;
        uniform vec3[30] neutralColor;
        uniform vec3[30] shadedColor; 

        out vec4 finalColor;

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);

            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
            bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
            bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
            bool isShaded = isLight || isShade || isNormal;

            int colIndex = int(glib_color.r * 29.0);
            vec3 shadedCol = float(isLight) * litColor[colIndex] + float(isShade) * shadedColor[colIndex] + float(isNormal) * neutralColor[colIndex];

            finalColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent)) * glib_color.a) * glib_uColor;
        }
    ";

    private const string GridVertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        uniform mat4 glib_uMatrix;

        void main()
        {
            gl_Position = glib_uMatrix * vec4(aPos.xyz, 1.0);
        }
    ";

    private const string GridFragmentShaderSource = @"
        #version 330 core

        out vec4 fragColor;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        void main()
        {
            fragColor = texture(glib_uTexture, vec2(0.0, 0.0)) * glib_uColor;
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
    private readonly TileRenderer tileRenderer;
    private readonly RlManaged.Shader propPreviewShader;

    public RlManaged.Shader PropPreviewShader { get => propPreviewShader; }
    public readonly RlManaged.Shader TilePreviewShader;
    public readonly RlManaged.Shader PaletteShader;
    
    private readonly Glib.Shader gridShader;
    private Glib.Mesh? gridMajor = null;
    private Glib.Mesh? gridMinor = null;
    private int gridWidth = 0;
    private int gridHeight = 0;

    private readonly RlManaged.Texture2D bigChainSegment;

    public int Palette = 0;
    public int FadePalette = -1;
    public float PaletteMix = 0f;
    public readonly Palette[] Palettes;

    public LevelEditRender()
    {
        editor = RainEd.Instance;
        //ReloadGridTexture();

        // load palettes
        var palettes = new List<Palette>();
        for (int i = 0;; i++)
        {
            var filePath = Path.Combine(Boot.AppDataPath, "assets", "palettes", "palette" + i + ".png");
            if (!File.Exists(filePath)) break;
            palettes.Add(new Palette(filePath));
        }
        Palettes = [..palettes];
        
        // load graphic shaders
        propPreviewShader = RlManaged.Shader.LoadFromMemory(null, PropShaderSrc);
        TilePreviewShader = RlManaged.Shader.LoadFromMemory(null, TileShaderSrc);
        PaletteShader = RlManaged.Shader.LoadFromMemory(null, PaletteShaderSrc);
        gridShader = RainEd.RenderContext!.CreateShader(GridVertexShaderSource, GridFragmentShaderSource);

        geoRenderer = new EditorGeometryRenderer(this);
        tileRenderer = new TileRenderer(this);

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
    public void InvalidateGeo(int layer)
    {
        geoRenderer.MarkNeedsRedraw(layer);
    }

    public void InvalidateGeo(int x, int y, int layer)
    {
        geoRenderer.MarkNeedsRedraw(x, y, layer);
    }

    public void InvalidateTileHead(int x, int y, int layer)
    {
        tileRenderer.Invalidate(x, y, layer);
    }

    public void ReloadLevel()
    {
        geoRenderer.ReloadLevel();
        tileRenderer.ReloadLevel();
    }

    #region Palettes
    private static float Lerp(float x, float y, float a)
    {
        return (y - x) * a + x;
    }

    public Color GetSunColor(PaletteLightLevel lightLevel, int sublayer, int index)
    {
        var p = Palettes[index].SunPalette;
        return lightLevel switch
        {
            PaletteLightLevel.Lit => p[sublayer].Lit,
            PaletteLightLevel.Neutral => p[sublayer].Neutral,
            PaletteLightLevel.Shaded => p[sublayer].Shaded,
            _ => new Color(0, 0, 0, 0)
        };
    }

    public Color GetPaletteColor(PaletteColor colorName, int index)
    {
        var p = Palettes[index];
        return colorName switch
        {
            PaletteColor.Sky => p.SkyColor,
            PaletteColor.Fog => p.FogColor,
            PaletteColor.Black => p.BlackColor,
            PaletteColor.ShortcutSymbol => p.ShortcutSymbolColor,
            _ => throw new ArgumentOutOfRangeException(nameof(colorName))
        };
    }

    public Color GetSunColorMix(PaletteLightLevel lightLevel, int sublayer, int index1, int index2, float mix)
    {
        var c1 = GetSunColor(lightLevel, sublayer, index1);
        var c2 = GetSunColor(lightLevel, sublayer, index2);

        return new Color(
            (byte) Lerp(c1.R, c2.R, mix),
            (byte) Lerp(c1.G, c2.G, mix),
            (byte) Lerp(c1.B, c2.B, mix),
            (byte) Lerp(c1.A, c2.A, mix)
        );
    }

    public Color GetPaletteColorMix(PaletteColor colorName, int index1, int index2, float mix)
    {
        var c1 = GetPaletteColor(colorName, index1);
        var c2 = GetPaletteColor(colorName, index2);

        return new Color(
            (byte) Lerp(c1.R, c2.R, mix),
            (byte) Lerp(c1.G, c2.G, mix),
            (byte) Lerp(c1.B, c2.B, mix),
            (byte) Lerp(c1.A, c2.A, mix)
        );
    }

    public Color GetSunColor(PaletteLightLevel lightLevel, int sublayer)
    {
        if (Palette == -1) return new Color(0, 0, 0, 0);
        return GetSunColorMix(lightLevel, sublayer, Palette, FadePalette, PaletteMix);
    }

    public Color GetPaletteColor(PaletteColor colorName)
    {
        if (Palette == -1) return new Color(0, 0, 0, 0);
        return GetPaletteColorMix(colorName, Palette, FadePalette, PaletteMix);
    }
    #endregion

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

        if (RainEd.Instance.Preferences.ViewPreviews)
        {
            tileRenderer.Render(layer, alpha);
        }
        else
        {
            tileRenderer.PreviewRender(layer, alpha);
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

        var rctx = RainEd.RenderContext!;

        // recreate grid mesh if it needs updating
        if (gridMajor is null || gridMinor is null || Level.Width != gridWidth || Level.Height != gridHeight)
        {
            var meshConfig = new Glib.MeshConfiguration([Glib.DataType.Vector3], true)
            {
                PrimitiveType = Glib.MeshPrimitiveType.Lines
            };

            gridWidth = Level.Width;
            gridHeight = Level.Height;

            gridMajor?.Dispose();
            gridMinor?.Dispose();
            
            gridMajor = rctx.CreateMesh(meshConfig);
            gridMinor = rctx.CreateMesh(meshConfig);
            
            // create minor grid lines
            var vertices = new List<Vector3>();
            var indices = new List<int>();

            int meshIndex = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    vertices.Add(new Vector3(x, y, 0f) * Level.TileSize);
                    vertices.Add(new Vector3(x+1, y, 0f) * Level.TileSize);
                    vertices.Add(new Vector3(x, y+1, 0f) * Level.TileSize);

                    indices.Add(meshIndex);
                    indices.Add(meshIndex + 1);
                    indices.Add(meshIndex);
                    indices.Add(meshIndex + 2);

                    meshIndex += 3;
                }
            }

            gridMinor.SetBufferData(0, [..vertices]);
            gridMinor.SetIndexBufferData([..indices]);
            gridMinor.Upload();

            // create major grid lines
            vertices.Clear();
            indices.Clear();
            meshIndex = 0;

            for (int x = 0; x < gridWidth; x += 2)
            {
                for (int y = 0; y < gridHeight; y += 2)
                {
                    vertices.Add(new Vector3(x, y, 0f) * Level.TileSize);
                    vertices.Add(new Vector3(x+2, y, 0f) * Level.TileSize);
                    vertices.Add(new Vector3(x, y+2, 0f) * Level.TileSize);

                    indices.Add(meshIndex);
                    indices.Add(meshIndex + 1);
                    indices.Add(meshIndex);
                    indices.Add(meshIndex + 2);

                    meshIndex += 3;
                }
            }

            gridMajor.SetBufferData(0, [..vertices]);
            gridMajor.SetIndexBufferData([..indices]);
            gridMajor.Upload();
        }

        // draw the meshes
        rctx.Shader = gridShader;
        rctx.DrawColor = Glib.Color.FromRGBA(255, 255, 255, 50);
        rctx.Draw(gridMinor);
        rctx.Draw(gridMajor);
        rctx.Shader = null;
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