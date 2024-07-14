$input v_texcoord0, v_color0

uniform vec4 u_color;
SAMPLER2D(u_texture, 0);

void main()
{
    gl_FragColor = texture2D(u_texture, v_texcoord0) * v_color0 * u_color;
}