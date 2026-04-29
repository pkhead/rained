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
    private const string PipeName = "RAINED_IPCPIPE";
    private readonly string LockFilePath = Path.Combine(Path.GetTempPath(), "RAINED_INST.lock");

    private bool _isDisposed = false;
    private volatile bool abort = false;
    private Thread? pipeThread;
    private FileStream? lockFileStream;
    public Action<string[]>? OnLevelOpenRequest;

    public bool Start(BootOptions bootOptions)
    {
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
            if (bootOptions.Files.Count > 0 && bootOptions.Lifetime != BootOptions.InstanceLifetime.PersistentNoReuse)
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    pipe.Connect(1000);

                    using var pipeWriter = new BinaryWriter(pipe);
                    pipeWriter.Write((byte)0);
                    pipeWriter.Write((byte)bootOptions.Files.Count);

                    foreach (var path in bootOptions.Files)
                    {
                        var data = Encoding.UTF8.GetBytes(Path.GetFullPath(path));
                        pipeWriter.Write((ushort)data.Length);
                        pipeWriter.Write(data);
                    }
                    pipeWriter.Flush();
                    return true;
                }
                catch
                {
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
                var msgType = pipeReader.ReadByte();
                if (msgType == 0)
                {
                    int count = pipeReader.ReadByte();
                    var paths = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        int len = pipeReader.ReadUInt16();
                        paths[i] = Encoding.UTF8.GetString(pipeReader.ReadBytes(len));
                    }
                    OnLevelOpenRequest?.Invoke(paths);
                }
            }
            catch (Exception) {}
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        abort = true;

        try {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(50);
        } catch { }

        pipeThread?.Join(500);

        lockFileStream?.Dispose();
        if (File.Exists(LockFilePath)) try { File.Delete(LockFilePath); } catch { }
    }
}
