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
    private static AssetManager? assetManager = null;

    private static FileBrowser? fileBrowser = null;

    private static TaskCompletionSource<bool>? overwritePromptTcs = null;
    private static string? overwritePromptTarget = null;
    private static Task? mergeTask = null;

    private static async Task<bool> PromptOverwrite(string name)
    {
        overwritePromptTcs = new();
        overwritePromptTarget = name;
        var res = await overwritePromptTcs.Task;
        
        overwritePromptTarget = null;
        overwritePromptTcs = null;
        return res;
    }

    // uncolored variant
    private static void ShowCategoryList(CategoryList categoryList, ref int selected, Vector2 listSize)
    {
        var categories = categoryList.Categories;

        if (ImGui.BeginListBox("##Categories", listSize))
        {
            var drawList = ImGui.GetWindowDrawList();
            float textHeight = ImGui.GetTextLineHeight();

            for (int i = 0; i < categories.Count; i++)
            {
                var group = categories[i];

                if (group.Color.HasValue)
                {
                    // colored variant
                    var cursor = ImGui.GetCursorScreenPos();
                    
                    // pad beginning of selectable to reserve space for the color square
                    if (ImGui.Selectable("  " + group.Name, i == selected))
                    {
                        if (selected != i)
                            groupIndex = 0;
                        
                        selected = i;
                    }

                    // draw color square
                    var col = group.Color.Value;
                    drawList.AddRectFilled(
                        p_min: cursor,
                        p_max: cursor + new Vector2(10f, textHeight),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, 1f))
                    );
                }
                else
                {
                    // non-colored variant
                    if (ImGui.Selectable(group.Name, i == selected))
                    {
                        if (selected != i)
                            groupIndex = 0;
                        
                        selected = i;
                    }
                }
            }

            ImGui.EndListBox();
        }
    }

    private static void ShowItemList(CategoryList categoryList, int selected, Vector2 listSize)
    {
        var categories = categoryList.Categories;
        
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
        
        /*ImGui.SameLine();
        if (ImGui.Button("Import .zip"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportZip, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("ZIP File", null, ".zip");
        }*/

        // render file browser
        FileBrowser.Render(ref fileBrowser);

        // render merge status
        if (mergeTask is not null)
        {
            ImGuiExt.EnsurePopupIsOpen("Merging");
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);

            var popupFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove;
            if (ImGuiExt.BeginPopupModal("Merging", popupFlags))
            {
                ImGui.Text("Merging...");

                // show overwrite prompt if needed
                if (overwritePromptTarget is not null)
                {
                    ImGuiExt.EnsurePopupIsOpen("Overwrite?");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
                    if (ImGuiExt.BeginPopupModal("Overwrite?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
                    {
                        ImGui.Text($"Overwrite \"{overwritePromptTarget}\"?");
                        ImGui.Separator();
                        
                        if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
                        {
                            if (btn == 0)
                            {
                                // Yes
                                overwritePromptTcs!.SetResult(true);
                            }
                            else if (btn == 1)
                            {
                                // No
                                overwritePromptTcs!.SetResult(false);
                            }

                            ImGui.CloseCurrentPopup();
                        }
                        
                        ImGui.End();
                    }
                }

                // show error if present
                if (mergeTask.IsFaulted)
                {
                    ImGuiExt.EnsurePopupIsOpen("Error");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
                    if (ImGuiExt.BeginPopupModal("Error", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
                    {
                        ImGui.Text($"An error occured while importing the pack.");
                        ImGui.Separator();
                        
                        if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
                        {
                            ImGui.CloseCurrentPopup();
                            mergeTask = null;
                        }

                        ImGui.End();
                    }
                }

                // end merge task when completed
                else if (mergeTask.IsCompleted)
                {
                    ImGui.CloseCurrentPopup();
                    mergeTask = null;
                }

                ImGui.EndPopup();
            }
        }
    }

    private static void ImportFile(string? path)
    {
        if (mergeTask is not null) return;
        if (string.IsNullOrEmpty(path)) return;

        RainEd.Logger.Information("Import Init.txt file '{Path}'", path);

        // reload asset list in case user modified
        // (also makes it less bug-prone)
        assetManager = new AssetManager();
        
        switch (curAssetTab)
        {
            case AssetType.Tile:
                mergeTask = assetManager.TileInit.Merge(path, PromptOverwrite);
                break;
            
            case AssetType.Prop:
                mergeTask = assetManager.PropInit.Merge(path, PromptOverwrite);
                break;

            case AssetType.Material:
                mergeTask = assetManager.MaterialsInit.Merge(path, PromptOverwrite);
                break;
        }
    }

    public static void Show()
    {
        assetManager ??= new AssetManager();

        ImGui.Text("Any changes here require a restart in order to take effect.");
        ImGui.Separator();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Data Path");
        ImGui.SameLine();

        var oldPath = RainEd.Instance.AssetDataPath;
        if (FileBrowser.Button("DataPath", FileBrowser.OpenMode.Directory, ref RainEd.Instance.AssetDataPath))
        {
            // if path changed, disable asset import until user restarts Rained
            if (Path.GetFullPath(oldPath) != Path.GetFullPath(RainEd.Instance.AssetDataPath))
            {
                assetManager = new AssetManager();
            }
        }
        ImGui.Separator();

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

                ShowCategoryList(assetManager.TileInit, ref selectedTileCategory, new Vector2(halfWidth, boxHeight));
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

                ShowCategoryList(assetManager.PropInit, ref selectedPropCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(assetManager.PropInit, selectedPropCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            /*if (ImGui.BeginTabItem("Materials"))
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
            }*/

            ImGui.EndTabBar();
        }
    }

    public static void Unload()
    {
        if (assetManager is not null)
            assetManager = null;
        
        selectedTileCategory = 0;
        selectedPropCategory = 0;
        selectedMatCategory = 0;
        groupIndex = 0;
    }
}