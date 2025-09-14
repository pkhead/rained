using System.Numerics;
using ImGuiNET;
using Rained.EditorGui;
using Rained;

namespace RainEd.EditorGui;

abstract class DirectoryTreeView
{
    enum ActivePopupID
    {
        None,
        CreateFolder,
        DeleteFolder,
        RenameFolder,
    }


    public readonly DirectoryTreeCache Cache;

    protected string TextInput { get => popupText; }

    private ActivePopupID activePopup = ActivePopupID.None;
    private string popupText = "";
    private string? popupItem;

    public DirectoryTreeView(string baseDir, string? fileFilter)
    {
        Cache = new(baseDir, fileFilter);
    }
    
    protected abstract void DirectoryContextMenu(string dirPath);
    protected abstract void FileContextMenu(string filePath);
    protected abstract void RequestFileDeletion(string filePath);
    protected abstract void RequestFileRename(string filePath);
    protected abstract void FileActivated(string filePath);

    public virtual void Render(string label, Vector2 size)
    {
        int id = 0;
        void RenderDirectory(string dirPath)
        {
            var flags = ImGuiTreeNodeFlags.SpanFullWidth;

            foreach (var subdirName in Cache.GetDirectories(dirPath))
            {
                ImGui.PushID(++id);
                string subdir = DirectoryTreeCache.Join(dirPath, subdirName);
                bool open = ImGui.TreeNodeEx(subdirName, flags);

                ImGui.OpenPopupOnItemClick("ctx", ImGuiPopupFlags.MouseButtonRight);
                if (ImGui.IsPopupOpen("ctx") && ImGui.BeginPopup("ctx"))
                {
                    if (ImGui.Selectable("Create Folder"))
                    {
                        activePopup = ActivePopupID.CreateFolder;
                        popupItem = subdir;
                    }

                    DirectoryContextMenu(subdir);

                    ImGui.Separator();
                    if (ImGui.Selectable("Delete"))
                    {
                        activePopup = ActivePopupID.DeleteFolder;
                        popupItem = subdir;
                    }

                    if (ImGui.Selectable("Rename"))
                    {
                        activePopup = ActivePopupID.RenameFolder;
                        popupItem = subdir;
                    }

                    ImGui.EndPopup();
                }

                if (open)
                {
                    RenderDirectory(subdir);
                    ImGui.TreePop();
                }

                ImGui.PopID();
            }

            foreach (var fileName in Cache.GetFiles(dirPath))
            {
                ImGui.PushID(++id);
                var filePath = DirectoryTreeCache.Join(dirPath, fileName);
                var fileNameNoExt = fileName[..fileName.LastIndexOf('.')];

                ImGui.TreeNodeEx(fileNameNoExt, flags | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);

                ImGui.OpenPopupOnItemClick("ctx", ImGuiPopupFlags.MouseButtonRight);
                if (ImGui.IsPopupOpen("ctx") && ImGui.BeginPopup("ctx"))
                {
                    FileContextMenu(filePath);

                    if (ImGui.Selectable("Delete"))
                        RequestFileDeletion(filePath);

                    if (ImGui.Selectable("Rename"))
                        RequestFileRename(filePath);

                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }
        }

        if (ImGui.Button("Refresh"))
        {
            Cache.Refresh();
        }

        bool popupWasInactive = activePopup == ActivePopupID.None;

        if (ImGui.BeginListBox(label, size))
        {
            RenderDirectory("/");

            ImGui.InvisibleButton("EmptyAreaButton", new Vector2(-0.0001f, -0.0001f));
            ImGui.OpenPopupOnItemClick("mainCtx", ImGuiPopupFlags.MouseButtonRight);
            if (ImGui.IsPopupOpen("mainCtx") && ImGui.BeginPopup("mainCtx"))
            {
                if (ImGui.Selectable("Create Folder"))
                {
                    activePopup = ActivePopupID.CreateFolder;
                    popupItem = "/";
                }

                DirectoryContextMenu("/");
                ImGui.EndPopup();
            }

            ImGui.EndListBox();
        }

        // if popup was requested, handle opening it here
        if (popupWasInactive && activePopup != ActivePopupID.None)
        {
            popupText = "";

            switch (activePopup)
            {
                case ActivePopupID.CreateFolder:
                    ImGui.OpenPopup("Create Folder");
                    break;

                case ActivePopupID.DeleteFolder:
                    ImGui.OpenPopup("Delete Folder?");
                    break;

                case ActivePopupID.RenameFolder:
                    ImGui.OpenPopup("Rename Folder");
                    popupText = PathGetName(popupItem!);
                    break;
            }

            ImGuiExt.CenterNextWindow(ImGuiCond.Once);
        }

        ShowActivePopup();
    }

    protected static string PathGetName(string path)
    {
        var idx = path.LastIndexOf('/');
        if (idx == -1) return path;
        return path[(idx + 1)..];
    }

    protected static string PathGetNameWithoutExtension(string path)
    {
        path = PathGetName(path);

        var idx = path.LastIndexOf('.');
        if (idx == -1) return path;
        return path[..idx];
    }

    protected int PopupNamePrompt(string hintText, bool canSubmit = true)
    {
        ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 20.0f);
        bool submit =
            ImGui.InputTextWithHint("##name", hintText, ref popupText, 128, ImGuiInputTextFlags.EnterReturnsTrue);

        // these are for windows only, since for unix the only invalid chars
        // is the forward slash. but i think the user should be concerned
        // about cross-platform compatibility anyway.
        bool hasInvalidChars = popupText.Contains('/')
            || popupText.Contains('\\')
            || popupText.Contains(':')
            || popupText.Contains('*')
            || popupText.Contains('?')
            || popupText.Contains('|')
            || popupText.Contains('<')
            || popupText.Contains('>')
            || popupText == "." || popupText == "..";

        if (hasInvalidChars)
        {
            ImGui.TextDisabled("Name contains invalid characters");
        }

        ImGui.Separator();

        bool cannotSubmit = hasInvalidChars || popupText.Length == 0;
        cannotSubmit = cannotSubmit || !canSubmit;

        ImGui.BeginDisabled(cannotSubmit);
        if (ImGui.Button("OK", StandardPopupButtons.ButtonSize))
        {
            submit = true;
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel", StandardPopupButtons.ButtonSize))
        {
            return 2;
        }

        return submit && !cannotSubmit ? 1 : 0;
    }

    private void ShowActivePopup()
    {
        // render active popup
        bool popupOpen = false;
        var flags = ImGuiWindowFlags.AlwaysAutoResize;
        if (ImGui.IsPopupOpen("Create Folder") && ImGui.BeginPopupModal("Create Folder", flags))
        {
            popupOpen = true;

            switch (PopupNamePrompt("Folder Name..."))
            {
                case 1: // submit
                    {
                        var dirPath = DirectoryTreeCache.Join(popupItem!, popupText);

                        try
                        {
                            Directory.CreateDirectory(Cache.ConvertToRealPath(dirPath));
                            Cache.RefreshDirectoryList(popupItem!, false);
                            Cache.RefreshDirectoryList(dirPath, false);
                        }
                        catch (Exception e)
                        {
                            Log.UserLogger.Error(e.Message);
                            EditorWindow.ShowNotification("An error occurred trying to create the folder");
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

        if (ImGui.IsPopupOpen("Delete Folder?") && ImGui.BeginPopupModal("Delete Folder?", flags))
        {
            popupOpen = true;

            ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 20.0f);
            ImGui.TextWrapped($"Are you sure you want to delete the folder \"{popupItem}\"?");

            ImGui.Separator();

            if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
            {
                if (btn == 0)
                {
                    Cache.DeleteDirectory(popupItem!);
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (ImGui.IsPopupOpen("Rename Folder") && ImGui.BeginPopupModal("Rename Folder", flags))
        {
            popupOpen = true;

            switch (PopupNamePrompt("Folder Name..."))
            {
                case 1: // submit
                    {
                        try
                        {
                            var newPath = DirectoryTreeCache.Join(popupItem!, "..", popupText);
                            Cache.MoveDirectory(popupItem!, newPath);
                        }
                        catch (Exception e)
                        {
                            Log.UserLogger.Error(e.Message);
                            EditorWindow.ShowNotification("An error occurred trying to rename the folder");
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

        if (!popupOpen)
        {
            activePopup = ActivePopupID.None;
            popupItem = null;
        }
    }
}