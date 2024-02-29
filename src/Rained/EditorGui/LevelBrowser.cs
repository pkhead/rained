using RlManaged;
using Raylib_cs;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;

namespace RainEd;

class LevelBrowser
{
    private static LevelBrowser? singleton = null;
    public static LevelBrowser? Singleton { get => singleton; }
    public static void Open(OpenMode openMode, Action<string> callback, string? defaultFileName)
    {
        singleton = new LevelBrowser(openMode, callback, Path.Combine(Boot.AppDataPath, "Data","LevelEditorProjects"));
    }

    private bool isOpen = false;
    private bool isDone = false;
    private readonly Action<string> callback;
    private string cwd; // current directory of file browser
    private readonly List<string> pathList = new(); // path as a list

    private readonly Stack<string> backStack = new();
    private readonly Stack<string> forwardStack = new();

    // path display mode - breadcrumb trail or string input
    private bool enterPath;
    private bool showPathInput;

    private string pathBuf;
    private string nameBuf;
    
    private int selected = -1;
    private bool scrollToSelected = false;
    private readonly List<Entry> entries = new();

    private bool openErrorPopup = false;
    private string errorMsg = string.Empty;

    public enum OpenMode
    {
        Write,
        Read
    };

    private readonly OpenMode mode;
    
    private enum EntryType { File, Directory };
    private struct Entry
    {
        public string Name;
        public string Extension;
        public EntryType Type;

        public Entry(string name, EntryType type)
        {
            Name = name;
            Type = type;
            Extension = Path.GetExtension(name);
        }
    }

    private struct BookmarkItem
    {
        public string Name;
        public string Path;
    }

    private class EntrySorter : IComparer<Entry>
    {
        int IComparer<Entry>.Compare(Entry a, Entry b) => a.Name.CompareTo(b.Name);
    }
    
    private static RlManaged.Texture2D icons = null!;
    private LevelBrowser(OpenMode mode, Action<string> callback, string? openDir)
    {
        icons ??= RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","filebrowser-icons.png"));

        this.mode = mode;
        this.callback = callback;
        cwd = openDir ?? Boot.AppDataPath;
        SetPath(cwd);
        pathBuf = cwd;
        nameBuf = string.Empty;
    }

    private static Rectangle GetIconRect(int index)
        => new Rectangle(index * 13, 0, 13, 13);

    private bool SetPath(string path)
    {
        if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
        if (!Directory.Exists(path))
        {
            openErrorPopup = true;
            errorMsg = "Directory does not exist";
            RainEd.Logger.Error("Directory {Path} does not exist", path);

            return false;
        }

        path = Path.GetFullPath(path);

        cwd = path;
        entries.Clear();

        // add directories
        foreach (var dirPath in Directory.EnumerateDirectories(path))
        {
            var dirName = Path.GetFileName(dirPath);
            var dirInfo = File.GetAttributes(dirPath);
            
            if (!dirInfo.HasFlag(FileAttributes.Hidden))
                if (!string.IsNullOrEmpty(dirName)) entries.Add(new Entry(dirName, EntryType.Directory));
        }
        entries.Sort(new EntrySorter());
        var firstFile = entries.Count;

        // add files
        foreach (var filePath in Directory.EnumerateFiles(path))
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = File.GetAttributes(filePath);

            if (fileInfo.HasFlag(FileAttributes.Hidden)) continue;
            if (Path.GetExtension(fileName) != ".txt") continue; // file filter, although hardcoded
                entries.Add(new Entry(fileName, EntryType.File));
        }
        entries.Sort(firstFile, entries.Count - firstFile, new EntrySorter());
        pathBuf = path;

        // path tree into list
        pathList.Clear();
        foreach (var p in path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            pathList.Add(p);
        }
        
