$input a_position
#include <bgfx_shader.sh>

void main()
{
    vec4 pos = mul(u_modelViewProj, vec4(a_position.xyz, 1.0));
    pos.xy = (round(pos.xy * u_viewRect.zw) + vec2(0.5, 0.5)) / u_viewRect.zw;
    gl_Position = pos;
}