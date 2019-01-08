#version 450
layout(local_size_x = 8, local_size_y = 8) in;

layout(rgba32f, binding=0) uniform image2D screen_buffer;

uniform uint frame;

uint global_id(ivec2 screen_pos) 
{
    const uint width = uint(gl_NumWorkGroups.x * gl_WorkGroupSize.x);
    return width * screen_pos.y + screen_pos.x;
}

void xor_shift(inout uint seed)
{
    seed ^= seed << 13;
    seed ^= seed >> 17;
    seed ^= seed << 05;
}

float random_float(inout uint seed)
{
    xor_shift(seed);
    return seed * (1.0 / 4294967295.0);
}

void update_screen_buffer(ivec2 pos, vec4 color)
{
    vec4 prev = imageLoad(screen_buffer, pos);
    vec4 new = (prev * frame + color) / (frame + 1);
    imageStore(screen_buffer, pos, new);
}

void main() 
{
    const ivec2 screen_pos = ivec2(gl_GlobalInvocationID.xy);
    
    uint seed = (screen_pos.x * 100999001 + screen_pos.y * 152252251 + frame * 377000773);
    float r = random_float(seed);
    float g = random_float(seed);
    float b = random_float(seed);
    vec4 color = vec4(r, g, b, 1.0);

    update_screen_buffer(screen_pos, color);
}
