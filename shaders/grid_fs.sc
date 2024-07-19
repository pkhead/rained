#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    gl_FragColor = texture2D(glib_texture, vec2(0.0, 0.0)) * glib_color;
}