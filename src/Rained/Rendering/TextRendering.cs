namespace Rained.Rendering;

using System.Numerics;
using ImGuiNET;
using RectpackSharp;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

static class TextRendering
{
    [StructLayout(LayoutKind.Sequential)]
    struct ImGuiGlyph
    {
        public uint Bitfield;
        public float AdvanceX; // Distance to next character
        public float X0, Y0, X1, Y1; // Glyph corners
        public float U0, V0, U1, V1; // Texture coordinates

        public bool Colored
        {
            readonly get => (Bitfield & 1) != 0;
            set => Bitfield = (Bitfield & ~1u) | (value ? 1u : 0u);
        }

        public bool Visible
        {
            readonly get => ((Bitfield >> 1) & 1) != 0;
            set => Bitfield = (Bitfield & ~2u) | (value ? 1u : 0u);
        }

        public uint Codepoint
        {
            readonly get => Bitfield >> 2;
            set => Bitfield = (Bitfield & 3u) | (value << 2);
        }
    }

    private static Glib.Texture? outlineFontTexture = null;
    private static ImGuiGlyph[] outlineFontGlyphs = [];

    public unsafe static void DrawText(string text, ImFontPtr font, Vector2 offset, Vector2 scale)
    {
        var rctx = RainEd.RenderContext;
        var fontTex = Boot.ImGuiController!.FontTexture;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            ImGuiGlyph* glyph = (ImGuiGlyph*) font.FindGlyph(c).NativePtr;
            if (glyph is null) continue;

            offset.X += DrawGlyph(ref *glyph, offset, scale, fontTex, rctx);
        }
    }

    public static void DrawTextOutlined(string text, Vector2 offset, Vector2 scale)
    {
        if (outlineFontTexture is null)
            GenerateOutlineFont();
        
        var rctx = RainEd.RenderContext;
        var fontTex = outlineFontTexture!;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            var glyphIndex = FindOutlineGlyph(c);
            if (glyphIndex == -1) continue;

            offset.X += DrawGlyph(ref outlineFontGlyphs[glyphIndex], offset, scale, fontTex, rctx);
        }
    }

    private static float DrawGlyph(
        ref readonly ImGuiGlyph glyph,
        Vector2 pos, Vector2 scale,
        Glib.Texture fontTex, Glib.RenderContext rctx
    )
    {
        var glyphW = glyph.X1 - glyph.X0;// * fontTex.Width;
        var glyphH = glyph.Y1 - glyph.Y0;// * fontTex.Height;

        if (glyph.Visible)
        {
            rctx.DrawTexture(
                texture: fontTex,
                srcRect: new Glib.Rectangle(
                    glyph.U0 * fontTex.Width, glyph.V0 * fontTex.Height,
                    (glyph.U1 - glyph.U0) * fontTex.Width, (glyph.V1 - glyph.V0) * fontTex.Height
                ),
                dstRect: new Glib.Rectangle(
                    pos + new Vector2(glyph.X0, glyph.Y0) * scale,
                    new Vector2(glyphW * scale.X, glyphH * scale.Y)
                )
            );
        }

        return glyph.AdvanceX * scale.X;
    }

    public static Vector2 CalcTextSize(ImFontPtr font, string text)
    {
        return font.CalcTextSizeA(font.FontSize, float.PositiveInfinity, float.PositiveInfinity, text);
    }

    /// <summary>
    /// Generate an outlined ProggyClean font texture atlas.
    /// </summary>
    /// <exception cref="Exception">Thrown if the texture atlas could not be created.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void GenerateOutlineFont()
    {
        Log.Debug("generate outline font");

        outlineFontTexture?.Dispose();
        var font = Fonts.DefaultFont;

        // get original font pixels
        ImGui.GetIO().Fonts.GetTexDataAsAlpha8(out byte* pixels, out int width, out int height);
        using var texImg = new Glib.Image(new ReadOnlySpan<byte>(pixels, width * height), width, height, Glib.PixelFormat.Grayscale);

        // initialize pack rects
        PackingRectangle[] rects = new PackingRectangle[font.Glyphs.Size];
        outlineFontGlyphs = new ImGuiGlyph[font.Glyphs.Size];
        int rectIndex = 0;

        // associate each packing rect with a glyph
        ImGuiGlyph* glyphsDataPtr = (ImGuiGlyph*) font.Glyphs.Data;
        for (int i = 0; i < font.Glyphs.Size; i++)
        {
            ImGuiGlyph* glyph = &glyphsDataPtr[i];
            if (glyph is null) continue;

            rects[rectIndex].Width = (uint)((glyph->U1 - glyph->U0) * width) + 2;
            rects[rectIndex].Height = (uint)((glyph->V1 - glyph->V0) * height) + 2;
            rects[rectIndex].Id = i;
            rectIndex++;
        }

        // pack the rectangle
        RectanglePacker.Pack(
            rects, out PackingRectangle bounds,
            maxBoundsWidth: (uint)Glib.Texture.MaxSize, maxBoundsHeight: (uint)Glib.Texture.MaxSize
        );

        if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.Width > Glib.Texture.MaxSize || bounds.Height > Glib.Texture.MaxSize)
            throw new Exception("Outlined font texture is out of space");
        
        // copy glyphs into output image
        var texWidth = (int)bounds.Width;
        var texHeight = (int)bounds.Height;
        using var outImg = Glib.Image.FromColor(texWidth, texHeight, Glib.Color.Transparent, Glib.PixelFormat.RGBA);

        foreach (var rect in rects)
        {
            ImGuiGlyph* glyph = &glyphsDataPtr[rect.Id];
            var glyphX0 = (int)(glyph->U0 * width);
            var glyphY0 = (int)(glyph->V0 * height);
            var glyphX1 = (int)(glyph->U1 * width);
            var glyphY1 = (int)(glyph->V1 * height);

            for (int y = 0; y < rect.Height; y++)
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    var dstX = (int)rect.X + x;
                    var dstY = (int)rect.Y + y;
                    var srcX = glyphX0 + x - 1;
                    var srcY = glyphY0 + y - 1;

                    bool opaque;
                    if (x >= 1 && y >= 1 && x < rect.Width - 1 && y < rect.Height - 1)
                        opaque = texImg.GetPixel(srcX, srcY).R != 0;
                    else
                        opaque = false;

                    if (opaque)
                    {
                        outImg.SetPixel(dstX, dstY, Glib.Color.White);
                    }
                    else if (
                        GetPixelOrTransparent(texImg, srcX+1, srcY+0, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX+0, srcY+1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX-1, srcY+0, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX+0, srcY-1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||

                        GetPixelOrTransparent(texImg, srcX-1, srcY-1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX+1, srcY-1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX+1, srcY+1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0 ||
                        GetPixelOrTransparent(texImg, srcX-1, srcY+1, glyphX0, glyphY0, glyphX1, glyphY1).R > 0
                    )
                    {
                        outImg.SetPixel(dstX, dstY, Glib.Color.Black);
                    }
                }
            }

            ref var outGlyph = ref outlineFontGlyphs[rect.Id];
            outGlyph.Bitfield = glyph->Bitfield;
            outGlyph.AdvanceX = glyph->AdvanceX;
            outGlyph.X0 = glyph->X0;
            outGlyph.X1 = glyph->X1;
            outGlyph.Y0 = glyph->Y0;
            outGlyph.Y1 = glyph->Y1;
            outGlyph.U0 = (float)rect.X / texWidth;
            outGlyph.V0 = (float)rect.Y / texHeight;
            outGlyph.U1 = (float)(rect.X + rect.Width) / texWidth;
            outGlyph.V1 = (float)(rect.Y + rect.Height) / texHeight;
        }

        outlineFontTexture = Glib.Texture.Load(outImg);
    }

    private static Glib.Color GetPixelOrTransparent(Glib.Image img, int x, int y, int minX, int minY, int maxX, int maxY)
    {
        if (x < minX || y < minY || x >= maxX || y >= maxY)
            return Glib.Color.Transparent;
        return img.GetPixel(x, y);
    }

    private static int FindOutlineGlyph(char c)
    {
        for (int i = 0; i < outlineFontGlyphs.Length; i++)
        {
            ref var g = ref outlineFontGlyphs[i];
            if (g.Codepoint == c)
                return i;
        }
        
        return -1;
    }
}