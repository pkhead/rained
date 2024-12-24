using Raylib_cs;

namespace Rained.Rendering;

record struct LevelRenderConfig()
{
    public bool DrawTiles = false;
    public bool DrawProps = false;
    public bool DrawObjects = false;

    public bool FillWater = false;
    public bool DrawPropsInFront = false;
    public bool Scissor = true;

    /// <summary>
    /// Strength of white-fade. Range: [0, 1]
    /// </summary>
    public float Fade = 0f;
    public int ActiveLayer = 0;
    public int LayerOffset = 2;
}