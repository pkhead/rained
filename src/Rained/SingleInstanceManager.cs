namespace Rained;

using System.Threading;
using System.IO.Pipes;
using System.Text;

/// <summary>
/// Functionality for new rainED processes to open a file in a pre-existing instance of rainED.
/// </summary>
public class SingleInstanceManager : IDisposable
{
    // public static SingleInstanceManager Instance {
    //     get
    //     {
    //         if (singleton is not null) return singleton;
    //         return singleton = new SingleInstanceManager();
    //     }
    // }

    // private static SingleInstanceManager? singleton;

    public bool IsSupported { get; private set; }

    public Action<string[]>? OnLevelOpenRequest;

    private bool _isDisposed = false;
    private bool abort = false;
    private Thread? pipeThread;
    private Mutex? instMutex;

    private const string PipeName = @"Global\RAINED_IPCPIPE";
    private const string WaitSignalName = @"Global\RAINED_IPCWAIT";
    private const string MutexName = @"Global\RAINED_INSTMTX";

    // on launch, acquire a mutex named RAINED_INSTMTX.
    // if mutex does not exist, this is the first rained instance to be launched.
    // if mutex does exist, then it is not the first rained instance to be launched.

    // for first instance:
    // on every frame, it will try to acquire the RAINED_PIPEBARRIER mutex if it exists. if it does exist and
    // successfully acquires the mutex, it will read the RAINED_IPCPIPE pipe, and then close the mutex.

    // for subsequent instances:
    // create the RAINED_IPCPIPE pipe, and write IPC transfer data to it. afterwards, create a mutex named
    // RAINED_PIPEBARRIER and wait on it.
    public SingleInstanceManager()
    {
        IsSupported = OperatingSystem.IsWindows();
    }

    /// <summary>
    /// Check if an instance of rainED is already running, and if so, open the given level file with it.
    /// </summary>
    /// <param name="filePath">The file path to open in the pre-existing instance of rainED.</param>
    /// <returns>True if the application should abort, false otherwise.</returns>
    public bool Start(string[] filePaths)
    {
        if (!IsSupported)
            throw new InvalidOperationException("SingleInstanceManager is not supported on this platform.");
        
        if (!OperatingSystem.IsWindows()) return false;

        instMutex = new Mutex(true, MutexName, out bool instMutexIsNew);

        if (instMutexIsNew)
        {
            Console.WriteLine("First instance!");

            instMutex.WaitOne();

            // this is the first instance of rainED
            pipeThread = new Thread(MainInstanceThreadProc);
            pipeThread.Start();

            return false;
        }
        else if (filePaths.Length > 0)
        {
            Console.WriteLine("Not First instance!");

            instMutex.Dispose();
            instMutex = null;

            // this is not the first instance of rainED
            using (var otherWait = EventWaitHandle.OpenExisting(WaitSignalName))
                otherWait.Set();
            
            using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1);
            pipe.WaitForConnection();

            var data = Encoding.UTF8.GetBytes("Hello, world!");
            var dataLen = data.Length;

            // first write, length of data as an unsigned 16-bit integer
            Console.WriteLine($"Write {dataLen} bytes to pipe");
            pipe.WriteByte((byte)((dataLen >> 8) & 0xFF));
            pipe.WriteByte((byte)(dataLen & 0xFF));

            // then, write data contents
            pipe.Write(data);
            pipe.Flush();

            // using var myWait = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\RAINED_IPCWAIT2");
            // myWait.WaitOne();

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

            // length of data is a unsigned 16-bit integer
            var dataLen = ((int)pipe.ReadByte()) | ((int)pipe.ReadByte() << 8);
            Log.Debug("Read {ByteCount} bytes from pipe", dataLen);
            var data = new byte[dataLen];
            pipe.Read(data, 0, dataLen);

            var list = new string[1];
            list[0] = Encoding.UTF8.GetString(data);
            Log.Debug("It read: {Contents}", list[0]);

            OnLevelOpenRequest?.Invoke(list);
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
        // singleton = null;

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