// TODO: consider using a mutex again, instead of a lockfile. mutexes works fine
// on  linux and mac, and while I think it just internally makes a file on those
// platforms, i think it's preferable to use a more direct methodology for
// Windows.

namespace Rained;

using System.Threading;
using System.IO.Pipes;
using System.Text;

/// <summary>
/// Functionality for new rainED processes to open a file in a pre-existing instance of rainED.
/// </summary>
class SingleInstanceManager : IDisposable
{
    public static bool IsSupported => true;

    public Action<string[]>? OnLevelOpenRequest;

    private const string PipeName = "RAINED_IPCPIPE";
    private readonly string LockFilePath = Path.Combine(Path.GetTempPath(), "RAINED_INST.lock");

    private bool _isDisposed = false;
    private volatile bool abort = false;
    private Thread? pipeThread;
    private FileStream? lockFileStream;

    /// <summary>
    /// Check if an instance of rainED is already running, and if so, open the given level file with it.
    /// </summary>
    /// <param name="bootOptions">The boot options.</param>
    /// <returns>True if the application should abort, false otherwise.</returns>
    public bool Start(BootOptions bootOptions)
    {
        if (bootOptions.Lifetime == BootOptions.InstanceLifetime.Batch)
            return false;
        
        if (!IsSupported)
            throw new InvalidOperationException("SingleInstanceManager is not supported on this platform.");

        // this is the first instance of rainED if it was able to successfully open the lockfile        
        bool isFirstInstance = false;
        try
        {
            lockFileStream = new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            isFirstInstance = true;
        }
        catch (IOException)
        {
            isFirstInstance = false;
        }

        if (isFirstInstance)
        {
            pipeThread = new Thread(MainInstanceThreadProc) { IsBackground = true };
            pipeThread.Start();
            return false;
        }
        else
        {
            // this is not the first instance of rainED.
            if (bootOptions.Files.Count > 0 && bootOptions.Lifetime != BootOptions.InstanceLifetime.PersistentNoReuse)
            {
                // request the first instance to open these list of level
                try
                {
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    pipe.Connect(1000);

                    using var pipeWriter = new BinaryWriter(pipe);

                    // write message type
                    // currently only 0 for "open levels" is implemented, but in the future
                    // i may want to add more types.
                    pipeWriter.Write((byte)0);

                    // write number of levels to open
                    pipeWriter.Write((byte)bootOptions.Files.Count);

                    // then, write the file paths of each level to open.
                    // each string is preceded by its length as a u16.
                    foreach (var path in bootOptions.Files)
                    {
                        var data = Encoding.UTF8.GetBytes(Path.GetFullPath(path));
                        pipeWriter.Write((ushort)data.Length);
                        pipeWriter.Write(data);
                    }

                    pipeWriter.Flush();
                    return true;
                }
                catch (Exception e)
                {
                    Boot.PrintError("error communicating with previous instance: " + e);
                    Environment.ExitCode = 1;
                    return true;
                }
            }

            return false;
        }
    }

    private void MainInstanceThreadProc()
    {
        while (!abort)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.In, 1);
                pipe.WaitForConnection();

                if (abort) break;

                using var pipeReader = new BinaryReader(pipe);
                
                var msgType = (int) pipeReader.ReadByte();
                switch (msgType)
                {
                    case 0:
                    {
                        Log.Information("received IPC message to open levels");

                        // read number of file paths to open
                        var levelCount = (int) pipeReader.ReadByte();
                        var filePaths = new string[levelCount];

                        for (int i = 0; i < levelCount; i++)
                        {
                            // read individual path to level. the string is preceded by its length as an unsigned 16-bit int
                            var strLen = (int) pipeReader.ReadUInt16();
                            var strData = new byte[strLen];
                            pipeReader.Read(strData, 0, strLen);

                            filePaths[i] = Encoding.UTF8.GetString(strData);
                        }

                        OnLevelOpenRequest?.Invoke(filePaths);
                        break;
                    }

                    default:
                        Log.Error("received IPC message, but message type is unknown ({MessageType})", msgType);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error("IPC pipe thread error: {Exception}", e.ToString());
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        abort = true;

        // create a fake connection to the pipe in order to unblock the thread
        try {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(5000);
        } catch { }

        pipeThread?.Join(5000);

        lockFileStream?.Dispose();

        try { File.Delete(LockFilePath); } catch { }
    }
}
