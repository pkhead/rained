using Raylib_cs;
using System.Numerics;
using Rained.EditorGui;
using Rained.Assets;
namespace Rained.LevelData;

struct BrushAtom
{
    public float rotation;
    public Rectangle rect;
    public int brush;
    public bool mode;
}

class LightMap : IDisposable
{
    private RlManaged.RenderTexture2D lightmapRt;
    private int width;
    private int height;

    public int Width { get => width; }
    public int Height { get => height; }
    public Texture2D Texture { get => lightmapRt.Texture; }
    public RenderTexture2D RenderTexture { get => lightmapRt; }

    public LightMap(int levelWidth, int levelHeight)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        // create light map render texture
        using var image = Glib.Image.FromColor(width, height, Glib.Color.Black);
        lightmapRt = RlManaged.RenderTexture2D.Load(width, height);
        lightmapRt.Texture.ID!.UpdateFromImage(image);
    }

    public LightMap(int levelWidth, int levelHeight, Image lightMapImage)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        using var croppedLightMap = RlManaged.Image.Copy(lightMapImage);
        AssetGraphicsProvider.CropImage(croppedLightMap);

        using var finalImage = RlManaged.Image.GenColor(width, height, Color.White);

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
        using var lightmapTex = RlManaged.Texture2D.LoadFromImage(finalImage);

        // put into a render texture
        lightmapRt = RlManaged.RenderTexture2D.Load(width, height);
        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(lightmapTex, 0, 0, Color.White);
        Raylib.EndTextureMode();
    }

    public void Dispose()
    {
        lightmapRt.Dispose();
        lightmapRt = null!;
    }

    public void Resize(int newWidth, int newHeight, int dstOriginX, int dstOriginY)
    {
        // convert from cell to lightmap coordinates
        newWidth = newWidth * 20 + 300;
        newHeight = newHeight * 20 + 300;
        dstOriginX *= 20;
        dstOriginY *= 20;

        var lightMapImage = GetImage();
        
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

        lightmapRt.Dispose();
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
        lightMapImage.Dispose();

        width = newWidth;
        height = newHeight;
    }

    public void RaylibBeginTextureMode()
    {
        Raylib.BeginTextureMode(lightmapRt);
    }

    public static void DrawAtom(BrushAtom atom)
    {
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

    public RlManaged.Image GetImage()
    {
        //var img = RlManaged.Image.LoadFromTexture(lightmapRt.Texture);
        if (lightmapRt.Texture.ID is not Glib.ReadableTexture tex) throw new Exception("Lightmap texture is not a ReadableTexture");
        var gimg = tex.GetImage();

        var img = new RlManaged.Image(new Image { image = gimg });

        if (RainEd.RenderContext.OriginBottomLeft)
            Raylib.ImageFlipVertical(img);
        
        return img;
    }
}