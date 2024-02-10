using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Drizzle.Lingo.Runtime;
using Drizzle.Ported;

namespace Drizzle.Logic.Rendering;

public sealed partial class LevelRenderer : ILingoRuntimeManager
{
    // This partial contains wrapping code around the rendering code to handle multi-threaded control and such.
    private readonly LingoRuntime _runtime;

    private MovieScript Movie => (MovieScript)_runtime.MovieScriptInstance;

    // NOTE: This is fired on the render thread, use fancy Rx ObserveOn if you use this!
    public event Action<RenderStatus>? StatusChanged;
    public event Action<RenderPreview>? PreviewSnapshot;

    // That's cam, final image.
    public event Action<int, LingoImage>? OnScreenRenderCompleted;
    private readonly Channel<RenderCmd> _cmdChannel;

    private readonly RenderStage? _pauseOnStage;
    private readonly int? _singleCamera;
    private RenderStage _stage;
    private bool _isPaused;
    private bool _previewRequested;

    public LevelRenderer(LingoRuntime runtime, RenderStage? pauseOnStage, int? singleCamera=null)
    {
        _pauseOnStage = pauseOnStage;
        _singleCamera = singleCamera;
        _runtime = runtime;

        _cmdChannel = Channel.CreateUnbounded<RenderCmd>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });
    }

    public void SetPaused(bool isPaused)
    {
        _cmdChannel.Writer.TryWrite(new RenderCmdSetPaused(isPaused));
    }

    public void RequestPreview()
    {
        _cmdChannel.Writer.TryWrite(new RenderCmdReqPreview());
    }

    private void RenderStartFrame(RenderStageStatus stageStatus)
    {
        if (_stage != stageStatus.Stage && stageStatus.Stage == _pauseOnStage)
            _isPaused = true;

        _stage = stageStatus.Stage;
        var reader = _cmdChannel.Reader;

        SendUpdateStatus(stageStatus);

        do
        {
            if (_isPaused)
                reader.WaitToReadAsync().AsTask().Wait();

            while (reader.TryRead(out var cmd))
            {
                switch (cmd)
                {
                    case RenderCmdSetPaused setPaused:
                        _isPaused = setPaused.Paused;
                        // Have to send this again to avoid problems with the pause checkbox.
                        // Look, I don't have my full network reconciliation setup here,
                        // I'm making do with what I got.
                        SendUpdateStatus(stageStatus);
                        break;
                    case RenderCmdSingleStep:
                        // Return so we exit out of this method and instantly get to the next frame.
                        return;
                    case RenderCmdCancel:
                        throw new RenderCancelledException();
                    case RenderCmdExec exec:
                        exec.Action();
                        break;
                    case RenderCmdReqPreview:
                        Console.WriteLine("PreviewRequest");
                        _previewRequested = true;
                        break;
                }
            }
        } while (_isPaused);
    }

    private void SendUpdateStatus(RenderStageStatus stageStatus)
    {
        StatusChanged?.Invoke(new RenderStatus(_cameraIndex, _countCamerasDone, _isPaused, stageStatus));
    }

    private void SendPreview(RenderPreview preview)
    {
        PreviewSnapshot?.Invoke(preview);

        _previewRequested = false;
    }

    private void RenderStartFrame(RenderStage stage)
    {
        RenderStartFrame(new RenderStageStatus(stage));
    }

    private bool ShouldSendPreview()
    {
        return _previewRequested;
    }

    public void CancelRender()
    {
        _cmdChannel.Writer.WriteAsync(new RenderCmdCancel()).AsTask().Wait();
    }

    public void SingleStep()
    {
        _cmdChannel.Writer.WriteAsync(new RenderCmdSingleStep()).AsTask().Wait();
    }

    public Task Exec(Action<LingoRuntime> action)
    {
        var tcs = new TaskCompletionSource();

        _cmdChannel.Writer.WriteAsync(new RenderCmdExec(() =>
        {
            try
            {
                action(_runtime);
                tcs.SetResult();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        })).AsTask().Wait();

        return tcs.Task;
    }

    public Task<T> Exec<T>(Func<LingoRuntime, T> func)
    {
        var tcs = new TaskCompletionSource<T>();

        _cmdChannel.Writer.WriteAsync(new RenderCmdExec(() =>
        {
            try
            {
                tcs.SetResult(func(_runtime));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        })).AsTask().Wait();

        return tcs.Task;
    }
}
