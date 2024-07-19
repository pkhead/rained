$input v_texcoord0, v_color0
#include <bgfx_shader.sh>
#include <palette.sh>

uniform vec2 u_textureSize;
uniform vec4 propRotation;
uniform vec3 lightDirection;
uniform float contourExponent;
uniform float highlightThreshold;
uniform float shadowThreshold;
uniform float propDepth;

void main()
{
    if (isTransparent(v_texcoord0)) discard;
    float center = texture2D(glib_texture, v_texcoord0).g;
    
    // get x partial derivative
    float row[3];
    row[0] = texture2D(glib_texture, v_texcoord0 - vec2(1.0, 0.0) / u_textureSize).g;
    row[1] = center;
    row[2] = texture2D(glib_texture, v_texcoord0 + vec2(1.0, 0.0) / u_textureSize).g;
    float slopeX = (row[2] - row[0]) / (3.0 / u_textureSize.x);

    // get y partial derivative
    row[0] = texture2D(glib_texture, v_texcoord0 - vec2(0.0, 1.0) / u_textureSize).g;
    row[1] = center;
    row[2] = texture2D(glib_texture, v_texcoord0 + vec2(0.0, 1.0) / u_textureSize).g;
    float slopeY = (row[2] - row[0]) / (3.0 / u_textureSize.y);

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

    gl_FragColor = vec4(shadedCol, v_color0.a) * glib_color;
}