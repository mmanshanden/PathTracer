﻿#version 430
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38
#define EPSILON 0.00001
#define PI      3.141592653589793238462643383279502
#define INVPI   0.318309886183790671537767526745028
#define INV2PI  0.159154943091895335768883763372514

#define MATERIAL_DIFFUSE 0
#define MATERIAL_EMISSIVE 1
#define MATERIAL_MIRROR 2
#define MATERIAL_DIELECTRIC 3
#define MATERIAL_METAL 4

layout(local_size_x = 8, local_size_y = 8) in;

struct Screen
{
    float rcp_width;
    float rcp_height;
    float ar;
};

struct Camera
{
    vec4 position;
    vec4 forward;
    vec4 right;
    vec4 up;
    float focal_distance;
};

struct Ray
{
    vec3 origin;
    vec3 direction;
    vec3 reciprocal;
};

struct Material
{
    vec4 color;
    vec4 emissive;
    int type;
    float index;
    float alpha;
};

struct Hit
{
    float distance;
    vec3 position;
    vec3 normal;
    int material;
};

struct Sphere
{
    vec4 center_radius;
    int material;
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

struct BoundingBox
{
    vec4 min;
    vec4 max;
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

layout(rgba32f, binding=0) uniform image2D screen_buffer;
layout(std140,  binding=1) uniform render_state
{
    uint   frame;
    uint   samples;
    Screen screen;
    Camera camera;
    vec4   sky_color;
};

layout(std430,  binding=2) buffer material_buffer { Material materials[]; };
layout(std430,  binding=3) buffer triangle_buffer { Triangle triangles[]; };
layout(std430,  binding=4) buffer node_buffer     { Node nodes[]; };


void xor_shift(inout uint seed)
{
    seed ^= seed << 13;
    seed ^= seed >> 17;
    seed ^= seed << 05;
}

float random_float(inout uint seed)
{
    xor_shift(seed);
    return float(seed) * (1.0 / 4294967295.0);
}

void update_screen_buffer(const ivec2 screen_pos, const vec4 color)
{
    vec4 prev = imageLoad(screen_buffer, screen_pos);
    vec4 new = (prev * samples + color) / (samples + 1);
    imageStore(screen_buffer, screen_pos, new);
}

Ray generate_ray(const ivec2 screen_pos, inout uint seed)
{
    const float x = (random_float(seed) + screen_pos.x) * screen.rcp_width - 0.5;
    const float y = (random_float(seed) + screen_pos.y) * screen.rcp_height - 0.5;

    const vec3 c = camera.forward.xyz * camera.focal_distance;
    const vec3 d = normalize(c + camera.right.xyz * x * screen.ar + camera.up.xyz * y);

    Ray r;
    r.direction  = d;
    r.origin     = camera.position.xyz;
    r.reciprocal = 1.0 / d;

    return r;
}

void ray_triangle_intersection(Ray ray, const int index, inout Hit hit)
{
    const Triangle tri = triangles[index];

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
        hit.position = ray.origin + ray.direction * t;
        hit.material = tri.material;
        hit.normal   = tri.v1.normal.xyz * u 
                     + tri.v2.normal.xyz * v 
                     + tri.v3.normal.xyz * w;

        hit.normal   = normalize(hit.normal);
    }
}

bool ray_aabb_test(Ray ray, const vec3 minimum, const vec3 maximum, out float tmin, out float tmax)
{
    const float t1 = (minimum.x - ray.origin.x) * ray.reciprocal.x;
    const float t2 = (maximum.x - ray.origin.x) * ray.reciprocal.x;
    const float t3 = (minimum.y - ray.origin.y) * ray.reciprocal.y;
    const float t4 = (maximum.y - ray.origin.y) * ray.reciprocal.y;
    const float t5 = (minimum.z - ray.origin.z) * ray.reciprocal.z;
    const float t6 = (maximum.z - ray.origin.z) * ray.reciprocal.z;

    tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    return (tmax > 0 && tmin < tmax);
}

bool ray_node_bounds_test(Ray ray, const int idx, out float tmin, out float tmax)
{
    const Node n = nodes[idx];

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

Hit intersect_scene(Ray ray)
{
    float tmin, tmax;

    Hit hit;
    hit.distance = FLT_MAX;
    hit.material = -1;

    int candidates[384];
    int c = 0;

    int stack[32];
    int p = 0;

    stack[0] = 0;

    while (p >= 0)
    {
        int idx = stack[p--];

        if (ray_node_bounds_test(ray, idx, tmin, tmax))
        {
            Node n = nodes[idx];

            if (n.count > 0)
            {
                for (int i = n.leftFirst; i < n.leftFirst + n.count; i++)
                {
                    candidates[c] = i;
                    c = min(c + 1, 383);
                }
            }
            else
            {
                stack[++p] = n.leftFirst;
                stack[++p] = n.leftFirst + 1;
            }
        }
    }

    for (int i = 0; i < c; i++)
    {
        ray_triangle_intersection(ray, candidates[i], hit);
    }

    return hit;
}

float schlick(const vec3 direction, const vec3 normal, const float n1, const float n2)
{
    float r0 = (n1 - n2) / (n1 + n2);
    r0 = r0 * r0;

    return r0 + (1.0 - r0) * pow(1 - dot(normal, -direction), 5);
}

vec3 diffuse_reflection(const vec3 normal, inout uint seed)
{
    // "random" unit vector
    const vec3 axis = abs(normal.x) > 0.95 ? vec3(0, 1, 0) : vec3(1, 0, 0);

    const vec3 u = normalize(cross(normal, axis));
    const vec3 v = cross(u, normal);

    const float r0 = random_float(seed);
    const float r1 = random_float(seed);

    const float r = sqrt(r0);
    const float theta = 2.0 * PI * r1;

    return r * cos(theta) * u + sqrt(1.0 - r0) * normal + r * sin(theta) * v;
}

vec4 brdf(const Material mat)
{
    return mat.color * INVPI;
}

float pdf(const vec3 n, const vec3 r)
{
    return dot(n, r) * INVPI;
}







vec3 schlick3(vec3 ks, vec3 l, vec3 n)
{
    const float ldotn = clamp(dot(l, n), 0, 1);
    return ks + (1.0 - ks) * pow(1.0 - ldotn, 5);
}

vec3 spherical_to_cartesian(const float theta, const float phi)
{
    return vec3(sin(theta) * cos(phi), cos(theta), sin(theta) * sin(phi));
}

void isnan_v(vec3 v)
{
    if (isnan(v.x) || isnan(v.y) || isnan(v.z))
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                update_screen_buffer(ivec2(x, y), vec4(4000000000,0,0,0));
            }
        }
    }
}

