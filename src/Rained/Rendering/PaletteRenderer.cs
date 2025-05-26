namespace Rained.Rendering;
using LevelData;
using System.Globalization;
using Raylib_cs;
using Rained.Assets;

class PaletteRenderer : IDisposable
{
    private readonly Dictionary<int, Palette> Palettes;
    private readonly Palette dummyPalette = new();

    public Glib.Texture Texture => paletteTexture;
    private readonly Glib.Texture paletteTexture;
    private readonly Glib.Image _paletteImgBuf; // for updating paletteTexture

    public int Index = 0;
    public int FadeIndex = 1;
    public float Mix = 0f;
    
    public PaletteRenderer()
    {
        var palettes = new Dictionary<int, Palette>();
        foreach (var filePath in ConfigDirectory.EnumerateFiles("palettes"))
        {
            int paletteNumber = 0;
            bool validFilename = false;

            var fileName = Path.GetFileName(filePath);
            if (fileName[^4..] == ".png" && fileName[..7] == "palette")
            {
                validFilename = int.TryParse(fileName[7..^4], CultureInfo.InvariantCulture, out paletteNumber);
            }

            if (validFilename)
            {
                try
                {
                    palettes[paletteNumber] = new Palette(filePath);
                }
                catch (Exception e)
                {
                    Log.UserLogger.Error("Could not load palette {PaletteNumber}: {Exception}", paletteNumber, e);
                }
            }
            else
            {
                Log.UserLogger.Warning("Invalid palette file name {FileName}, ignoring.", Path.GetFileName(filePath));
            }
        }
        
        Palettes = palettes;

        // create palette texture which will be computed and sent to the palette shader
        // (Glib.PixelFormat.RGB does not work for some reason)
        paletteTexture = Glib.Texture.Create(30, 3, Glib.PixelFormat.RGBA);
        _paletteImgBuf = Glib.Image.FromColor(30, 3, Glib.Color.Black, Glib.PixelFormat.RGBA);
    }

    public Palette GetPalette(int index)
    {
        if (Palettes.TryGetValue(index, out Palette? p))
            return p;
        else
            return dummyPalette;
    }

    private static float Lerp(float x, float y, float a)
    {
        return (y - x) * a + x;
    }

    public Color GetSunColor(PaletteLightLevel lightLevel, int sublayer, int index)
    {
        var p = GetPalette(index).SunPalette;
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
        var p = GetPalette(index);
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
        return GetSunColorMix(lightLevel, sublayer, Index, FadeIndex, Mix);
    }

    public Color GetPaletteColor(PaletteColor colorName)
    {
        return GetPaletteColorMix(colorName, Index, FadeIndex, Mix);
    }

    /// <summary>
    /// Update the texture used to send palette information to the palette shader.
    /// </summary>
    public void UpdateTexture()
    {
        for (int i = 0; i < 30; i++)
        {
            var litColorRGB = GetSunColor(PaletteLightLevel.Lit, i);
            var neutralColorRGB = GetSunColor(PaletteLightLevel.Neutral, i);
            var shadedColorRGB = GetSunColor(PaletteLightLevel.Shaded, i);

            var litColor = Glib.Color.FromRGBA(litColorRGB.R, litColorRGB.G, litColorRGB.B);
            var neutralColor = Glib.Color.FromRGBA(neutralColorRGB.R, neutralColorRGB.G, neutralColorRGB.B);
            var shadedColor = Glib.Color.FromRGBA(shadedColorRGB.R, shadedColorRGB.G, shadedColorRGB.B);

            _paletteImgBuf.SetPixel(i, 0, litColor);
            _paletteImgBuf.SetPixel(i, 1, neutralColor);
            _paletteImgBuf.SetPixel(i, 2, shadedColor);
        }

        paletteTexture.UpdateFromImage(_paletteImgBuf);
    }

    /// <summary>
    /// Use the palette shader for the upcoming draw operations.
    /// <br/><br/>
    /// Make sure the palette was recently updated using UpdatePaletteTexture before use.
    /// </summary>
    public void BeginPaletteShaderMode()
    {
        var shader = Shaders.PaletteShader;
        Raylib.BeginShaderMode(shader);
        shader.GlibShader.SetUniform("u_paletteTex", paletteTexture);
    }

    public void Dispose()
    {
        paletteTexture.Dispose();
        _paletteImgBuf.Dispose();
    }
}