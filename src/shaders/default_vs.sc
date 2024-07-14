$input a_position, a_texcoord0, a_color0
$output v_texcoord0, v_color0

void main()
{
    gl_Position = mult(u_viewProj, vec4(a_position.xyz, 1.0));
    v_texcoord0 = a_texcoord0;
    v_color0 = a_color0;
}