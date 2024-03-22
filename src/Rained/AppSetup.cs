/*
* App setup
* This runs when a preferences.json file could not be located on boot, which probably means
* that the user needs to set up their Data folder
*/

using ImGuiNET;
using RainEd;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using System.IO.Compression;
using Drizzle.Ported;

class AppSetup
{
    // 0 = not started
    // 1 = downloading
    // 2 = extracting
    private int downloadStage = 0;
    private float downloadProgress = 0f;

    private string? callbackRes = null;
    private float callbackWait = 1f;
    private FileBrowser? fileBrowser = null;
    private Task? downloadTask = null;
    
    public bool Start(out string? assetDataPath)
    {
        assetDataPath = null;

        while (true)
        {
            if (Raylib.WindowShouldClose())
            {
                return false;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            rlImGui.Begin();

            if (!ImGui.IsPopupOpen("Configure Data"))
            {
                ImGui.OpenPopup("Configure Data");
            }

            // center popup modal
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));

            if (ImGuiExt.BeginPopupModal("Configure Data", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove))
            {
                if (callbackRes is not null)
                {
                    ImGui.Text("Launching Rained...");
                }
                else if (downloadTask is not null)
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
                }
                else
                {
                    ImGui.Text("Please configure the Rain World level editor data folder.\nIf you are unsure what to do, select \"Download And Install Data\".");

                    ImGui.Separator();

                    FileBrowser.Render(ref fileBrowser);

                    if (ImGui.Button("Choose Data Folder"))
                    {
                        fileBrowser = new FileBrowser(FileBrowser.OpenMode.Directory, FIleBrowserCallback, Boot.AppDataPath);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Download And Install Data"))
                    {
                        downloadTask = DownloadData();
                    }
                }

                ImGui.EndPopup();
            }

            rlImGui.End();

            if (callbackRes is not null)
            {
                // wait a bit so that the Launching Rained... message can appear
                callbackWait -= Raylib.GetFrameTime();
                if (callbackWait <= 0f)
                {
                    assetDataPath = callbackRes;
                    break;
                }
            }

            // when download is complete, signal app launch
            if (downloadTask is not null && downloadTask.IsCompletedSuccessfully)
            {
                downloadTask = null;
                callbackRes = Path.Combine(Boot.AppDataPath, "Data");
            }

            Raylib.EndDrawing();
        }

        return true;
    }

    private void FIleBrowserCallback(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            callbackRes = path;
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
                int maxEntries = zip.Entries.Count;
                int processedEntries = 0;

                foreach (var entry in zip.Entries)
                {
                    // replace the root folder name from "Drizzle.Data-community" to simply "Data"
                    var modifiedName = "Data" + entry.FullName[entry.FullName.IndexOf('/')..];

                    if (entry.FullName.EndsWith('/'))
                    {
                        Directory.CreateDirectory(Path.Combine(extractPath, modifiedName));
                    }
                    else
                    {
                        entry.ExtractToFile(Path.Combine(extractPath, modifiedName), true);
                    }

                    processedEntries++;
                    downloadProgress = (float)processedEntries / maxEntries; 
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