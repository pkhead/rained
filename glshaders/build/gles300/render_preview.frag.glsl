#version 300 es
precision mediump float;
#line 1 0
in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform sampler2D u_texture0;

out vec4 fragColor;

void main()
{
    vec4 texelColor = texture(u_texture0, v_texcoord0);
    bool isWhite = texelColor.r == 1.0 && texelColor.g == 1.0 && texelColor.b == 1.0;
    vec3 correctColor = texelColor.bgr;

    fragColor = vec4(
        mix(correctColor, vec3(1.0), v_color0.r * 0.8),
        1.0 - float(isWhite)
    ) * u_color;
}
