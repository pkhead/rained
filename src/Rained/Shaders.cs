namespace RainEd;

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

    public static void LoadShaders()
    {
        EffectsMatrixShader = RlManaged.Shader.Load(null, "effect_matrix_fs");
        BevelTreatmentShader = RlManaged.Shader.Load(null, "bevel_fs");
        SoftPropShader = RlManaged.Shader.Load(null, "softprop_fs");
        PropShader = RlManaged.Shader.Load(null, "prop_fade_fs");
        TileShader = RlManaged.Shader.Load(null, "tile_fs");
        PaletteShader = RlManaged.Shader.Load(null, "palette_fs");
        LevelLightShader = RlManaged.Shader.Load(null, "level_light_fs");
        GridShader = RlManaged.Shader.Load("grid_vs", "grid_fs");
        RenderPreviewLayerShader = RlManaged.Shader.Load(null, "render_preview_fs");
        RenderPreviewLightShader = RlManaged.Shader.Load(null, "bitmap_render_preview_fs");
    }

    private const string EffectsMatrixShaderSource = @"
        #version 330 core

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
    private const string PropShaderSrc = @"
        #version 330 core

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
    private const string TileShaderSrc = @"
        #version 330 core

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

    private const string PaletteShaderSrc = @"
        #version 330 core

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform sampler2D paletteTex;
        uniform vec4 glib_uColor;

        out vec4 finalColor;

        vec3 getLitColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, 0.5 / 3.0)).rgb;
        }

        vec3 getNeutralColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (1.0+0.5) / 3.0)).rgb;
        }

        vec3 getShadeColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (2.0+0.5) / 3.0)).rgb;
        }

        void main()
        {
            bool inBounds = glib_texCoord.x >= 0.0 && glib_texCoord.x <= 1.0 && glib_texCoord.y >= 0.0 && glib_texCoord.y <= 1.0;

            vec4 texelColor = texture(glib_uTexture, glib_texCoord);

            bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
            bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
            bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
            bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
            bool isShaded = isLight || isShade || isNormal;

            float colIndex = floor(glib_color.r * 29.0);
            vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

            finalColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent)) * glib_color.a) * glib_uColor;
        }
    ";

    private const string BevelTreatmentShaderSrc =
        """
        #version 330 core

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;
        uniform sampler2D paletteTex;

        uniform int bevelSize;
        uniform vec2 textureSize;
        uniform mat2 propRotation;
        uniform vec3 lightDirection;

        out vec4 finalColor;

        vec3 getLitColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, 0.5 / 3.0)).rgb;
        }

        vec3 getNeutralColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (1.0+0.5) / 3.0)).rgb;
        }

        vec3 getShadeColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (2.0+0.5) / 3.0)).rgb;
        }

        bool isTransparent(vec2 coords)
        {
            bool inBounds = coords.x >= 0.0 && coords.x <= 1.0 && coords.y >= 0.0 && coords.y <= 1.0;
            vec4 texelColor = texture(glib_uTexture, coords);
            return (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
        }

        void main()
        {
            if (isTransparent(glib_texCoord)) discard;

            float bevelDst = bevelSize + 1.0;
            vec2 bevelDir = vec2(0.0, 0.0);

            finalColor = vec4(vec3(0.0, 0.0, 0.0), float(bevelSize) / 4.0);

            float newDist;
            bool trans;
            bool replace;
            int dx, dy;
            for (int i = 0; i < 4 * bevelSize * bevelSize; i++)
            {
                dy = i / (bevelSize * 2) - bevelSize;
                dx = i % (bevelSize * 2) - bevelSize;

                newDist = length(vec2(dx, dy));
                trans = isTransparent(glib_texCoord + vec2(dx, dy) / textureSize);
                replace = trans && newDist < bevelDst;

                if (replace)
                {
                    bevelDst = newDist;
                    bevelDir = normalize(vec2(dx, dy));
                }
            }
            
            vec2 lightDir = normalize(lightDirection.xy);
            vec2 globalBevelDir = normalize(propRotation[0] * bevelDir.x + propRotation[1] * bevelDir.y);

            bool isLight = bevelDst <= bevelSize && dot(lightDir, globalBevelDir) > 0.5;
            bool isShade = bevelDst <= bevelSize && dot(lightDir, globalBevelDir) <= 0;
            bool isNormal = !isLight && !isShade;

            float colIndex = floor(glib_color.r * 29.0);
            vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

            finalColor = vec4(shadedCol, glib_color.a) * glib_uColor;
        }
        """;
    
    private const string SoftPropShaderSrc =
        """
        #version 330 core

        in vec2 glib_texCoord;
        in vec4 glib_color;

        uniform sampler2D glib_uTexture;
        uniform vec4 glib_uColor;
        uniform sampler2D paletteTex;

        uniform vec2 textureSize;
        uniform mat2 propRotation;
        uniform vec3 lightDirection;
        uniform float contourExponent;
        uniform float highlightThreshold;
        uniform float shadowThreshold;
        uniform int propDepth;

        out vec4 finalColor;

        vec3 getLitColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, 0.5 / 3.0)).rgb;
        }

        vec3 getNeutralColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (1.0+0.5) / 3.0)).rgb;
        }

        vec3 getShadeColor(float index)
        {
            return texture(paletteTex, vec2((index+0.5) / 30.0, (2.0+0.5) / 3.0)).rgb;
        }

        bool isTransparent(vec2 coords)
        {
            bool inBounds = coords.x >= 0.0 && coords.x <= 1.0 && coords.y >= 0.0 && coords.y <= 1.0;
            vec4 texelColor = texture(glib_uTexture, coords);
            return (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
        }

        void main()
        {
            if (isTransparent(glib_texCoord)) discard;
            float center = texture(glib_uTexture, glib_texCoord).g;
            
            // get x partial derivative
            float row[3];
            row[0] = texture(glib_uTexture, glib_texCoord - vec2(1.0, 0.0) / textureSize).g;
            row[1] = center;
            row[2] = texture(glib_uTexture, glib_texCoord + vec2(1.0, 0.0) / textureSize).g;
            float slopeX = (row[2] - row[0]) / (3.0 / textureSize.x);

            // get y partial derivative
            row[0] = texture(glib_uTexture, glib_texCoord - vec2(0.0, 1.0) / textureSize).g;
            row[1] = center;
            row[2] = texture(glib_uTexture, glib_texCoord + vec2(0.0, 1.0) / textureSize).g;
            float slopeY = (row[2] - row[0]) / (3.0 / textureSize.y);

            // calculate curve normal
            vec3 normal = cross( normalize(vec3(propRotation[0], slopeX)), normalize(vec3(propRotation[1], slopeY)) );
            normal = normalize(normal);
            
            // shadeValue is used to determine if this pixel is a shade, highlight, or neutral
            vec3 lightDir = normalize(lightDirection);
            float shadeValue =  max(0.0, dot(lightDir, normal));

            float depth = (pow(1.0 - center, contourExponent) * propDepth) / 29.0 + glib_color.r;

            bool isNormal = shadeValue > shadowThreshold && shadeValue < highlightThreshold;
            bool isLight = shadeValue >= highlightThreshold;
            bool isShade = shadeValue <= shadowThreshold;

            float colIndex = floor(clamp(depth, 0.0, 1.0) * 29.0);
            vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

            finalColor = vec4(shadedCol, glib_color.a) * glib_uColor;
        }
        """;

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

    private const string LevelLightShaderSrc = @"
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