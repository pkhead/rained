using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Rained.LevelData;

namespace Rained;

/// <summary>
/// Provides methods to perform platform-specific tasks.
/// </summary>
static partial class Platform
{
    // import win32 MessageBox function
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint RegisterClipboardFormatW(string lpszFormat);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int EmptyClipboard();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalAlloc(uint uFlags, nint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalUnlock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalSize(nint hMem);

    private static void ThrowWin32Error()
    {
        throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        //Marshal.GetPInvokeErrorMessage()
    }

    private static void ThrowWin32ErrorIfZero(uint value)
    {
        if (value == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
    }

    private static void ThrowWin32ErrorIfZero(int value)
    {
        if (value == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
    }

    private static void ThrowWin32ErrorIfZero(nint value)
    {
        if (value == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
    }

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
            // try using kdialog
            try
            {
                var procStartInfo = new ProcessStartInfo("kdialog", ["--title", windowTitle, "--error", windowContents])
                {
                    UseShellExecute = false
                };

                Process.Start(procStartInfo)!.WaitForExit();
                success = true;
            }
            catch (Exception)
            {
                // if kdialog didn't work, try using zenity
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
    }

    #region Clipboard
    public enum ClipboardDataType
    {
        LevelCells
    }

    private static bool _win32_didRegisterFormats = false;
    private static uint _win32_levelCellsFmt;

    private const string LevelCellsMimeType = "application/x.rainworld-level-cells";

    private static void Win32RegisterFormats()
    {
        if (_win32_didRegisterFormats) return;
        _win32_levelCellsFmt = RegisterClipboardFormatW(LevelCellsMimeType);
        ThrowWin32ErrorIfZero(_win32_levelCellsFmt);
        
        _win32_didRegisterFormats = true;
    }

    public static unsafe bool SetClipboard(Glib.Window glibWindow, ClipboardDataType type, ReadOnlySpan<byte> data)
    {
        if (OperatingSystem.IsWindows())
        {
            Win32RegisterFormats();
            var formatId = type switch
            {
                ClipboardDataType.LevelCells => _win32_levelCellsFmt,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            bool isClipboardOpen = false;
            bool success = false;
            try
            {
                (nint hwnd, _, _) =
                    glibWindow.SilkWindow.Native?.Win32 ?? throw new Exception("Could not fetch native handles");

                ThrowWin32ErrorIfZero( OpenClipboard(hwnd) );
                isClipboardOpen = true;
                
                ThrowWin32ErrorIfZero( EmptyClipboard() );
                
                // create allocation
                var alloc = GlobalAlloc(0x2, data.Length);
                ThrowWin32ErrorIfZero( alloc );
                
                // copy size of data as well as data itself into allocation
                {
                    var allocData = GlobalLock(alloc);
                    ThrowWin32ErrorIfZero(allocData);

                    fixed (byte* dataPtr = data)
                        Buffer.MemoryCopy(dataPtr, (void*)allocData, data.Length, data.Length);
                    
                    GlobalUnlock(alloc);
                    if (Marshal.GetLastPInvokeError() != 0)
                        ThrowWin32Error();
                }
                
                ThrowWin32ErrorIfZero( SetClipboardData(formatId, alloc) );
                
                success = true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            finally
            {
                if (isClipboardOpen) CloseClipboard();
            }

            return success;
        }
        else if (OperatingSystem.IsLinux())
        {
            var mimeType = type switch
            {
                ClipboardDataType.LevelCells => LevelCellsMimeType,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            // use xclip to write contents to clipboard
            try
            {
                var proc = Process.Start("xclip", ["-selection", "clip", "-t", mimeType, "-i"]);
                using (var binStream = new BinaryWriter(proc.StandardInput.BaseStream))
                {
                    binStream.Write(data);
                }
                proc.WaitForExit();
                if (proc.ExitCode == 0) return true;
            }
            catch {}
            return false;
        }
        else
        {
            return false;
        }
    }

    public static unsafe bool GetClipboard(Glib.Window glibWindow, ClipboardDataType type, [NotNullWhen(true)] out byte[]? data)
    {
        data = null;

        if (OperatingSystem.IsWindows())
        {
            bool isClipboardOpen = false;
            bool success = false;
            try
            {
                (nint hwnd, _, _) =
                    glibWindow.SilkWindow.Native?.Win32 ?? throw new Exception("Could not fetch native handles");
                
                Win32RegisterFormats();
                var formatId = type switch
                {
                    ClipboardDataType.LevelCells => _win32_levelCellsFmt,
                    _ => throw new ArgumentOutOfRangeException(nameof(type))
                };
                
                if (IsClipboardFormatAvailable(formatId) == 0)
                    return false;
                
                ThrowWin32ErrorIfZero( OpenClipboard(hwnd) );
                isClipboardOpen = true;

                // get handle to clipboard data
                var hGlobal = GetClipboardData(formatId);
                ThrowWin32ErrorIfZero( hGlobal );

                // obtain data from handle
                {                    
                    var allocData = GlobalLock(hGlobal);
                    ThrowWin32ErrorIfZero( allocData );
                    
                    var size = GlobalSize(hGlobal);
                    ThrowWin32ErrorIfZero(size);

                    data = new byte[size];
                    fixed (byte* dataPtr = data)
                        Buffer.MemoryCopy((void*)allocData, dataPtr, data.Length, size);
                    
                    GlobalUnlock(hGlobal);
                    if (Marshal.GetLastPInvokeError() != 0)
                        ThrowWin32Error();
                }

                success = true;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            finally
            {
                if (isClipboardOpen) CloseClipboard();
            }

            return success;
        }
        else if (OperatingSystem.IsLinux())
        {
            var mimeType = type switch
            {
                ClipboardDataType.LevelCells => LevelCellsMimeType,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            // use xclip to fetch contents of clipboard
            try
            {
                var proc = Process.Start("xclip", ["-selection", "clip", "-t", mimeType, "-o"]);
                using (var ms = new MemoryStream())
                {
                    proc.StandardOutput.BaseStream.CopyTo(ms);
                    data = ms.ToArray();
                }
                proc.WaitForExit();
                if (proc.ExitCode == 0) return data.Length > 0;
            }
            catch {}
            return false;
        }
        else
        {
            return false;
        }
    }
    #endregion Clipboard

    #region High-precision sleep
    public partial class SleepHandler : IDisposable
    {
        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint CreateWaitableTimerW(nint lpTimerAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, string? lpTimerName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static partial bool SetWaitableTimer(nint hTimer, long* pDueTime, int lPeriod, nint pfnCompletionRoutine, nint lpArgToCompletionRoutine, [MarshalAs(UnmanagedType.Bool)] bool fResume);

        [LibraryImport("kernel32.dll")]
        private static partial int WaitForSingleObject(nint hHandle, uint dwMilliseconds);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(nint hObject);

        [LibraryImport("winmm.dll")]
        private static partial uint timeBeginPeriod(uint uPeriod);

        [LibraryImport("winmm.dll")]
        private static partial uint timeEndPeriod(uint uPeriod);
        
        private nint _handle;

        public SleepHandler()
        {
            if (OperatingSystem.IsWindows())
            {
                _handle = CreateWaitableTimerW(0, true, null);
                if (_handle == 0)
                {
                    throw new Exception("Could not create sleep handler");
                }
            }
            else{
                _handle = 0;
            }
        }

        public unsafe void Wait(double seconds)
        {
            if (OperatingSystem.IsWindows())
            {
                bool period = timeBeginPeriod(2) == 0;

                long dueTime = -(long)(seconds * 1e7);
                if (!SetWaitableTimer(_handle, &dueTime, 0, 0, 0, false))
                {
                    throw new Exception("Could not wait sleep handler");
                }

                WaitForSingleObject(_handle, uint.MaxValue);
                if (period) timeEndPeriod(2);
            }
            else
            {
                Thread.Sleep((int)(seconds * 1000.0));
            }
        }

        public void Dispose()
        {
            if (OperatingSystem.IsWindows())
            {
                CloseHandle(_handle);
            }
            
            GC.SuppressFinalize(this);
        }
    }
    #endregion High-precision sleep
}