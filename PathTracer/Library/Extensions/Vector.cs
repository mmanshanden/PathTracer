using System.Numerics;

namespace PathTracer.Library.Extensions
{
    static class Vector
    {
        public static Vector2 Normalized(this Vector2 vector)
        {
            return Vector2.Normalize(vector);
        }

        public static Vector3 Normalized(this Vector3 vector)
        {
            return Vector3.Normalize(vector);
        }

        public static Vector4 Normalized(this Vector4 vector)
        {
            return Vector4.Normalize(vector);
        }
    }
}
