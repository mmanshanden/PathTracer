#version 430

layout(rgba32f, binding=0) uniform image2D screen_buffer;

out vec4 color;

void main()
{
    const ivec2 screen_pos = ivec2(gl_FragCoord.xy);
	color = imageLoad(screen_buffer, screen_pos);
}
