/**
* This is used by PreferencesWindow
*/
using Rained.Assets;
using ImGuiNET;
using System.Numerics;
using Rained.EditorGui.Editors;
using Raylib_cs;
using Rained.LevelData;
using System.IO;
namespace Rained.EditorGui;

static class AssetManagerGUI
{
    public enum AssetType
    {
        Tile, Prop, Material
    }
    
    internal static AssetType curAssetTab = AssetType.Tile;
    private static int selectedTileCategory = 0;
	private static int selectedTileCategory2 = 0;
	private static int selectedPropCategory = 0;
	private static int selectedPropCategory2 = 0;
	private static int selectedMatCategory = 0;
    private static int groupIndex = 0;
	private static int groupIndex2 = 0;
	private static int exportGroupIndex = 0;
	private static int exportContenderIndex = 0;
	private static AssetManager? assetManager = null;
	private static string searchQuery = "";
	private static bool categoryEditMode;
	private static CategoryList.InitItem? _replaceItem;
	private static CategoryList.InitItem? _draggingItem;
	private static CategoryList.InitCategory? _dragToCategory;
	private static CategoryList.InitCategory? _draggedCategory;

	private static FileBrowser? fileBrowser = null;
    private static readonly List<string> missingDirs = []; // data directory validation
    private static string errorMsg = string.Empty;
	private static bool unsavedChanges;

	// variables related to the merge process
	private static TaskCompletionSource<PromptResult>? mergePromptTcs = null;
    private static PromptOptions? mergePrompt = null;
    private static TaskCompletionSource<int>? importOptionTcs = null;
    private static Task? mergeTask = null;
    

    // fields related to delete confirmation prompt
    private static bool noAskBeforeDeletion = false;
	private static int wantDelete = 0; // 0 = no, 1 = category, 2 = asset

	// fields related to edit prompt
	private static int wantEdit; // 0 = no, 1 = category, 2 = asset
	private static string newItemName = "";
	private static Vector3 categoryColor = new(1f, 0f, 0f);
	private static bool showColorPicker;

