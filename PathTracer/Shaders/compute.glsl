#version 430
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38
#define EPSILON 0.00001
#define PI      3.141592653589793238462643383279502
#define INVPI   0.318309886183790671537767526745028

#define MATERIAL_DIFFUSE 0
#define MATERIAL_EMISSIVE 1
#define MATERIAL_MIRROR 2
#define MATERIAL_DIELECTRIC 3

layout(local_size_x = 8, local_size_y = 8) in;

struct Screen
{
    float rcp_width;
    float rcp_height;
    float ar;
};

struct Camera
{
    vec3 position;
    vec3 forward;
    vec3 right;
    vec3 up;
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
    int type;
    float index;
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
    BoundingBox box;
    int leftFirst;
    int count;
};

layout(rgba32f, binding=0) uniform image2D screen_buffer;

layout(std430,  binding=1) buffer material_buffer { Material materials[]; };
layout(std430,  binding=2) buffer sphere_buffer   { Sphere spheres[]; };
layout(std430,  binding=3) buffer triangle_buffer { Triangle triangles[]; };
layout(std430,  binding=4) buffer node_buffer     { Node nodes[]; };
layout(std430,  binding=5) buffer index_buffer    { int indices[]; };

uniform uint   frame;
uniform uint   samples;
uniform Screen screen;
uniform Camera camera;
uniform vec4   sky_color;
uniform int    sphere_count;

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

    const vec3 c = camera.forward * camera.focal_distance;
    const vec3 d = normalize(c + camera.right * x * screen.ar + camera.up * y);

    Ray r;
    r.direction  = d;
    r.origin     = camera.position;
    r.reciprocal = 1.0 / d;

    return r;
}

void ray_sphere_intersection(Ray ray, const Sphere sphere, inout Hit hit)
{
    const vec3 v = ray.origin - sphere.center_radius.xyz;

    const float a = dot(ray.direction, ray.direction);
    const float b = dot(2 * ray.direction, v);
    const float c = dot(v, v) - sphere.center_radius.w * sphere.center_radius.w;

    const float d = b * b - 4 * a * c;

    if (d < 0)
    {
        return;
    }

    const float sqrtd = sqrt(d);
    const float rcp = 1.0 / (2 * a);

    float t0 = (-b + sqrtd) * rcp;
    float t1 = (-b - sqrtd) * rcp;

    if (t0 > t1)
    {
        const float tmp = t1;
        t1 = t0;
        t0 = tmp;
    }

    if (t0 < 0)
    {
        t0 = t1;

        if (t0 < 0)
        {
            return;
        }
    }

    if (t0 < hit.distance)
    {
        hit.distance = t0;
        hit.position = ray.origin + ray.direction * t0;
        hit.normal   = normalize(hit.position - sphere.center_radius.xyz);
        hit.material = sphere.material;
    }
}

void ray_triangle_intersection(Ray ray, const Triangle triangle, inout Hit hit)
{
    const vec3 edge1 = triangle.v2.position.xyz - triangle.v1.position.xyz;
    const vec3 edge2 = triangle.v3.position.xyz - triangle.v1.position.xyz;
    
    const vec3 h = cross(ray.direction, edge2);
    const float a = dot(edge1, h);
    
    const float f = 1.0 / a;
    const vec3 s = ray.origin - triangle.v1.position.xyz;
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
    
    if (t > 0.0 && t < hit.distance)
    {
        hit.distance = t;
        hit.position = ray.origin + ray.direction * t;
        hit.normal   = normalize(cross(edge2, edge1));
        hit.material = triangle.material;
    }
}

bool ray_aabb_test(Ray ray, const vec3 minimum, const vec3 maximum)
{
    const float t1 = (minimum.x - ray.origin.x) * ray.reciprocal.x;
    const float t2 = (maximum.x - ray.origin.x) * ray.reciprocal.x;
    const float t3 = (minimum.y - ray.origin.y) * ray.reciprocal.y;
    const float t4 = (maximum.y - ray.origin.y) * ray.reciprocal.y;
    const float t5 = (minimum.z - ray.origin.z) * ray.reciprocal.z;
    const float t6 = (maximum.z - ray.origin.z) * ray.reciprocal.z;

    const float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    const float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    return (tmax > 0 && tmin < tmax);
}

Hit intersect_scene(Ray ray)
{
    Hit hit;
    hit.distance = FLT_MAX;
    hit.material = -1;

    Node stack[10];
    int stack_pos = 0;

    stack[0] = nodes[0];

    while (stack_pos >= 0)
    {
        Node n = stack[stack_pos];
        stack_pos--;

        if (ray_aabb_test(ray, n.box.min.xyz, n.box.max.xyz))
        {
            if (n.count > 0)
            {
                for (int i = n.leftFirst; i < n.leftFirst + n.count; i++)
                {
                    int j = indices[i];
                    Triangle t = Triangle(triangles[j].v1, triangles[j].v2, triangles[j].v3, triangles[j].material);
                    ray_triangle_intersection(ray, t, hit);
                }
            }
            else
            {
                stack_pos++;
                stack[stack_pos] = nodes[n.leftFirst];
                stack_pos++;
                stack[stack_pos] = nodes[n.leftFirst + 1];
            }
        }
    }

    for (int i = 0; i < 20; i++)
    {        
        // THIS DOES NOT WORK (FOR SOME REASON):
        // ray_sphere_intersection(ray, spheres[i], hit);

        Sphere s = Sphere(spheres[i].center_radius, spheres[i].material);
        ray_sphere_intersection(ray, s, hit);
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

    return r * cos(theta) * u + r * sin(theta) * v + sqrt(1.0 - r0) * normal;
}

vec4 brdf(const Material mat)
{
    return mat.color * INVPI;
}

float pdf(const vec3 n, const vec3 r)
{
    const float d = dot(n, r) * INVPI;

    // protect against division by zero
    if (d < 0.0000001) return 0.0000001;
    return d;
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

        if (mat.type == MATERIAL_EMISSIVE)
        {
            return throughput * mat.color;
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
            throughput = brdf(mat) * throughput * dot(hit.normal, new_dir) / pdf(hit.normal, new_dir);
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
