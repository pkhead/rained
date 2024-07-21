$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    vec4 texelColor = texture2D(glib_texture, v_texcoord0);
    bool isWhite = texelColor.r == 1.0;
    
    gl_FragColor = vec4(
        vec3(1.0, 0.0, 0.0),
        1.0 - float(isWhite)
    ) * glib_color;
}