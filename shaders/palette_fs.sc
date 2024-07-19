$input v_texcoord0, v_color0
#include <bgfx_shader.sh>
#include <palette.sh>

void main()
{
    vec4 texelColor = texture2D(glib_texture, v_texcoord0);
    bool isTransp = isTransparent(v_texcoord0);
    bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
    bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
    bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
    bool isShaded = isLight || isShade || isNormal;

    float colIndex = floor(v_color0.r * 29.0);
    vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

    gl_FragColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransp)) * v_color0.a) * glib_color;
}