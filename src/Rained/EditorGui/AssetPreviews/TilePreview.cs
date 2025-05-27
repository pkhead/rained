namespace Rained.EditorGui.AssetPreviews;
using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using Rained.Assets;
using Rained.Rendering;

static class TilePreview
{
    public static void RenderTileLayers(Tile tile)
    {
        var tileTexture = RainEd.Instance.AssetGraphics.GetTileTexture(tile.Name)
            ?? throw new Exception("Could not load tile graphics");
        
        var totalTileWidth = tile.Width + tile.BfTiles * 2;
        var totalTileHeight = tile.Height + tile.BfTiles * 2;

        Raylib.BeginShaderMode(Shaders.TileShader);
        Color drawCol = tile.Category.Color;
        var dstRec = new Rectangle(0, 0, totalTileWidth * 20, totalTileHeight * 20);

        // draw front of box tile
        if (tile.Type == TileType.Box)
        {
            var height = tile.Height * 20;
            var srcRec = new Rectangle(
                0f,
                tile.ImageYOffset + height * tile.Width,
                totalTileWidth * 20, totalTileHeight * 20
            );

            tileTexture.DrawRectangle(srcRec, dstRec, drawCol);
        }

        // draw voxel tile
        else
        {
            for (int l = tile.LayerCount - 1; l >= 0; l--)
            {
                var srcRec = Rendering.TileRenderer.GetGraphicSublayer(tile, l, 0);

                float lf = (float)l / tile.LayerCount;

                // fade to white as the layer is further away
                // from the front
                float a = lf;
                var col = new Color
                {
                    R = (byte)(drawCol.R * (1f - a) + (drawCol.R * 0.5) * a),
                    G = (byte)(drawCol.G * (1f - a) + (drawCol.G * 0.5) * a),
                    B = (byte)(drawCol.B * (1f - a) + (drawCol.B * 0.5) * a),
                    A = 255
                };

                tileTexture.DrawRectangle(srcRec, dstRec, col);
            }
        }

        Raylib.EndShaderMode();
    }

