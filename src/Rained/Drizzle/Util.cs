using Drizzle.Logic.Rendering;
using System.Diagnostics;

namespace Rained.Drizzle;

class ProgressNoSync<T> : IProgress<T>
{
    public event EventHandler<T>? ProgressChanged;

    public void Report(T e)
    {
        ProgressChanged?.Invoke(this, e);
    }
}

class TempDirectoryHandle(string? prefix = null) : IDisposable
{
    public string Path => Info.FullName;
    public readonly DirectoryInfo Info = Directory.CreateTempSubdirectory(prefix);

    private bool _disposed = false;

    ~TempDirectoryHandle()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        Info.Delete(true);
    }
}

static class DrizzleUtil
{
    public static float GetStatusProgress(RenderStatus status, int cameraCount)
    {
        var stageEnum = status.Stage.Stage;

        // from 0 to 1
        float stageProgress = 0f;

        switch (status.Stage)
        {
            case RenderStageStatusLayers layers:
                stageProgress = (3 - layers.CurrentLayer) / 3f;
                break;

            case RenderStageStatusLight light:
                stageProgress = light.CurrentLayer / 30f;
                break;

            case RenderStageStatusEffects effects:
                stageProgress = Math.Clamp((effects.CurrentEffect - 1f) / effects.EffectNames.Count, 0f, 1f);
                break;
        }

        float percentMin, percentMax; // out of 10.0f
        switch (stageEnum)
        {
            case RenderStage.Start:
                percentMin = 0.0f;
                percentMax = 0.25f;
                break;

            case RenderStage.CameraSetup:
                percentMin = 0.25f;
                percentMax = 0.50f;
                break;

            case RenderStage.RenderLayers:
                percentMin = 0.5f;
                percentMax = 2.0f;
                break;

            case RenderStage.RenderPropsPreEffects:
                percentMin = 2.0f;
                percentMax = 2.5f;
                break;

            case RenderStage.RenderEffects:
                percentMin = 2.5f;
                percentMax = 6.5f;
                break;

            case RenderStage.RenderPropsPostEffects:
                percentMin = 6.5f;
                percentMax = 7.0f;
                break;

            case RenderStage.RenderLight:
                percentMin = 7.0f;
                percentMax = 9.0f;
                break;

            case RenderStage.Finalize:
                percentMin = 9.0f;
                percentMax = 9.25f;
                break;

            case RenderStage.RenderColors:
                percentMin = 9.25f;
                percentMax = 9.50f;
                break;

            case RenderStage.Finished:
                percentMin = 9.50f;
                percentMax = 9.75f;
                break;

            case RenderStage.SaveFile:
                percentMin = 9.75f;
                percentMax = 10.0f;
                break;

            default:
                throw new UnreachableException("invalid RenderStage enum");
        }

        float camProgress = float.Lerp(percentMin, percentMax, stageProgress) / 10f;
        return (status.CountCamerasDone + camProgress) / cameraCount;
    }
}