	// fields related to the export prompt
	private static int wantExport;
	internal static Dictionary<CategoryList.InitCategory, List<CategoryList.InitItem>> pendingExportFiles = [];
	private static int selectedExportCategory = -1;
	internal static AssetType nextAssetTab;
	internal static bool firstOpen = false;
	private static Task? exportTask;
	private static string exportLocation;
	private static readonly List<(int, CategoryList.InitCategory)> searchResults = [];

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
    private unsafe static void ShowCategoryList(AssetManager.CategoryListIndex listIndex, ref int selected, Vector2 listSize, int tab)
    {
		if (categoryEditMode && tab == 1)
		{
			ImGui.Separator();
			ImGui.Text("Rearrange Mode");
			ImGui.SameLine();
			ImGui.TextDisabled("(?)");
			ImGui.SetItemTooltip(
					"""
					Click and drag to move items
					between categories.
					""");

		}

		if (ImGui.BeginListBox($"##Categories{tab}", listSize))
        {
            var drawList = ImGui.GetWindowDrawList();
            float textHeight = ImGui.GetTextLineHeight();

			const string leftPadding = "  ";
			float colorWidth = ImGui.CalcTextSize(leftPadding).X - ImGui.GetStyle().ItemInnerSpacing.X;
			foreach ((var i, var group) in searchResults)
            {
                if (group.Color.HasValue)
                {
                    // colored variant
                    var cursor = ImGui.GetCursorScreenPos();
                    
                    // pad beginning of selectable to reserve space for the color square
                    if (ImGui.Selectable("  " + group.Name, i == selected || searchResults.Count == 1))
                    {
						if (selected != i)
						{
							if (tab == 0)
								groupIndex = 0;
							else if (tab == 1)
								groupIndex2 = 0;
						}
						
						selected = i;
                    }

                    // draw color square
                    var col = group.Color.Value;
                    drawList.AddRectFilled(
                        p_min: cursor,
                        p_max: cursor + new Vector2(colorWidth, textHeight),
                        ImGui.ColorConvertFloat4ToU32(new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, 1f))
                    );
                }
                else
                {
                    // non-colored variant
                    if (ImGui.Selectable(group.Name, i == selected || searchResults.Count == 1))
                    {
						if (selected != i)
						{
							if (tab == 0)
								groupIndex = 0;
							else if (tab == 1)
								groupIndex2 = 0;
						}
						selected = i;
                    }
                }

				if (ImGui.BeginDragDropSource())
				{
					_draggedCategory = group;
					ImGui.SetDragDropPayload("InitCategory", IntPtr.Zero, 0);

					ImGui.EndDragDropSource();
				}

				if (ImGui.BeginDragDropTarget())
				{
					ImGui.AcceptDragDropPayload("InitCategory");
					ImGui.AcceptDragDropPayload("InitItem");
					if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
					{
						_dragToCategory = group;
						if (_draggingItem != null)
						{
							MoveItem(tab);
						}
						else if (_draggedCategory != null)
						{
							MoveCategory(tab);
						}
					}

					ImGui.EndDragDropTarget();
				}

				if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
				{
					categoryEditMode = !categoryEditMode;
				}

				//if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
				//{
				//	int nextIndex = i + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
				//	if (nextIndex >= 0 && nextIndex < categories.Count)
				//	{
				//		categories[i] = categories[nextIndex];
				//		categories[nextIndex] = group;
				//		ImGui.ResetMouseDragDelta();

				//		selected = nextIndex;

				//		MoveCategory(i, nextIndex);
				//	}
				//}

				//if (ImGui::IsItemActive() && !ImGui::IsItemHovered())
				//{
				//	int n_next = n + (ImGui::GetMouseDragDelta(0).y < 0.f ? -1 : 1);
				//	if (n_next >= 0 && n_next < IM_ARRAYSIZE(item_names))
				//	{
				//		item_names[n] = item_names[n_next];
				//		item_names[n_next] = item;
				//		ImGui::ResetMouseDragDelta();
				//	}
				//}
			}

            ImGui.EndListBox();
        }
    }


	private unsafe static void ShowItemList(AssetManager.CategoryListIndex categoryList, int selected, Vector2 listSize, int tab)
    {
        var categories = assetManager!.GetCategories(categoryList);
        
        // group listing list box
        ImGui.SameLine();
        if (ImGui.BeginListBox($"##Items{tab}", listSize))
        {
            if (categories.Count > 0)
            {
                var itemList = categories[selected].Items;

                for (int i = 0; i < itemList.Count; i++)
                {
                    var tile = itemList[i];

					// don't show this prop if it doesn't pass search test
					if (!tile.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
						continue;

					if (ImGui.Selectable(tile.Name, (tab == 0 && i == groupIndex) || (tab == 1 && i == groupIndex2)))
                    {
						if (tab == 0)
							groupIndex = i;
						else if (tab == 1)
							groupIndex2 = i;
                    }

					ReAddTilePreviews(tile, selected);

					if (ImGui.BeginDragDropSource())
					{
						_draggingItem = tile;
						ImGui.SetDragDropPayload("InitItem", IntPtr.Zero, 0);
						ImGui.Text($"{tile.RawLine}");
						ImGui.EndDragDropSource();
					}

					if (_draggingItem != null && ImGui.BeginDragDropTarget())
					{
						ImGui.AcceptDragDropPayload("InitItem");
						if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
						{
							_replaceItem = tile;
							MoveItem(tab);
						}

						ImGui.EndDragDropTarget();
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
	private static void MoveCategory(int tab)
	{
		unsavedChanges = true;
		switch (curAssetTab)
		{
			case AssetType.Tile:
				MoveCategory(AssetManager.CategoryListIndex.Tile, tab == 0 ? selectedTileCategory : selectedTileCategory2);
				break;

			case AssetType.Prop:
				MoveCategory(AssetManager.CategoryListIndex.Prop, tab == 0 ? selectedPropCategory : selectedPropCategory2);
				break;

			case AssetType.Material:
				MoveCategory(AssetManager.CategoryListIndex.Materials, selectedMatCategory);
				break;
		}
	}

	private static void MoveCategory(AssetManager.CategoryListIndex index, int selectedCategory)
	{
		var categories = assetManager!.GetCategories(index);
		var category = categories[selectedCategory];

		if (_draggedCategory == null || _dragToCategory == null)
			throw new Exception("Dragged item or target is null.");

		if (categories.Contains(_dragToCategory))
			assetManager.MoveCategory(index, _draggedCategory, _dragToCategory);

		_dragToCategory = null;
		_draggedCategory = null;
	}

	private static void MoveItem(int tab)
	{
		unsavedChanges = true;
		switch (curAssetTab)
		{
			case AssetType.Tile:
				MoveItem(AssetManager.CategoryListIndex.Tile, tab == 0 ? selectedTileCategory : selectedTileCategory2);
				break;

			case AssetType.Prop:
				MoveItem(AssetManager.CategoryListIndex.Prop, tab == 0 ? selectedPropCategory : selectedPropCategory2);
				break;

			case AssetType.Material:
				MoveItem(AssetManager.CategoryListIndex.Materials, selectedMatCategory);
				break;
		}
	}

	private static void MoveItem(AssetManager.CategoryListIndex index, int selectedCategory)
	{
		var categories = assetManager!.GetCategories(index);
		var category = categories[selectedCategory];

		if (_draggingItem == null) 
			throw new Exception("Dragged item is null.");

		if (!category.Items.Contains(_draggingItem))
			assetManager.MoveItem(index, _draggingItem, category, _replaceItem);
		else if (_dragToCategory != null && _dragToCategory != category)
			assetManager.MoveItem(index, _draggingItem, _dragToCategory);
		else if (_replaceItem != null)
			assetManager.MoveItem(index, category, _draggingItem, _replaceItem);

		_draggingItem = null;
		_replaceItem = null;
		_dragToCategory = null;
	}

	private static void UpdateCategory(AssetManager.CategoryListIndex assetIndex, int selectedCategory)
	{
		assetManager!.UpdateCategory(assetIndex, selectedCategory);
	}

	private static void UpdateCategory()
	{
		switch (curAssetTab)
		{
			case AssetType.Tile:
				UpdateCategory(AssetManager.CategoryListIndex.Tile, selectedTileCategory);
				break;

			case AssetType.Prop:
				UpdateCategory(AssetManager.CategoryListIndex.Prop, selectedPropCategory);
				break;

			case AssetType.Material:
				UpdateCategory(AssetManager.CategoryListIndex.Materials, selectedMatCategory);
				break;
		}
	}

	//private static void MoveCategory(int selected, int next)
	//{
	//	switch (curAssetTab)
	//	{
	//		case AssetType.Tile:
	//			MoveCategory(AssetManager.CategoryListIndex.Tile, selected, next);
	//			break;

	//		case AssetType.Prop:
	//			MoveCategory(AssetManager.CategoryListIndex.Prop, selected, next);
	//			break;

	//		case AssetType.Material:
	//			MoveCategory(AssetManager.CategoryListIndex.Materials, selected, next);
	//			break;
	//	}
	//}

	//private static void MoveCategory(AssetManager.CategoryListIndex assetIndex, int selectedCategory, int next)
	//{
	//	if (next == 0) return;
	//	assetManager!.MoveCategory(assetIndex, selectedCategory, next);
	//}
	internal static void ProcessSearch()
	{
		searchResults.Clear();

		// normal props
		var groups = assetManager!.GetCategories(GetCurrentAssetList());

		for (int i = 0; i < groups.Count; i++)
		{
			var group = groups[i];
			foreach (var item in group.Items)
			{
				if (item.Name.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
				{
					searchResults.Add((groups.IndexOf(group), group));
					break;
				}
			}
		}
	}

	private static void AssetControls()
    {
        // render file browser
        FileBrowser.Render(ref fileBrowser);

		int deleteReq = 0;
		int editReq = 0;
		int exportReq = 0;
		if (ImGui.BeginMenuBar())
		{
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("Import Init.txt"))
				{
					fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportFileCallback, RainEd.Instance.AssetDataPath);
					fileBrowser.AddFilter("Text file", ".txt");
				}

				/*ImGui.SameLine();
				if (ImGui.Button("Import .zip"))
				{
					fileBrowser = new FileBrowser(FileBrowser.OpenMode.Read, ImportZip, RainEd.Instance.AssetDataPath);
					fileBrowser.AddFilter("ZIP File", ".zip");
				}*/

				ImGui.BeginDisabled(!unsavedChanges);
				if (ImGui.MenuItem("Apply Changes"))
				{
					unsavedChanges = false;
					assetManager!.Commit();
				}
				ImGui.EndDisabled();
				ImGui.SetItemTooltip(
						"""
					Clicking apply will commit
					any unsaved changes to
					the Init.txt.
					""");

				ImGui.Separator();

				if (ImGui.MenuItem("Export .zip"))
				{
					exportReq = 1;
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Edit"))
			{
				if (ImGui.MenuItem("Delete Category"))
				{
					deleteReq = 1;
				}
				if (ImGui.MenuItem("Delete Asset"))
				{
					deleteReq = 2;
				}
				ImGui.Separator();

				if (ImGui.MenuItem("Edit Category"))
				{
					editReq = 1;
				}
				if (ImGui.MenuItem("Edit Asset"))
				{
					editReq = 2;
				}
				ImGui.EndMenu();
			}

			ImGui.EndMenuBar();
		}


		var searchInputFlags = ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EscapeClearsAll;

		// search bar
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		if (ImGui.InputTextWithHint("##Search", "Search...", ref searchQuery, 128, searchInputFlags))
		{
			ProcessSearch();
		}

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
			unsavedChanges = true;
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

		// Processes edit request
		if (editReq > 0)
		{
			wantEdit = editReq;
		}

		// Edits the raw init line, may cause unexpected issues due to possibly format irregularities
		if (wantEdit > 0)
		{
			unsavedChanges = true;
			ImGuiExt.EnsurePopupIsOpen("Edit");
			ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);

			if (ImGuiExt.BeginPopupModal("Edit", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
			{
				int idx = GetCurrentCategoryIndex();
				var categories = assetManager!.GetCategories(GetCurrentAssetList());

				if (wantEdit == 1)
				{
					var category = categories[idx];
					if (curAssetTab != AssetType.Material)
					{
						if (categoryColor == new Vector3(1f, 0f, 0f) && category.Color != null)
						{
							Lingo.Color col = category.Color.Value;
							categoryColor = new((float)col.R / 255f, (float)col.G / 255f, (float)col.B / 255f);
						}

						if (ImGui.ColorButton("##CategoryCol", new(categoryColor.X, categoryColor.Y, categoryColor.Z, 1f), ImGuiColorEditFlags.NoTooltip))
						{
							showColorPicker = !showColorPicker;
						}
						ImGui.SameLine();
					}
					if (string.IsNullOrEmpty(newItemName))
					{
						newItemName = category.Name;
					}
					
					ImGui.InputTextWithHint("##CategoryName", "Name", ref newItemName, 128);

					if (showColorPicker)
					{
						ImGui.ColorPicker3("##Color", ref categoryColor, ImGuiColorEditFlags.InputRGB | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);
					}
				}
				else if (wantEdit == 2)
				{
					var item = categories[idx].Items[groupIndex];
					if (string.IsNullOrEmpty(newItemName))
					{
						newItemName = item.Name;
					}

					ImGui.InputTextWithHint("##ItemName", "File Name", ref newItemName, 128);
					ImGui.SameLine();
					ImGui.TextDisabled("(?)");
					ImGui.SetItemTooltip(
							"""
					Renaming the asset name will also attempt 
					to rename the corrosponding image file, 
					as renaming assets by themselves causes 
					a invalid file path error.    
					""");
				}

				ImGui.Separator();

				if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btn))
				{
					if (btn == 0) // if OK was pressed
					{
						if (wantEdit == 1)
						{
							var category = categories[idx];
							category.Name = newItemName;
							
							category.Color = new((int)(categoryColor.X * 255f), (int)(categoryColor.Y * 255f), (int)(categoryColor.Z * 255f));

							if (curAssetTab == AssetType.Tile && RainEd.Instance.TileDatabase.Categories.Any(cat => cat.Name == category.Name))
							{
								Lingo.Color col = category.Color.Value;
								RainEd.Instance.TileDatabase.Categories[idx].Color = new(col.R, col.G, col.B, 1);
							}

							categoryColor = new(1f, 0f, 0f);
							newItemName = "";

							UpdateCategory();
						}
						else if (wantEdit == 2)
						{
							var item = categories[idx].Items[groupIndex];
							item.Name = newItemName;
							newItemName = "";
							// Handle Init editing or something here
						}
					}

					else // if Cancel was pressed
					{
						categoryColor = new(1f, 0f, 0f);
						newItemName = "";
					}

					ImGui.CloseCurrentPopup();
					wantEdit = 0;
					showColorPicker = false;
				}
				ImGui.SameLine();
				ImGui.TextDisabled("(?)");
				ImGui.SetItemTooltip(
						"""
					Any changes made here will
					also attempt to be reflected
					in the Init.txt file.
					""");

				ImGui.EndPopup();
			}
		}

		if (exportReq > 0)
		{
			pendingExportFiles.Clear();
			wantExport = exportReq;
		}

		if (fileBrowser == null && wantExport == 3)
			wantExport = 1;

		if (wantExport == 1 && exportTask is null)
		{
			// Maybe move this to its own dedicated space at some point, but it's rather isolated to this use case
			RenderAssetExporter();			
		}
		else if (wantExport == 2)
		{
			wantExport = 3;
			fileBrowser = new FileBrowser(FileBrowser.OpenMode.Write, ExportAssetZip, RainEd.Instance.AssetDataPath);
			fileBrowser.AddFilter("ZIP File", ".zip");
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
							ProcessSearch();
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
					ProcessSearch();
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

		if (exportTask is not null)
		{
			ImGuiExt.EnsurePopupIsOpen("Exporting");
			ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
			ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);

			var popupFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove;
			if (ImGuiExt.BeginPopupModal("Exporting", popupFlags))
			{
				ImGui.Text("Exporting...");

				// show error if present
				if (exportTask.IsFaulted)
				{
					ImGuiExt.EnsurePopupIsOpen("Error");
					ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
					ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
					if (ImGuiExt.BeginPopupModal("Error", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
					{
						var exception = exportTask.Exception.InnerExceptions[0];
						ImGui.Text($"An error occured while exporting the pack.");

						ImGui.Separator();

						if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
						{
							ImGui.CloseCurrentPopup();
							exportTask = null;
						}

						ImGui.End();
					}
				}

				else if (exportTask.IsCompleted)
				{
					ImGui.SameLine();
					ImGui.Text("completed!");

					if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
					{
						ImGui.CloseCurrentPopup();
						exportTask = null;
					}
					ImGui.SameLine();

					if (ImGui.Button("Show In File Browser") && File.Exists(exportLocation))
						RainEd.Instance.ShowPathInSystemBrowser(exportLocation, true);
				}

				ImGui.EndPopup();
			}
		}
    }

	private static void RenderAssetExporter()
	{
		ImGuiExt.EnsurePopupIsOpen("Export Asset Pack");
		ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
		ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize / 2f, ImGuiCond.Appearing);

		if (ImGui.BeginPopupModal("Export Asset Pack", ImGuiWindowFlags.NoSavedSettings))
		{
			ImGui.Text("Create a new asset pack and compress it to a .zip file, including the Copy_To_Init.txt and image files.");
			ImGui.Separator();

			var categoryList = assetManager!.GetCategories(GetCurrentAssetList());
			// Everything pertaining to lists goes here
			if (ImGui.BeginListBox("##CategoryCanidates", new(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y - (ImGui.GetTextLineHeight() * 2f))))
			{
				var drawList = ImGui.GetWindowDrawList();
				float textHeight = ImGui.GetTextLineHeight();

				for (int i = 0; i < categoryList.Count; i++)
				{
					var group = categoryList[i];
					if (pendingExportFiles.ContainsKey(group))
						continue;

					if (group.Color.HasValue)
					{
						// colored variant
						var cursor = ImGui.GetCursorScreenPos();

						// pad beginning of selectable to reserve space for the color square
						if (ImGui.Selectable("  " + group.Name, false))
						{
							if (!pendingExportFiles.ContainsKey(group))
							{
								pendingExportFiles.Add(group, [.. group.Items]);
								if (selectedExportCategory == -1)
									selectedExportCategory = 0;
							}
						}

						// draw color square
						var col = group.Color!.Value;
						drawList.AddRectFilled(
							p_min: cursor,
							p_max: cursor + new Vector2(10f, textHeight),
							ImGui.ColorConvertFloat4ToU32(new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, 1f))
						);
					}
					else
					{
						// non-colored variant
						if (ImGui.Selectable(group.Name, false))
						{
							if (!pendingExportFiles.ContainsKey(group))
							{
								pendingExportFiles.Add(group, [.. group.Items]);
								if (selectedExportCategory == -1)
									selectedExportCategory = 0;
							}
						}
					}
				}

				ImGui.EndListBox();
			}

			float lineHeight = ImGui.GetItemRectSize().Y;

			if (selectedExportCategory != -1 && pendingExportFiles.Count > 0)
			{
				var keys = pendingExportFiles.Keys.ToList();
				bool anyItemsBeenRemoved = selectedExportCategory != -1 && categoryList.Contains(keys[selectedExportCategory]) && categoryList[categoryList.IndexOf(keys[selectedExportCategory])].Items.Any(item => !pendingExportFiles[keys[selectedExportCategory]].Contains(item));
				var removedItems = categoryList[categoryList.IndexOf(keys[selectedExportCategory])].Items.Where(item => !pendingExportFiles[keys[selectedExportCategory]].Contains(item)).ToList();

				ImGui.SameLine();
				if (ImGui.BeginChild("##ExportLists", new(-1, lineHeight)))
				{
					var drawList = ImGui.GetWindowDrawList();
					float textHeight = ImGui.GetTextLineHeight();

					if (ImGui.BeginListBox("##ExportCategories", new(ImGui.GetContentRegionAvail().X / 2f, ImGui.GetContentRegionAvail().Y - (ImGui.GetTextLineHeight() * 2f))))
					{
						for (int i = 0; i < keys.Count; i++)
						{
							var group = keys[i];
							if (group.Color.HasValue)
							{
								// colored variant
								var cursor = ImGui.GetCursorScreenPos();

								// pad beginning of selectable to reserve space for the color square
								if (ImGui.Selectable("  " + group.Name, i == selectedExportCategory))
								{
									selectedExportCategory = i;
									exportGroupIndex = 0;
								}

								// draw color square
								var col = group.Color!.Value;
								drawList.AddRectFilled(
									p_min: cursor,
									p_max: cursor + new Vector2(10f, textHeight),
									ImGui.ColorConvertFloat4ToU32(new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, 1f))
								);
							}
							else
							{
								// non-colored variant
								if (ImGui.Selectable(group.Name, i == selectedExportCategory))
								{
									selectedExportCategory = i;
									exportGroupIndex = 0;
								}
							}
						}
						ImGui.EndListBox();
					}
					ImGui.SameLine();

					float height = ImGui.GetContentRegionAvail().Y - (ImGui.GetTextLineHeight() * 2f);
					float itemHeight = height;
					if (anyItemsBeenRemoved)
						itemHeight /= 2f;
					else
						exportContenderIndex = 0;

					if (ImGui.BeginChild("##ItemsLists", new(-1, height)))
					{
						if (anyItemsBeenRemoved)
						{
							ImGui.Text("Included Items");
						}

						if (ImGui.BeginListBox("##ExportItems", new(-1, itemHeight)))
						{
							if (selectedExportCategory != -1)
							{
								var selectedCategoryItems = pendingExportFiles[keys[selectedExportCategory]];
								for (int i = 0; i < selectedCategoryItems.Count; i++)
								{
									var tile = selectedCategoryItems[i];

									if (ImGui.Selectable(tile.Name, i == exportGroupIndex))
									{
										exportGroupIndex = i;
									}

									ReAddTilePreviews(tile, selectedExportCategory);
								}
							}

							ImGui.EndListBox();
						}

						if (anyItemsBeenRemoved)
						{
							ImGui.Separator();
							ImGui.Text("Unincluded Items");

							if (ImGui.BeginListBox("##RemovedItems", new(-1, -1)))
							{

								for (int i = 0; i < removedItems.Count; i++)
								{
									var tile = removedItems[i];

									if (ImGui.Selectable(tile.Name, i == exportContenderIndex))
									{
										exportContenderIndex = i;
									}

									ReAddTilePreviews(tile, selectedExportCategory);
								}

								ImGui.EndListBox();
							}
						}

						ImGui.EndChild();
					}

					ImGui.BeginDisabled(selectedExportCategory == -1);
					if (ImGui.Button("Remove Category") && pendingExportFiles.ContainsKey(keys[selectedExportCategory]))
					{
						pendingExportFiles.Remove(keys[selectedExportCategory]);
						selectedExportCategory = Math.Max(0, selectedExportCategory - 1);
						if (pendingExportFiles.Count == 0)
							selectedExportCategory = -1;
					}
					ImGui.EndDisabled();

					ImGui.SameLine();
					ImGui.BeginDisabled(exportGroupIndex == -1);
					if (ImGui.Button("Remove Item") && pendingExportFiles.ContainsKey(keys[selectedExportCategory]) && pendingExportFiles[keys[selectedExportCategory]].Count > exportGroupIndex)
					{
						pendingExportFiles[keys[selectedExportCategory]].RemoveAt(exportGroupIndex);
						if (pendingExportFiles[keys[selectedExportCategory]].Count <= exportGroupIndex)
							exportGroupIndex--;
					}
					ImGui.EndDisabled();

					ImGui.SameLine();
					if (anyItemsBeenRemoved && removedItems.Count > exportContenderIndex && ImGui.Button("Readd Item"))
					{
						pendingExportFiles[keys[selectedExportCategory]].Add(removedItems[exportContenderIndex]);
						removedItems.RemoveAt(exportContenderIndex);
						if (removedItems.Count <= exportContenderIndex)
							exportContenderIndex--;
					}

					ImGui.EndChild();
				}
			}
			else
			{
				ImGui.SameLine();
				if (ImGui.BeginChild("##Disclaimer", new(-1, ImGui.GetItemRectSize().Y)))
				{
					ImGui.Spacing();
					ImGui.Text("Add a category to begin exporting.");

					ImGui.Spacing();

					ImGui.TextWrapped("This menu can be used to export images and a corrosponding seperate Init.txt into their own compressed .zip file.");

					ImGui.Spacing();

					ImGui.TextWrapped("The benefits of using this system includes being able to specify items without the need to seperate the files manually, over taking the time to copy and paste init lines yourself.");

					ImGui.Spacing();

					ImGui.EndChild();
				}
			}

				ImGui.Separator();

			ImGui.BeginDisabled(pendingExportFiles.Count == 0);
			if (ImGui.Button("Export"))
			{
				selectedExportCategory = 0;
				wantExport = 2;
				ImGui.CloseCurrentPopup();
			}
			ImGui.EndDisabled();

			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				selectedExportCategory = -1;
				wantExport = 0;
				ImGui.CloseCurrentPopup();
			}
			ImGui.EndPopup();
		}
	}

	private static void ExportAssetZip(string[]? paths)
	{
		if (paths == null || paths.Length == 0) return;
		if (exportTask is not null) return;
		exportTask = assetManager!.Export(GetCurrentAssetList(), paths[0]);
		exportLocation = paths[0];
		Log.Information("Export .zip file {Path}", paths[0]);
	}

	private static void ReAddTilePreviews(CategoryList.InitItem tile, int selected)
	{
		// Restores previews to the preferences tab, uses the standard method for displaying previews
		switch (curAssetTab)
		{
			case AssetType.Tile:
				var tileDb = RainEd.Instance.TileDatabase;

				if (tileDb.HasTile(tile.Name))
				{
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

						var contrastRatio = TileCatalogWidget.ContrastRatio(fgCol, bgCol);
						var invertContrast = contrastRatio < 3f;

						if (invertContrast)
						{
							ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(
								1f - bgCol4.X,
								1f - bgCol4.Y,
								1f - bgCol4.Z,
								bgCol4.W
							));
						}

						ImGui.BeginTooltip();
						TileCatalogWidget.RenderTilePreview(tileDb.GetTileFromName(tile.Name));
						ImGui.EndTooltip();

						if (invertContrast) ImGui.PopStyleColor();
					}
				}
				break;

			case AssetType.Prop:
				var propDb = RainEd.Instance.PropDatabase;

				if (propDb.Categories.Count > selected)
				{
					var propList = propDb.Categories[selected].Props;

					// Replace this if there's a better method to search for an existing prop
					if (propList.Any(propInit => propInit.Name == tile.Name))
					{
						var prop = propList.Where(propInit => propInit.Name == tile.Name).FirstOrDefault();
						if (prop != default && ImGui.BeginItemTooltip())
						{
							PropEditor.UpdatePreview(prop);
							ImGuiExt.ImageRenderTextureScaled(PropEditor.previewTexture, new Vector2(Boot.PixelIconScale, Boot.PixelIconScale));
							ImGui.EndTooltip();
						}
					}
				}
				break;

			case AssetType.Material:
				// show material preview when hovered
				if (ImGui.IsItemHovered())
				{
					if (MaterialCatalogWidget._activeMatPreview != tile.Name)
					{
						MaterialCatalogWidget._activeMatPreview = tile.Name;
						MaterialCatalogWidget._loadedMatPreview?.Dispose();
						MaterialCatalogWidget._loadedMatPreview = RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath, "assets", "mat-previews", tile.Name + ".png"));
					}

					if (MaterialCatalogWidget._loadedMatPreview is not null && Raylib_cs.Raylib.IsTextureReady(MaterialCatalogWidget._loadedMatPreview))
					{
						ImGui.BeginTooltip();
						ImGuiExt.ImageSize(MaterialCatalogWidget._loadedMatPreview, MaterialCatalogWidget._loadedMatPreview.Width * Boot.PixelIconScale, MaterialCatalogWidget._loadedMatPreview.Height * Boot.PixelIconScale);
						ImGui.EndTooltip();
					}
				}

				break;
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

    private static void ImportFileCallback(string[]? paths)
    {
        if (paths == null || paths.Length == 0) return;
        if (mergeTask is not null) return;
        var path = paths[0];

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
				ProcessSearch();
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
		if (firstOpen)
		{
			ProcessSearch();
		}

        ImGui.Text("Any changes here require a restart in order to take effect. Right click a category to enter rearrange mode.");
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
		if (ImGui.BeginChild("##AssetChild", new(), ImGuiChildFlags.None, ImGuiWindowFlags.MenuBar))
		{
			// set group index to 0 when tab changed

			if (curAssetTab != nextAssetTab)
			{
				categoryEditMode = false;
				groupIndex = 0;
				curAssetTab = nextAssetTab;
				ProcessSearch();
			}

			AssetControls();
			var halfWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().ItemSpacing.X / 2f;
			var boxHeight = ImGui.GetContentRegionAvail().Y;
			if (categoryEditMode)
			{
				boxHeight = (ImGui.GetContentRegionAvail().Y / 2f) - (ImGui.GetTextLineHeight());
			}

			switch (curAssetTab)
			{
				case AssetType.Tile:
					ShowCategoryList(AssetManager.CategoryListIndex.Tile, ref selectedTileCategory, new Vector2(halfWidth, boxHeight), 0);
					ShowItemList(AssetManager.CategoryListIndex.Tile, selectedTileCategory, new Vector2(halfWidth, boxHeight), 0);

					if (categoryEditMode)
					{
						ShowCategoryList(AssetManager.CategoryListIndex.Tile, ref selectedTileCategory2, new Vector2(halfWidth, boxHeight), 1);
						ShowItemList(AssetManager.CategoryListIndex.Tile, selectedTileCategory2, new Vector2(halfWidth, boxHeight), 1);
					}
					break;

				case AssetType.Prop:
					ShowCategoryList(AssetManager.CategoryListIndex.Prop, ref selectedPropCategory, new Vector2(halfWidth, boxHeight), 0);
					ShowItemList(AssetManager.CategoryListIndex.Prop, selectedPropCategory, new Vector2(halfWidth, boxHeight), 0);

					if (categoryEditMode)
					{
						ShowCategoryList(AssetManager.CategoryListIndex.Prop, ref selectedPropCategory2, new Vector2(halfWidth, boxHeight), 1);
						ShowItemList(AssetManager.CategoryListIndex.Prop, selectedPropCategory2, new Vector2(halfWidth, boxHeight), 1);
					}
					break;

				case AssetType.Material:
					ShowCategoryList(AssetManager.CategoryListIndex.Materials, ref selectedMatCategory, new Vector2(halfWidth, boxHeight), 0);
					ShowItemList(AssetManager.CategoryListIndex.Materials, selectedMatCategory, new Vector2(halfWidth, boxHeight), 0);
					break;
			}

			ImGui.EndChild();
		}
    }

    public static void Unload()
    {
        if (assetManager is not null)
            assetManager = null;
        
        noAskBeforeDeletion = false;
        selectedTileCategory = 0;
		selectedTileCategory2 = 0;
        selectedPropCategory = 0;
		selectedPropCategory2 = 0;
        selectedMatCategory = 0;
        groupIndex = 0;
    }
}