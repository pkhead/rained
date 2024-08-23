#version 300 es
precision mediump float;
#line 1 0
in vec2 v_texcoord0;
in vec4 v_color0;

out vec4 fragColor;

uniform sampler2D u_texture0;
uniform vec4 u_color;

void main() {
    vec4 texel = texture(u_texture0, v_texcoord0);
    fragColor = vec4(vec3(1.0) - texel.rgb, texel.a) * v_color0 * u_color;
}
