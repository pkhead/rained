namespace Rained;

/// <summary>
/// A standard repository for shaders.
/// </summary>
static class Shaders
{
    /// <summary>
    /// The shader used for displaying the texture of an effect matrix.
    /// </summary>
    public static RlManaged.Shader EffectsMatrixShader { get; private set; } = null!;

    /// <summary>
    /// The shader used for prop rendering in the editor. <br /><br />
    /// White pixels are transparent, the red color component controls transparency, and the green color component controls white blend 
    /// </summary>
    public static RlManaged.Shader PropShader { get; private set; } = null!;

    /// <summary>
    /// The shader used for tile rendering in the editor. White pixels are transparent.
    /// </summary>
    public static RlManaged.Shader TileShader { get; private set; } = null!;

    /// <summary>
    /// The shader for rendering a tile or a prop with the standard color treatment, where it is given that pixel shade values are baked into the texture. 
    /// </summary>
    public static RlManaged.Shader PaletteShader { get; private set; } = null!;

    /// <summary>
    /// The shader for coloring a standard-type prop with the bevel color treatment.
    /// </summary>
    public static RlManaged.Shader BevelTreatmentShader { get; private set; } = null!;

    /// <summary>
    /// The shader for coloring a soft prop. It is not entirely accurate to how it is colored when rendering.
    /// </summary>
    public static RlManaged.Shader SoftPropShader { get; private set; } = null!;

    public static RlManaged.Shader LevelLightShader { get; private set; } = null!;

    /// <summary>
    /// The vertex+fragment shader used for rendering the line mesh grid.
    /// </summary>
    public static RlManaged.Shader GridShader { get; private set; } = null!;

    /// <summary>
    /// The shader for rendering a BGRA sublayer of the render preview.
    /// </summary>
    public static RlManaged.Shader RenderPreviewLayerShader { get; private set; } = null!;

    /// <summary>
    /// The shader for rendering a grayscale sublayer of the light render preview.
    /// </summary>
    public static RlManaged.Shader RenderPreviewLightShader { get; private set; } = null!;

    /// <summary>
    /// Bgfx does not have a repeat sampling mode, so I have to reimplement
    /// it in a shader.
    /// </summary>
    public static RlManaged.Shader UvRepeatShader { get; private set; } = null!;

    public static RlManaged.Shader OutlineMarqueeShader { get; private set; } = null!;

    public static void LoadShaders()
    {
        EffectsMatrixShader = RlManaged.Shader.Load(null, "effect_matrix.frag");
        BevelTreatmentShader = RlManaged.Shader.Load(null, "bevel.frag");
        SoftPropShader = RlManaged.Shader.Load(null, "softprop.frag");
        PropShader = RlManaged.Shader.Load(null, "prop_fade.frag");
        TileShader = RlManaged.Shader.Load(null, "tile.frag");
        PaletteShader = RlManaged.Shader.Load(null, "palette.frag");
        LevelLightShader = RlManaged.Shader.Load(null, "level_light.frag");
        GridShader = RlManaged.Shader.Load("grid.vert", "grid.frag");
        RenderPreviewLayerShader = RlManaged.Shader.Load(null, "render_preview.frag");
        RenderPreviewLightShader = RlManaged.Shader.Load(null, "bitmap_render_preview.frag");
        UvRepeatShader = RlManaged.Shader.Load(null, "uv_repeat.frag");
        OutlineMarqueeShader = RlManaged.Shader.Load(null, "outline_marquee.frag");
    }
}