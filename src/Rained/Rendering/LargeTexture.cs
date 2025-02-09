using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using SixLabors.ImageSharp.Processing;

namespace Rained.Rendering;

/// <summary>
/// Class for handling textures that may be larger than the hardware limit.
/// This splits the main image across a grid of sub-textures and provides functions for rendering them as a whole.
/// </summary>
class LargeTexture : IDisposable
{
    private readonly Glib.Texture[] textures;
    private readonly int cols, rows;
    private readonly int width, height;
    private readonly int gridSize;

    public int Width => width;
    public int Height => height;

    public LargeTexture(Glib.Image gimage)
    {
        ArgumentNullException.ThrowIfNull(gimage);

        var image = gimage.ImageSharpImage;
        gridSize = Glib.Texture.MaxSize;

        width = gimage.Width;
        height = gimage.Height;

        if (image.Width <= gridSize && image.Height <= gridSize)
        {
            textures = new Glib.Texture[1];
            textures[0] = Glib.Texture.Load(gimage);

            cols = 1;
            rows = 1;
            return;
        }

        cols = image.Width / gridSize;
        rows = image.Height / gridSize;
        textures = new Glib.Texture[cols * rows];
        int i = 0;

        for (int y = 0; y < image.Height; y += gridSize)
        {
            var imageHeight = Math.Min(image.Height, y + gridSize) - y;
            for (int x = 0; x < image.Width; x += gridSize)
            {
                var imageWidth = Math.Min(image.Width, x + gridSize) - x;

                using var crop = gimage.Clone();
                crop.ImageSharpImage.Mutate(t =>
                    t.Crop(new SixLabors.ImageSharp.Rectangle(x, y, imageWidth, imageHeight))
                );

                textures[i++] = Glib.Texture.Load(crop);
            }
        }
    }

    public LargeTexture(Image image) : this(image.image!) {}

    public void Dispose()
    {
        foreach (var tex in textures)
            tex.Dispose();
    }

    private delegate void TextureDraw(Glib.Texture texture, Glib.Rectangle srcRect, Glib.Rectangle dstRect);

    private void DrawRectangle(Glib.Rectangle srcRect, Glib.Rectangle dstRect, TextureDraw drawTexture)
    {
        if (textures.Length == 1)
        {
            drawTexture(
                texture: textures[0],
                srcRect: srcRect,
                dstRect: dstRect
            );
            return;
        }

        int i = 0;

        // convert srcRect to normalized uv space
        srcRect.X /= width;
        srcRect.Y /= height;
        srcRect.Width /= width;
        srcRect.Height /= height;

        var gridL = (int)(srcRect.Left * cols);
        var gridR = (int)(srcRect.Right * cols);
        var gridT = (int)(srcRect.Top * rows);
        var gridB = (int)(srcRect.Bottom * rows);

        var gridSizeF = (float)gridSize / height;
        for (int row = gridT; row <= gridB; row++)
        {
            float rf = (float)row / rows;
            var srcV0 = (srcRect.Top - rf) / gridSizeF;
            var srcV1 = (srcRect.Bottom - rf) / gridSizeF;
            srcV0 = Math.Max(srcV0, 0f);
            srcV1 = Math.Max(srcV1, 1f);

            for (int col = gridL; col <= gridR; col++)
            {
                float cf = (float)col / cols;
                var srcU0 = (srcRect.Left - cf) / gridSizeF;
                var srcU1 = (srcRect.Right - cf) / gridSizeF;
                srcU0 = Math.Max(srcU0, 0f);
                srcU1 = Math.Max(srcU1, 1f);
                
                if (srcU0 >= 0f && srcU1 >= 0f && srcV0 <= 1f && srcV1 <= 1f)
                {
                    var tex = textures[i];

                    var dstX0 = (cf + srcU0 * gridSizeF) * dstRect.Width;
                    var dstY0 = (rf + srcV0 * gridSizeF) * dstRect.Height;
                    var dstX1 = (cf + srcU1 * gridSizeF) * dstRect.Width;
                    var dstY1 = (rf + srcV1 * gridSizeF) * dstRect.Height;

                    drawTexture(
                        texture: tex,
                        srcRect: new Glib.Rectangle(
                            srcU0 * gridSize,
                            srcV0 * gridSize,
                            (srcU1 - srcU0) * gridSize,
                            (srcV1 - srcV0) * gridSize
                        ),
                        dstRect: new Glib.Rectangle(dstX0, dstY0, dstX1 - dstX0, dstY1 - dstY0)
                    );

                }
            }
        }
    }

    public void DrawRectangle(Glib.Rectangle srcRect, Glib.Rectangle dstRect)
    {
        var rctx = RainEd.RenderContext;
        DrawRectangle(srcRect, dstRect, rctx.DrawTexture);
    }

    public void DrawRectangle(Rectangle srcRec, Rectangle dstRec, Color color)
    {
        var rctx = RainEd.RenderContext;
        rctx.DrawColor = Raylib.ToGlibColor(color);
        DrawRectangle(
            new Glib.Rectangle(srcRec.Position, srcRec.Size),
            new Glib.Rectangle(srcRec.Position, srcRec.Size)
        );
    }

    public void DrawRectangle(float dstX, float dstY, float dstW, float dstH)
    {
        DrawRectangle(new Glib.Rectangle(0f, 0f, width, height), new Glib.Rectangle(dstX, dstY, dstW, dstH));
    }

    public void DrawRectangle(float dstX, float dstY, float dstW, float dstH, Glib.Color color)
    {
        var rctx = RainEd.RenderContext;
        rctx.DrawColor = color;
        DrawRectangle(dstX, dstY, dstW, dstH);
    }

    public void DrawRectangle(float posX, float posY, float width, float height, Color color)
        => DrawRectangle(posX, posY, width, height, Raylib_cs.Raylib.ToGlibColor(color));
    
    public void ImGuiDraw(Vector2 imageSize)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();

        DrawRectangle(
            srcRect: new Glib.Rectangle(0f, 0f, width, height),
            dstRect: new Glib.Rectangle(0f, 0f, imageSize.X, imageSize.Y),
            drawTexture: (tex, sr, dr) =>
            {
                drawList.AddImage(
                    ImGuiExt.TextureID(tex),
                    cursor + dr.Position, cursor + dr.Position + dr.Size,
                    sr.Position / gridSize,
                    (sr.Position + sr.Size) / gridSize
                );
            }
        );

        ImGui.Dummy(imageSize);
    }
}