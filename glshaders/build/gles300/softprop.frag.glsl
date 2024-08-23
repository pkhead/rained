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

uniform vec4 v4_textureSize;
uniform vec4 v4_propRotation;
uniform vec4 v4_lightDirection;
uniform vec4 v4_softPropShadeInfo;

#undef textureSize
#define textureSize v4_textureSize.xy
#define propRotation v4_propRotation
#define lightDirection v4_lightDirection.xyz
#define contourExponent v4_softPropShadeInfo.x
#define highlightThreshold v4_softPropShadeInfo.y
#define shadowThreshold v4_softPropShadeInfo.z
#define propDepth v4_softPropShadeInfo.w

out vec4 fragColor;

void main()
{
    if (isTransparent(v_texcoord0)) discard;
    float center = texture(u_texture0, v_texcoord0).g;

    // get x partial derivative
    float row[3];
    row[0] = texture(u_texture0, v_texcoord0 - vec2(1.0, 0.0) / textureSize).g;
    row[1] = center;
    row[2] = texture(u_texture0, v_texcoord0 + vec2(1.0, 0.0) / textureSize).g;
    float slopeX = (row[2] - row[0]) / (3.0 / textureSize.x);

    // get y partial derivative
    row[0] = texture(u_texture0, v_texcoord0 - vec2(0.0, 1.0) / textureSize).g;
    row[1] = center;
    row[2] = texture(u_texture0, v_texcoord0 + vec2(0.0, 1.0) / textureSize).g;
    float slopeY = (row[2] - row[0]) / (3.0 / textureSize.y);

    // calculate curve normal
    vec3 normal = cross( normalize(vec3(propRotation.xy, slopeX)), normalize(vec3(propRotation.zw, slopeY)) );
    normal = normalize(normal);

    // shadeValue is used to determine if this pixel is a shade, highlight, or neutral
    vec3 lightDir = normalize(lightDirection);
    float shadeValue =  max(0.0, dot(lightDir, normal));

    float depth = (pow(1.0 - center, contourExponent) * propDepth) / 29.0 + v_color0.r;

    bool isNormal = shadeValue > shadowThreshold && shadeValue < highlightThreshold;
    bool isLight = shadeValue >= highlightThreshold;
    bool isShade = shadeValue <= shadowThreshold;

    float colIndex = floor(clamp(depth, 0.0, 1.0) * 29.0);
    vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

    fragColor = vec4(shadedCol, v_color0.a) * u_color;
}
