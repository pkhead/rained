/*
* App setup
* This runs when a preferences.json file could not be located on boot, which probably means
* that the user needs to set up their Data folder
*/

using ImGuiNET;
using RainEd;
using Raylib_cs;
using System.Numerics;
using System.IO.Compression;
using System.Diagnostics;

class AppSetup
{
    // 0 = not started
    // 1 = downloading
    // 2 = extracting
    private int downloadStage = 0;
    private float downloadProgress = 0f;

    private string? callbackRes = null;
    private List<string> missingDirs = [];
    private float callbackWait = 1f;
    private FileBrowser? fileBrowser = null;
    private Task? downloadTask = null;

    private const string StartupText = """
    Welcome to the Rained setup screen! Please configure the location of the Rain World level editor data folder.

    If you have installed a Rain World level editor before, you may click the "Choose Data folder" button to point Rained's data folder to your previous installation.

    If you have installed the official editor, or a mod of it like the Community Editor, you would thus select the folder where the executable is contained. Otherwise, you should find and select the Drizzle data folder for your previous editor. 

    If you are unsure what to do, select "Download Data".
    """;

    private enum SetupState
    {
        // where the user decides if they want to use a pre-exicsting RWLE install,
        // or download data from the internet
        SetupChoice,

        // where the user configures the data download
        // can do vanilla, or certain parts of solar's.
        DownloadConfiguration,

        // stuff is being downloaded from the internet.
        Downloading,

        // setup is done, now launching rained...
        Finished
    }

    private SetupState setupState = SetupState.SetupChoice;
    
    public bool Start(out string? assetDataPath)
    {
        assetDataPath = null;

        if (Boot.WindowScale == 1.0f)
        {
            Fonts.SetFont("ProggyClean");
        }
        else
        {
            Fonts.SetFont("ProggyVector-Regular");
        }

        while (true)
        {
            if (Raylib.WindowShouldClose())
            {
                return false;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Boot.ImGuiController!.Update(Raylib.GetFrameTime());

            ImGuiExt.EnsurePopupIsOpen("Configure Data");
            ImGuiExt.CenterNextWindow(ImGuiCond.Always);

            bool exitAppSetup = false;
            
            if (ImGuiExt.BeginPopupModal("Configure Data", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove))
            {
                switch (setupState)
                {
                    case SetupState.SetupChoice:
                        ShowSetupChoice();
                        break;
                    
                    case SetupState.DownloadConfiguration:
                        ShowDownloadConfiguration();
                        break;
                    
                    case SetupState.Downloading:
                        ShowDownload();
                        break;
                    
                    case SetupState.Finished:
                        exitAppSetup = ShowFinished(out assetDataPath);
                        break;
                    
                    default:
                        throw new UnreachableException("Invalid setup state mode");
                }

                ImGui.EndPopup();
            }

            Boot.ImGuiController!.Render();
            Raylib.EndDrawing();

            if (exitAppSetup) break;
        }

        return true;
    }

    private void ShowSetupChoice()
    {
        ImGui.PushTextWrapPos(ImGui.GetTextLineHeight() * 50.0f);
        ImGui.TextWrapped(StartupText);
        ImGui.PopTextWrapPos();

        ImGui.Separator();

        FileBrowser.Render(ref fileBrowser);

        if (ImGui.Button("Choose Data Folder"))
        {
            fileBrowser = new FileBrowser(FileBrowser.OpenMode.Directory, FileBrowserCallback, Boot.AppDataPath);
        }

        ImGui.SameLine();
        if (ImGui.Button("Download Data"))
        {
            setupState = SetupState.Downloading;
            downloadTask = DownloadData();
        }

        // show missing dirs popup
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
    }

    private void ShowDownloadConfiguration()
    {

    }

    private void ShowDownload()
    {
        if (downloadStage == 1)
        {
            ImGui.Text("Downloading from\nhttps://github.com/SlimeCubed/Drizzle.Data/archive/refs/heads/community.zip...");
        }
        else if (downloadStage == 2)
        {
            ImGui.Text("Extracting...");
        }
        else
        {
            ImGui.Text("Starting...");
        }

        ImGui.ProgressBar(downloadProgress, new Vector2(ImGui.GetTextLineHeight() * 50.0f, 0.0f));

        // when download is complete, signal app launch
        if (downloadTask is not null && downloadTask.IsCompletedSuccessfully)
        {
            downloadTask = null;
            callbackRes = Path.Combine(Boot.AppDataPath, "Data");
            setupState = SetupState.Finished;
        }
    }

    private bool ShowFinished(out string? assetDataPath)
    {
        Debug.Assert(callbackRes is not null);
        ImGui.Text("Launching Rained...");

        // wait a bit so that the Launching Rained... message can appear
        callbackWait -= Raylib.GetFrameTime();
        if (callbackWait <= 0f)
        {
            assetDataPath = callbackRes;
            return true;
        }

        assetDataPath = null;
        return false;
    }

    private void FileBrowserCallback(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            // check for any missing directories
            missingDirs.Clear();
            missingDirs.Add("Graphics");
            missingDirs.Add("Props");
            missingDirs.Add("Levels");

            for (int i = missingDirs.Count - 1; i >= 0; i--)
            {
                if (Directory.Exists(Path.Combine(path, missingDirs[i])))
                {
                    missingDirs.RemoveAt(i);
                }
            }

            if (missingDirs.Count == 0)
            {
                callbackRes = path;
                setupState = SetupState.Finished;
            }
        }
    }

