

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

                if (ImGui.Begin(WindowName, ref isWindowOpen, ImGuiWindowFlags.NoDocking))
                {


                    // show navigation sidebar
                    ImGui.BeginChild("Nav", new Vector2(ImGui.GetTextLineHeight() * 12.0f, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border);
                    {
                        for (int i = 0; i < NavTabs.Length; i++)
                        {
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
                        }
                    }
                    ImGui.EndChild();

                    ImGui.SameLine();
                    ImGui.BeginChild("Manager", ImGui.GetContentRegionAvail());

                    AssetManagerGUI.Show();
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

                    ImGui.EndChild();
                }
                ImGui.End();

                if (!isWindowOpen)
                {
                    AssetManagerGUI.Unload();
                }
            }
        }
    }
}