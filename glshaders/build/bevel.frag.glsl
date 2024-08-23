#version 300 es
precision mediump float;

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
#line 5 0

in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform vec4 v4_textureSize;
uniform vec4 v4_propRotation;
uniform vec4 v4_lightDirection;
uniform vec4 v4_bevelData;

#define u_textureSize v4_textureSize.xy
#define propRotation v4_propRotation
#define lightDirection v4_lightDirection.xyz
#define bevelSize v4_bevelData.x

out vec4 fragColor;

void main()
{
    if (isTransparent(v_texcoord0)) discard;

    float bevelDst = bevelSize + 1.0;
    vec2 bevelDir = vec2(0.0, 0.0);

    vec4 finalColor = vec4(vec3(0.0, 0.0, 0.0), bevelSize / 4.0);

    float newDist;
    bool trans;
    bool replace;
    int dx, dy;
    int bevelSizeI = int(bevelSize);
    for (int i = 0; i < 4 * bevelSizeI * bevelSizeI; i++)
    {
        dy = i / (bevelSizeI * 2) - bevelSizeI;
        dx = i % (bevelSizeI * 2) - bevelSizeI;

        newDist = length(vec2(dx, dy));
        trans = isTransparent(v_texcoord0 + vec2(dx, dy) / u_textureSize);
        replace = trans && newDist < bevelDst;

        if (replace)
        {
            bevelDst = newDist;
            bevelDir = normalize(vec2(dx, dy));
        }
    }

    vec2 lightDir = normalize(lightDirection.xy);
    vec2 globalBevelDir = normalize(propRotation.xy * bevelDir.x + propRotation.zw * bevelDir.y);

    bool isLight = bevelDst <= bevelSize && dot(lightDir, globalBevelDir) > 0.5;
    bool isShade = bevelDst <= bevelSize && dot(lightDir, globalBevelDir) <= 0.0;
    bool isNormal = !isLight && !isShade;

    float colIndex = floor(v_color0.r * 29.0);
    vec3 shadedCol = float(isLight) * getLitColor(colIndex) + float(isShade) * getShadeColor(colIndex) + float(isNormal) * getNeutralColor(colIndex);

    finalColor = vec4(shadedCol, v_color0.a) * u_color;
    fragColor = finalColor;
}
