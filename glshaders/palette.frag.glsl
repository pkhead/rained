#include "palette.glsl"

uniform vec4 u_color;

in vec2 v_texcoord0;
in vec4 v_color0;

out vec4 fragColor;

void main()
{
    if (isTransparent(v_texcoord0)) discard;
    
    vec4 texelColor = texture(u_texture0, v_texcoord0);
    bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
    bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
    bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
    bool isShaded = isLight || isShade || isNormal;

    float colIndex = floor(v_color0.r * 29.0);
    vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

    fragColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), v_color0.a) * u_color;
}