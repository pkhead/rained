#version 300 es
precision mediump float;
#line 1 0
in vec2 v_texcoord0;
in vec4 v_color0;

uniform vec4 u_color;
uniform sampler2D u_texture0;
uniform float time;

out vec4 fragColor;

void main()
{
    vec4 col = texture(u_texture0, v_texcoord0) * u_color * v_color0;
    bool marquee = mod(gl_FragCoord.x + gl_FragCoord.y + time * 50.0, 10.0) < 5.0;
    fragColor = vec4(col.rgb, col.a * float(marquee));
}
