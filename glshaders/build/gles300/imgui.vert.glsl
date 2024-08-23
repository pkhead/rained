#version 300 es
precision mediump float;
#line 1 0
in vec2 a_pos;
in vec4 a_color0;
in vec2 a_texcoord0;

out vec2 v_texcoord0;
out vec4 v_color0;

uniform vec4 u_mvp;

void main()
{
    gl_Position = u_mvp * vec4(a_pos.xy, 0.0, 1.0);
    v_texcoord0 = a_texcoord0;
    v_color0 = a_color0;
}
