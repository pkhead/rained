namespace Rained.EditorGui.Editors;
using Raylib_cs;
using System.Numerics;
using Rained.Assets;
using ImGuiNET;
using Rained.Rendering;

interface ITileSelectionState
{
    public int SelectedTileGroup { get; set; }
    public Tile? SelectedTile { get; }
    public void SelectTile(Tile tile);
}

class TileCatalogWidget(ITileSelectionState selectionState) : TileEditorCatalog
{
    private readonly ITileSelectionState state = selectionState;
    private RlManaged.RenderTexture2D? _hoverPreview = null;
    private readonly List<int> tileSearchResults = [];

    protected override void ProcessSearch(string searchQuery)
    {
        var tileDb = RainEd.Instance.TileDatabase;

        tileSearchResults.Clear();

        // find groups that have any entries that pass the search query
        for (int i = 0; i < tileDb.Categories.Count; i++)
        {
            // if search query is empty, add this group to the search query
            if (searchQuery == "")
            {
                tileSearchResults.Add(i);
                continue;
            }

            // search is not empty, so scan the tiles in this group
            // if there is one tile that that passes the search query, the
            // group gets put in the list
            // (further searching is done in DrawViewport)
            for (int j = 0; j < tileDb.Categories[i].Tiles.Count; j++)
            {
                // this tile passes the search, so add this group to the search results
                if (tileDb.Categories[i].Tiles[j].Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    tileSearchResults.Add(i);
                    break;
                }
            }
        }
    }

    public override void ShowGroupList()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        var drawList = ImGui.GetWindowDrawList();
        float textHeight = ImGui.GetTextLineHeight();

        foreach (var i in tileSearchResults)
        {
            var group = tileDb.Categories[i];
            var cursor = ImGui.GetCursorScreenPos();

            if (ImGui.Selectable("  " + group.Name, state.SelectedTileGroup == i) || tileSearchResults.Count == 1)
                state.SelectedTileGroup = i;
            
            drawList.AddRectFilled(
                p_min: cursor,
                p_max: cursor + new Vector2(10f, textHeight),
                ImGui.ColorConvertFloat4ToU32(new Vector4(group.Color.R / 255f, group.Color.G / 255f, group.Color.B / 255f, 1f))
            );
        }
    }

    public override void ShowAssetList()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        var tileList = tileDb.Categories[state.SelectedTileGroup].Tiles;

        for (int i = 0; i < tileList.Count; i++)
        {
            var tile = tileList[i];

            // don't show this prop if it doesn't pass search test
            if (!tile.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase))
                continue;
            
            if (ImGui.Selectable(tile.Name, tile == state.SelectedTile))
            {
                state.SelectTile(tile);
            }

            if (ImGui.IsItemHovered())
            {
                var fgCol = Color.White;
                var bgCol4 = ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg];
                var bgCol = new Color(
                    (byte)(bgCol4.X * 255f),
                    (byte)(bgCol4.Y * 255f),
                    (byte)(bgCol4.Z * 255f),
                    (byte)(bgCol4.W * 255f)
                );

                var contrastRatio = ContrastRatio(fgCol, bgCol);
                var invertContrast = contrastRatio < 3f;

                if (invertContrast) {
                    ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(
                        1f - bgCol4.X,
                        1f - bgCol4.Y,
                        1f - bgCol4.Z,
                        bgCol4.W
                    ));
                }

                ImGui.BeginTooltip();
                RenderTilePreview(tile);
                ImGui.EndTooltip();

                if (invertContrast) ImGui.PopStyleColor();
            }
        }
    }

    private static void RenderTileLayers(Tile tile)
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

            Raylib.DrawTexturePro(tileTexture, srcRec, dstRec, Vector2.Zero, 0f, drawCol);
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

                Raylib.DrawTexturePro(tileTexture, srcRec, dstRec, Vector2.Zero, 0f, col);
            }
        }

        Raylib.EndShaderMode();
    }

    private void RenderTilePreview(Tile tile)
    {
        var prefs = RainEd.Instance.Preferences;

        if (prefs.ViewPreviews)
        {
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

            if (_hoverPreview == null || _hoverPreview.Texture.Width != previewWidth || _hoverPreview.Texture.Height != previewHeight)
            {
                _hoverPreview?.Dispose();
                _hoverPreview = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(_hoverPreview);
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
            
            ImGuiExt.ImageRenderTextureScaled(_hoverPreview, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
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
            
            if (_hoverPreview == null || _hoverPreview.Texture.Width != previewWidth || _hoverPreview.Texture.Height != previewHeight)
            {
                _hoverPreview?.Dispose();
                _hoverPreview = RlManaged.RenderTexture2D.Load((int)previewWidth, (int)previewHeight);
            }

            Raylib.BeginTextureMode(_hoverPreview);

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

            ImGuiExt.ImageRenderTextureScaled(_hoverPreview, Vector2.One * Boot.PixelIconScale);
            //ImGuiExt.ImageRect(previewTexture!, previewWidth, previewHeight, previewRect.Value, tile.Category.Color);
        }

        return;

        // fallback case
        renderPlaceholder:
        ImGuiExt.ImageSize(RainEd.Instance.PlaceholderTexture, 16, 16);
    }

    // https://www.w3.org/TR/2008/REC-WCAG20-20081211/#relativeluminancedef
    private static float RelativeLuminance(Color color)
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
    private static float ContrastRatio(Color colorA, Color colorB)
    {
        var lumA = RelativeLuminance(colorA);
        var lumB = RelativeLuminance(colorB);

        var lMax = Math.Max(lumA, lumB);
        var lMin = Math.Min(lumA, lumB);
        return (lMax + 0.05f) / (lMin + 0.05f); 
    }
}