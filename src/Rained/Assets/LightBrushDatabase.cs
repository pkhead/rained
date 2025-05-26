using Raylib_cs;
using SixLabors.ImageSharp.PixelFormats;

namespace Rained.Assets;

struct LightBrush
{
    public string Name;
    public RlManaged.Texture2D Texture;
}

class LightBrushDatabase
{
    private readonly List<LightBrush> lightBrushes;

    public List<LightBrush> Brushes { get => lightBrushes; }

    private static LightBrush? LoadBrush(string path)
    {
        Glib.Image glibImg;
        SixLabors.ImageSharp.Image<Rgba32> imgSharpImg;

        using (var img = RlManaged.Image.Load(path))
        {
            if (!Raylib.IsImageReady(img)) return null;
            AssetGraphicsProvider.CropImage(img);

            glibImg = ((Image)img).image!.ConvertToFormat(Glib.PixelFormat.RGBA);
            imgSharpImg = (SixLabors.ImageSharp.Image<Rgba32>) glibImg.ImageSharpImage;
        }

        // ignore alpha channel and round colors to black and white
        imgSharpImg.ProcessPixelRows((accessor) =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                for (int x = 0; x < pixelRow.Length; x++)
                {
                    ref var pixel = ref pixelRow[x];

                    var lum = (pixel.R + pixel.G + pixel.B) / (255f * 3f);
                    if (lum >= 0.5f)
                    {
                        pixel.R = 255;
                        pixel.G = 255;
                        pixel.B = 255;
                    }
                    else
                    {
                        pixel.R = 0;
                        pixel.G = 0;
                        pixel.B = 0;
                    }
                }
            } 
        });

        var tex = RlManaged.Texture2D.LoadFromImage(new Image()
        {
            image = glibImg
        });
        if (!Raylib.IsTextureReady(tex)) return null;
        glibImg.Dispose(); // this also disposes imgSharpImage

        return new LightBrush()
        {
            Name = Path.GetFileName(path),
            Texture = tex
        };
    }

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
            var filePath = Path.Combine(Boot.AppDataPath,"assets","light",fileName.Trim());
            var brush = LoadBrush(filePath);

            if (brush is null)
            {
                Log.UserLogger.Error("Could not load light brush " + filePath);
            }
            else
            {
                lightBrushes.Add(brush.Value);
            }
        }

        // load drizzle light data
        var lightsDir = Path.Combine(AssetDataPath.GetPath(), "Lights");
        if (Directory.Exists(lightsDir))
        {
            foreach (var filePath in Directory.GetFileSystemEntries(lightsDir))
            {
                var brush = LoadBrush(filePath);

                if (brush is null)
                {
                    Log.UserLogger.Error("Could not load light brush " + filePath);
                }
                else
                {
                    lightBrushes.Add(brush.Value);
                }
            }
        }
    }
}