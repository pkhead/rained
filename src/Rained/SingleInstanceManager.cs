// TODO: put try-catches to handle any sort of error gracefully...

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

    private bool _isDisposed = false;
    private bool abort = false;
    private Thread? pipeThread;
    private Mutex? instMutex;

    private const string PipeName = @"Global\RAINED_IPCPIPE";
    private const string MutexName = @"Global\RAINED_INSTMTX";
    private static readonly string PipeCompletionFile = Path.Combine(Path.GetTempPath(), "RAINED_IPCPIPESIG");

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

        bool forceNewInstance = bootOptions.Lifetime == BootOptions.InstanceLifetime.PersistentNoReuse;
        var filePaths = bootOptions.Files;

        instMutex = new Mutex(true, MutexName, out bool instMutexIsNew);

        if (instMutexIsNew)
        {
            instMutex.WaitOne();

            // this is the first instance of rainED
            // start level request listener thread
            pipeThread = new Thread(MainInstanceThreadProc) { IsBackground = true };
            pipeThread.Start();

            return false;
        }
        else
        {
            instMutex.Dispose();
            instMutex = null;

            if (filePaths.Count > 0 && !forceNewInstance)
            {
                // this is not the first instance of rainED
                // request the first instance to open these list of levels
                if (filePaths.Count > byte.MaxValue)
                {
                    Boot.PrintError("too many file paths to open");
                    Environment.ExitCode = 2;
                    return false;
                }

                // Debug.Assert(OperatingSystem.IsWindows());
                // using (var otherWait = new EventWaitHandle(false, EventResetMode.AutoReset, WaitSignalName))
                //     otherWait.Set();
                
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1);
                pipe.WaitForConnection();

                var pipeWriter = new BinaryWriter(pipe);

                // write message type
                pipeWriter.Write((byte)0);

                // write number of levels to open
                pipeWriter.Write((byte)(filePaths.Count & 0xFF));

                // then, write the file paths of each level to open.
                // each string is preceded by its length as an unsigned 16-bit integer
                foreach (var path in filePaths)
                {
                    var pathData = Encoding.UTF8.GetBytes(Path.GetFullPath(path));
                    var pathLen = pathData.Length;

                    pipeWriter.Write((ushort)pathLen);
                    pipeWriter.Write(pathData, 0, int.Min(ushort.MaxValue, pathLen));
                }

                // done
                pipeWriter.Flush();
                
                if (OperatingSystem.IsWindows())
                {
                    pipe.WaitForPipeDrain();
                }
                else
                {
                    Thread.Sleep(1000);
                    while (!File.Exists(PipeCompletionFile))
                        Thread.Sleep(10);
                }

                return true;
            }
        }

        return false;
    }

    private void MainInstanceThreadProc()
    {
        while (true)
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.In);

            // wait for a client, periodically checking if the thread should abort.
            // it is a background thread, so it doesn't need to be manually aborted
            // before the process should end, but i suppose it is preferable to cleanly
            // dispose of the OS handles.
            while (true)
            {
                if (abort) return;

                try
                {
                    pipe.Connect(500);
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            var pipeReader = new BinaryReader(pipe);

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

            if (!OperatingSystem.IsWindows())
                File.Create(PipeCompletionFile);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (disposing)
        {
            instMutex?.Dispose();
        }

        if (pipeThread is not null)
        {
            abort = true;
            pipeThread.Join();
        }
    }
}