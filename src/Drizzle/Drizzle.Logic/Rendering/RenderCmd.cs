using System;

namespace Drizzle.Logic.Rendering;

public abstract record RenderCmd;

public record RenderCmdSetPaused(bool Paused) : RenderCmd;

public record RenderCmdSingleStep : RenderCmd;

public record RenderCmdCancel : RenderCmd;

public record RenderCmdExec(Action Action) : RenderCmd;

/// <summary>
/// The rendering thread may send a set of preview images for display in the UI.
/// </summary>
public record RenderCmdReqPreview : RenderCmd;

[Serializable]
public class RenderCancelledException : OperationCanceledException
{
    public RenderCancelledException()
    {
    }

    public RenderCancelledException(string message) : base(message)
    {
    }

    public RenderCancelledException(string message, Exception inner) : base(message, inner)
    {
    }
}

[Serializable]
public class RenderCameraException : Exception
{
    public RenderCameraException()
    {
    }

    public RenderCameraException(string message) : base(message)
    {
    }

    public RenderCameraException(string message, Exception inner) : base(message, inner)
    {
    }
}
