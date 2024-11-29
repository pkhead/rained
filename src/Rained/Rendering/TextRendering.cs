namespace Rained.Rendering;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

static class TextRendering
{
    [StructLayout(LayoutKind.Sequential)]
    readonly struct ImGuiGlyph
    {
        public readonly uint Bitfield;
        public readonly float AdvanceX; // Distance to next character
        public readonly float X0, Y0, X1, Y1; // Glyph corners
        public readonly float U0, V0, U1, V1; // Texture coordinates

        public bool Colored => (Bitfield & 1) != 0;
        public bool Visible => ((Bitfield >> 1) & 1) != 0;
        public uint Codepoint => Bitfield >> 2;
    }

    public unsafe static void DrawText(string text, ImFontPtr font, Vector2 offset, Vector2 scale)
    {
        var rctx = RainEd.RenderContext;
        var fontTex = Boot.ImGuiController!.FontTexture;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            ImGuiGlyph* glyph = (ImGuiGlyph*) font.FindGlyph(c).NativePtr;
            var glyphW = glyph->X1 - glyph->X0;// * fontTex.Width;
            var glyphH = glyph->Y1 - glyph->Y0;// * fontTex.Height;

            if (glyph->Visible)
            {
                rctx.DrawTexture(
                    texture: fontTex,
                    srcRect: new Glib.Rectangle(
                        glyph->U0 * fontTex.Width, glyph->V0 * fontTex.Height,
                        (glyph->U1 - glyph->U0) * fontTex.Width, (glyph->V1 - glyph->V0) * fontTex.Height
                    ),
                    dstRect: new Glib.Rectangle(
                        offset + new Vector2(glyph->X0, glyph->Y0) * scale,
                        new Vector2(glyphW * scale.X, glyphH * scale.Y)
                    )
                );
            }

            offset.X += glyph->AdvanceX * scale.X;
            //var advX = font.IndexAdvanceX[c];
            //offset.X += (c < font.IndexAdvanceX.Size ? advX : font.FallbackAdvanceX) * scale.X;
        }
    }

    public static Vector2 CalcTextSize(ImFontPtr font, string text)
    {
        return font.CalcTextSizeA(font.FontSize, float.PositiveInfinity, float.PositiveInfinity, text);
    }
}