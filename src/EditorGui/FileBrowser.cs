using System.Numerics;
using ImGuiNET;

namespace RainEd;

public class FileBrowser
{
    private static FileBrowser? singleton = null;
    public static FileBrowser? Singleton { get => singleton; }
    public static void Open(OpenMode openMode)
    {
        singleton = new FileBrowser(openMode);
    }

    private bool isOpen = false;
    private bool isDone = false;
    private string cwd;
    private List<string> pathList = new();

    private Stack<string> backStack = new();
    private Stack<string> forwardStack = new();

    private bool enterPath;
    private bool showPathInput;

    private string pathBuf;
    private string nameBuf;
    
    private int selected = -1;
    private bool scrollToSelected = false;
    private List<Entry> entries = new();

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
        public EntryType Type;

        public Entry(string name, EntryType type)
        {
            Name = name;
            Type = type;
        }
    }

    private class EntrySorter : IComparer<Entry>
    {
        int IComparer<Entry>.Compare(Entry a, Entry b) => a.Name.CompareTo(b.Name);
    }
    
    private FileBrowser(OpenMode mode)
    {
        this.mode = mode;
        cwd = Directory.GetCurrentDirectory();
        SetPath(cwd);
        pathBuf = cwd;
        nameBuf = string.Empty;
    }

    public void SetPath(string newPath)
    {
        cwd = newPath;
        entries.Clear();

        // add directories
        foreach (var dirPath in Directory.EnumerateDirectories(newPath))
        {
            var dirName = Path.GetFileName(dirPath);
            if (!string.IsNullOrEmpty(dirName)) entries.Add(new Entry(dirName, EntryType.Directory));
        }
        entries.Sort(new EntrySorter());
        var firstFile = entries.Count;

        // add files
        foreach (var filePath in Directory.EnumerateFiles(newPath))
        {
            var fileName = Path.GetFileName(filePath);
            entries.Add(new Entry(fileName, EntryType.File));
        }
        entries.Sort(firstFile, entries.Count - firstFile, new EntrySorter());
        pathBuf = newPath;

        // fill path list
        pathList.Clear();
        var parentDir = newPath;
        while (!string.IsNullOrEmpty(parentDir))
        {
            var name = Path.GetFileName(parentDir);
            if (string.IsNullOrEmpty(name))
            {
                pathList.Insert(0, parentDir);
                break;
            }
            else
            {
                pathList.Insert(0, name);
            }

            parentDir = Path.GetDirectoryName(parentDir);
        }
    }

    public void Render()
    {
        if (!isOpen)
        {
            isOpen = true;
            ImGui.OpenPopup("File Browser");
        }

        ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 60f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("File Browser"))
        {
            var windowSize = ImGui.GetWindowSize();
            ImGui.Button("<"); ImGui.SameLine();
            ImGui.Button(">"); ImGui.SameLine();
            ImGui.Button("^"); ImGui.SameLine();
            ImGui.Button("Refresh");

            // current path
            ImGui.SameLine();
            if (enterPath || showPathInput)
            {
                if (!enterPath)
                    ImGui.SetKeyboardFocusHere();
                
                enterPath = true;
                showPathInput = true;

                ImGui.SetNextItemWidth(-0.0001f);
                if (ImGui.InputText("##Path", ref pathBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    SetPath(pathBuf);
                }
            }
            else
            {
                if (ImGui.Button("Type"))
                    showPathInput = true;
                ImGui.SameLine();

                if (pathList.Count == 0)
                {
                    ImGui.NewLine();
                }
                else
                {
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
                            backStack.Push(cwd);
                            forwardStack.Clear();
                            SetPath(string.Join(Path.DirectorySeparatorChar, pathList.ToArray(), 0, i+1));
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
                        if (selected >= 0 && scrollToSelected)
                        {
                            ImGui.SetScrollHereY();
                        }

                        var entryName = entry.Type == EntryType.Directory ?
                            entry.Name + Path.DirectorySeparatorChar :
                            entry.Name;
                        if (ImGui.Selectable(entryName, selected == i, ImGuiSelectableFlags.AllowDoubleClick))
                        {
                            selected = i;

                            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                                ok = true;
                        }
                    }
                }
            }
            scrollToSelected = false;
            ImGui.EndChild();

            if (ImGui.Button("OK")) ok = true;
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                isDone = true;
            }
            ImGui.SameLine();

            if (mode == OpenMode.Write)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-0.0001f);

                var oldName = nameBuf;
                var enterPressed = ImGui.InputTextWithHint("##Name", "File name", ref nameBuf, 128, ImGuiInputTextFlags.EnterReturnsTrue);
            
                // find a file/directory that has the same name
                if (nameBuf != oldName)
                {
                    selected = -1;

                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i].Name == nameBuf)
                        {
                            selected = i;
                            scrollToSelected = true;
                            break;
                        }
                    }

                    if (enterPressed) ok = true;
                }
            }

            if (ok)
            {
                if (mode == OpenMode.Write)
                {
                    if (selected >= 0)
                    {
                        var entry = entries[selected];
                        if (entry.Type == EntryType.Directory)
                        {
                            ActivateEntry(entry);
                        }
                    }
                }
            }

            if (isDone)
                ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void ActivateEntry(Entry entry)
    {
        if (entry.Type == EntryType.Directory)
        {
            var oldPath = cwd;
            SetPath(Path.Join(cwd, entry.Name));
            backStack.Push(oldPath);
            forwardStack.Clear();
            selected = -1;
        }
    }
}