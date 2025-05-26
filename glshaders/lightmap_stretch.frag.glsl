#include "inv_bilinear.glsl"

in vec2 v_texcoord0;
in vec4 v_color0;

out vec4 fragColor;

uniform sampler2D u_texture0;
uniform vec4 u_color;

uniform vec4 u_vert_ab;
uniform vec4 u_vert_cd;

void main() {
    vec2 uv = inv_bilinear(v_texcoord0, u_vert_ab.xy, u_vert_ab.zw, u_vert_cd.xy, u_vert_cd.zw);

    vec4 col = vec4(1.0, 1.0, 1.0, 1.0);
    if (max( abs(uv.x - 0.5), abs(uv.y - 0.5) ) < 0.5) {
        col = texture(u_texture0, uv);
    }
    
    fragColor = col * v_color0 * u_color;
}