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
    Camera camera;
};

layout(rgba32f, binding=0) uniform image2D     screen_buffer;
layout(std430,  binding=0) buffer              buffer_ray_direction      { vec4  __d[]; };
layout(std430,  binding=1) buffer              buffer_ray_origin         { vec4  __o[]; };
layout(std430,  binding=2) buffer              buffer_sample_throughput  { vec4  __t[]; };
layout(std430,  binding=3) buffer              buffer_intersection       { uvec4 __h[]; };
layout(std430,  binding=4) buffer              buffer_atomics            { uint  __q; uint __p; };

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

struct Material
{
    vec4 color;
    vec4 emissive;
    int type;
    float index;
};

struct Vertex
{
    vec4 position;
    vec4 normal;
};

struct Triangle
{
    Vertex v1;
    Vertex v2;
    Vertex v3;
    int material;
};

struct Node
{
    float bounds_min_x;
    float bounds_min_y;
    float bounds_min_z;
    float bounds_max_x;
    float bounds_max_y;
    float bounds_max_z;
    int leftFirst;
    int count;
};

layout(std430,  binding=10) buffer material_buffer { Material materials[]; };
layout(std430,  binding=11) buffer triangle_buffer { Triangle triangles[]; };
layout(std430,  binding=12) buffer node_buffer     { Node nodes[]; };

void rayTriangleIntersection(Ray ray, const int triangle_index, inout Hit hit)
{
    const Triangle tri = triangles[triangle_index];

    const vec3 edge1 = tri.v1.position.xyz - tri.v3.position.xyz;
    const vec3 edge2 = tri.v2.position.xyz - tri.v3.position.xyz;
    
    const vec3 h = cross(ray.direction, edge2);
    const float a = dot(edge1, h);
    
    const float f = 1.0 / a;
    const vec3 s = ray.origin - tri.v3.position.xyz;
    const float u = f * dot(s, h);
    
    if (u < 0.0 || u > 1.0)
    {
        return;
    }
    
    const vec3 q = cross(s, edge1);
    const float v = f * dot(ray.direction, q);
    
    if (v < 0.0 || u + v > 1.0)
    {
        return;
    }
    
    const float t = f * dot(edge2, q);
    const float w = 1.0 - u - v;

    if (t > 0.0 && t < hit.distance)
    {
        hit.distance = t;
        hit.material = tri.material;
        hit.normal   = tri.v1.normal.xyz * u 
                     + tri.v2.normal.xyz * v 
                     + tri.v3.normal.xyz * w;

        hit.normal   = normalize(hit.normal);
    }
}

bool rayNodeBoundsTest(Ray ray, const int node_index, out float tmin, out float tmax)
{
    const Node n = nodes[node_index];

    const float t1 = (n.bounds_min_x - ray.origin.x) * ray.reciprocal.x;
    const float t2 = (n.bounds_max_x - ray.origin.x) * ray.reciprocal.x;
    const float t3 = (n.bounds_min_y - ray.origin.y) * ray.reciprocal.y;
    const float t4 = (n.bounds_max_y - ray.origin.y) * ray.reciprocal.y;
    const float t5 = (n.bounds_min_z - ray.origin.z) * ray.reciprocal.z;
    const float t6 = (n.bounds_max_z - ray.origin.z) * ray.reciprocal.z;

    tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    return (tmax > 0 && tmin < tmax);
}

Hit intersectScene(Ray ray)
{
    float tmin, tmax;

    Hit hit;
    hit.distance = FLT_MAX;
    hit.material = -1;

    int candidates[128]; // size is scene dependent
    int c = 0;

    int stack[32];
    int p = 0;

    stack[0] = 0;

    while (p >= 0)
    {
        const int  idx  = stack[p];
        const Node node = nodes[idx];
        
        const Node left  = nodes[node.leftFirst];
        const Node right = nodes[node.leftFirst + 1];

        const bool lhit = rayNodeBoundsTest(ray, node.leftFirst, tmin, tmax);
        const bool rhit = rayNodeBoundsTest(ray, node.leftFirst + 1, tmin, tmax);

        const bool ltraverse = lhit && left.count < 0;
        const bool rtraverse = rhit && right.count < 0;

        if (lhit && !ltraverse)
        {
            candidates[c] = node.leftFirst;
            c = min(c + 1, 127);
        }

        if (rhit && !rtraverse)
        {
            candidates[c] = node.leftFirst + 1;
            c = min(c + 1, 127);
        }

        if (!ltraverse && !rtraverse)
        {
            p--;
        }
        else
        {
            stack[p] = ltraverse ? node.leftFirst : node.leftFirst + 1;

            if (ltraverse && rtraverse)
            {
                stack[++p] = node.leftFirst + 1;
            }
        }
    }

    for (int i = 0; i < c; i++)
    {
        const int idx   = candidates[i];
        const Node node = nodes[idx];

        for (int j = node.leftFirst; j < node.leftFirst + node.count; j++)
        {
            rayTriangleIntersection(ray, j, hit);
        }       
    }

    return hit;
}

void main()
{
    const uint index = gl_GlobalInvocationID.x;

    Ray ray = getRay(index);
    Hit hit = intersectScene(ray);

    storeHit(index, hit);
}