    /// <summary>
    /// Render tile preview image, as seen when hovering over a tile in the tile catalog.
    /// </summary>
    /// <param name="framebuffer">Intermediary framebuffer to use.</param>
    public static void RenderTilePreview(Tile tile, ref RlManaged.RenderTexture2D? framebuffer)
    {
        var prefs = RainEd.Instance.Preferences;

        if (prefs.ViewPreviews)
        {
            if (tile is null)
                goto renderPlaceholder;

            var tileTexture = RainEd.Instance.AssetGraphics.GetTileTexture(tile.Name);
            if (tileTexture is null)
                goto renderPlaceholder;

            var totalTileWidth = tile.Width + tile.BfTiles * 2;
            var totalTileHeight = tile.Height + tile.BfTiles * 2;
            
            var previewWidth = totalTileWidth * 20;
            var previewHeight = totalTileHeight * 20;
            if (prefs.ViewTileSpecsOnTooltip)
            {
                previewHeight += totalTileHeight * 20;
                previewHeight += 3; // spacing inbetween graphics and geometry
            }
            
            previewWidth += 2;
            previewHeight += 2;

            if (framebuffer is null || framebuffer.Texture.Width != previewWidth || framebuffer.Texture.Height != previewHeight)
            {
                framebuffer?.Dispose();
                framebuffer = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(framebuffer);
            Raylib.ClearBackground(Color.Blank);
            Rlgl.PushMatrix();
            Rlgl.Translatef(1f, 1f, 0f);

            // first, draw tile graphics
            RenderTileLayers(tile);

            // show tile inner border
            Raylib.DrawRectangleLines(tile.BfTiles * 20, tile.BfTiles * 20, tile.Width * 20, tile.Height * 20,
                new Color(255, 255, 255, 200));

            // show tile outer border
            Raylib.DrawRectangleLines(0, 0, totalTileWidth * 20, totalTileHeight * 20,
                new Color(255, 255, 255, 150));
            
            // then, draw tile specs
            if (prefs.ViewTileSpecsOnTooltip)
            {
                Rlgl.Translatef(0f, totalTileHeight * 20 + 3f, 0f);
                Rlgl.Translatef(tile.BfTiles * 20f, tile.BfTiles * 20f, 0f);
                TileRenderer.DrawTileSpecs(tile, 0, 0,
                    tileSize: 20
                );
            }

            Rlgl.PopMatrix();
            Raylib.EndTextureMode();
            
            ImGuiExt.ImageRenderTextureScaled(framebuffer, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
        }
        else
        {
            var previewTexFound = RainEd.Instance.AssetGraphics.GetTilePreviewTexture(tile, out var previewTexture, out var previewRect);
            if (!previewTexFound || previewTexture is null || previewRect is null)
                goto renderPlaceholder;
                
            var previewWidth = previewRect.Value.Width + 2;
            float previewHeight;

            if (prefs.ViewTileSpecsOnTooltip)
            {
                previewHeight = previewRect.Value.Height * 2 + 5;
            }            
            else
            {
                previewHeight = previewRect.Value.Height + 2;
            }
            
            if (framebuffer is null || framebuffer.Texture.Width != previewWidth || framebuffer.Texture.Height != previewHeight)
            {
                framebuffer?.Dispose();
                framebuffer = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(framebuffer);

            // image will have 1 pixel of padding to accomodate for
            // lines
            Raylib.ClearBackground(Color.Blank);
            Rlgl.PushMatrix();
            Rlgl.Translatef(1f, 1f, 0f);
            
            // draw preview texture
            Raylib.DrawTextureRec(previewTexture, previewRect.Value, Vector2.Zero, tile.Category.Color);

            // draw tile specs
            if (prefs.ViewTileSpecsOnTooltip)
            {
                Rlgl.Translatef(0f, previewRect.Value.Height + 3f, 0f);
                TileRenderer.DrawTileSpecs(tile, 0, 0,
                    tileSize: 16
                );
            }

            Rlgl.PopMatrix();
            Raylib.EndTextureMode();

            ImGuiExt.ImageRenderTextureScaled(framebuffer, Vector2.One * Boot.PixelIconScale);
            //ImGuiExt.ImageRect(previewTexture!, previewWidth, previewHeight, previewRect.Value, tile.Category.Color);
        }

        return;

        // fallback case
        renderPlaceholder:
        ImGuiExt.ImageSize(RainEd.Instance.PlaceholderTexture, 16, 16);
    }

    // https://www.w3.org/TR/2008/REC-WCAG20-20081211/#relativeluminancedef
    public static float RelativeLuminance(Color color)
    {
        static float CalcComponentValue(float v)
        {
            if (v <= 0.03928f)
                return v / 12.92f;
            else
                return MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        return
            CalcComponentValue(color.R / 255f) * 0.2126f +
            CalcComponentValue(color.G / 255f) * 0.7152f +
            CalcComponentValue(color.B / 255f) * 0.0722f;
    }

    // https://www.w3.org/TR/2008/REC-WCAG20-20081211/#contrast-ratiodef
    public static float ContrastRatio(Color colorA, Color colorB)
    {
        var lumA = RelativeLuminance(colorA);
        var lumB = RelativeLuminance(colorB);

        var lMax = Math.Max(lumA, lumB);
        var lMin = Math.Min(lumA, lumB);
        return (lMax + 0.05f) / (lMin + 0.05f); 
    }

    /// <summary>
    /// For use in tile preview popup backgrounds, determine if the background
    /// color should be inverted for better contrast.
    /// </summary>
    public static bool ShouldInvertContrast(Color fgCol, Color bgCol) => ContrastRatio(fgCol, bgCol) < 3f;
}