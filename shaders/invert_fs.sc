$input v_texcoord0, v_color0
#include <bgfx_shader.sh>

uniform vec4 glib_color;
SAMPLER2D(glib_texture, 0);

void main()
{
    vec4 col = v_color0 * glib_color * texture2D(glib_texture, v_texcoord0);
    gl_FragColor = vec4(vec3(1.0 - col.rgb), col.a);
}