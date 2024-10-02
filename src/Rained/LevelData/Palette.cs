using Raylib_cs;
namespace RainEd.LevelData;

enum PaletteLightLevel
{
    Lit, Neutral, Shaded
};

enum PaletteColor
{
    Sky, Fog, Black, ShortcutSymbol
}

struct PaletteShadeColors
{
    public Color Lit;
    public Color Neutral;
    public Color Shaded;
}

record Palette
{
    public readonly Color SkyColor;
    public readonly Color FogColor;
    public readonly Color BlackColor;
    public readonly Color ShortcutSymbolColor;

    private readonly PaletteShadeColors[] sunPalette;
    private readonly PaletteShadeColors[] shadowPalette;

    public ReadOnlySpan<PaletteShadeColors> SunPalette => sunPalette;
    public ReadOnlySpan<PaletteShadeColors> ShadowPalette => shadowPalette;

    public Palette()
    {
        SkyColor = Color.Black;
        FogColor = Color.Black;
        BlackColor = Color.Black;
        ShortcutSymbolColor = Color.Black;

        sunPalette = new PaletteShadeColors[30];
        shadowPalette = new PaletteShadeColors[30];

        for (int i = 0; i < 30; i++)
        {
            sunPalette[i] = new PaletteShadeColors()
            {
                Lit = Color.Black,
                Neutral = Color.Black,
                Shaded = Color.Black,
            };

            shadowPalette[i] = new PaletteShadeColors()
            {
                Lit = Color.Black,
                Neutral = Color.Black,
                Shaded = Color.Black,
            };
        }
    }

    public Palette(string filePath) : this()
    {
        using var img = RlManaged.Image.Load(filePath);
        if (!Raylib.IsImageReady(img))
            throw new Exception($"Could not load palette image '${filePath}'");

        SkyColor = Raylib.GetImageColor(img, 0, 0);
        FogColor = Raylib.GetImageColor(img, 1, 0);
        BlackColor = Raylib.GetImageColor(img, 2, 0);
        ShortcutSymbolColor = Raylib.GetImageColor(img, 13, 0);

        sunPalette = new PaletteShadeColors[30];
        shadowPalette = new PaletteShadeColors[30];

        for (int i = 0; i < 30; i++)
        {
            sunPalette[i] = new PaletteShadeColors()
            {
                Lit = Raylib.GetImageColor(img, i, 2),
                Neutral = Raylib.GetImageColor(img, i, 3),
                Shaded = Raylib.GetImageColor(img, i, 4),
            };

            shadowPalette[i] = new PaletteShadeColors()
            {
                Lit = Raylib.GetImageColor(img, i, 5),
                Neutral = Raylib.GetImageColor(img, i, 6),
                Shaded = Raylib.GetImageColor(img, i, 7),
            };
        }
    }
}