float smith_ggx_masking(vec3 l, vec3 v, vec3 n, float a2)
{
    float ndotl = clamp(dot(n, l), 0, 1);
    float ndotv = clamp(dot(n, v), 0, 1);

    float c = sqrt(a2 + (1.0 - a2) * ndotv * ndotv) + ndotv;

    if (c < 0.000001)
    {
        return 0;
    }

    return 2.0 * ndotv / c;
}

float smith_ggx_shadowmasking(vec3 l, vec3 v, vec3 n, float a2)
{
    float ndotl = clamp(dot(n, l), 0, 1);
    float ndotv = clamp(dot(n, v), 0, 1);

    float a = ndotv * sqrt(a2 + (1.0 - a2) * ndotl * ndotl);
    float b = ndotl * sqrt(a2 + (1.0 - a2) * ndotv * ndotv);

    float denom = a + b;

    if (denom < 0.00001)
    {
        return 0;
    }

    return 2.0 * ndotl * ndotv / denom;
}

// https://cdn2.unrealengine.com/Resources/files/2013SiggraphPresentationsNotes-26915738.pdf
float g1_schlick(vec3 v, vec3 n, float a)
{
    float k = (a + 1) * (a + 1) * 0.125;
    return dot(n, v) / (dot(n, v) * (1 - k) + k);
}

float g_schlick(vec3 l, vec3 v, vec3 n, float a)
{
    return g1_schlick(l, n, a) * g1_schlick(v, n, a);
}

