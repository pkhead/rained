/**
* This is used by PreferencesWindow
*/
using Rained.Assets;
using ImGuiNET;
using System.Numerics;
namespace Rained.EditorGui;

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
    private static readonly List<string> missingDirs = []; // data directory validation
    private static string errorMsg = string.Empty;

    // variables related to the merge process
    private static TaskCompletionSource<PromptResult>? mergePromptTcs = null;
    private static PromptOptions? mergePrompt = null;
    private static TaskCompletionSource<int>? importOptionTcs = null;
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
    private static void ShowCategoryList(AssetManager.CategoryListIndex listIndex, ref int selected, Vector2 listSize)
    {
        var categories = assetManager!.GetCategories(listIndex);

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

    private static void ShowItemList(AssetManager.CategoryListIndex categoryList, int selected, Vector2 listSize)
    {
        var categories = assetManager!.GetCategories(categoryList);
        
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

    private static void DeleteCategory(AssetManager.CategoryListIndex assetIndex, ref int selected)
    {
        var categories = assetManager!.GetCategories(assetIndex);
        if (categories.Count == 0) return;
        
        assetManager.DeleteCategory(assetIndex, selected);
        
        if (categories.Count == 0)
        {
            selected = 0;
        }
        else
        {
            selected = Math.Clamp(selected, 0, categories.Count - 1);
        }
    }

    private static void DeleteCategory()
    {
        switch (curAssetTab)
        {
            case AssetType.Tile:
                DeleteCategory(AssetManager.CategoryListIndex.Tile, ref selectedTileCategory);
                break;

            case AssetType.Prop:
                DeleteCategory(AssetManager.CategoryListIndex.Prop, ref selectedPropCategory);
                break;

            case AssetType.Material:
                DeleteCategory(AssetManager.CategoryListIndex.Materials, ref selectedMatCategory);
                break;
        }
    }

    private static void DeleteItem(AssetManager.CategoryListIndex assetIndex, int selectedCategory)
    {
        var categories = assetManager!.GetCategories(assetIndex);

        var category = categories[selectedCategory];
        if (category.Items.Count == 0) return;
        assetManager.DeleteItem(assetIndex, selectedCategory, groupIndex);
        
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
                DeleteItem(AssetManager.CategoryListIndex.Tile, selectedTileCategory);
                break;

            case AssetType.Prop:
                DeleteItem(AssetManager.CategoryListIndex.Prop, selectedPropCategory);
                break;

            case AssetType.Material:
                DeleteItem(AssetManager.CategoryListIndex.Materials, selectedMatCategory);
                break;
        }
    }

    private static void AssetControls()
    {
        // render file browser
        FileBrowser.Render(ref fileBrowser);

        if (ImGui.Button("Import Init.txt"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportFileCallback, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("Text file", ".txt");
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

        ImGui.SameLine();
        if (ImGui.Button("Apply"))
        {
            assetManager!.Commit();
        }
        
        /*ImGui.SameLine();
        if (ImGui.Button("Import .zip"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportZip, RainEd.Instance.AssetDataPath);
            fileBrowser.AddFilter("ZIP File", ".zip");
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
                var categories = assetManager!.GetCategories(GetCurrentAssetList());

                if (wantDelete == 1)
                {
                    ImGui.TextUnformatted($"Are you sure you want to delete the category \"{categories[idx].Name}\"?");
                }
                else if (wantDelete == 2)
                {
                    ImGui.TextUnformatted($"Are you sure you want to delete the asset \"{categories[idx].Items[groupIndex].Name}\"?");
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
                
                if (importOptionTcs is not null)
                {
                    ImGuiExt.EnsurePopupIsOpen("Choose...");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
                    if (ImGuiExt.BeginPopupModal("Choose...", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
                    {
                        ImGui.TextUnformatted("Please choose the desired import method.");
                        ImGui.Separator();

                        if (ImGui.Button("Replace", StandardPopupButtons.ButtonSize))
                        {
                            importOptionTcs.SetResult(0);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SetItemTooltip("Replaces the current init file with the imported one.");

                        ImGui.SameLine();
                        if (ImGui.Button("Append", StandardPopupButtons.ButtonSize))
                        {
                            importOptionTcs.SetResult(1);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SetItemTooltip("Appends the imported init file to the current one.");

                        ImGui.SameLine();
                        if (ImGui.Button("Merge", StandardPopupButtons.ButtonSize))
                        {
                            importOptionTcs.SetResult(2);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SetItemTooltip("Intelligently merges the two init files, attempting to prevent duplicates.");

                        ImGui.SameLine();
                        if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
                        {
                            importOptionTcs.SetResult(-1);
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
                        var exception = mergeTask.Exception.InnerExceptions[0];
                        if (exception is MergeException)
                        {
                            ImGui.TextUnformatted(exception.Message);
                        }
                        else
                        {
                            ImGui.Text($"An error occured while importing the pack.");
                        }

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
                    Log.Information("Merge was canceled");
                    
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

    private static async Task ImportFile(string path)
    {
        var newInit = new CategoryList(path, curAssetTab != AssetType.Material);

        importOptionTcs = new();
        var res = await importOptionTcs.Task;
        importOptionTcs = null;

        // 0: Replace
        // 1: Append
        // 2: Merge

        if (res == 0)
            assetManager!.Replace(GetCurrentAssetList(), newInit);

        else if (res == 1)
            assetManager!.Append(GetCurrentAssetList(), newInit);
        
        else
            Log.Information("Init.txt import was cancelled");
    }

    private static void ImportFileCallback(string? path)
    {
        if (mergeTask is not null) return;
        if (string.IsNullOrEmpty(path)) return;

        Log.Information("Import Init.txt file '{Path}'", path);
        mergeTask = ImportFile(path);
    }

    private static AssetManager.CategoryListIndex GetCurrentAssetList()
        => curAssetTab switch
        {
            AssetType.Tile => AssetManager.CategoryListIndex.Tile,
            AssetType.Prop => AssetManager.CategoryListIndex.Prop,
            AssetType.Material => AssetManager.CategoryListIndex.Materials,
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

    public static void SetDataPath(string newPath)
    {
        var oldPath = RainEd.Instance.AssetDataPath;

        try
        {
            // check for any missing directories
            missingDirs.Clear();
            missingDirs.Add("Graphics");
            missingDirs.Add("Props");
            missingDirs.Add("Levels");

            for (int i = missingDirs.Count - 1; i >= 0; i--)
            {
                if (Directory.Exists(Path.Combine(newPath, missingDirs[i])))
                {
                    missingDirs.RemoveAt(i);
                }
            }

            if (missingDirs.Count == 0)
            {
                RainEd.Instance.AssetDataPath = newPath;
                assetManager = new AssetManager();
            }
        }
        catch (Exception e)
        {
            Log.UserLogger.Error(e.ToString());
            errorMsg = "Malformed Init.txt file in the new data folder";

            RainEd.Instance.AssetDataPath = oldPath;
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
        var newPath = oldPath;
        if (FileBrowser.Button("DataPath", FileBrowser.OpenMode.Directory, ref newPath))
        {
            // if path changed, disable asset import until user restarts Rained
            if (Path.GetFullPath(oldPath) != Path.GetFullPath(newPath))
                SetDataPath(newPath);
        }
        ImGui.Separator();

        // show missing directory prompt if necessary
        if (missingDirs.Count > 0)
        {
            ImGuiExt.EnsurePopupIsOpen("Error");
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            if (ImGuiExt.BeginPopupModal("Error", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.Text("The given data folder is missing the following subdirectories:");
                foreach (var dir in missingDirs)
                {
                    ImGui.BulletText(dir);
                }

                ImGui.Separator();
                if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
                {
                    ImGui.CloseCurrentPopup();
                    missingDirs.Clear();
                }

                ImGui.EndPopup();
            }
        }

        // general error message
        if (!string.IsNullOrEmpty(errorMsg))
        {
            ImGuiExt.EnsurePopupIsOpen("Error");
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            if (ImGuiExt.BeginPopupModal("Error", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.TextUnformatted(errorMsg);

                ImGui.Separator();
                if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
                {
                    ImGui.CloseCurrentPopup();
                    errorMsg = string.Empty;
                }

                ImGui.EndPopup();
            }
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

                ShowCategoryList(AssetManager.CategoryListIndex.Tile, ref selectedTileCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(AssetManager.CategoryListIndex.Tile, selectedTileCategory, new Vector2(halfWidth, boxHeight));

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

                ShowCategoryList(AssetManager.CategoryListIndex.Prop, ref selectedPropCategory, new Vector2(halfWidth, boxHeight));
                ShowItemList(AssetManager.CategoryListIndex.Prop, selectedPropCategory, new Vector2(halfWidth, boxHeight));

                ImGui.EndTabItem();
            }

            if (assetManager.GetCategories(AssetManager.CategoryListIndex.Materials) is not null)
            {
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

                    ShowCategoryList(AssetManager.CategoryListIndex.Materials, ref selectedMatCategory, new Vector2(halfWidth, boxHeight));
                    ShowItemList(AssetManager.CategoryListIndex.Materials, selectedMatCategory, new Vector2(halfWidth, boxHeight));

                    ImGui.EndTabItem();
                }
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