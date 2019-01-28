#version 430
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38
#define EPSILON 0.00001
#define PI      3.141592653589793238462643383279502
#define INVPI   0.318309886183790671537767526745028

layout(local_size_x = 64) in;

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
    float  focal_distance;
    Camera camera;
};

layout(rgba32f, binding=0) uniform image2D     screen_buffer;
layout(std430,  binding=0) buffer              buffer_ray_direction      { vec4  _d[]; };
layout(std430,  binding=1) buffer              buffer_ray_origin         { vec4  _o[]; };
layout(std430,  binding=2) buffer              buffer_sample_throughput  { vec4  _t[]; };
layout(std430,  binding=3) buffer              buffer_intersection       { uvec4 _h[]; };
layout(std430,  binding=4) buffer              buffer_atomics            { uint  _q; uint _p; };


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
    ray.origin     = _o[index].xyz;
    ray.direction  = _d[index].xyz;
    ray.reciprocal = 1.0 / ray.direction;
    ray.pixelidx   = uint(_o[index].w);

    return ray;
}

void storeRay(uint index, Ray ray)
{
    _o[index] = vec4(ray.origin, ray.pixelidx);
    _d[index] = vec4(ray.direction, 0);
}

Hit getHit(uint index)
{
    Hit hit;
    hit.distance = uintBitsToFloat(_h[index].x);
    hit.normal   = unpackNormal(_h[index].y);
    hit.material = int(_h[index].z) - 1;

    return hit;
}

void storeHit(uint index, Hit hit)
{
    uint d = floatBitsToUint(hit.distance);
    uint n = packNormal(hit.normal);
    uint m = uint(hit.material + 1);

    _h[index] = uvec4(d, n, m, 0);
}

#define MATERIAL_DIFFUSE 0
#define MATERIAL_MIRROR 1
#define MATERIAL_DIELECTRIC 2
#define MATERIAL_METAL 3

struct Material
{
    vec4 color;
    vec4 emissive;
    int type;
    float index;
    float roughness;
};

layout(std430,  binding=10) buffer material_buffer { Material materials[]; };

float schlick(const vec3 direction, const vec3 normal, const float n1, const float n2)
{
    float r0 = (n1 - n2) / (n1 + n2);
    r0 = r0 * r0;

    return r0 + (1.0 - r0) * pow(1 - dot(normal, -direction), 5);
}

vec3 schlick3(const vec3 ks, const vec3 l, const vec3 n)
{
    const float ldotn = clamp(dot(l, n), 0, 1);
    return ks + (1.0 - ks) * pow(1.0 - ldotn, 5);
}

float smithGgxMasking(vec3 v, vec3 n, float a2)
{
    const float ndotv = clamp(dot(n, v), 0, 1);

    const float c = sqrt(a2 + (1.0 - a2) * ndotv * ndotv) + ndotv;

    if (c == 0)
    {
        return 0;
    }

    return 2.0 * ndotv / c;
}

float smithGgxShadowMasking(vec3 l, vec3 v, vec3 n, float a2)
{
    const float ndotl = clamp(dot(n, l), 0, 1);
    const float ndotv = clamp(dot(n, v), 0, 1);

    const float a = ndotv * sqrt(a2 + (1.0 - a2) * ndotl * ndotl);
    const float b = ndotl * sqrt(a2 + (1.0 - a2) * ndotv * ndotv);

    const float denom = a + b;

    if (denom == 0)
    {
        return 0;
    }

    return 2.0 * ndotl * ndotv / denom;
}

float ggxNormalDistribution(vec3 n, vec3 m, float a2)
{
    float ndotm = dot(n, m);
    float a = ndotm * ndotm * (a2 - 1) + 1;
    float denom = a * a * PI;

    if (denom == 0)
    {
        return 0;
    }

    return a2 / denom;
}

vec3 lambertImportanceSample(const vec3 normal, inout uint seed)
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

vec4 lambertBrdf(const Material mat)
{
    return mat.color * INVPI;
}

float lambertPdf(const vec3 normal, const vec3 bounce)
{
    const float ndotr = clamp(dot(normal, bounce), 0, 1);
    return ndotr * INVPI;
}

vec3 ggxImportance(const vec3 view, const float alpha, inout uint seed)
{
    const float r1 = randomFloat(seed);
    const float r2 = randomFloat(seed);

    const vec3 wo = normalize(vec3(view.x * alpha, view.y * alpha, view.z));

    const vec3 t1 = (wo.z < 0.999) ? normalize(cross(wo, vec3(0, 0, 1))) : vec3(1, 0, 0);
    const vec3 t2 = cross(t1, wo);

    float a = 1.0f / (1.0f + wo.z);
    float r = sqrt(r1);
    float phi = (r2 < a) ? (r2 / a) * PI : PI + (r2 - a) / (1.0f - a) * PI;
    float p1 = r * cos(phi);
    float p2 = r * sin(phi) * ((r2 < a) ? 1.0f : wo.z);

    vec3 n = p1 * t1 + p2 * t2 + sqrt(max(0.0, 1.0 - p1 * p1 - p2 * p2)) * wo;

    return normalize(vec3(alpha * n.x, alpha * n.y, max(0.0, n.z)));
}

