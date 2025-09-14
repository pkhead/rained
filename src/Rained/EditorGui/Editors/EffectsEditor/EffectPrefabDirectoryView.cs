using ImGuiNET;
using Rained.Assets;
using System.Numerics;
using Rained.LevelData;
using RainEd.EditorGui;
namespace Rained.EditorGui.Editors;

class EffectPrefabDirectoryView : DirectoryTreeView
{
    enum ActionPromptRequest
    {
        None,
        DeletePrefab,
        RenamePrefab,
        CreatePrefab,
    };

    private ActionPromptRequest actionPromptRequest = ActionPromptRequest.None;
    private string? actionFilePath = null;
    private List<int> selectedEffects = null!;

    public EffectPrefabDirectoryView() :
        base(Path.Combine(Boot.AppDataPath, "config", "prefabs", "effects"), "*.json")
    { }

    protected override void DirectoryContextMenu(string dirPath)
    {
        if (ImGui.Selectable("Create Prefab"))
        {
            actionFilePath = dirPath;
            actionPromptRequest = ActionPromptRequest.CreatePrefab;
        }
    }

    protected override void FileContextMenu(string filePath)
    { }

    protected override void RequestFileDeletion(string filePath)
    {
        actionFilePath = filePath;
        actionPromptRequest = ActionPromptRequest.DeletePrefab;
    }

    protected override void RequestFileRename(string filePath)
    {
        actionFilePath = filePath;
        actionPromptRequest = ActionPromptRequest.RenamePrefab;
    }

    protected override void FileActivated(string filePath)
    { }

    public override void Render(string label, Vector2 size)
    {
        base.Render(label, size);

        if (actionPromptRequest != ActionPromptRequest.None)
        {
            switch (actionPromptRequest)
            {
                case ActionPromptRequest.DeletePrefab:
                    ImGui.OpenPopup("Delete Prefab?");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Once);
                    break;

                case ActionPromptRequest.CreatePrefab:
                    ImGui.OpenPopup("Create Prefab");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Once);
                    selectedEffects = [];
                    break;

                case ActionPromptRequest.RenamePrefab:
                    ImGui.OpenPopup("Rename Prefab");
                    ImGuiExt.CenterNextWindow(ImGuiCond.Once);
                    break;
            }

            actionPromptRequest = ActionPromptRequest.None;
        }

        var flags = ImGuiWindowFlags.AlwaysAutoResize;

        if (ImGui.BeginPopupModal("Delete Prefab?", flags))
        {
            ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 20.0f);
            ImGui.TextWrapped($"Are you sure you want to delete the file \"{actionFilePath}\"?");

            ImGui.Separator();

            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
            {
                if (btn == 0)
                {
                    Cache.DeleteFile(actionFilePath!);
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("Create Prefab", flags))
        {
            // show effect stack
            var level = RainEd.Instance.Level;

            ImGui.Text("Select effects to add");
            if (ImGui.BeginListBox("##sources", new Vector2(-0.00001f, ImGui.GetFrameHeight() * 12.0f)))
            {
                if (level.Effects.Count == 0)
                {
                    ImGui.TextDisabled("(no effects)");
                }
                else
                {
                    for (int i = 0; i < level.Effects.Count; i++)
                    {
                        ImGui.PushID(i);
                        var effect = level.Effects[i];

                        bool isSelected = selectedEffects.Contains(i);
                        if (ImGui.Selectable(effect.Data.name, isSelected))
                        {
                            if (isSelected)
                                selectedEffects.Remove(i);
                            else
                                selectedEffects.Add(i);
                        }

                        ImGui.PopID();
                    }
                }

                ImGui.EndListBox();
            }

            switch (PopupNamePrompt("Prefab Name...", selectedEffects.Count > 0))
            {
                case 1: // submit
                    {
                        var prefabPath = DirectoryTreeCache.Join(actionFilePath!, TextInput + ".json");

                        try
                        {
                            selectedEffects.Sort();
                            var effectPrefab = new EffectPrefab()
                            {
                                Items = [.. selectedEffects.Select(
                                            x => new EffectPrefab.EffectPrefabItem(level.Effects[x])
                                        )]
                            };

                            effectPrefab.WriteToFile(Cache.ConvertToRealPath(prefabPath)!);
                            Cache.RefreshDirectoryList(actionFilePath!, false);
                        }
                        catch (Exception e)
                        {
                            Log.UserLogger.Error(e.Message);
                            EditorWindow.ShowNotification("An error occurred trying to create the prefab");
                        }

                        ImGui.CloseCurrentPopup();
                    }
                    break;

                case 2: // cancel
                    ImGui.CloseCurrentPopup();
                    break;
            }

            ImGui.EndPopup();
        }

        // TODO: rename prefab
    }
}