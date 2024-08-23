in vec2 v_texcoord0;
in vec4 v_color0;

uniform sampler2D u_texture0;
uniform vec4 u_color;

out vec4 fragColor;

void main()
{
    fragColor = texture(u_texture0, v_texcoord0) * v_color0 * u_color;
}