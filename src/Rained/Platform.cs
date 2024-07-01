using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace RainEd;

/// <summary>
/// Provides methods to perform platform-specific tasks.
/// </summary>
static partial class Platform
{
    // import win32 MessageBox function
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Display an error message to the user. The application will be blocked until the message is closed.
    /// </summary>
    /// <param name="windowTitle">The title of the error window.</param>
    /// <param name="windowContents">The message to display within the window.</param>
    /// <returns>True if the operation is supported on the running platform, false if not.</returns>
    public static bool DisplayError(string windowTitle, string windowContents)
    {
        bool success = false;

        if (OperatingSystem.IsWindows())
        {
            success = true;
            MessageBoxW(new IntPtr(0), windowContents, windowTitle, 0x10);
        }
        else if (OperatingSystem.IsLinux())
        {
            // try using zenity
            try
            {
                var procStartInfo = new ProcessStartInfo("zenity", ["--error", "--text", windowContents, "--title", windowTitle])
                {
                    UseShellExecute = false,
                };

                Process.Start(procStartInfo)!.WaitForExit();
                success = true;
            }
            catch (Exception)
            {}
        }

        return success;
    }

    /// <summary>
    /// Open a URL in the user's preferred browser application.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <returns>True if the operation is supported on the running platform, false if not.</return>
    public static bool OpenURL(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", url);
            return true;
        }
        else // assume linux
        {
            try
            {
                Process.Start("xdg-open", url);
                return true;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Open a file or folder using the user's preferred viewer application for the item.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <returns>True if the operation is supported on the running platform, false if not.</return>
    public static bool OpenPath(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", path);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // I can't test if this actually works as intended
                Process.Start("open", path);
            }
            else // assume Linux
            {
                Process.Start("xdg-open", path);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Show a file or directory in the user's file system browser.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <returns>True if the operation is supported on the running platform, false if not.</return>
    public static bool RevealPath(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", "/select," + path);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // I can't test if this actually works as intended
                Process.Start("open", "-R " + path);
            }
            else // assume Linux
            {
                Process.Start("xdg-open", Path.GetDirectoryName(path)!);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Send a file to the trash bin.
    /// </summary>
    /// <param name="filePath">The path of the file to trash.</param>
    /// <returns>True if the operation is supported on the running platform, false if not.</returns>
    public static bool TrashFile(string file)
    {
        file = Path.GetFullPath(file);
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($"Attempt to trash nonexistent file \"{file}\"");
        }

        if (OperatingSystem.IsWindows())
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            return true;
        }
        else if (OperatingSystem.IsMacOS())
        {
            // run AppleScript interpreter to trash file
            // (untested)
            try
            {
                var proc = Process.Start("osascript", ["-e", $"tell application \"Finder\" to delete POSIX file \"{file}\""])!;
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
            }
            catch {}

            return false;
        }
        else // assume Linux...
        {
            bool success = false;
            
            // try GIO
            try
            {
                var proc = Process.Start("gio", ["trash", file])!;
                proc.WaitForExit();
                if (proc.ExitCode == 0) success = true;
            }
            catch {}
            if (success) return true;

            // then, try KFMCLIENT (KDE)
            try
            {
                var proc = Process.Start("kfmclient", ["move", file, "trash:/"])!;
                proc.WaitForExit();
                if (proc.ExitCode == 0) success = true;
            }
            catch {}
            if (success) return true;

            // fallback, try directly sending the file to the trash folder following XDG specifications
            // for some reason, thunar on xfce on debian does not seem to recognize the .trashinfo file
            // not sure why, it's exactly the same as the result from gio...
            try
            {
                string xdgDataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local","share");
                {
                    var env = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                    if (!string.IsNullOrEmpty(env))
                    {
                        xdgDataHome = env;
                    }
                }

                var trashFilesFolder = Path.Combine(xdgDataHome, "Trash", "files");
                var trashInfoFolder = Path.Combine(xdgDataHome, "Trash", "info");
                if (!Directory.Exists(trashFilesFolder) || !Directory.Exists(trashInfoFolder))
                {
                    throw new PlatformNotSupportedException();
                }

                // the Trash folders does exists!!!
                var fileName = Path.GetFileName(file);

                File.Move(file, Path.Combine(trashFilesFolder, fileName));
                
                using (var infoFile = new StreamWriter(Path.Combine(trashInfoFolder, fileName + ".trashinfo"), false))
                {
                    var dateStr = DateTime.Now.ToString(@"yyyy-MM-dd\THH:mm:ss", CultureInfo.InvariantCulture);
                    
                    infoFile.WriteLine("[Trash Info]");
                    infoFile.WriteLine("Path=" + file);
                    infoFile.WriteLine("DeletionDate=" + dateStr);
                }

                success = true;
            }
            catch {}
            if (success) return true;

            return false;
        }

        return false;
    }
}