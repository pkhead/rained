using Raylib_cs;
using System.Numerics;
namespace RainEd.Light;

struct LightBrush
{
    public string Name;
    public RlManaged.Texture2D Texture;
}

struct BrushAtom
{
    public float rotation;
    public Rectangle rect;
    public int brush;
    public bool mode;
}

class LightBrushDatabase
{
    private readonly static string levelLightShaderSrc = @"
        #version 330

        in vec2 fragTexCoord;
        in vec4 fragColor;

        uniform sampler2D texture0;
        uniform vec4 colDiffuse;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(texture0, fragTexCoord);
            finalColor = vec4(1.0, 1.0, 1.0, 1.0 - texelColor.r) * fragColor * colDiffuse;
        }
    ";

    private readonly List<LightBrush> lightBrushes;

    public List<LightBrush> Brushes { get => lightBrushes; }
    public readonly RlManaged.Shader Shader;

    public LightBrushDatabase()
    {
        lightBrushes = new List<LightBrush>();

        foreach (var fileName in File.ReadLines(Path.Combine(Boot.AppDataPath,"assets","light","init.txt")))
        {
            // if this line is empty, skip
            if (string.IsNullOrWhiteSpace(fileName)) continue;

            // this line is a comment, skip
            if (fileName[0] == '#') continue;
            
            // load light texture
            var tex = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","light",fileName.Trim()));
            lightBrushes.Add(new LightBrush()
            {
                Name = fileName.Trim(),
                Texture = tex
            });
        }

        Shader = RlManaged.Shader.LoadFromMemory(null, levelLightShaderSrc);
    }
}

class LightMap : IDisposable
{
    private RlManaged.RenderTexture2D lightmapRt;
    private int width;
    private int height;

    public int Width { get => width; }
    public int Height { get => height; }
    public Texture2D Texture { get => lightmapRt.Texture; }

    public LightMap(int levelWidth, int levelHeight)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        // create light map render texture
        lightmapRt = RlManaged.RenderTexture2D.Load(width, height);
        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.White);
        Raylib.EndTextureMode();
    }

    public LightMap(int levelWidth, int levelHeight, Image lightMapImage)
    {
        width = levelWidth * 20 + 300;
        height = levelHeight * 20 + 300;

        // emulate director auto-cropping
        // This Sucks
        int imgMinX = -1;
        int imgMinY = -1;
        int imgMaxX = -1;
        int imgMaxY = -1;

        // find imgMinY
        for (int y = 0; y < lightMapImage.Height; y++)
        {
            for (int x = 0; x < lightMapImage.Width; x++)
            {
                var color = Raylib.GetImageColor(lightMapImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMinY = y;
                    goto exitTopSearch;
                }
            }
        }
        exitTopSearch:;

        // find imgMinX
        for (int x = 0; x < lightMapImage.Width; x++)
        {
            for (int y = 0; y < lightMapImage.Height; y++)
            {
                var color = Raylib.GetImageColor(lightMapImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMinX = x;
                    goto exitLeftSearch;
                }
            }
        }
        exitLeftSearch:;

        // find imgMaxY
        for (int y = lightMapImage.Height - 1; y >= 0; y--)
        {
            for (int x = lightMapImage.Width - 1; x >= 0; x--)
            {
                var color = Raylib.GetImageColor(lightMapImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMaxY = y;
                    goto exitBottomSearch;
                }
            }
        }
        exitBottomSearch:;

        // find imgMaxX
        for (int x = lightMapImage.Width - 1; x >= 0; x--)
        {
            for (int y = lightMapImage.Height - 1; y >= 0; y--)
            {
                var color = Raylib.GetImageColor(lightMapImage, x, y);
                if (color.R != 255 || color.G != 255 || color.B != 255)
                {
                    imgMaxX = x;
                    goto exitRightSearch;
                }
            }
        }
        exitRightSearch:;

        // center cropped image
        using var finalImage = RlManaged.Image.GenColor(width, height, Color.White);
        if (imgMinX >= 0)
        {
            int subWidth = imgMaxX - imgMinX + 1;
            int subHeight = imgMaxY - imgMinY + 1;

            Raylib.ImageDraw(
                dst: ref finalImage.Ref(),
                src: lightMapImage,
                srcRec: new Rectangle(imgMinX, imgMinY, subWidth, subHeight),
                dstRec: new Rectangle((width - subWidth) / 2f, (height - subHeight) / 2f, subWidth, subHeight),
                tint: Color.White
            );
        }

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

        // resize light map image
        using var lightMapImage = GetImage();
        Raylib.ImageResizeCanvas(
            ref lightMapImage.Ref(),
            newWidth, newHeight,
            dstOriginX, dstOriginY,
            Color.White
        );

        // get light map as a texture
        using var lightmapTex = RlManaged.Texture2D.LoadFromImage(lightMapImage);

        // put into a render texture
        lightmapRt.Dispose();
        lightmapRt = RlManaged.RenderTexture2D.Load(newWidth, newHeight);
        Raylib.BeginTextureMode(lightmapRt);
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(lightmapTex, 0, 0, Color.White);
        Raylib.EndTextureMode();

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
        var img = RlManaged.Image.LoadFromTexture(lightmapRt.Texture);
        Raylib.ImageFlipVertical(ref img.Ref());
        Raylib.ImageFormat(ref img.Ref(), PixelFormat.UncompressedGrayscale);

        return img;
    }
}