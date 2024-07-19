$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    vec4 texelColor = texture2D(glib_texture, v_texcoord0);
    gl_FragColor = mix(vec4(1.0, 0.0, 1.0, 1.0), vec4(0.0, 1.0, 0.0, 1.0), texelColor.r) * v_color0 * glib_color;
}