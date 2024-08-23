#version 300 es
precision mediump float;

in vec3 a_position;

uniform mat4 u_mvp;

void main()
{
    vec4 pos = u_mvp * vec4(a_position, 1.0);
    //pos.xy = (round(pos.xy * u_viewRect.zw) + vec2(0.5, 0.5)) / u_viewRect.zw;
    gl_Position = pos;
}
