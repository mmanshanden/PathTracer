#version 430
layout (local_size_x = 8, local_size_y = 8) in;

layout(rgba32f, binding=0) uniform image2D screen_buffer;

void main() 
{
    const ivec2 screen_pos = ivec2(gl_GlobalInvocationID.xy);
    const vec4 color = vec4(screen_pos.x / 512.0, screen_pos.y / 512.0, 0, 1);

    imageStore(screen_buffer, screen_pos, color);
}
