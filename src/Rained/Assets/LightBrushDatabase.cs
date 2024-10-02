namespace RainEd.Assets;

struct LightBrush
{
    public string Name;
    public RlManaged.Texture2D Texture;
}

class LightBrushDatabase
{
    private readonly List<LightBrush> lightBrushes;

    public List<LightBrush> Brushes { get => lightBrushes; }

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
    }
}