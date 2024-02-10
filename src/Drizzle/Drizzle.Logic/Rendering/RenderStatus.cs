using System.Collections.Generic;

namespace Drizzle.Logic.Rendering;

public record RenderStatus(int CameraIndex, int CountCamerasDone, bool IsPaused, RenderStageStatus Stage);

public record RenderStageStatus(RenderStage Stage);

public record RenderStageStatusLayers(int CurrentLayer) : RenderStageStatus(RenderStage.RenderLayers);

// Pass in stage here due to pre/post effects distinction.
public record RenderStageStatusProps(RenderStage Stage) : RenderStageStatus(Stage);

public record RenderStageStatusEffects(
        int TotalEffectsCount,
        int CurrentEffect,
        int VertRepeater,
        IReadOnlyList<string> EffectNames)
    : RenderStageStatus(RenderStage.RenderEffects);

public record RenderStageStatusLight(int CurrentLayer) : RenderStageStatus(RenderStage.RenderLight);

public record RenderStageStatusFinalize() : RenderStageStatus(RenderStage.Finalize);

public record RenderStageStatusRenderColors() : RenderStageStatus(RenderStage.SaveFile);