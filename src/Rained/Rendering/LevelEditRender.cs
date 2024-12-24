using Raylib_cs;
using System.Numerics;
using Rained.EditorGui;
using Rained.LevelData;
using Rained.Assets;
using ImGuiNET;
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
    private readonly PropRenderer propRenderer;

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
        propRenderer = new PropRenderer(this);

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
    
    static readonly Glib.Color[] NodeColors = [
        // exit
        Glib.Color.FromRGBA(200, 200, 200),

        // den
        Glib.Color.FromRGBA(255, 0, 255),

        // region transportation
        Glib.Color.FromRGBA(52, 50, 52),

        // side exit
        Glib.Color.FromRGBA(128, 216, 128),

        // sky exit
        Glib.Color.FromRGBA(52, 216, 255),

        // sea exit
        Glib.Color.FromRGBA(0, 0, 255),

        // batfly hive
        Glib.Color.FromRGBA(0, 255, 0),

        // garbage worm
        Glib.Color.FromRGBA(255, 128, 0)
    ];

    public void RenderNodes(Color color)
    {
        var rctx = RainEd.RenderContext!;
        var idx = 0;
        var filter = RainEd.Instance.Preferences.NodeViewFilter.Flags;

        foreach (var (nodePos, nodeType) in RainEd.Instance.CurrentTab!.NodeData.Nodes)
        {
            if (!filter[(int)nodeType]) continue;
            
            var text = idx.ToString();
            var pos = new Vector2(nodePos.X + 0.5f, nodePos.Y + 0.5f);

            rctx.DrawColor = NodeColors[(int)nodeType];
            var txtSize = TextRendering.CalcOutlinedTextSize(text);
            var scale = 2f / ViewZoom;
            TextRendering.DrawTextOutlined(
                text: text,
                offset: pos * Level.TileSize - txtSize / 2f * scale,
                scale: new Vector2(scale, scale)
            );

            idx++;
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
                            Level.IsInBorder(x, y) ? color : new Color(255, 0, 0, 255)
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
        propRenderer.RenderLayer(srcLayer, alpha);
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
            var camCenter = camera.Position + Camera.Size / 2f;

            // draw 16:9 ouline
            if (showWidescreen)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        (camCenter - Camera.WidescreenSize / 2f) * Level.TileSize,
                        Camera.WidescreenSize * Level.TileSize
                    ),
                    2f / ViewZoom,
                    new Color(0, 255, 0, 255)       
                );
            }

            // 4:3 outline
            if (showStandard)
            {
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(
                        (camCenter - Camera.StandardSize / 2f) * Level.TileSize,
                        Camera.StandardSize * Level.TileSize
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
    
    private void FillWater()
    {
        var level = RainEd.Instance.Level;

        float waterHeight = level.WaterLevel + level.BufferTilesBot + 0.5f;
        Raylib.DrawRectangle(
            0,
            (int)((level.Height - waterHeight) * Level.TileSize),
            level.Width * Level.TileSize,
            (int)(waterHeight * Level.TileSize),
            new Color(0, 0, 255, 100)
        );
    }

    /// <summary>
    /// Render level into single framebuffer.
    /// </summary>
    public void RenderLevel(LevelRenderConfig config)
    {
        var level = RainEd.Instance.Level;

        // draw level background
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, LevelWindow.BackgroundColor);
        
        // draw the layers
        var drawTiles = config.DrawTiles || RainEd.Instance.Preferences.ViewTiles;
        var drawProps = config.DrawProps || RainEd.Instance.Preferences.ViewProps;

        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            var alpha = l == config.ActiveLayer ? 255 : 50;
            var color = LevelWindow.GeoColor(config.Fade, alpha);
            int offset = (l - config.ActiveLayer) * config.LayerOffset;

            Rlgl.PushMatrix();
            Rlgl.Translatef(offset, offset, 0f);
            RenderGeometry(l, color);

            if (drawTiles)
                RenderTiles(l, (int)(alpha * (100.0f / 255.0f)));
            
            if (drawProps)
                RenderProps(l, (int)(alpha * (100.0f / 255.0f)));
            
            if (config.DrawObjects)
                RenderObjects(l, new Color(255, 255, 255, alpha));
            
            Rlgl.PopMatrix();

            // draw water behind first layer if set
            if (config.FillWater && l == 1 && level.HasWater && !level.IsWaterInFront)
                FillWater();
        }

        // draw water
        if (config.FillWater && level.HasWater && level.IsWaterInFront)
            FillWater();
    }

    /// <summary>
    /// Render level into multiple framebuffers, and then composite it into the main one.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="mainFrame"></param>
    /// <param name="layerFrames"></param>
    public void RenderLevelComposite(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames, LevelRenderConfig config)
    {
        var level = RainEd.Instance.Level;
        var window = RainEd.Instance.LevelView;

        // draw level background
        Raylib.DrawRectangle(0, 0, level.Width * Level.TileSize, level.Height * Level.TileSize, LevelWindow.BackgroundColor);
        
        // draw the layers
        var drawTiles = config.DrawTiles || RainEd.Instance.Preferences.ViewTiles;
        var drawProps = config.DrawProps || RainEd.Instance.Preferences.ViewProps;
        var propAlpha = config.DrawProps ? 255 : 100;

        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            // draw layer into framebuffer
            int offset = (l - config.ActiveLayer) * config.LayerOffset;
            Raylib.BeginTextureMode(layerFrames[l]);

            if (config.Scissor) Raylib.EndScissorMode();
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            if (config.Scissor) window.BeginLevelScissorMode();

            Rlgl.PushMatrix();
                Rlgl.Translatef(offset, offset, 0f);
                RenderGeometry(l, LevelWindow.GeoColor(config.Fade, 255));

                // if drawTiles was explicitly set, they are drawn opaque.
                // but if it was not set, and is being drawn because of the view setting,
                // they are partially transparent.
                if (drawTiles)
                    RenderTiles(l, config.DrawTiles ? 255 : 100);

                // same goes for props.
                if (drawProps && !config.DrawPropsInFront)
                    RenderProps(l, propAlpha);
                
                if (config.DrawObjects)
                    RenderObjects(l, new Color(255, 255, 255, 255));
                
            Rlgl.PopMatrix();
        }

        // draw alpha-blended result into main frame
        Raylib.BeginTextureMode(mainFrame);
        for (int l = Level.LayerCount-1; l >= 0; l--)
        {
            Rlgl.PushMatrix();
            Rlgl.LoadIdentity();

            var alpha = l == config.ActiveLayer ? 255 : 50;
            RlExt.DrawRenderTexture(layerFrames[l], 0, 0, new Color(255, 255, 255, alpha));

            // draw water behind first layer if set
            if (config.FillWater && l == 1 && level.HasWater && !level.IsWaterInFront)
                FillWater();
            
            Rlgl.PopMatrix();
        }

        // draw props in front of geo
        if (drawProps && config.DrawPropsInFront)
        {
            for (int l = Level.LayerCount-1; l >= 0; l--)
            {
                int offset = (l - config.ActiveLayer) * config.LayerOffset;

                // draw props into layer's framebuffer
                Raylib.BeginTextureMode(layerFrames[l]);
                Raylib.ClearBackground(Color.Blank);

                Rlgl.PushMatrix();
                Rlgl.Translatef(offset, offset, 0);
                RenderProps(l, propAlpha);
                Rlgl.PopMatrix();
            }

            for (int l = Level.LayerCount-1; l >= 0; l--)
            {
                // draw alpha-blended results into main frame
                Raylib.BeginTextureMode(mainFrame);
                Rlgl.PushMatrix();
                    Rlgl.LoadIdentity();

                    var alpha = l == config.ActiveLayer ? 255 : 50;
                    RlExt.DrawRenderTexture(layerFrames[l], 0, 0, new Color(255, 255, 255, alpha));
                Rlgl.PopMatrix();
            }
        }

        // draw water
        if (config.FillWater && level.HasWater && level.IsWaterInFront)
            FillWater();
    }
    
    public void Dispose()
    {
        Palette.Dispose();
    }
}