// shader used for tile rendering in the editor.
// white pixels
$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    bool inBounds = v_texcoord0.x >= 0.0 && v_texcoord0.x <= 1.0 && v_texcoord0.y >= 0.0 && v_texcoord0.y <= 1.0;
    vec4 texelColor = texture2D(glib_texture, v_texcoord0);

    bool isTransparent = (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
    bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
    bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
    bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
    bool isShaded = isLight || isShade || isNormal;

    float light = float(isLight) * 1.0 + float(isShade) * 0.4 + float(isNormal) * 0.8;
    vec3 shadedCol = v_color0.rgb * light;
    gl_FragColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransparent))) * v_color0 * glib_color;
}