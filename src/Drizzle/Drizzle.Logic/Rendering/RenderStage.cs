namespace Drizzle.Logic.Rendering;

public enum RenderStage
{
    Start,
    CameraSetup,
    RenderLayers,
    RenderPropsPreEffects,
    RenderEffects,
    RenderPropsPostEffects,
    RenderLight,
    Finalize,
    RenderColors,
    Finished,
    SaveFile,
}