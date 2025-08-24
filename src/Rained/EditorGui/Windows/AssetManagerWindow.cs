

using ImGuiNET;
using Raylib_cs;
using System.Numerics;

namespace Rained.EditorGui.Windows
{
    public class AssetManagerWindow
    {
        private const string WindowName = "Asset Manager";
        private static bool isWindowOpen = false; 
        
        private static bool openPopupCmd = false;
        private static bool openCloseConfirm = false;

        public static void OpenWindow()
        {
            openPopupCmd = true;
        }

        enum AssetTabEnum : int
        {
            Tiles = 0,
            Props = 1,
            Mats = 2,
        }

        private static AssetTabEnum selectedAssetTab = AssetTabEnum.Tiles;
        private readonly static string[] NavTabs = ["Tiles", "Props", "Materials"];

        private static TaskCompletionSource<bool>? closePromptTcs = null;

        public static void ShowWindow()
        {
            if (openPopupCmd)
            {
                openPopupCmd = false;
                isWindowOpen = true;
                AssetManagerGUI.firstOpen = true;

                // center popup modal
                ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 50f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.FirstUseEver);
            }

            if (isWindowOpen)
            {
                var lastNavTab = selectedAssetTab;
                AssetManagerGUI.Init();

                var p_open = true;
                if (ImGui.Begin(WindowName, ref p_open, ImGuiWindowFlags.NoDocking))
                {
                    // show navigation sidebar
                    ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
                    {
                        for (int i = 0; i < NavTabs.Length; i++)
                        {
                            ImGui.BeginDisabled(!AssetManagerGUI.Manager!.HasCategoryList((Assets.AssetManager.CategoryListIndex)i));

                            if (ImGui.Selectable(NavTabs[i], i == (int)selectedAssetTab))
                            {
                                selectedAssetTab = (AssetTabEnum)i;
                                switch (selectedAssetTab)
                                {
                                    case AssetTabEnum.Tiles:
                                        AssetManagerGUI.nextAssetTab = AssetManagerGUI.AssetType.Tile;
                                        break;

                                    case AssetTabEnum.Props:
                                        AssetManagerGUI.nextAssetTab = AssetManagerGUI.AssetType.Prop;
                                        break;

                                    case AssetTabEnum.Mats:
                                        AssetManagerGUI.nextAssetTab = AssetManagerGUI.AssetType.Material;
                                        break;
                                }
                            }

                            ImGui.EndDisabled();
                        }
                    }
                    ImGui.EndChild();

                    ImGui.SameLine();
                    if (ImGui.BeginChild("Manager", ImGui.GetContentRegionAvail()))
                    {
                        AssetManagerGUI.Show();
                    }
                    ImGui.EndChild();

                    //switch (selectedAssetTab)
                    //{
                    //	case AssetTabEnum.Tiles:
                    //		ShowGeneralTab(justOpened || lastNavTab != selectedAssetTab);
                    //		break;

                    //	case AssetTabEnum.Props:
                    //		ShowShortcutsTab();
                    //		break;

                    //	case AssetTabEnum.Mats:
                    //		ShowThemeTab(justOpened || lastNavTab != selectedAssetTab);
                    //		break;
                    //}

                    if (openCloseConfirm)
                    {
                        ImGui.OpenPopup("Unsaved Changes");
                        openCloseConfirm = false;
                    }

                    ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                    if (ImGui.BeginPopupModal("Unsaved Changes"))
                    {
                        ImGui.Text("Do you want to apply your changes before proceeding?");
                        ImGui.Separator();

                        if (ImGui.Button("Yes", StandardPopupButtons.ButtonSize))
                        {
                            AssetManagerGUI.Manager?.Commit();
                            isWindowOpen = false;
                            ImGui.CloseCurrentPopup();
                            closePromptTcs?.SetResult(true);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("No", StandardPopupButtons.ButtonSize))
                        {
                            isWindowOpen = false;
                            ImGui.CloseCurrentPopup();
                            closePromptTcs?.SetResult(true);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
                        {
                            ImGui.CloseCurrentPopup();
                            closePromptTcs?.SetResult(false);
                        }

                        AssetManagerGUI.HasUnsavedChanges = false;
                        ImGui.EndPopup();
                    }
                }
                ImGui.End();

                if (!p_open)
                {
                    if (AssetManagerGUI.HasUnsavedChanges)
                        openCloseConfirm = true;
                    else
                        isWindowOpen = false;
                }

                if (!isWindowOpen)
                {
                    AssetManagerGUI.Unload();
                }
            }
        }

        public static async Task<bool> AppClose()
        {
            if (!isWindowOpen) return true;
            if (!AssetManagerGUI.HasUnsavedChanges) return true;

            closePromptTcs = new();
            openCloseConfirm = true;
            var res = await closePromptTcs.Task;
            closePromptTcs = null;
            return res;
        }
    }

}