#version 300 es
precision mediump float;

uniform vec4 u_color;
uniform sampler2D u_texture0;

out vec4 fragColor;

void main()
{
    fragColor = texture(u_texture0, vec2(0.0, 0.0)) * u_color;
}