vec3 facetnormal(vec3 wo, float ax, float ay, inout uint seed)
{
    float u1 = random_float(seed);
    float u2 = random_float(seed);

    // Stretch the view vector so we are sampling as though
    // roughness==1
    vec3 v = normalize(vec3(wo.x * ax, wo.y * ay, wo.z));

    // Build an orthonormal basis with v, t1, and t2
    vec3 t1 = (v.z < 0.999f) ? normalize(cross(v, vec3(0, 0, 1))) : vec3(1, 0, 0);
    vec3 t2 = cross(t1, v);

    // Choose a point on a disk with each half of the disk weighted
    // proportionally to its projection onto direction v
    float a = 1.0f / (1.0f + v.z);
    float r = sqrt(u1);
    float phi = (u2 < a) ? (u2 / a) * PI : PI + (u2 - a) / (1.0f - a) * PI;
    float p1 = r * cos(phi);
    float p2 = r * sin(phi) * ((u2 < a) ? 1.0f : v.z);

    // Calculate the normal in this stretched tangent space
    vec3 n = p1 * t1 + p2 * t2 + sqrt(max(0.0f, 1.0f - p1 * p1 - p2 * p2)) * v;

    // Unstretch and normalize the normal
    return normalize(vec3(ax * n.x, ay * n.y, max(0.0f, n.z)));
}

vec3 SampleFacetNormal(inout uint seed, vec3 _v, float _a)
{
    float r1 = random_float(seed);
    float r2 = random_float(seed);

    vec3 v = normalize(vec3(_v.x * _a, _v.y, _v.z * _a));

    vec3 t1 = (v.y < 0.999) ? normalize(cross(v, vec3(0,0,1))) : vec3(1,0,0);
    vec3 t2 = cross(t1, v);

    float a = 1.0 / (1.0 + v.y);
    float r = sqrt(r1);
    float phi = r2 < a ? (r2 / a) * PI : PI + (r2 - a) / (1.0 - a) * PI;

    float p1 = r * cos(phi);
    float p2 = r * sin(phi) * ((r2 < a) ? 1.0 : v.y);

    vec3 n = p1 * t1 + p2 * t2 + sqrt(max(0.0, 1.0 - p1 * p1 - p2 * p2)) * v;

    return normalize(vec3(a * n.x, max(0.0, n.y), a * n.z));
}

vec4 ImportanceSampleGgxVdn(inout uint seed, vec3 n, vec3 v, const Material mat, out vec3 l)
{
    vec3 W = abs(n.x) > 0.9 ? vec3(0, 1, 0) : vec3(1, 0, 0);

    vec3 N = n;
    vec3 T = normalize(cross(N, W));
    vec3 B = cross(N, T);

    vec3 tangent_v = vec3(dot(v, T), dot(v, B), dot(v, N));
    vec3 tangent_n = vec3(dot(n, T), dot(n, B), dot(n, N));

    float a2 = mat.alpha * mat.alpha;

    vec3 tangent_m = facetnormal(tangent_v, mat.alpha, mat.alpha, seed);
    vec3 tangent_l = reflect(-tangent_v, tangent_m);


    if (dot(tangent_l, tangent_m) < 0)
    {
        return vec4(0);
    }

    l = (tangent_l.x * T + tangent_l.y * B + tangent_l.z * N);

    float ndotl = dot(tangent_n, tangent_l);
    float ldotm = dot(tangent_l, tangent_m);

    vec4 f = vec4(schlick3(mat.color.xyz, tangent_m, tangent_l), 0);

    isnan_v(f.xyz);
    

    float g1 = smith_ggx_masking(tangent_l, tangent_v, tangent_n, a2);
    float g2 = smith_ggx_shadowmasking(tangent_l, tangent_v, tangent_n, a2);

    // alternative:
    //float g1 = g1_schlick(tangent_v, tangent_m, mat.alpha);
    //float g2 = g_schlick(tangent_l, tangent_v, tangent_m, mat.alpha);

    if (g1 < 0.00001)
    {
        return vec4(0);
    }

    return f * (g2 / g1);   
}

