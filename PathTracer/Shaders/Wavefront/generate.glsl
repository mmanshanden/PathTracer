#version 430
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38
#define EPSILON 0.00001
#define PI      3.141592653589793238462643383279502
#define INVPI   0.318309886183790671537767526745028

layout(local_size_x = 32) in;

void xorShift(inout uint seed)
{
    seed ^= seed << 13;
    seed ^= seed >> 17;
    seed ^= seed << 05;
}

float randomFloat(inout uint seed)
{
    xorShift(seed);
    return float(seed) * (1.0 / 4294967295.0);
}

uint packNormal(vec3 n)
{
    float f = 65535.0 / sqrt(8.0 * n.z + 8.0);
    return uint(n.x * f + 32767.0) + (uint(n.y * f + 32767.0) << 16);
}

vec3 unpackNormal(uint p)
{
    vec4 nn = vec4(float(p & 65535) * (2.0 / 65535.0), float(p >> 16) * (2.0 / 65535.0), 0, 0);
    nn += vec4(-1, -1, 1, -1);
    float l = dot(vec3( nn.x, nn.y, nn.z), vec3(-nn.x, -nn.y, -nn.w));
    nn.z = l; l = sqrt(l); nn.x *= l; nn.y *= l; 
    return vec3(nn) * 2.0f + vec3(0, 0, -1); 
}

struct Ray
{
    vec3 origin;
    vec3 direction;
    vec3 reciprocal;
    uint pixelidx;
};

struct Hit
{
    float distance;
    vec3 normal;
    int material;
};

struct Camera
{
    vec4 position;
    vec4 forward;
    vec4 right;
    vec4 up;
};

struct Screen
{
    int width;
    int height;
};

layout(std140, binding=0) uniform render_state
{
    Screen screen;
    vec4   sky_color;
};

layout(std140, binding=1) uniform frame_state
{
    uint   frames;
    uint   samples;
    Camera camera;
};

layout(rgba32f, binding=0) uniform image2D     screen_buffer;
layout(         binding=0) uniform atomic_uint atomic;
layout(std430,  binding=0) buffer              buffer_ray_direction      { vec4  __d[]; };
layout(std430,  binding=1) buffer              buffer_ray_origin         { vec4  __o[]; };
layout(std430,  binding=2) buffer              buffer_sample_throughput  { vec4  __t[]; };
layout(std430,  binding=3) buffer              buffer_sample_emittance   { vec4  __e[]; };
layout(std430,  binding=4) buffer              buffer_intersection       { uvec4 __h[]; };
layout(std430,  binding=5) buffer              buffer_queue              { int   __q[]; };

void updateScreenBuffer(const uint pixelidx, const vec4 color)
{
    const ivec2 screen_pos = ivec2(pixelidx % screen.width, pixelidx / screen.width);

    const vec4 prev = imageLoad(screen_buffer, screen_pos);
    const vec4 new = (prev * samples + color) / (samples + 1);
    imageStore(screen_buffer, screen_pos, new);
}

Ray getRay(uint index)
{
    Ray ray;
    ray.origin     = __o[index].xyz;
    ray.direction  = __d[index].xyz;
    ray.reciprocal = 1.0 / ray.direction;
    ray.pixelidx   = uint(__o[index].w);

    return ray;
}

void storeRay(uint index, Ray ray)
{
    __o[index] = vec4(ray.origin, ray.pixelidx);
    __d[index] = vec4(ray.direction, 0);
}

Hit getHit(uint index)
{
    Hit hit;
    hit.distance = uintBitsToFloat(__h[index].x);
    hit.normal   = unpackNormal(__h[index].y);
    hit.material = int(__h[index].z) - 1;

    return hit;
}

void storeHit(uint index, Hit hit)
{
    uint d = floatBitsToUint(hit.distance);
    uint n = packNormal(hit.normal);
    uint m = uint(hit.material + 1);

    __h[index] = uvec4(d, n, m, 0);
}

Ray generateRay(const uint pixelidx, inout uint seed)
{
    const ivec2 screen_pos = ivec2(pixelidx % screen.width, pixelidx / screen.width);

    const float x = (randomFloat(seed) + screen_pos.x) / float(screen.width) - 0.5;
    const float y = (randomFloat(seed) + screen_pos.y) / float(screen.height) - 0.5;

    const float ar = float(screen.width) / screen.height;

    const vec3 c = camera.forward.xyz * 1.0;
    const vec3 d = normalize(c + camera.right.xyz * x * ar + camera.up.xyz * y);

    Ray r;
    r.direction  = d;
    r.origin     = camera.position.xyz;
    r.reciprocal = 1.0 / d;
    r.pixelidx   = pixelidx;
    return r;
}

void main()
{
    const uint index = gl_GlobalInvocationID.x;

    uint seed = index * 57028723 * frames * 87029659 + 2983742873;
    xorShift(seed); xorShift(seed); xorShift(seed); 

    Ray ray = generateRay(index, seed);  

    storeRay(index, ray);
    __t[index] = vec4(1);
}
