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