vec4 ImportanceSampleGgx(inout uint seed, vec3 n, vec3 v, const Material mat, out vec3 l)
{
    vec3 W = abs(n.x) > 0.9 ? vec3(0, 1, 0) : vec3(1, 0, 0);

    vec3 N = n;
    vec3 T = normalize(cross(N, W));
    vec3 B = cross(N, T);

    vec3 tangent_v = vec3(dot(v, T), dot(v, N), dot(v, B));
    vec3 tangent_n = vec3(dot(n, T), dot(n, N), dot(n, B));

    float a2 = mat.alpha * mat.alpha;

    float r0 = random_float(seed);
    float r1 = random_float(seed);

    float theta = acos(sqrt((1.0 - r0) / (r0 * (a2 - 1.0) + 1.0)));
    float phi   = (2.0 * PI) * r1;

    vec3 tangent_m = spherical_to_cartesian(theta, phi);
    vec3 tangent_l = reflect(-tangent_v, tangent_m);

    l = (tangent_l.x * T + tangent_l.y * N + tangent_l.z * B);
    
    float ndotl = dot(tangent_n, tangent_l);
    float ldotm = dot(tangent_l, tangent_m);

    if (ndotl > 0 && ldotm > 0)
    {
        vec4 f = vec4(schlick3(mat.color.xyz, tangent_m, tangent_l), 0);
        float g = smith_ggx_shadowmasking(tangent_l, tangent_v, tangent_n, a2);

        float vdotm = dot(tangent_v, tangent_m);
        float ndotv = dot(tangent_n, tangent_v);
        float ndotm = dot(tangent_n, tangent_m);

        float denom = ndotv * ndotm;

        if (denom == 0)
        {
            return vec4(0);
        }

        float weight = abs(vdotm) / denom;

        return f * g * weight;
    }
    else
    {
        return vec4(0);
    }

    return vec4(tangent_v, 0);
}







vec4 Sample(Ray ray, inout uint seed)
{
    vec4 throughput = vec4(1);

    for (int i = 0; i < 10; i++)
    {
        Hit hit = intersect_scene(ray);

        if (hit.material < 0)
        {
            return throughput * sky_color;
        }
        
        const Material mat = materials[hit.material];

        if (mat.emissive.x > 0 || mat.emissive.y > 0 || mat.emissive.z > 0)
        {
            return throughput * mat.emissive;
        }

        vec3 new_dir;

        if (mat.type == MATERIAL_DIELECTRIC)
        {
            float n1 = 1.0;
            float n2 = mat.index;

            if (dot(ray.direction, hit.normal) > 0)
            {
                n1 = mat.index;
                n2 = 1.0;

                hit.normal = hit.normal * -1;

                // throughput.x = throughput.x * exp(-mat.color.x * hit.distance);
                // throughput.y = throughput.y * exp(-mat.color.y * hit.distance);
                // throughput.z = throughput.z * exp(-mat.color.z * hit.distance);
            }

            const float r = random_float(seed);
            const float s = schlick(ray.direction, hit.normal, n1, n2);

            if (r < s)
            {
                new_dir    = reflect(ray.direction, hit.normal);
                throughput = throughput * mat.color;
            }
            else
            {
                new_dir    = refract(ray.direction, hit.normal, n1 / n2); 
                throughput = throughput * mat.color;
            }
        }

        if (mat.type == MATERIAL_MIRROR)
        {
            new_dir    = reflect(ray.direction, hit.normal);
            throughput = throughput * mat.color;
        }

        if (mat.type == MATERIAL_DIFFUSE)
        {            
            new_dir    = diffuse_reflection(hit.normal, seed);
            float pdf  = pdf(hit.normal, new_dir);

            if (pdf > 0)
            {
                throughput = brdf(mat) * throughput * dot(hit.normal, new_dir) / pdf;
            }
        }

        if (mat.type == MATERIAL_METAL)
        {
            //throughput = throughput * ImportanceSampleGgx(seed, hit.normal, -ray.direction, mat, new_dir);
            vec4 s = ImportanceSampleGgxVdn(seed, hit.normal, -ray.direction, mat, new_dir);
            s.w = 0;
            throughput = throughput * s;
            

        }

        ray.origin     = hit.position + new_dir * EPSILON;
        ray.direction  = new_dir;
        ray.reciprocal = 1.0 / new_dir;
    }

    return throughput;
}

void main() 
{
    const ivec2 screen_pos = ivec2(gl_GlobalInvocationID.xy);
    uint seed = screen_pos.x * 100999001 + screen_pos.y * 152252251 + frame * 377000773;
    
    Ray ray = generate_ray(screen_pos, seed);
    const vec4 color = Sample(ray, seed);

    update_screen_buffer(screen_pos, color);
}
