using Raylib_cs;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RainEd;

partial class FileBrowser
{
    private bool isOpen = false;
    private bool isDone = false;
    private string callbackStr = string.Empty;

    private readonly Action<string> callback;
    private string cwd; // current directory of file browser
    private readonly List<string> pathList = new(); // path as a list

    private readonly Stack<string> backStack = new();
    private readonly Stack<string> forwardStack = new();
    private string folderName = "New Folder";

    // path display mode - breadcrumb trail or string input
    private bool enterPath;
    private bool showPathInput;

    private string pathBuf;
    private string nameBuf;
    
    private int selected = -1;
    private bool scrollToSelected = false;
    private readonly List<Entry> entries = new();
    private readonly List<(int, Entry)> filteredEntries = new();
    private readonly List<FileFilter> fileFilters = new();
    private FileFilter selectedFilter;
    private bool needFilterRefresh = false;
    private readonly List<BookmarkItem> bookmarks = new();
    private readonly List<BookmarkItem> drives = new();

    private bool openErrorPopup = false;
    private bool openOverwritePopup = false;
    private string overwriteFileName = "";
    private string errorMsg = string.Empty;

    public enum OpenMode
    {
        Write,
        Read,
        Directory
    };

    private readonly OpenMode mode;
    
    private enum EntryType { File, Directory };
    private struct Entry
    {
        public string Name;
        public string Extension;
        public EntryType Type;
        public int IconIndex = 6; // file icon

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

        public BookmarkItem(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }

    private record FileFilter
    {
        public string FilterName;
        public string FullText;
        public string[] AllowedExtensions;
        public Func<string, bool, bool>? FilterCallback;

        public FileFilter(string name, string[] extensions, Func<string, bool, bool>? filterCallback = null)
        {
            FilterName = name;
            AllowedExtensions = extensions;
            FilterCallback = filterCallback;

            var strBuilder = new StringBuilder();
            strBuilder.Append(name);
            strBuilder.Append(" (");
            
            for (int i = 0; i < extensions.Length; i++)
            {
                var ext = extensions[i];
                if (i > 0) strBuilder.Append(", ");
                strBuilder.Append('*');
                strBuilder.Append(ext);
            }

            strBuilder.Append(')');
            FullText = strBuilder.ToString();
        }

        // the isRw parameter -- if teh SetPath function had identified it as a
        // Rain World level file
        public bool Match(string fileName, bool isRw)
        {
            foreach (var ext in AllowedExtensions)
            {
                if (ext == ".*") return true;
            }

            var pathExt = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(pathExt)) return false;
            
