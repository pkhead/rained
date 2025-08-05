using Raylib_cs;
using System.Numerics;
using Rained.EditorGui;
using Rained.Assets;
using System.Diagnostics;
namespace Rained.LevelData;

struct BrushAtom
{
    /// <summary>
    /// In degrees. Thanks, Raylib.
    /// </summary>
    public float rotation;
    public Rectangle rect;
    public int brush;
    public bool mode;
}

class LightMap : IDisposable
{
    private static bool UseHardwareAcceleration => RainEd.Instance is not null && (width < Glib.Texture.MaxSize && height < Glib.Texture.MaxSize);

    private RlManaged.RenderTexture2D? lightmapRt;
    private RlManaged.Image? _lightmapImage;
    private int width;
    private int height;

    public int Width { get => width; }
    public int Height { get => height; }
    public Texture2D? Texture { get => lightmapRt?.Texture; }
    public RlManaged.RenderTexture2D? RenderTexture { get => lightmapRt; }
    public bool IsLoaded => lightmapRt is not null;

    public LightMap(int levelWidth, int levelHeight)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        // create light map render texture
        if (UseHardwareAcceleration)
        {
            if (width >= Glib.Texture.MaxSize || height >= Glib.Texture.MaxSize)
            {
                Log.UserLogger.Error("Lightmap too large to be loaded.");
                lightmapRt = null;
                return;
            }

            lightmapRt = RlManaged.RenderTexture2D.Load(width, height);
            using var image = Glib.Image.FromColor(width, height, Glib.Color.Black);
            lightmapRt.Texture.ID!.UpdateFromImage(image);
        }
        else
        {
            var glibImage = Glib.Image.FromColor(width, height, Glib.Color.White, Glib.PixelFormat.Grayscale);
            _lightmapImage = new RlManaged.Image(new Image() { image = glibImage });
        }
    }

    public LightMap(int levelWidth, int levelHeight, Image lightMapImage)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        using var croppedLightMap = RlManaged.Image.Copy(lightMapImage);
        AssetGraphicsProvider.CropImage(croppedLightMap);

        var finalImage = RlManaged.Image.GenColor(width, height, Color.White);

        if (croppedLightMap.Width != width || croppedLightMap.Height != height)
        {
            Log.Information("Cropped light map. To fix, add a black pixel to the top-left and bottom-right pixels of the image.");
            EditorWindow.ShowNotification("Cropped light map");
        }

        var subWidth = croppedLightMap.Width;
        var subHeight = croppedLightMap.Height;
        Raylib.ImageDraw(
            dst: finalImage,
            src: croppedLightMap,
            srcRec: new Rectangle(0, 0, subWidth, subHeight),
            dstRec: new Rectangle((width - subWidth) / 2f, (height - subHeight) / 2f, subWidth, subHeight),
            tint: Color.White
        );

        // get light map as a texture
        if (UseHardwareAcceleration)
        {
            using var lightmapTex = RlManaged.Texture2D.LoadFromImage(finalImage);
            finalImage.Dispose();

            // put into a render texture
            if (width >= Glib.Texture.MaxSize || height >= Glib.Texture.MaxSize)
            {
                Log.UserLogger.Error("Lightmap too large to be loaded.");
                lightmapRt = null;
                return;
            }

            lightmapRt = RlManaged.RenderTexture2D.Load(width, height);
            Raylib.BeginTextureMode(lightmapRt);
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexture(lightmapTex, 0, 0, Color.White);
            Raylib.EndTextureMode();
        }
        else
        {
            _lightmapImage = finalImage;
        }
    }

    public void Dispose()
    {
        if (lightmapRt is not null)
        {
            lightmapRt.Dispose();
            lightmapRt = null!;
        }

        if (_lightmapImage is not null)
        {
            _lightmapImage.Dispose();
            _lightmapImage = null;
        }
    }

    public void Resize(int newWidth, int newHeight, int dstOriginX, int dstOriginY)
    {
        // convert from cell to lightmap coordinates
        newWidth = newWidth * 20 + 300;
        newHeight = newHeight * 20 + 300;
        dstOriginX *= 20;
        dstOriginY *= 20;

        if (UseHardwareAcceleration)
        {
            if (newWidth >= Glib.Texture.MaxSize || newHeight >= Glib.Texture.MaxSize)
            {
                Log.UserLogger.Error("Lightmap too large to be loaded.");
                lightmapRt = null;
            }
            else
            {
                using var lightMapImage = GetImage();
                
                // vertical flip dest rect (idk why i need to do this it worked before)
                //dstOriginY = newHeight - dstOriginY - lightMapImage.Height;

                // put into a render texture
                // resize light map image
                Raylib.ImageResizeCanvas(
                    ref lightMapImage.Ref(),
                    newWidth, newHeight,
                    dstOriginX, dstOriginY,
                    Color.White
                );

                Raylib.ImageFlipVertical(lightMapImage);

                // get light map as a texture
                var lightmapTex = RlManaged.Texture2D.LoadFromImage(lightMapImage);

                lightmapRt?.Dispose();
                lightmapRt = RlManaged.RenderTexture2D.Load(newWidth, newHeight);

                Raylib.BeginTextureMode(lightmapRt);
                Raylib.ClearBackground(Color.Black);

                // texture is loaded upside down...
                Raylib.DrawTexturePro(
                    lightmapTex,
                    new Rectangle(0f, lightmapTex.Height, lightmapTex.Width, -lightmapTex.Height),
                    new Rectangle(0f, 0f, lightmapTex.Width, lightmapTex.Height),
                    Vector2.Zero, 0f,
                    Color.White
                );

                Raylib.EndTextureMode();
            }
        }
        else
        {
            Debug.Assert(_lightmapImage is not null);
            Raylib.ImageResizeCanvas(
                ref _lightmapImage.Ref(),
                newWidth, newHeight,
                dstOriginX, dstOriginY,
                Color.White
            );
        }

        width = newWidth;
        height = newHeight;
    }

    public void RaylibBeginTextureMode()
    {
        if (!UseHardwareAcceleration)
            throw new InvalidOperationException("Lightmap storage is not on the GPU!");
        
        Raylib.BeginTextureMode(lightmapRt!);
    }

    public static void DrawAtom(BrushAtom atom)
    {
        if (!UseHardwareAcceleration)
            throw new NotImplementedException("LightMap.DrawAtom is not implemented in software mode");

        var tex = RainEd.Instance.LightBrushDatabase.Brushes[atom.brush].Texture;
        Raylib.DrawTexturePro(
            tex,
            new Rectangle(0, 0, tex.Width, tex.Height),
            atom.rect,
            new Vector2(atom.rect.Width, atom.rect.Height) / 2f,
            atom.rotation,
            atom.mode ? Color.Black : Color.White
        );
    }

    public static void UpdateWarpShaderUniforms(ReadOnlySpan<Vector2> warpPoints)
    {
        var w = RainEd.Instance.Level.LightMap.Width;
        var h = RainEd.Instance.Level.LightMap.Height;
        var shader = Shaders.LightStretchShader;

        shader.GlibShader.SetUniform("u_vert_ab", new Vector4(
            warpPoints[3].X / w, 1f - warpPoints[3].Y / h,
            warpPoints[2].X / w, 1f - warpPoints[2].Y / h
        ));
        shader.GlibShader.SetUniform("u_vert_cd", new Vector4(
            warpPoints[1].X / w, 1f - warpPoints[1].Y / h,
            warpPoints[0].X / w, 1f - warpPoints[0].Y / h
        ));
    }

    public RlManaged.Image GetImage()
    {
        if (!UseHardwareAcceleration)
            return RlManaged.Image.Copy(_lightmapImage!);
        
        //var img = RlManaged.Image.LoadFromTexture(lightmapRt.Texture);
        if (lightmapRt is null) throw new InvalidOperationException("Lightmap texture could not be loaded");
        if (lightmapRt.Texture.ID is not Glib.ReadableTexture tex) throw new InvalidOperationException("Lightmap texture is not a ReadableTexture");
        var gimg = tex.GetImage();

        var img = new RlManaged.Image(new Image { image = gimg });

        if (RainEd.RenderContext.OriginBottomLeft)
            Raylib.ImageFlipVertical(img);
        
        return img;
    }
}
