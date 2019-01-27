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
layout(std430,  binding=3) buffer              buffer_intersection       { uvec4 __h[]; };

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

#define MATERIAL_DIFFUSE 0
#define MATERIAL_EMISSIVE 1
#define MATERIAL_MIRROR 2
#define MATERIAL_DIELECTRIC 3

struct Material
{
    vec4 color;
    vec4 emissive;
    int type;
    float index;
};

layout(std430,  binding=10) buffer material_buffer { Material materials[]; };

float schlick(const vec3 direction, const vec3 normal, const float n1, const float n2)
{
    float r0 = (n1 - n2) / (n1 + n2);
    r0 = r0 * r0;

    return r0 + (1.0 - r0) * pow(1 - dot(normal, -direction), 5);
}

vec3 diffuseReflection(const vec3 normal, inout uint seed)
{
    // "random" unit vector
    const vec3 axis = abs(normal.x) > 0.95 ? vec3(0, 1, 0) : vec3(1, 0, 0);

    const vec3 u = normalize(cross(normal, axis));
    const vec3 v = cross(u, normal);

    const float r0 = randomFloat(seed);
    const float r1 = randomFloat(seed);

    const float r = sqrt(r0);
    const float theta = 2.0 * PI * r1;

    return r * cos(theta) * u + r * sin(theta) * v + sqrt(1.0 - r0) * normal;
}

vec4 brdf(const Material mat)
{
    return mat.color * INVPI;
}

float pdf(const vec3 n, const vec3 r)
{
    const float ndotr = clamp(dot(n, r), 0, 1);
    return ndotr * INVPI;
}

void main()
{
    const uint index = gl_GlobalInvocationID.x;
    uint seed = index * 57028723 * frames * 87029659 + 984524691;
    xorShift(seed); xorShift(seed); xorShift(seed); 

    Ray ray = getRay(index);
    Hit hit = getHit(index);
    
    if (hit.material == -1)
    {
        updateScreenBuffer(ray.pixelidx, __t[index] * sky_color);
        return;
    }

    const Material mat = materials[hit.material];

    if (mat.emissive.x > 0 || mat.emissive.y > 0 || mat.emissive.z > 0)
    {
        updateScreenBuffer(ray.pixelidx, __t[index] * mat.emissive);
        return;
    }

    vec3 bounce;
    vec4 throughput = __t[index];

    if (mat.type == MATERIAL_DIELECTRIC)
    {
        float n1 = 1.0;
        float n2 = mat.index;

        if (dot(ray.direction, hit.normal) > 0)
        {
            n1 = mat.index;
            n2 = 1.0;

            hit.normal = hit.normal * -1;

            throughput.x = throughput.x * exp(-mat.color.x * hit.distance);
            throughput.y = throughput.y * exp(-mat.color.y * hit.distance);
            throughput.z = throughput.z * exp(-mat.color.z * hit.distance);
        }

        const float r = randomFloat(seed);
        const float s = schlick(ray.direction, hit.normal, n1, n2);

        if (r < s)
        {
            bounce     = reflect(ray.direction, hit.normal);
            throughput = throughput * mat.color;
        }
        else
        {
            bounce     = refract(ray.direction, hit.normal, n1 / n2); 
            throughput = throughput * mat.color;
        }
    }

    if (mat.type == MATERIAL_MIRROR)
    {
        bounce     = reflect(ray.direction, hit.normal);
        throughput = throughput * mat.color;
    }

    if (mat.type == MATERIAL_DIFFUSE)
    {
        bounce    = diffuseReflection(hit.normal, seed);
        vec4 brdf = brdf(mat);
        float pdf = pdf(hit.normal, bounce);

        if (pdf == 0)
        {
            return;
        }

        throughput = throughput * brdf * dot(bounce, hit.normal) / pdf;
    }

    float roulette = max(max(throughput.x, throughput.y), throughput.z);

    if (randomFloat(seed) > roulette)
    {
        updateScreenBuffer(ray.pixelidx, vec4(0));
        return;
    }

    throughput = throughput * (1.0 / roulette);

    ray.origin     = ray.origin + ray.direction * hit.distance + bounce * EPSILON;
    ray.direction  = bounce;
    ray.reciprocal = 1.0 / bounce;
    
    uint q = atomicCounterIncrement(atomic);

    storeRay(q, ray);
    __t[q] = throughput;
}
