#version 300 es
precision mediump float;
#line 1 0
// the shader used for prop rendering in the editor.
// white pixels are transparent
// the R color component controls transparency and the G color component controls white blend

in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform sampler2D u_texture0;

out vec4 fragColor;

void main()
{
    bool inBounds = abs(v_texcoord0.x - 0.5f) <= 0.5f && abs(v_texcoord0.y - 0.5f) <= 0.5f;
    vec4 texelColor = texture(u_texture0, v_texcoord0);
    bool isTransparent = length(texelColor.rgb - vec3(1.0, 1.0, 1.0)) < 0.05 || texelColor.a == 0.0 || !inBounds;
    if (isTransparent) discard;

    vec3 color = mix(texelColor.rgb, vec3(1.0), v_color0.y);
    fragColor = vec4(color, v_color0.x) * u_color;
}
