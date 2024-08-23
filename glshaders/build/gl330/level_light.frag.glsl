#version 330 core
#line 1 0
in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform sampler2D u_texture0;

out vec4 fragColor;

void main()
{
    vec4 texelColor = texture(u_texture0, v_texcoord0);
    fragColor = vec4(1.0, 1.0, 1.0, 1.0 - texelColor.r) * v_color0 * u_color;
}
