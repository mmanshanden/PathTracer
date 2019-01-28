using PathTracer.Library.Extensions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 112)]
    struct Triangle
    {
        [FieldOffset(00)] public Vertex V1;
        [FieldOffset(32)] public Vertex V2;
        [FieldOffset(64)] public Vertex V3;
        [FieldOffset(96)] public int Material;

        public void Intersect(ref Ray ray, ref Intersection i)
        {
            Vector3 edge1 = this.V2.Position - this.V1.Position;
            Vector3 edge2 = this.V3.Position - this.V1.Position;

            Vector3 h = Vector3.Cross(ray.Direction, edge2);
            float a = Vector3.Dot(edge1, h);

            float f = 1.0f / a;
            Vector3 s = ray.Origin - this.V1.Position;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0 || u > 1.0)
            {
                return;
            }

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.Direction, q);

            if (v < 0.0f || u + v > 1.0f)
            {
                return;
            }

            float dist = f * Vector3.Dot(edge2, q);

            if (dist > 0 && dist < i.Distance)
            {
                i.Distance = dist;
                i.Point = ray.Traverse(dist);
            }
        }
    }
}
