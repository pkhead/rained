//!imgui_varying.def.sc
$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    gl_FragColor = texture2D(glib_texture, v_texcoord0) * v_color0 * glib_color;
}