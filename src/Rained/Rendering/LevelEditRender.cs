using Raylib_cs;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Rained.LevelData;
using Rained.Assets;
namespace Rained.Rendering;
using CameraBorderModeOption = UserPreferences.CameraBorderModeOption;

class LevelEditRender : IDisposable
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

    // shortcut objects that can be used to make a valid shortcut connection
    // seems to just be everything except a garbage worm spawn
    private static readonly LevelObject[] ConnectableShortcutObjects = new[] {
        LevelObject.Shortcut, LevelObject.CreatureDen, LevelObject.Entrance,
        LevelObject.WhackAMoleHole, LevelObject.ScavengerHole
    };

    private readonly RainEd editor;
    private Level Level { get => editor.Level; }

    public Vector2 ViewTopLeft;
    public Vector2 ViewBottomRight;
    public float ViewZoom = 1f;
    
    private readonly EditorGeometryRenderer geoRenderer;
    private readonly TileRenderer tileRenderer;

    private Glib.Mesh? gridMajor = null;
    private Glib.Mesh? gridMinor = null;
    private int gridWidth = 0;
    private int gridHeight = 0;

    private readonly RlManaged.Texture2D bigChainSegment;

    public bool UsePalette = false;
    public PaletteRenderer Palette;

    public LevelEditRender()
    {
        editor = RainEd.Instance;

        // load palettes
        Palette = new PaletteRenderer();

        geoRenderer = new EditorGeometryRenderer(this);
        tileRenderer = new TileRenderer(this);

        // TODO: this is actually unused for now
        using var chainSegmentImg = RlManaged.Image.Load(Path.Combine(DrizzleCast.DirectoryPath, "Internal_144_bigChainSegment.png"));
        
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

    public void RenderGeometry(int layer, Color color)
    {
        geoRenderer.Render(layer, color);
    }

    public void RenderObjects(int layer, Color color)
    {
        int viewL = (int) Math.Floor(ViewTopLeft.X);
        int viewT = (int) Math.Floor(ViewTopLeft.Y);
        int viewR = (int) Math.Ceiling(ViewBottomRight.X);
        int viewB = (int) Math.Ceiling(ViewBottomRight.Y);

        for (int x = Math.Max(0, viewL); x < Math.Min(Level.Width, viewR); x++)
        {
            for (int y = Math.Max(0, viewT); y < Math.Min(Level.Height, viewB); y++)
            {
                ref var cell = ref Level.Layers[layer, x, y];

                // draw object graphics
                for (int i = 1; i < 32; i++)
                {
                    LevelObject objType = (LevelObject) (1 << (i-1));
                    if (cell.Has(objType) && !Level.ShortcutObjects.Contains(objType) && ObjectTextureOffsets.TryGetValue(objType, out Vector2 offset))
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
                foreach (LevelObject objType in Level.ShortcutObjects)
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
        rctx.CullMode = Glib.CullMode.None;

        bool renderPalette;

        // palette rendering mode
        if (UsePalette)
        {
            renderPalette = true;
            Palette.UpdateTexture();
        }

        // normal rendering mode
        else
        {
            renderPalette = false;
        }

        Span<Vector2> transformQuads = stackalloc Vector2[4];
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
            var variation = prop.Variation == -1 ? 0 : prop.Variation;
            var depthOffset = Math.Max(0, prop.DepthOffset - srcDepth);
        
            // draw missing texture if needed
            if (propTexture is null)
            {
                rctx.Shader = null;
                var srcRect = new Rectangle(Vector2.Zero, 2.0f * Vector2.One);

                using var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, displayTexture.GlibTexture);
                        
                // top-left
                batch.TexCoord(srcRect.X / displayTexture.Width, srcRect.Y / displayTexture.Height);
                batch.Vertex(quad[0] * Level.TileSize);

                // bottom-left
                batch.TexCoord(srcRect.X / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                batch.Vertex(quad[3] * Level.TileSize);

                // bottom-right
                batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                batch.Vertex(quad[2] * Level.TileSize);

                // top-right
                batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, srcRect.Y / displayTexture.Height);
                batch.Vertex(quad[1] * Level.TileSize);
            }
            else
            {
                var isStdProp = prop.PropInit.Type is PropType.Standard or PropType.VariedStandard;

                if (renderPalette)
                {
                    if (isStdProp && prop.PropInit.ColorTreatment == PropColorTreatment.Standard)
                    {
                        rctx.Shader = Shaders.PaletteShader.GlibShader;
                    }
                    else if (isStdProp && prop.PropInit.ColorTreatment == PropColorTreatment.Bevel)
                    {
                        rctx.Shader = Shaders.BevelTreatmentShader.GlibShader;
                    }
                    else if (prop.PropInit.SoftPropRender is not null)
                    {
                        rctx.Shader = Shaders.SoftPropShader.GlibShader;

                        var softProp = prop.PropInit.SoftPropRender.Value;

                        // i don't really know how these options work...
                        float highlightThreshold = 0.666f;
                        float shadowThreshold = 0.333f;
                        rctx.Shader.SetUniform("v4_softPropShadeInfo", new Vector4(
                            softProp.ContourExponent,
                            highlightThreshold,
                            shadowThreshold,
                            prop.CustomDepth
                        ));
                    }
                    else
                    {
                        rctx.Shader = Shaders.PropShader.GlibShader;
                    }
                }
                else
                {
                    rctx.Shader = Shaders.PropShader.GlibShader;
                }

                if (rctx.Shader != Shaders.PropShader.GlibShader)
                {
                    if (rctx.Shader.HasUniform("u_paletteTex"))
                        rctx.Shader.SetUniform("u_paletteTex", Palette.Texture);
                    if (rctx.Shader.HasUniform("v4_textureSize"))
                        rctx.Shader.SetUniform("v4_textureSize", new Vector4(displayTexture.Width, displayTexture.Height, 0f, 0f));
                    
                    if (rctx.Shader.HasUniform("v4_bevelData"))
                    {
                        rctx.Shader.SetUniform("v4_bevelData", new Vector4(prop.PropInit.Bevel, 0f, 0f, 0f));
                    }
                    
                    if (rctx.Shader.HasUniform("v4_lightDirection"))
                    {
                        var correctedAngle = Level.LightAngle + MathF.PI / 2f;
                        var lightDist = 1f - Level.LightDistance / 10f;
                        var lightZ = lightDist * (3.0f - 0.5f) + 0.5f; // an approximation
                        rctx.Shader.SetUniform("v4_lightDirection", new Vector4(MathF.Cos(correctedAngle), MathF.Sin(correctedAngle), lightZ, 0f));
                    }
                    
                    if (rctx.Shader.HasUniform("v4_propRotation"))
                    {
                        var right = Vector2.Normalize(quad[1] - quad[0]);
                        var up = Vector2.Normalize(quad[3] - quad[0]);
                        rctx.Shader.SetUniform("v4_propRotation", new Vector4(right.X, right.Y, up.X, up.Y));
                    }

                    rctx.DrawBatch(); // force flush batch, as uniform changes aren't detected
                }

                // draw each sublayer of the prop
                for (int depth = prop.PropInit.LayerCount - 1; depth >= 0; depth--)
                {
                    float startFade =
                        (prop.PropInit.Type is PropType.SimpleDecal or PropType.VariedDecal)
                        ? 0.364f : 0f;
                    
                    float whiteFade = Math.Clamp((1f - startFade) * ((depthOffset + depth / 2f) / 10f) + startFade, 0f, 1f);
                    var srcRect = prop.PropInit.GetPreviewRectangle(variation, depth);

                    if (renderPalette && rctx.Shader != Shaders.PropShader.GlibShader)
                    {
                        // R channel represents sublayer
                        // A channel is alpha, as usual
                        float sublayer = (float)depth / prop.PropInit.LayerCount * prop.PropInit.Depth + prop.DepthOffset;
                        rctx.DrawColor = new Glib.Color(Math.Clamp(sublayer / 29f, 0f, 1f), 0f, 0f, 1f);
                    }
                    else
                    {
                        rctx.DrawColor = new Glib.Color(alpha / 255f, whiteFade, 0f, 0f);
                    }
                    
                    using (var batch = rctx.BeginBatchDraw(Glib.BatchDrawMode.Quads, displayTexture.GlibTexture))
                    {
                        transformQuads[0] = quad[0] * Level.TileSize;
                        transformQuads[1] = quad[1] * Level.TileSize;
                        transformQuads[2] = quad[2] * Level.TileSize;
                        transformQuads[3] = quad[3] * Level.TileSize;

                        if (prop.IsAffine)
                        {
                            // top-left
                            batch.TexCoord(srcRect.X / displayTexture.Width, srcRect.Y / displayTexture.Height);
                            batch.Vertex(transformQuads[0]);

                            // bottom-left
                            batch.TexCoord(srcRect.X / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                            batch.Vertex(transformQuads[3]);

                            // bottom-right
                            batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, (srcRect.Y + srcRect.Height) / displayTexture.Height);
                            batch.Vertex(transformQuads[2]);

                            // top right
                            batch.TexCoord((srcRect.X + srcRect.Width) / displayTexture.Width, srcRect.Y / displayTexture.Height);
                            batch.Vertex(transformQuads[1]);
                        }
                        else
                        {
                            DrawDeformedMesh(batch, transformQuads, new Rectangle(
                                srcRect.X / displayTexture.Width,
                                srcRect.Y / displayTexture.Height,
                                srcRect.Width / displayTexture.Width,
                                srcRect.Height / displayTexture.Height)
                            );
                        };
                    }
                }
            }

            rctx.Shader = null;
            rctx.CullMode = Glib.CullMode.None;

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
        
        rctx.Shader = null;
    }

    private static void DrawDeformedMesh(Glib.BatchDrawHandle batch, ReadOnlySpan<Vector2> quad, Rectangle uvRect)
    {
        const float uStep = 1.0f / 6.0f;
        const float vStep = 1.0f / 6.0f;
        float nextU;
        float nextV;

        for (float u = 0f; u < 1f; u += uStep)
        {
            nextU = Math.Min(u + uStep, 1f);

            Vector2 uPos0 = Vector2.Lerp(quad[0], quad[1], u);
            Vector2 uPos1 = Vector2.Lerp(quad[3], quad[2], u);
            Vector2 nextUPos0 = Vector2.Lerp(quad[0], quad[1], nextU);
            Vector2 nextUPos1 = Vector2.Lerp(quad[3], quad[2], nextU);

            for (float v = 0f; v < 1f; v += vStep)
            {
                nextV = Math.Min(v + vStep, 1f);

                Vector2 vPos0 = Vector2.Lerp(uPos0, uPos1, v);
                Vector2 vPos1 = Vector2.Lerp(uPos0, uPos1, nextV);
                Vector2 vPos2 = Vector2.Lerp(nextUPos0, nextUPos1, nextV);
                Vector2 vPos3 = Vector2.Lerp(nextUPos0, nextUPos1, v);

                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(u, v));
                batch.Vertex(vPos0);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(u, nextV));
                batch.Vertex(vPos1);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, nextV));
                batch.Vertex(vPos2);
                batch.TexCoord(uvRect.Position + uvRect.Size * new Vector2(nextU, v));
                batch.Vertex(vPos3);
            }
        }
    }

    public void RenderGrid()
    {
        if (!RainEd.Instance.Preferences.ViewGrid) return;

        var rctx = RainEd.RenderContext!;

        // recreate grid mesh if it needs updating
        if (gridMajor is null || gridMinor is null || Level.Width != gridWidth || Level.Height != gridHeight)
        {
            var meshConfig = new Glib.MeshConfiguration()
                .AddBuffer(Glib.AttributeName.Position, Glib.DataType.Float, 3, Glib.MeshBufferUsage.Static)
                .SetIndexed(true)
                .SetPrimitiveType(Glib.MeshPrimitiveType.Lines);

            gridWidth = Level.Width;
            gridHeight = Level.Height;

            gridMajor?.Dispose();
            gridMinor?.Dispose();
                        
            // create minor grid lines
            var vertices = new List<Vector3>();
            var indices = new List<uint>();

            uint meshIndex = 0;
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

            gridMinor = Glib.Mesh.Create(meshConfig, [..indices], vertices.Count);
            gridMinor.SetBufferData(0, [..vertices]);
            if (indices.Count > 0)
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

            gridMajor = Glib.Mesh.Create(meshConfig, [..indices], vertices.Count);
            gridMajor.SetBufferData(0, [..vertices]);
            if (indices.Count > 0)
                gridMajor.Upload();
        }

        // draw the meshes
        // opacity decreases as user zooms out. value is sqrt'd to
        // make it fall off at a better rate.
        var opacity = MathF.Sqrt(Math.Clamp(ViewZoom, 0f, 1f));
        rctx.Shader = Shaders.GridShader.GlibShader;
        rctx.DrawColor = new Glib.Color(1f, 1f, 1f, opacity * (50f/255f));
        if (gridMinor.GetIndexVertexCount() > 0) rctx.Draw(gridMinor);
        if (gridMajor.GetIndexVertexCount() > 0) rctx.Draw(gridMajor);
        rctx.Shader = null;
    }

    public void RenderCameraBorders()
    {
        if (!RainEd.Instance.Preferences.ViewCameras) return;

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

    public void Dispose()
    {
        Palette.Dispose();
    }
}