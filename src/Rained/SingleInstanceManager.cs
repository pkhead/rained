namespace Rained;

using System.Threading;
using System.IO.Pipes;
using System.Text;
using System.Diagnostics;

/// <summary>
/// Functionality for new rainED processes to open a file in a pre-existing instance of rainED.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    public static bool IsSupported => true;

    public Action<string[]>? OnLevelOpenRequest;

    private bool _isDisposed = false;
    private bool abort = false;
    private Thread? pipeThread;
    private Mutex? instMutex;

    private const string PipeName = @"Global\RAINED_IPCPIPE";
    private const string WaitSignalName = @"Global\RAINED_IPCWAIT";
    private const string MutexName = @"Global\RAINED_INSTMTX";

    /// <summary>
    /// Check if an instance of rainED is already running, and if so, open the given level file with it.
    /// </summary>
    /// <param name="filePath">The file path to open in the pre-existing instance of rainED.</param>
    /// <returns>True if the application should abort, false otherwise.</returns>
    public bool Start(string[] filePaths)
    {
        if (!IsSupported)
            throw new InvalidOperationException("SingleInstanceManager is not supported on this platform.");        

        instMutex = new Mutex(true, MutexName, out bool instMutexIsNew);

        if (instMutexIsNew)
        {
            instMutex.WaitOne();

            // this is the first instance of rainED
            // start level request listener thread
            pipeThread = new Thread(MainInstanceThreadProc);
            pipeThread.Start();

            return false;
        }
        else if (filePaths.Length > 0)
        {
            // this is not the first instance of rainED
            // request the first instance to open these list of levels
            if (filePaths.Length > byte.MaxValue)
            {
                Console.Error.WriteLine("error: too many file paths to open!");
                Environment.ExitCode = 1;
                return false;
            }

            instMutex.Dispose();
            instMutex = null;

            // Debug.Assert(OperatingSystem.IsWindows());
            using (var otherWait = new EventWaitHandle(false, EventResetMode.AutoReset, WaitSignalName))
                otherWait.Set();
            
            using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1);
            pipe.WaitForConnection();

            var pipeWriter = new BinaryWriter(pipe);

            // first, write number of levels to open
            pipeWriter.Write((byte)(filePaths.Length & 0xFF));

            // then, write the file paths of each level to open.
            // each string is preceded by its length as an unsigned 16-bit integer
            foreach (var path in filePaths)
            {
                var pathData = Encoding.UTF8.GetBytes(path);
                var pathLen = pathData.Length;

                pipeWriter.Write((ushort)pathLen);
                pipeWriter.Write(pathData, 0, int.Min(ushort.MaxValue, pathLen));
            }

            // done
            pipeWriter.Flush();

            return true;
        }

        return false;
    }

    private void MainInstanceThreadProc()
    {
        using var myWait = new EventWaitHandle(false, EventResetMode.AutoReset, WaitSignalName);
        while (true)
        {
            // wait on the signal, periodically checking if the thread should abort.
            while (true)
            {
                if (abort) return;
                var sig = myWait.WaitOne(20);
                if (sig) break;
            }

            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.In);
            pipe.Connect();

            var pipeReader = new BinaryReader(pipe);

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