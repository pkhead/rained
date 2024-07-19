#ifndef PALETTE_SH_HEADER_GUARD
#define PALETTE_SH_HEADER_GUARD
#include <bgfx_shader.sh>

SAMPLER2D(glib_texture, 0);
SAMPLER2D(paletteTex, 1);
uniform vec4 glib_color;

vec3 getLitColor(float index)
{
    return texture2D(paletteTex, vec2((index+0.5) / 30.0, 0.5 / 3.0)).rgb;
}

vec3 getNeutralColor(float index)
{
    return texture2D(paletteTex, vec2((index+0.5) / 30.0, (1.0+0.5) / 3.0)).rgb;
}

vec3 getShadeColor(float index)
{
    return texture2D(paletteTex, vec2((index+0.5) / 30.0, (2.0+0.5) / 3.0)).rgb;
}

bool isTransparent(vec2 coords)
{
    bool inBounds = coords.x >= 0.0 && coords.x <= 1.0 && coords.y >= 0.0 && coords.y <= 1.0;
    vec4 texelColor = texture2D(glib_texture, coords);
    return (texelColor.rgb == vec3(1.0, 1.0, 1.0) || texelColor.a == 0.0) || !inBounds;
}
#endif // PALETTE_SH_HEADER_GUARD