    private async Task DownloadData()
    {
        var tempZipFile = Path.GetTempFileName();
        Console.WriteLine("Zip located at " + tempZipFile);

        try
        {
            // download the zip file
            downloadStage = 1;
            using (var client = new HttpClient())
            {
                using var outputStream = File.OpenWrite(tempZipFile);

                var response = await client.GetAsync("https://github.com/SlimeCubed/Drizzle.Data/archive/refs/heads/community.zip");
                response.EnsureSuccessStatusCode();
                
                var contentLength = response.Content.Headers.ContentLength;
                using var download = await response.Content.ReadAsStreamAsync();

                // read http response into tempZipFile
                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await download.ReadAsync(buffer).ConfigureAwait(false)) != 0)
                {
                    await outputStream.WriteAsync(buffer).ConfigureAwait(false);
                    totalBytesRead += bytesRead;

                    if (contentLength.HasValue)
                        downloadProgress = (float)totalBytesRead / contentLength.Value;
                }
            }
            
            // begin extracting the zip file
            downloadStage = 2;
            downloadProgress = 0f;
            
            // ensure last character of dest path ends with a directory separator char
            // (for security reasons)
            var extractPath = Boot.AppDataPath;
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;
            
            using (var zip = ZipFile.OpenRead(tempZipFile))
            {
                // get the number of entries that aren't in the Cast folder
                // (the cast folder will not be extracted)
                int entryCount = 0;
                string ignoreFilter = "Drizzle.Data-community/Cast";
                foreach (var entry in zip.Entries)
                {
                    var fullName = entry.FullName;
                    if (fullName.Length >= ignoreFilter.Length && fullName[0..ignoreFilter.Length] == ignoreFilter)
                        continue;

                    entryCount++;
                }

                int processedEntries = 0;

                foreach (var entry in zip.Entries)
                {
                    // replace the root folder name from "Drizzle.Data-community" to simply "Data"
                    // also, ignore anything in cast - i already copied the data in there into assets/drizzle-cast
                    var modifiedName = "Data" + entry.FullName[entry.FullName.IndexOf('/')..];
                    if (modifiedName.Length >= 9 && modifiedName[0..9] == "Data/Cast") continue;

                    if (entry.FullName.EndsWith('/'))
                    {
                        Directory.CreateDirectory(Path.Combine(extractPath, modifiedName));
                    }
                    else
                    {
                        entry.ExtractToFile(Path.Combine(extractPath, modifiedName), true);
                    }

                    processedEntries++;
                    downloadProgress = (float)processedEntries / entryCount; 
                }
            }
        }
        finally
        {
            Console.WriteLine("Delete " + tempZipFile);
            File.Delete(tempZipFile);
        }
    }
}