            foreach (var ext in AllowedExtensions)
            {
                if (pathExt == ext)
                {
                    if (FilterCallback is null) return true;
                    else if (FilterCallback(fileName, isRw))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string Enforce(string fileName)
        {
            if (AllowedExtensions[0] != ".*" && Path.GetExtension(fileName) != AllowedExtensions[0])
                return fileName + AllowedExtensions[0];

            return fileName;
        }
    }

    private class EntrySorter : IComparer<Entry>
    {
        int IComparer<Entry>.Compare(Entry a, Entry b) => a.Name.CompareTo(b.Name);
    }

    private static void LoadIcons()
    {
        icons ??= RlManaged.Texture2D.Load(Path.Combine(Boot.AppDataPath,"assets","filebrowser-icons.png"));
    } 
    
    private static RlManaged.Texture2D icons = null!;
    public FileBrowser(OpenMode mode, Action<string> callback, string? openDir)
    {
        LoadIcons();

        this.mode = mode;
        this.callback = callback;
        fileFilters.Add(new FileFilter("Any", [ ".*" ]));
        selectedFilter = fileFilters[0];

        var projectsFolderPath = string.Empty;
        var doesProjectsFolderExist = false;

        if (RainEd.Instance is not null)
        {
            projectsFolderPath = Path.Combine(RainEd.Instance.AssetDataPath, "LevelEditorProjects");
            doesProjectsFolderExist = Directory.Exists(projectsFolderPath);
        }

        if (RainEd.Instance is not null)
        {
            if (openDir is not null)
            {
                cwd = openDir;
            }
            else
            {
                cwd = doesProjectsFolderExist ? projectsFolderPath : RainEd.Instance.AssetDataPath;
            }
        }
        else
        {
            cwd = openDir ?? Boot.AppDataPath;
        }
        
        SetPath(cwd);
        pathBuf = cwd;
        nameBuf = string.Empty;

        if (RainEd.Instance is not null && doesProjectsFolderExist)
        {
            AddBookmark("Projects", projectsFolderPath);
        }

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            AddBookmark("User", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        AddBookmark("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        AddBookmark("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddBookmark("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        AddBookmark("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        AddBookmark("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

        // idk why Environment.SpecialFolder doesn't have a MyDownloads enum;
        // xdg_user_dirs standardizes a download location, and mac has one as well.
        var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (Directory.Exists(downloadsFolder))
        {
            AddBookmark("Downloads", downloadsFolder);
        }

        // list drives
        try
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (var driveInfo in DriveInfo.GetDrives())
                {
                    if (!driveInfo.IsReady) continue;
                    var driveType = driveInfo.DriveType;

                    if (driveType == DriveType.NoRootDirectory) continue;

                    drives.Add(new BookmarkItem()
                    {
                        Name = driveInfo.Name,
                        Path = driveInfo.RootDirectory.FullName
                    });
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                // DriveInfo.GetDrives() lists a bunch of system stuff
                // that the user probably doesn't want to save their
                // rain world data in. I'm not sure how to properly
                // filter that stuff out. Instead, I will use
                // lsblk since it doesn't list that stuff.
                var proc = Process.Start(new ProcessStartInfo("lsblk")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Arguments = "-P -o NAME,MOUNTPOINT"
                });

                if (proc is null)
                {
                    drives.Add(new BookmarkItem("/", "/"));
                }
                else
                {
                    var stdout = proc.StandardOutput;
                    while (!stdout.EndOfStream)
                    {
                        var line = stdout.ReadLine();
                        if (string.IsNullOrEmpty(line)) break;

                        var match = MyRegex().Match(line);
                        string mountPoint = match.Groups[2].Value;
                        if (mountPoint.Length == 0 || mountPoint[0] != '/') continue; // mountpoint may be empty or [SWAP]
                        if (mountPoint.Length >= 5 && mountPoint[0..5] == "/boot") continue; // ignore efi boot stuff

                        var name = mountPoint == "/" ? mountPoint : Path.GetFileNameWithoutExtension(mountPoint);
                        drives.Add(new BookmarkItem(name, mountPoint));
                    }
                }
            }
            else if (OperatingSystem.IsFreeBSD())
            {
                // what the hell is free bsd
                drives.Add(new BookmarkItem("/", "/"));
            }
            
            // mac os doesn't get a drive listing i suppose
        }
        catch (Exception e)
        {
            if (RainEd.Instance is not null)
            {
                RainEd.Logger.Error("Could not get drive info:\n{Exception}", e.ToString());
            }
        }
    }

    private void AddBookmark(string name, string path)
    {
        if (path == "") return;
        if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
        bookmarks.Add(new BookmarkItem(name, path));
    }

    public void AddFilterWithCallback(string filterName, Func<string, bool, bool>? callback = null, params string[] allowedExtensions)
    {
        fileFilters.Add(new FileFilter(filterName, allowedExtensions, callback));

        // default filter is the first filter added, else "Any"
        selectedFilter = fileFilters[1];
    }

    public void AddFilter(string filterName, params string[] allowedExtensions)
        => AddFilterWithCallback(filterName, null, allowedExtensions);

    private static Rectangle GetIconRect(int index)
        => new Rectangle(index * 13, 0, 13, 13);

    private bool SetPath(string path)
    {
        if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
        if (!Directory.Exists(path))
        {
            openErrorPopup = true;
            errorMsg = "Directory does not exist";
            if (RainEd.Instance is not null) RainEd.Logger.Error("Directory {Path} does not exist", path);

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
            {
                if (!string.IsNullOrEmpty(dirName))
                {
                    entries.Add(new Entry(dirName, EntryType.Directory)
                    {
                        IconIndex = 5 // folder icon
                    });
                }
            }
        }
        entries.Sort(new EntrySorter());
        var firstFile = entries.Count;

        // add files
        var charBuf = new byte[4];

        foreach (var filePath in Directory.EnumerateFiles(path))
        {
            var fileName = Path.GetFileName(filePath);
            var fileInfo = File.GetAttributes(filePath);

            if (fileInfo.HasFlag(FileAttributes.Hidden)) continue;

            // quick read to see if it's a rain world level file
            // all rain world level files start with four brackets
            var icon = 6; // file icon
            if (Path.GetExtension(filePath) == ".txt")
            {
                try
                {
                    using var stream = File.OpenRead(filePath);
                    int n = stream.Read(charBuf, 0, 4);
                    if (n == 4 && charBuf[0] == '[' && charBuf[1] == '[' && charBuf[2] == '[' && charBuf[3] == '[')
                    {
                        icon = 7; // slugcat icon
                    }
                }
                catch
                {}
            }
            else if (Path.GetExtension(filePath) == ".lua")
            {
                icon = 9; // lua icon
            }

            entries.Add(new Entry(fileName, EntryType.File)
            {
                IconIndex = icon
            });
        }
        entries.Sort(firstFile, entries.Count - firstFile, new EntrySorter());
        pathBuf = path;

        // path tree into list
        pathList.Clear();
        foreach (var p in path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            pathList.Add(p);
        }

        needFilterRefresh = true;
        
        return true;
    }

    private void RefreshFilter()
    {
        filteredEntries.Clear();

        if (mode == OpenMode.Directory)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Name[0] == '.') continue; // hidden files/folders
                if (entry.Type == EntryType.Directory)
                {
                    filteredEntries.Add((i, entry));
                }
            }    
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Name[0] == '.') continue; // hidden files/folders
                if (entry.Type == EntryType.Directory || selectedFilter.Match(Path.Combine(cwd, entry.Name), entry.IconIndex == 7))
                {
                    filteredEntries.Add((i, entry));
                }
            }
        }
    }