        return true;
    }

    public void Render()
    {
        var winName = mode == OpenMode.Write ? "Save Level" : "Open Level";

        if (!isOpen)
        {
            isOpen = true;
            ImGui.OpenPopup(winName + "###File Browser");
            ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        }

        ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 60f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal(winName + "###File Browser"))
        {
            var windowSize = ImGui.GetWindowSize();

            // back button
            if (rlImGui.ImageButtonRect("<", icons, 13, 13, GetIconRect(0)))
            {
                if (backStack.TryPop(out string? newPath))
                {
                    var oldDir = cwd;
                    if (SetPath(newPath!))
                    {
                        forwardStack.Push(oldDir);
                        selected = -1;
                    }
                }
            } ImGui.SameLine();
            ImGui.SetItemTooltip("Back");

            // forward button
            if (rlImGui.ImageButtonRect(">", icons, 13, 13, GetIconRect(1)))
            {
                if (forwardStack.TryPop(out string? newPath))
                {
                    var oldDir = cwd;
                    if (SetPath(newPath!))
                    {
                        backStack.Push(oldDir);
                        selected = -1;
                    }
                }  
            } ImGui.SameLine();
            ImGui.SetItemTooltip("Forward");

            if (rlImGui.ImageButtonRect("^", icons, 13, 13, GetIconRect(2))/* && cwd.PathString != "/"*/)
            {
                var oldDir = cwd;
                if (SetPath(Path.Combine(cwd, "..")))
                {
                    selected = -1;
                    backStack.Push(oldDir);
                    forwardStack.Clear();
                }
            }
            ImGui.SameLine();
            ImGui.SetItemTooltip("Go To Parent Directory");

            if (rlImGui.ImageButtonRect("Refresh", icons, 13, 13, GetIconRect(4)))
            {
                if (Directory.Exists(cwd))
                    SetPath(cwd);
                else
                    SetPath(Path.GetFullPath(Path.DirectorySeparatorChar.ToString()));
            }
            ImGui.SetItemTooltip("Refresh");

            // current path
            ImGui.SameLine();
            if (enterPath || showPathInput)
            {
                bool closeTextInput = rlImGui.ImageButtonRect("Type", icons, 13, 13, GetIconRect(3));
                ImGui.SameLine();
                ImGui.SetItemTooltip("Close Text Input");

                if (!enterPath)
                    ImGui.SetKeyboardFocusHere();
                
                ImGui.SetNextItemWidth(-0.0001f);
                if (ImGui.InputText("##Path", ref pathBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    SetPath(Path.GetFullPath(pathBuf));
                }

                enterPath = !closeTextInput;
                showPathInput = false;
            }
            else
            {
                if (rlImGui.ImageButtonRect("Type", icons, 13, 13, GetIconRect(8)))
                    showPathInput = true;
                ImGui.SetItemTooltip("Open Text Input");
                
                ImGui.SameLine();

                if (pathList.Count == 0)
                {
                    ImGui.NewLine();
                }
                else
                {
                    ImGui.AlignTextToFramePadding();

                    for (int i = 0; i < pathList.Count; i++)
                    {
                        var ent = pathList[i];
                        if (i >= 1)
                        {
                            ImGui.SameLine();
                        }

                        ImGui.PushID(i);
                        if (ImGui.SmallButton(ent))
                        {
                            var old = cwd;
                            if (SetPath(string.Join(Path.DirectorySeparatorChar, pathList.ToArray(), 0, i+1)))
                            {
                                backStack.Push(old);
                                forwardStack.Clear();
                            }
                        }

                        ImGui.PopID();
                    }
                }
            }

            var style = ImGui.GetStyle();
            var listingHeight = windowSize.Y - ImGui.GetFrameHeightWithSpacing() * 3f +
                style.ItemSpacing.Y - style.WindowPadding.Y * 2f;

            // list files in cwd
            var ok = false;

            ImGui.BeginChild("Listing", new Vector2(-0.0001f, listingHeight));
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    if (entry.Name[0] != '.')
                    {
                        if (selected == i && scrollToSelected)
                        {
                            ImGui.SetScrollHereY();
                        }

                        // this is the offset into the file icon texture
                        int fileTypeIcon = entry.Type == EntryType.Directory ? 5 : 6;

                        rlImGui.ImageRect(icons, 13, 13, GetIconRect(fileTypeIcon));
                        ImGui.SameLine();
                        
                        var entryName = entry.Name;
                        if (entry.Type == EntryType.Directory)
                        {
                            entryName += Path.DirectorySeparatorChar;
                        } 

                        if (ImGui.Selectable(entryName, selected == i, ImGuiSelectableFlags.AllowDoubleClick))
                        {
                            selected = i;

                            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                                ok = true;
                        }

                        if (ImGui.IsItemActivated() && entry.Type == EntryType.File)
                        {
                            nameBuf = entryName;
                        }
                    }
                }
            }
            scrollToSelected = false;
            ImGui.EndChild();

            if (ImGui.Button("OK")) ok = true;
            ImGui.SameLine();
            if (ImGui.Button("Cancel") || (!ImGui.GetIO().WantTextInput && ImGui.IsKeyPressed(ImGuiKey.Escape)))
            {
                isDone = true;
            }
            ImGui.SameLine();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(-0.0001f);

            var oldName = nameBuf;
            var enterPressed = ImGui.InputTextWithHint("##Name", "Level Name", ref nameBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue);
        
            // find a file/directory that has the same name
            if (nameBuf != oldName)
            {
                // modify name to match current filter (txt files)
                string name = nameBuf;
                if (name.Length <= 4 || name.Substring(name.Length - 4, 4) != ".txt")
                    name += ".txt";
                
                selected = -1;

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Name == name)
                    {
                        selected = i;
                        scrollToSelected = true;
                        break;
                    }
                }
            }

            if (enterPressed) ok = true;
            if (ok)
            {
                if (mode == OpenMode.Write)
                {
                    if (selected >= 0 && entries[selected].Type == EntryType.Directory)
                        ActivateEntry(entries[selected]);
                    else
                    {
                        if (nameBuf != "" && nameBuf != "." && nameBuf != "..")
                        {
                            // modify name to match current filter (txt files)
                            string name = nameBuf;
                            if (name.Length <= 4 || name.Substring(name.Length - 4, 4) != ".txt")
                                name += ".txt";
                            
                            isDone = true;
                            callback(Path.Combine(cwd, name));
                        }
                    }
                }
                else if (mode == OpenMode.Read && selected >= 0)
                {
                    var ent = entries[selected];
                    if (ent.Type == EntryType.Directory)
                        ActivateEntry(ent);
                    else
                    {
                        isDone = true;
                        callback(Path.Combine(cwd, ent.Name));
                    }
                }
            }

            if (isDone)
            {
                ImGui.CloseCurrentPopup();
                callback(string.Empty);
            }

            // show error popup if necessary
            if (openErrorPopup)
            {
                openErrorPopup = false;
                ImGui.OpenPopup("Error");
            }

            bool errorClose = true;
            if (ImGui.BeginPopupModal("Error", ref errorClose, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.Text(errorMsg);
                if (ImGui.Button("OK"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void ActivateEntry(Entry entry)
    {
        if (entry.Type == EntryType.Directory)
        {
            var oldPath = cwd;
            if (SetPath(Path.Combine(cwd, entry.Name)))
            {
                backStack.Push(oldPath);
                forwardStack.Clear();
                selected = -1;
            }
        }
    }
}