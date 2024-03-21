/**
* This is used by PreferencesWindow
*/
using RainEd.Assets;
using ImGuiNET;
using System.Numerics;
namespace RainEd;

static class AssetManagerGUI
{
    enum AssetType
    {
        Tile, Prop, Material
    }
    
    private static AssetType curAssetTab = AssetType.Tile;
    private static int selectedTileCategory = 0;
    private static int selectedPropCategory = 0;
    private static int selectedMatCategory = 0;
    private static int groupIndex = 0;
    private static AssetManager assetManager = null!;
    private static bool needRestart = false;

    private static FileBrowser? fileBrowser = null;

    // uncolored variant
    private static void ShowCategoryList<T>(List<T> categories, ref int selected, Vector2 listSize)
        where T : InitCategory
    {
        if (ImGui.BeginListBox("##Categories", listSize))
        {
            for (int i = 0; i < categories.Count; i++)
            {
                var group = categories[i];
                var cursor = ImGui.GetCursorScreenPos();
                if (ImGui.Selectable(group.Name, i == selected))
                {
                    if (selected != i)
                        groupIndex = 0;
                    
                    selected = i;
                }
            }

            ImGui.EndListBox();
        }
    }

    // colored variant
    private static void ShowColoredCategoryList(List<ColoredInitCategory> categories, ref int selected, Vector2 listSize)
    {
        if (ImGui.BeginListBox("##Categories", listSize))
        {
            var drawList = ImGui.GetWindowDrawList();
            float textHeight = ImGui.GetTextLineHeight();

            for (int i = 0; i < categories.Count; i++)
            {
                var group = categories[i];
                var cursor = ImGui.GetCursorScreenPos();
                if (ImGui.Selectable("  " + group.Name, i == selected))
                {
                    if (selected != i)
                        groupIndex = 0;
                    
                    selected = i;
                }

                drawList.AddRectFilled(
                    p_min: cursor,
                    p_max: cursor + new Vector2(10f, textHeight),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(group.Color.R / 255f, group.Color.G / 255f, group.Color.B / 255, 1f))
                );
            }

            ImGui.EndListBox();
        }
    }

    private static void ShowItemList<T>(List<T> categories, int selected, Vector2 listSize)
        where T : ColoredInitCategory
    {
        // group listing list box
        ImGui.SameLine();
        if (ImGui.BeginListBox("##Items", listSize))
        {
            var itemList = categories[selected].Items;

            for (int i = 0; i < itemList.Count; i++)
            {
                var tile = itemList[i];

                // don't show this prop if it doesn't pass search test
                //if (!tile.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                //    continue;
                
                if (ImGui.Selectable(tile.Name, i == groupIndex))
                {
                    groupIndex = i;
                }
            }

            ImGui.EndListBox();
        }
    }

    private static void AssetControls()
    {
        static bool isInitFile(string path, bool isRw)
        {
            return Path.GetFileName(path) == "Init.txt";
        }

        if (ImGui.Button("Import Init.txt"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportFile, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("Init.txt File", isInitFile, ".txt");
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Import .zip"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportZip, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("ZIP File", null, ".zip");
        }

        // render file browser
        FileBrowser.Render(ref fileBrowser);
    }

    private static void ImportFile(string? path)
    {
        if (path is null) return;

        RainEd.Logger.Information("Import Init.txt file '{Path}'", path);
    }

    private static void ImportZip(string? path)
    {
        if (path is null) return;

        RainEd.Logger.Information("Import zip file '{Path}'", path);
    }

    public static void Show()
    {
        assetManager ??= new AssetManager();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Data Path");
        ImGui.SameLine();

        var oldPath = RainEd.Instance.AssetDataPath;
        if (FileBrowser.Button("DataPath", FileBrowser.OpenMode.Directory, ref RainEd.Instance.AssetDataPath))
        {
            // if path changed, disable asset import until user restarts Rained
            if (Path.GetFullPath(oldPath) != Path.GetFullPath(RainEd.Instance.AssetDataPath))
            {
                needRestart = true;
            }
        }
        ImGui.Separator();
        
        if (needRestart)
        {
            ImGui.Text("(A restart is required before making further changes)");
            ImGui.BeginDisabled();
        }

        // show tile database
        if (ImGui.BeginTabBar("AssetType"))
        {
            if (ImGui.BeginTabItem("Tiles"))
            {
                // set group index to 0 when tab changed
                if (curAssetTab != AssetType.Tile)
                {
                    groupIndex = 0;
                    curAssetTab = AssetType.Tile;
                }

                AssetControls();
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetContentRegionAvail().Y;

                ShowColoredCategoryList(assetManager.TileInit, ref selectedTileCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(assetManager.TileInit, selectedTileCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Props"))
            {
                // set group index to 0 when tab changed
                if (curAssetTab != AssetType.Prop)
                {
                    groupIndex = 0;
                    curAssetTab = AssetType.Prop;
                }

                AssetControls();
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetContentRegionAvail().Y;

                ShowColoredCategoryList(assetManager.PropInit, ref selectedPropCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(assetManager.PropInit, selectedPropCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Materials"))
            {
                // set group index to 0 when tab changed
                if (curAssetTab != AssetType.Material)
                {
                    groupIndex = 0;
                    curAssetTab = AssetType.Material;
                }

                AssetControls();
                var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
                var boxHeight = ImGui.GetContentRegionAvail().Y;

                ShowCategoryList(assetManager.MaterialsInit, ref selectedMatCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(assetManager.TileInit, selectedMatCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (needRestart)
        {
            ImGui.EndDisabled();
        }
    }
}