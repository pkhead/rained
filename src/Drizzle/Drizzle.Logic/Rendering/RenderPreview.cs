using Drizzle.Lingo.Runtime;

namespace Drizzle.Logic.Rendering;

/// <summary>
/// Represents the necessary state to render a preview for the editor window.
/// </summary>
public abstract record RenderPreview;

public sealed record RenderPreviewEffects(
    LingoImage[] Layers,
    LingoImage BlackOut1,
    LingoImage BlackOut2) :
    RenderPreview;

public sealed record RenderPreviewProps(LingoImage[] Layers) : RenderPreview;
public sealed record RenderPreviewLights(LingoImage[] Layers) : RenderPreview;
