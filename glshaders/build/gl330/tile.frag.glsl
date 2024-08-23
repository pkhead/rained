#version 330 core
#line 1 0
// shader used for tile rendering in the editor.
// white pixels

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

    fragColor = v_color0 * u_color;

    bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
    bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
    bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
    bool isShaded = isLight || isShade || isNormal;

    float light = float(isLight) * 1.0 + float(isShade) * 0.4 + float(isNormal) * 0.8;
    vec3 shadedCol = v_color0.rgb * light;
    //vec3 finalColor = shadedCol * float(isShaded) + texelColor.rgb * (1.0 - float(isShaded));
    vec3 finalColor = isShaded ? shadedCol : texelColor.rgb;
    fragColor = vec4(finalColor, 1.0) * v_color0 * u_color;
}
