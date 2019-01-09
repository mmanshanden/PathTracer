﻿#version 450
#define FLT_MAX 3.402823466e+38
#define FLT_MIN 1.175494351e-38

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

struct Hit
{
    float distance;
    vec3 position;
    vec3 normal;
};

struct Sphere
{
    vec3 center;
    float radius;
};

layout(rgba32f, binding=0) uniform image2D screen_buffer;

uniform uint   frame;
uniform Screen screen;
uniform Camera camera;


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

void update_screen_buffer(ivec2 screen_pos, vec4 color)
{
    vec4 prev = imageLoad(screen_buffer, screen_pos);
    vec4 new = (prev * frame + color) / (frame + 1);
    imageStore(screen_buffer, screen_pos, new);
}

Ray generate_ray(ivec2 screen_pos)
{
    const float x = screen_pos.x * screen.rcp_width - 0.5;
    const float y = screen_pos.y * screen.rcp_height - 0.5;

    const vec3 c = camera.forward * camera.focal_distance;
    const vec3 d = normalize(c + camera.right * x * screen.ar + camera.up * y);

    Ray r;
    r.direction  = d;
    r.origin     = camera.position;
    r.reciprocal = 1.0 / d;

    return r;
}

void ray_sphere_intersection(Ray ray, Sphere sphere, inout Hit hit)
{
    vec3 v = ray.origin - sphere.center;

    float a = dot(ray.direction, ray.direction);
    float b = dot(2 * ray.direction, v);
    float c = dot(v, v) - sphere.radius * sphere.radius;

    float d = b * b - 4 * a * c;

    if (d < 0)
    {
        return;
    }

    float sqrtd = sqrt(d);

    float rcp = 1.0 / (2 * a);
    float t0 = (-b + sqrtd) * rcp;
    float t1 = (-b - sqrtd) * rcp;

    if (t0 > t1)
    {
        float tmp = t1;
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
        hit.normal   = normalize(hit.position - sphere.center);
    }
}

bool ray_aabb_test(Ray ray, vec3 minimum, vec3 maximum)
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

void main() 
{
    const ivec2 screen_pos = ivec2(gl_GlobalInvocationID.xy);
    
    uint seed = (screen_pos.x * 100999001 + screen_pos.y * 152252251 + frame * 377000773);
    
    const Ray ray = generate_ray(screen_pos);

    vec4 color;

    Sphere s1;
    s1.center = vec3(1, 0, 0);
    s1.radius = 1.0;

    Sphere s2;
    s2.center = vec3(-1, 0, 0);
    s2.radius = 1.0;

    Hit hit;
    hit.distance = FLT_MAX;

    ray_sphere_intersection(ray, s1, hit);
    ray_sphere_intersection(ray, s2, hit);

    if (hit.distance < FLT_MAX)
    {
        color = vec4(hit.normal, 1);
    }
    else
    {
        color = vec4(0);
    }

    update_screen_buffer(screen_pos, color);
}