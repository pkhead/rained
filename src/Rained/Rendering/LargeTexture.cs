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

        // image size divided by grid size, rounded up
        cols = (image.Width + gridSize - 1) / gridSize;
        rows = (image.Height + gridSize - 1) / gridSize;
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

    public delegate void TextureDrawMethod(Glib.Texture texture, Glib.Rectangle srcRect, Glib.Rectangle dstRect);

    public void DrawRectangle(Glib.Rectangle srcRect, Glib.Rectangle dstRect, TextureDrawMethod drawTexture)
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

        var gridL = (int)(srcRect.Left / gridSize);
        var gridR = (int)(srcRect.Right / gridSize);
        var gridT = (int)(srcRect.Top / gridSize);
        var gridB = (int)(srcRect.Bottom / gridSize);

        for (int row = gridT; row <= gridB; row++)
        {
            if (row < 0 || row >= rows) continue;

            float rf = (float)row * gridSize;
            var srcV0 = (srcRect.Top - rf) / gridSize;
            var srcV1 = (srcRect.Bottom - rf) / gridSize;
            srcV0 = Math.Max(srcV0, 0f);
            srcV1 = Math.Min(srcV1, 1f);
            
            for (int col = gridL; col <= gridR; col++)
            {
                if (col < 0 || col >= cols) continue;

                float cf = (float)col * gridSize;
                var srcU0 = (srcRect.Left - cf) / gridSize;
                var srcU1 = (srcRect.Right - cf) / gridSize;
                srcU0 = Math.Max(srcU0, 0f);
                srcU1 = Math.Min(srcU1, 1f);
                
                if (srcU0 >= 0f && srcV0 >= 0f && srcU1 <= 1f && srcV1 <= 1f)
                {
                    var tex = textures[row * cols + col];

                    var gv = (cf + srcU0 * gridSize - srcRect.X) / srcRect.Width;
                    var gu = (rf + srcV0 * gridSize - srcRect.Y) / srcRect.Height;
                    var dstX = dstRect.X + dstRect.Width * gv;
                    var dstY = dstRect.Y + dstRect.Height * gu;
                    var dstW = (srcU1 - srcU0) * (gridSize / srcRect.Width) * dstRect.Width;
                    var dstH = (srcV1 - srcV0) * (gridSize / srcRect.Height) * dstRect.Height;

                    drawTexture(
                        texture: tex,
                        srcRect: new Glib.Rectangle(
                            srcU0 * gridSize,
                            srcV0 * gridSize,
                            (srcU1 - srcU0) * gridSize,
                            (srcV1 - srcV0) * gridSize
                        ),
                        dstRect: new Glib.Rectangle(dstX, dstY, dstW, dstH)
                    );

                }
            }
        }
    }

    public void DrawRectangle(Rectangle srcRect, Rectangle dstRect, TextureDrawMethod drawTexture)
        => DrawRectangle(Raylib.ToGlibRectangle(srcRect), Raylib.ToGlibRectangle(dstRect), drawTexture);

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
            new Glib.Rectangle(dstRec.Position, dstRec.Size)
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