// the shader used for prop rendering in the editor.
// white pixels are transparent
// the R color component controls transparency and the G color component controls white blend 
$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    bool inBounds = abs(v_texcoord0.x - 0.5f) <= 0.5f && abs(v_texcoord0.y - 0.5f) <= 0.5f;
    vec4 texelColor = texture2D(glib_texture, v_texcoord0);
    bool isTransparent = length(texelColor.rgb - vec3(1.0, 1.0, 1.0)) < 0.05 || texelColor.a == 0.0 || !inBounds;
    if (isTransparent) discard;

    vec3 color = mix(texelColor.rgb, vec3_splat(1.0), v_color0.y);
    gl_FragColor = vec4(color, (1.0 - float(isTransparent)) * v_color0.x) * glib_color;
}