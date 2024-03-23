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

    // variables related to the merge process
    

    private static TaskCompletionSource<PromptResult>? mergePromptTcs = null;
    private static PromptOptions? mergePrompt = null;
    private static Task? mergeTask = null;
    

    // fields related to delete confirmation prompt
    private static bool noAskBeforeDeletion = true;
    private static int wantDelete = 0; // 0 = no, 1 = category, 2 = asset

    private static async Task<PromptResult> PromptOverwrite(PromptOptions prompt)
    {
        mergePromptTcs = new();
        mergePrompt = prompt;
        var res = await mergePromptTcs.Task;
        
        mergePromptTcs = null;
        mergePrompt = null;

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
            if (categories.Count > 0)
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
            }

            ImGui.EndListBox();
        }
    }

    private static void DeleteCategory(CategoryList assetList, ref int selected)
    {
        assetList.DeleteCategory(assetList.Categories[selected]);
        
        if (assetList.Categories.Count == 0)
        {
            selected = 0;
        }
        else
        {
            selected = Math.Clamp(selected, 0, assetList.Categories.Count - 1);
        }
    }

    private static void DeleteCategory()
    {
        switch (curAssetTab)
        {
            case AssetType.Tile:
                DeleteCategory(assetManager!.TileInit, ref selectedTileCategory);
                break;

            case AssetType.Prop:
                DeleteCategory(assetManager!.PropInit, ref selectedPropCategory);
                break;

            case AssetType.Material:
                DeleteCategory(assetManager!.MaterialsInit, ref selectedMatCategory);
                break;
        }
    }

    private static void DeleteItem(CategoryList assetList, int selectedCategory)
    {
        var category = assetList.Categories[selectedCategory];
        assetList.DeleteItem(category.Items[groupIndex]);
        
        if (category.Items.Count == 0)
        {
            groupIndex = 0;
        }
        else
        {
            groupIndex = Math.Clamp(groupIndex, 0, category.Items.Count - 1);
        }
    }

    private static void DeleteItem()
    {
        switch (curAssetTab)
        {
            case AssetType.Tile:
                DeleteItem(assetManager!.TileInit, selectedTileCategory);
                break;

            case AssetType.Prop:
                DeleteItem(assetManager!.PropInit, selectedPropCategory);
                break;

            case AssetType.Material:
                DeleteItem(assetManager!.MaterialsInit, selectedMatCategory);
                break;
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

        int deleteReq = 0;
        ImGui.SameLine();
        if (ImGui.Button("Delete Category"))
        {
            deleteReq = 1;
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete Asset"))
        {
            deleteReq = 2;
        }
        
        /*ImGui.SameLine();
        if (ImGui.Button("Import .zip"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportZip, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("ZIP File", null, ".zip");
        }*/

        // process delete request
        // if noAskBeforeDeletion is true, immediately perform the operation
        // otherwise, show the delete confirmation prompt
        if (deleteReq > 0)
        {
            if (noAskBeforeDeletion)
            {
                if (deleteReq == 1)
                    DeleteCategory();
                else if (deleteReq == 2)
                    DeleteItem();
            }
            else
            {
                wantDelete = deleteReq;
            }
        }

        // delete confirmation prompt
        if (wantDelete > 0)
        {
            ImGuiExt.EnsurePopupIsOpen("Delete?");
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);

            if (ImGuiExt.BeginPopupModal("Delete?", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                int idx = GetCurrentCategoryIndex();

                if (wantDelete == 1)
                {
                    ImGui.TextUnformatted($"Are you sure you want to delete the category \"{GetCurrentAssetList().Categories[idx].Name}\"");
                }
                else if (wantDelete == 2)
                {
                    ImGui.TextUnformatted($"Are you sure you want to delete the asset \"{GetCurrentAssetList().Categories[idx].Items[groupIndex].Name}\"");
                }

                ImGui.Separator();

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.Checkbox("Don't ask me next time", ref noAskBeforeDeletion);
                ImGui.PopStyleVar();

                if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
                {
                    if (btn == 0) // if yes was pressed
                    {
                        if (wantDelete == 1)
                            DeleteCategory();
                        else if (wantDelete == 2)
                            DeleteItem();
                    }

                    // if no was pressed
                    else
                    {
                        noAskBeforeDeletion = false;
                    }

                    ImGui.CloseCurrentPopup();
                    wantDelete = 0;
                }

                ImGui.EndPopup();
            }
        }

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

                // show prompt if needed
                if (mergePrompt is not null)
                {
                    ImGuiExt.EnsurePopupIsOpen("Action Needed");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
                    if (ImGuiExt.BeginPopupModal("Action Needed", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
                    {
                        ImGui.TextUnformatted(mergePrompt.Text);

                        if (mergePrompt.CheckboxText.Length > 0)
                        {
                            for (int i = 0; i < mergePrompt.CheckboxText.Length; i++)
                            {
                                ImGui.Checkbox(mergePrompt.CheckboxText[i], ref mergePrompt.CheckboxValues[i]);
                            }

                            ImGui.Separator();
                            if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btn))
                            {
                                if (btn == 0)
                                    mergePromptTcs!.SetResult(PromptResult.Yes);

                                else if (btn == 1)
                                    mergePromptTcs!.SetCanceled();

                                ImGui.CloseCurrentPopup();
                            }
                        }
                        
                        // yes/no prompt
                        else
                        {
                            ImGui.Separator();
                            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
                            {
                                if (btn == 0)
                                {
                                    // Yes
                                    mergePromptTcs!.SetResult(PromptResult.Yes);
                                }
                                else if (btn == 1)
                                {
                                    // No
                                    mergePromptTcs!.SetResult(PromptResult.No);
                                }

                                ImGui.CloseCurrentPopup();
                            }

                            ImGui.SameLine();
                            if (ImGui.Button("Yes To All", StandardPopupButtons.ButtonSize))
                            {
                                mergePromptTcs!.SetResult(PromptResult.YesToAll);
                                ImGui.CloseCurrentPopup();
                            }

                            ImGui.SameLine();
                            if (ImGui.Button("No To All", StandardPopupButtons.ButtonSize))
                            {
                                mergePromptTcs!.SetResult(PromptResult.NoToAll);
                                ImGui.CloseCurrentPopup();
                            }
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
                            assetManager = new AssetManager();
                        }

                        ImGui.End();
                    }
                }

                else if (mergeTask.IsCanceled)
                {
                    RainEd.Logger.Information("Merge was canceled");
                    
                    ImGui.CloseCurrentPopup();
                    mergeTask = null;
                    assetManager = new AssetManager();
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
        mergeTask = GetCurrentAssetList().Merge(path, PromptOverwrite);
    }

    private static CategoryList GetCurrentAssetList()
        => curAssetTab switch
        {
            AssetType.Tile => assetManager!.TileInit,
            AssetType.Prop => assetManager!.PropInit,
            AssetType.Material => assetManager!.MaterialsInit,
            _ => throw new ArgumentOutOfRangeException(nameof(curAssetTab))
        };
    
    private static int GetCurrentCategoryIndex()
        => curAssetTab switch
        {
            AssetType.Tile => selectedTileCategory,
            AssetType.Prop => selectedPropCategory,
            AssetType.Material => selectedMatCategory,
            _ => throw new ArgumentOutOfRangeException(nameof(curAssetTab))
        };

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
                ShowItemList(assetManager.MaterialsInit, selectedMatCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public static void Unload()
    {
        if (assetManager is not null)
            assetManager = null;
        
        noAskBeforeDeletion = false;
        selectedTileCategory = 0;
        selectedPropCategory = 0;
        selectedMatCategory = 0;
        groupIndex = 0;
    }
}