    /// <summary>
    /// Render the file browser window, setting the referenced variable to null
    /// once the window has closed.
    /// </summary>
    /// <param name="fileBrowser"></param>
    /// <returns></returns>
    public static void Render(ref FileBrowser? fileBrowser)
    {
        if (fileBrowser is not null && !fileBrowser.Render())
        {
            fileBrowser = null;
        }
    }

    /// <summary>
    /// Render the file browser window
    /// </summary>
    /// <returns>`true` if the file browser window is still open, `false` if not</returns>
    /// <exception cref="Exception"></exception>
    public bool Render()
    {
        Vector4 textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];

        string winName = mode switch
        {
            OpenMode.Write => "Save File",
            OpenMode.Read => "Open File",
            OpenMode.Directory => "Open Folder",
            _ => throw new Exception("Invalid open mode")
        };

        if (!isOpen)
        {
            isOpen = true;
            ImGui.OpenPopup(winName + "###File Browser");

            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetTextLineHeight() * 60f, ImGui.GetTextLineHeight() * 30f), ImGuiCond.Appearing);
        }

        if (ImGui.BeginPopupModal(winName + "###File Browser"))
        {
            var windowSize = ImGui.GetWindowSize();

            // this makes the back, fwd, and up buttons closer together
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            // back button
            if (rlImGui.ImageButtonRect("<", icons, 13, 13, GetIconRect(0), textColor))
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
            if (rlImGui.ImageButtonRect(">", icons, 13, 13, GetIconRect(1), textColor))
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

            // pop style var before creating the up button,
            // because spacing is added after creating a button, not before.
            ImGui.PopStyleVar();

            if (rlImGui.ImageButtonRect("^", icons, 13, 13, GetIconRect(2), textColor))
            {
                var oldDir = cwd;
                if (SetPath(Path.Combine(cwd, "..")))
                {
                    selected = -1;
                    backStack.Push(oldDir);
                    forwardStack.Clear();
                }
            }
            ImGui.SetItemTooltip("Go To Parent Directory");

            ImGui.SameLine();
            if (rlImGui.ImageButtonRect("Refresh", icons, 13, 13, GetIconRect(4), textColor))
            {
                if (Directory.Exists(cwd))
                    SetPath(cwd);
                else
                    SetPath(Path.GetFullPath(Path.DirectorySeparatorChar.ToString()));
            }
            ImGui.SetItemTooltip("Refresh");

            ImGui.SameLine();
            if (rlImGui.ImageButtonRect("NewFolder", icons, 13, 13, GetIconRect(5), textColor))
            {
                ImGui.OpenPopup("Create Folder");
                folderName = "";
            }
            ImGui.SetItemTooltip("Create Folder");

            // create folder popup
            ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
            if (ImGuiExt.BeginPopupModal("Create Folder", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 20.0f);
                ImGui.InputTextWithHint("##Name", "Name", ref folderName, 128);

                ImGui.Separator();
                if (StandardPopupButtons.Show(PopupButtonList.OKCancel, out int btn))
                {
                    // if ok is pressed
                    if (btn == 0)
                    {
                        // check that a directory with the same name
                        // does not already exist
                        if (Directory.Exists(Path.Combine(cwd, folderName)))
                        {
                            errorMsg = "Directory with the same name already exists!";
                            ImGui.OpenPopup("Error");
                        }
                        else
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(cwd, folderName));

                                ImGui.CloseCurrentPopup();
                                folderName = "";
                            }
                            catch (Exception e)
                            {
                                if (RainEd.Instance is not null)
                                    RainEd.Logger.Error("Could not create directory!\n" + e);
                                
                                errorMsg = e.Message;
                                ImGui.OpenPopup("Error");
                            }

                            SetPath(cwd); // refresh
                        }
                    }

                    // if cancel was pressed
                    else if (btn == 1)
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }

                // show an error popup if necessary
                bool errClose = true;
                ImGuiExt.CenterNextWindow(ImGuiCond.Appearing);
                if (ImGui.BeginPopupModal("Error", ref errClose, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
                {
                    ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 30.0f);
                    ImGui.TextWrapped(errorMsg);
                    ImGui.PopTextWrapPos();
                    
                    ImGui.Separator();
                    if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
                        ImGui.CloseCurrentPopup();
                    
                    ImGui.EndPopup();
                }

                ImGui.EndPopup();
            }

            // current path
            ImGui.SameLine();
            if (enterPath || showPathInput)
            {
                bool closeTextInput = rlImGui.ImageButtonRect("Type", icons, 13, 13, GetIconRect(3), textColor);
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
                if (rlImGui.ImageButtonRect("Type", icons, 13, 13, GetIconRect(8), textColor))
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

                            var newPath = Path.Combine([.. pathList[0..(i+1)]]);
                            if (!OperatingSystem.IsWindows())
                            {
                                newPath = '/' + newPath;
                            }

                            if (SetPath(newPath))
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
            
            // list bookmarks/locations
            void ListBookmark(BookmarkItem location)
            {
                if (ImGui.Selectable(location.Name, cwd == location.Path))
                {
                    if (cwd != location.Path)
                    {
                        var old = cwd;
                        if (SetPath(location.Path))
                        {
                            backStack.Push(old);
                            forwardStack.Clear();
                            selected = -1;
                        }
                    }
                }
            }

            ImGui.BeginChild("Locations", new Vector2(ImGui.GetTextLineHeight() * 10f, listingHeight));
            {
                // list user locations
                foreach (var location in bookmarks)
                {
                    ListBookmark(location);
                }

                // list drives, if available
                if (drives.Count > 0)
                {
                    ImGui.Separator();

                    foreach (var drive in drives)
                    {
                        ListBookmark(drive);
                    }
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();

            var ok = false;

            // used only in directory mode. ok = open subfolder, dirSelect = submit directory
            var dirSelect = false;

            // list files in cwd
            ImGui.BeginChild("Listing", new Vector2(-0.0001f, listingHeight));
            {
                if (needFilterRefresh)
                {
                    RefreshFilter();
                    needFilterRefresh = false;
                }

                foreach ((var i, var entry) in filteredEntries)
                {
                    if (selected == i && scrollToSelected)
                    {
                        ImGui.SetScrollHereY();
                    }

                    // this is the offset into the file icon texture
                    int fileTypeIcon = entry.Type == EntryType.Directory ? 5 : 6;

                    rlImGui.ImageRect(icons, 13, 13, GetIconRect(entry.IconIndex), textColor);
                    ImGui.SameLine();
                    
                    var entryName = entry.Name;
                    if (entry.Type == EntryType.Directory)
                    {
                        entryName += Path.DirectorySeparatorChar;
                    } 

                    if (ImGui.Selectable(entryName, selected == i, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        selected = i;
                    }

                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        ok = true;

                    if (ImGui.IsItemActivated() && entry.Type == EntryType.File)
                    {
                        nameBuf = entryName;
                    }
                }
            }
            ImGui.EndChild();

            scrollToSelected = false;

            // ok and cancel buttons
            if (mode == OpenMode.Directory)
            {
                if (ImGui.Button("Open"))
                    ok = true;

                ImGui.SameLine();
                if (ImGui.Button("OK"))
                    dirSelect = true; 
            }
            else
            {
                if (ImGui.Button("OK")) ok = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel") || (!ImGui.GetIO().WantTextInput && EditorWindow.IsKeyPressed(ImGuiKey.Escape)))
            {
                isDone = true;
            }
            ImGui.SameLine();

            // file filter
            if (mode != OpenMode.Directory)
            {
                ImGui.SetNextItemWidth(ImGui.GetTextLineHeight() * 12f);
                if (ImGui.BeginCombo("##Filter", selectedFilter.FilterName))
                {
                    foreach (var filter in fileFilters)
                    {
                        bool isSelected = filter == selectedFilter;
                        if (ImGui.Selectable(filter.FullText, isSelected))
                        {
                            selectedFilter = filter;
                            needFilterRefresh = true;
                        }
                        
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(-0.0001f);

            var oldName = nameBuf;
            var enterPressed = ImGui.InputTextWithHint(
                "##Name",
                mode == OpenMode.Directory ? "Folder Name" : "File Name",
                ref nameBuf, 128,
                ImGuiInputTextFlags.EnterReturnsTrue
            );

            // find a file/directory that has the same name
            if (nameBuf != oldName)
            {
                // modify name to match current filter
                string name = selectedFilter.Enforce(nameBuf);
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
                            string name = selectedFilter.Enforce(nameBuf);
                            var absPath = Path.Combine(cwd, name);

                            if (File.Exists(absPath))
                            {
                                openOverwritePopup = true;
                                overwriteFileName = absPath;
                            }
                            else
                            {
                                isDone = true;
                                callbackStr = Path.Combine(cwd, name);
                            }
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
                        callbackStr = Path.Combine(cwd, ent.Name);
                    }
                }
                else if (mode == OpenMode.Directory && selected >= 0)
                {
                    var ent = entries[selected];
                    if (ent.Type == EntryType.Directory)
                        ActivateEntry(ent);
                }
            }

            // Directory Mode folder submit
            if (dirSelect)
            {
                if (selected >= 0)
                {
                    var ent = entries[selected];
                    if (ent.Type == EntryType.Directory)
                    {
                        isDone = true;
                        callbackStr = Path.Combine(cwd, ent.Name);
                    }
                }
                else
                {
                    isDone = true;
                    callbackStr = cwd;
                }
            }

            // show overwrite popup
            if (openOverwritePopup)
            {
                openOverwritePopup = false;
                ImGui.OpenPopup("Overwrite");
            }

            bool overwriteClose = true;
            if (ImGui.BeginPopupModal("Overwrite", ref overwriteClose, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.TextUnformatted($"{Path.GetFileName(overwriteFileName)} will be overwritten! Are you sure?");
                ImGui.Separator();
                if (StandardPopupButtons.Show(PopupButtonList.YesNo, out int btn))
                {
                    if (btn == 0) // yes
                    {
                        isDone = true;
                        callbackStr = overwriteFileName;
                    }
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }

            if (isDone)
            {
                ImGui.CloseCurrentPopup();
                callback(callbackStr);
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
                ImGui.Separator();
                if (StandardPopupButtons.Show(PopupButtonList.OK, out _))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
            
            ImGui.EndPopup();

            return true;
        }

        return false;
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

    // file browser button
    private static uint activeFileBrowserButton = 0;
    private static string? fileBrowserReturnValue = null;
    private static bool fileBrowserReturn = false;
    private static FileBrowser? fileBrowserButtonInstance = null;

    public static bool Button(string id, OpenMode openMode, ref string path)
    {
        static void Callback(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            fileBrowserReturn = true;
            fileBrowserReturnValue = path;
        }

        LoadIcons();

        var textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];
        if (rlImGui.ImageButtonRect(id, icons, 13, 13, GetIconRect(5), textColor))
        {
            activeFileBrowserButton = ImGui.GetItemID();
            fileBrowserReturn = false;
            fileBrowserButtonInstance = new FileBrowser(openMode, Callback, Path.GetDirectoryName(path));
        }
        uint buttonId = ImGui.GetItemID();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(path);

        if (activeFileBrowserButton == buttonId)
        {
            fileBrowserButtonInstance!.Render();

            if (fileBrowserReturn)
            {
                if (fileBrowserReturnValue is not null)
                    path = fileBrowserReturnValue;

                fileBrowserReturn = false;
                activeFileBrowserButton = 0;
                fileBrowserButtonInstance = null;
                return true;
            }

        }

        return false;
    }

    // used for getting drive list from lsblk
    [GeneratedRegex("NAME=\"(.*?)\" MOUNTPOINT=\"(.*?)\"")]
    private static partial Regex MyRegex();
}