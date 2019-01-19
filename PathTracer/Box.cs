using PathTracer.Library.Extensions;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    struct Box
    {
        [FieldOffset(00)] public Vector3 Min;
        [FieldOffset(12)] public Vector3 Max;

        public static Box Negative = new Box()
        {
            Max = new Vector3(float.NegativeInfinity),
            Min = new Vector3(float.PositiveInfinity)
        };

        public Vector3 Centroid()
        {
            return 0.5f * this.Min + 0.5f * this.Max;
        }

        public int Domain()
        {
            Vector3 dim = this.Max - this.Min;

            float l = dim.MaxValue();

            if (l == dim.X)
            {
                return 0;
            }
            else if (l == dim.Y)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        public float Area()
        {
            Vector3 d = this.Max - this.Min;
            return 2.0f * (d.X * d.Y + d.X * d.Z + d.Y * d.Z);
        }

        public static Box Union(Box a, Box b)
        {
            return new Box()
            {
                Max = Vector3.Max(a.Max, b.Max),
                Min = Vector3.Min(a.Min, b.Min)
            };
        }

        public static Box FromTriangle(Triangle triangle)
        {
            return Box.FromVertices(triangle.V1, triangle.V2, triangle.V3);
        }

        public static Box FromVertices(Vertex a, Vertex b, Vertex c)
        {
            return new Box()
            {
                Max = Vector4.Max(Vector4.Max(a.Position, b.Position), c.Position).ToVector3() + new Vector3(0.00001f),
                Min = Vector4.Min(Vector4.Min(a.Position, b.Position), c.Position).ToVector3() - new Vector3(0.00001f),
            };
        }
    }
}
