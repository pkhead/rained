namespace Rained.EditorGui.Editors;
using Raylib_cs;
using System.Numerics;
using Rained.Assets;
using ImGuiNET;
using Rained.Rendering;
using Rained.EditorGui.AssetPreviews;

interface ITileSelectionState
{
    public int SelectedTileGroup { get; set; }
    public Tile? SelectedTile { get; }
    public void SelectTile(Tile tile);
}

class TileCatalogWidget(ITileSelectionState selectionState) : TileEditorCatalog
{
    private readonly ITileSelectionState state = selectionState;
    private static RlManaged.RenderTexture2D? _hoverPreview = null;
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

    protected override void RenderGroupList()
    {
        var tileDb = RainEd.Instance.TileDatabase;

        foreach (var i in tileSearchResults)
        {
            var group = tileDb.Categories[i];

            if (ColoredSelectable(group.Name, group.Color, state.SelectedTileGroup == i) || tileSearchResults.Count == 1)
                state.SelectedTileGroup = i;
        }
    }

    protected override void RenderItemList()
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

                bool invertContrast = TilePreview.ShouldInvertContrast(fgCol, bgCol);

                if (invertContrast) {
                    ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(
                        1f - bgCol4.X,
                        1f - bgCol4.Y,
                        1f - bgCol4.Z,
                        bgCol4.W
                    ));
                }

                ImGui.BeginTooltip();
                TilePreview.RenderTilePreview(tile, ref _hoverPreview);
                ImGui.EndTooltip();

                if (invertContrast) ImGui.PopStyleColor();
            }
        }
    }
}