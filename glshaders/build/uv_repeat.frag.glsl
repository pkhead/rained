#version 300 es
precision mediump float;

in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform sampler2D u_texture0;

out vec4 fragColor;

void main()
{
    vec2 uv = v_texcoord0 - floor(v_texcoord0);
    fragColor = texture(u_texture0, uv) * v_color0 * u_color;
}
