namespace RainEd;

/// <summary>
/// A standard repository for shaders.
/// </summary>
static class Shaders
{
    /// <summary>
    /// The shader used for displaying the texture of an effect matrix.
    /// </summary>
    public static RlManaged.Shader EffectsMatrixShader = null!;

    /// <summary>
    /// The shader used for prop rendering in the editor. <br /><br />
    /// White pixels are transparent, the red color component controls transparency, and the green color component controls white blend 
    /// </summary>
    public static RlManaged.Shader PropShader = null!;

    /// <summary>
    /// The shader used for tile rendering in the editor. White pixels are transparent.
    /// </summary>
    public static RlManaged.Shader TileShader = null!;
    public static RlManaged.Shader PaletteShader = null!;
    public static RlManaged.Shader LevelLightShader = null!;

    /// <summary>
    /// The vertex+fragment shader used for rendering the line mesh grid.
    /// </summary>
    public static RlManaged.Shader GridShader = null!;

    public static void LoadShaders()
    {
        EffectsMatrixShader = RlManaged.Shader.LoadFromMemory(null, EffectsMatrixShaderSource);
        PropShader = RlManaged.Shader.LoadFromMemory(null, PropShaderSrc);
        TileShader = RlManaged.Shader.LoadFromMemory(null, TileShaderSrc);
        PaletteShader = RlManaged.Shader.LoadFromMemory(null, PaletteShaderSrc);
        LevelLightShader = RlManaged.Shader.LoadFromMemory(null, levelLightShaderSrc);
        GridShader = RlManaged.Shader.LoadFromMemory(GridVertexShaderSource, GridFragmentShaderSource);
    }

    private readonly static string EffectsMatrixShaderSource = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            finalColor = mix(vec4(1.0, 0.0, 1.0, 1.0), vec4(0.0, 1.0, 0.0, 1.0), texelColor.r) * glib_color * glib_uColor;
        }
    ";

    // the shader used for prop rendering in the editor.
    // white pixels are transparent
    // the R color component controls transparency and the G color component controls white blend 
    private readonly static string PropShaderSrc = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            vec3 color = mix(texelColor.rgb, vec3(1.0), glib_color.y);

            finalColor = vec4(color, (1.0 - float(isTransparent)) * glib_color.x) * glib_uColor;
        }
    ";

    // the shader used for tile rendering in the editor.
    // while pixels are transparent.
    private readonly static string TileShaderSrc = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);

            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
            bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
            bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
            bool isShaded = isLight || isShade || isNormal;

            float light = float(isLight) * 1.0 + float(isShade) * 0.4 + float(isNormal) * 0.8;
            vec3 shadedCol = glib_color.rgb * light;

            finalColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent)) * glib_color.a) * glib_uColor;
        }
    ";

    private readonly static string PaletteShaderSrc = @"
        #version 330

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        uniform vec3[30] litColor;
        uniform vec3[30] neutralColor;
        uniform vec3[30] shadedColor; 

        out vec4 finalColor;

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);

            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
            bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
            bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
            bool isShaded = isLight || isShade || isNormal;

            int colIndex = int(glib_color.r * 29.0);
            vec3 shadedCol = float(isLight) * litColor[colIndex] + float(isShade) * shadedColor[colIndex] + float(isNormal) * neutralColor[colIndex];

            finalColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent)) * glib_color.a) * glib_uColor;
        }
    ";

    private const string GridVertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        uniform mat4 glib_uMatrix;

        void main()
        {
            gl_Position = glib_uMatrix * vec4(aPos.xyz, 1.0);
        }
    ";

    private const string GridFragmentShaderSource = @"
        #version 330 core

        out vec4 fragColor;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        void main()
        {
            fragColor = texture(glib_uTexture, vec2(0.0, 0.0)) * glib_uColor;
        }
    ";

    private readonly static string levelLightShaderSrc = @"
        #version 330 core
        
        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        void main()
        {
            vec4 texelColor = texture(glib_uTexture, glib_texCoord);
            finalColor = vec4(1.0, 1.0, 1.0, 1.0 - texelColor.r) * glib_color * glib_uColor;
        }
    ";
}