float ggxPdf(const vec3 view, const vec3 light, const vec3 m, const vec3 normal, const float a2)
{
    const float vdotm = dot(view, m);
    const float vdotn = dot(view, normal);

    const float denom = vdotn * 4;

    if (denom == 0)
    {
        return 0;
    }

    float g1 = smithGgxMasking(view, m, a2);
    float d = ggxNormalDistribution(normal, m, a2);

    return g1 * d / denom;
}

vec4 ggxBrdf(const Material mat, const vec3 view, const vec3 light, const vec3 m, const vec3 normal, float a2)
{
    vec4 f = vec4(schlick3(mat.color.xyz, m, light), 0);
    
    float g2 = smithGgxShadowMasking(light, view, m, a2);
    float d = ggxNormalDistribution(normal, m, a2);
    
    float ldotn = dot(light, normal);
    float vdotn = dot(view, normal);
    float denom = 4 * ldotn * vdotn;

    if (denom == 0)
    {
        return vec4(0);
    }

    return (f * g2 * d) / denom;
}

vec4 sampleGgx(const Material mat, const vec3 world_n, const vec3 world_v, out vec3 world_l, inout uint seed)
{
    vec3 W = abs(world_n.x) > 0.9 ? vec3(0, 1, 0) : vec3(1, 0, 0);

    vec3 N = world_n;
    vec3 T = normalize(cross(N, W));
    vec3 B = cross(N, T);

    // convert to tangent space
    vec3 v = vec3(dot(world_v, T), dot(world_v, B), dot(world_v, N));
    vec3 n = vec3(dot(world_n, T), dot(world_n, B), dot(world_n, N));

    vec3 m = ggxImportance(v, mat.roughness, seed);
    vec3 l = reflect(-v, m);

    if (dot(l, m) < 0)
    {
        return vec4(0);
    }

    // back to world space
    world_l = l.x * T + l.y * B + l.z * N;

    float a2 = mat.roughness * mat.roughness;
    
    vec4 f = vec4(schlick3(mat.color.xyz, m, l), 0);
    float g1 = smithGgxMasking(v, m, a2);
    float g2 = smithGgxShadowMasking(l, v, m, a2);

    if (g1 == 0)
    {
        return vec4(0);
    }

    return f * (g2 / g1);

    // above is derived (simplified) version of below

    vec4 brdf = ggxBrdf(mat, v, l, m, n, a2);
    float pdf = ggxPdf(v, l, m, n, a2);

    if (pdf == 0)
    {
        return vec4(0);
    }

    return dot(n, l) * brdf / pdf;
}

shared uint lockstep;
shared uint thread_index;
shared uint thread_count;

void main()
{
    const uint index = gl_GlobalInvocationID.x;
    uint seed = index * 57028723 * frames * 87029659 + 984524691;
    xorShift(seed); xorShift(seed); xorShift(seed); 

    Ray ray = getRay(index);
    Hit hit = getHit(index);
    
    if (hit.material == -1)
    {
        updateScreenBuffer(ray.pixelidx, _t[index] * sky_color);
        return;
    }

    const Material mat = materials[hit.material];

    if (mat.emissive.x > 0 || mat.emissive.y > 0 || mat.emissive.z > 0)
    {
        updateScreenBuffer(ray.pixelidx, _t[index] * mat.emissive);
        return;
    }

    vec3 bounce;
    vec4 throughput = _t[index];

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
        bounce           = lambertImportanceSample(hit.normal, seed);
        const vec4  brdf = lambertBrdf(mat);
        const float pdf  = lambertPdf(hit.normal, bounce);

        if (pdf == 0)
        {
            throughput = vec4(0);
            return;
        }

        throughput = throughput * brdf * dot(bounce, hit.normal) / pdf;
    }

    if (mat.type == MATERIAL_METAL)
    {
        throughput = throughput * sampleGgx(mat, hit.normal, -ray.direction, bounce, seed);
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
    
    thread_count = 0;
    lockstep = 1;
    
    uint q = atomicAdd(thread_count, 1);

    if (atomicAnd(lockstep, 0) == 1)
    {
        thread_index = atomicAdd(_q, thread_count);
    }

    storeRay(thread_index + q, ray);
    _t[thread_index + q] = throughput;
}
