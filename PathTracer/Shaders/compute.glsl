#version 430
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38
#define EPSILON 0.001
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

layout(rgba32f, binding=0) uniform image2D screen_buffer;
layout(std430,  binding=1) buffer          material_buffer { Material materials[]; };
layout(std430,  binding=2) buffer          sphere_buffer   { Sphere spheres[]; };

uniform uint   frame;
uniform uint   samples;
uniform Screen screen;
uniform Camera camera;

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

bool ray_aabb_test(const Ray ray, const vec3 minimum, const vec3 maximum)
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

Hit intersect_scene(const Ray ray)
{
    Hit hit;
    hit.distance = FLT_MAX;
    hit.material = -1;

    for (int i = 0; i < 60; i++)
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
    const float r0 = random_float(seed);
    const float r1 = random_float(seed);

    const float r = sqrt(1.0 - r0 * r0);
    const float theta = 2.0 * PI * r1;

    const float x = cos(theta) * r;
    const float y = sin(theta) * r;

    const vec3 refl = vec3(x, y, r0);

    if (dot(normal, refl) < 0)
    {
        return refl * -1;
    }

    return refl;
}

vec4 Sample(Ray ray, inout uint seed)
{
    vec4 color = vec4(1);

    for (int i = 0; i < 10; i++)
    {
        Hit hit = intersect_scene(ray);

        if (hit.material < 0)
        {
            return color * vec4(142.0 / 255.0, 178.0 / 255.0, 237.0 / 255.0, 1);
        }

        const Material mat = materials[hit.material];

        if (mat.type == MATERIAL_EMISSIVE)
        {
            return color * mat.color;
        }

        if (mat.type == MATERIAL_DIELECTRIC)
        {
            float n1 = 1.0;
            float n2 = mat.index;

            if (dot(ray.direction, hit.normal) > 0)
            {
                n1 = mat.index;
                n2 = 1.0;

                hit.normal = hit.normal * -1;

                color.x = exp(-mat.color.x * hit.distance);
                color.y = exp(-mat.color.y * hit.distance);
                color.z = exp(-mat.color.z * hit.distance);
            }

            const float s = schlick(ray.direction, hit.normal, n1, n2);

            const vec3 r = random_float(seed) < s ? reflect(ray.direction, hit.normal) 
                                                  : refract(ray.direction, hit.normal, n1 / n2);

            color = color * mat.color;
            
            ray.origin = hit.position + r * EPSILON;
            ray.direction = r;
            ray.reciprocal = 1.0 / r;   

            continue;
        }

        if (mat.type == MATERIAL_MIRROR)
        {
            vec3 r = reflect(ray.direction, hit.normal);
            color = color * mat.color;
            
            ray.origin = hit.position + r * EPSILON;
            ray.direction = r;
            ray.reciprocal = 1.0 / r;

            continue;
        }

        else 
        {
            vec3 r = diffuse_reflection(hit.normal, seed);
            vec4 brdf = mat.color;
            color = brdf * color * dot(hit.normal, r);

            ray.origin     = hit.position + r * EPSILON;
            ray.direction  = r;
            ray.reciprocal = 1.0 / r;
        }
    }

    return color;
}

void main() 
{
    const ivec2 screen_pos = ivec2(gl_GlobalInvocationID.xy);
    uint seed = screen_pos.x * 100999001 + screen_pos.y * 152252251 + frame * 377000773;
    
    const Ray ray = generate_ray(screen_pos, seed);
    const vec4 color = Sample(ray, seed);

    update_screen_buffer(screen_pos, color);
}
