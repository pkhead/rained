#version 300 es
precision mediump float;
#line 1 0
#line 1 1
#ifndef PALETTE_INC
#define PALETTE_INC

uniform sampler2D u_texture0;
uniform sampler2D u_paletteTex;

vec3 getLitColor(float index)
{
    return texture(u_paletteTex, vec2((index+0.5) / 30.0, 0.5 / 3.0)).rgb;
}

vec3 getNeutralColor(float index)
{
    return texture(u_paletteTex, vec2((index+0.5) / 30.0, (1.0+0.5) / 3.0)).rgb;
}

vec3 getShadeColor(float index)
{
    return texture(u_paletteTex, vec2((index+0.5) / 30.0, (2.0+0.5) / 3.0)).rgb;
}

bool isTransparent(vec2 coords)
{
    bool inBounds = abs(coords.x - 0.5f) <= 0.5f && abs(coords.y - 0.5f) <= 0.5f;
    vec4 texelColor = texture(u_texture0, coords);
    return length(texelColor.rgb - vec3(1.0, 1.0, 1.0)) < 0.05 || texelColor.a == 0.0 || !inBounds;
}

#endif // PALETTE_INC
#line 2 0

uniform vec4 u_color;

in vec2 v_texcoord0;
in vec4 v_color0;

out vec4 fragColor;

void main()
{
    vec4 texelColor = texture(u_texture0, v_texcoord0);
    bool isTransp = isTransparent(v_texcoord0);
    bool isLight = length(texelColor.rgb - vec3(0.0, 0.0, 1.0)) < 0.3;
    bool isShade = length(texelColor.rgb - vec3(1.0, 0.0, 0.0)) < 0.3;
    bool isNormal = length(texelColor.rgb - vec3(0.0, 1.0, 0.0)) < 0.3;
    bool isShaded = isLight || isShade || isNormal;

    float colIndex = floor(v_color0.r * 29.0);
    vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

    fragColor = vec4(shadedCol * float(isShaded) + texelColor.rgb * float(!isShaded), (1.0 - float(isTransp)) * v_color0.a) * u_color;
}
