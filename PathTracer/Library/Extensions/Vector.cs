using System;
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

        public static float MinValue(this Vector3 vector)
        {
            return MathF.Min(MathF.Min(vector.X, vector.Y), vector.Z);
        }

        public static float MaxValue(this Vector3 vector)
        {
            return MathF.Max(MathF.Max(vector.X, vector.Y), vector.Z);
        }

        public static float MinValue3(this Vector4 vector)
        {
            return MathF.Min(MathF.Min(vector.X, vector.Y), vector.Z);
        }

        public static float MaxValue3(this Vector4 vector)
        {
            return MathF.Max(MathF.Max(vector.X, vector.Y), vector.Z);
        }

        public static float MinValue(this Vector4 vector)
        {
            return MathF.Min(MathF.Min(vector.X, vector.Y), MathF.Min(vector.Z, vector.W));
        }

        public static float MaxValue(this Vector4 vector)
        {
            return MathF.Max(MathF.Max(vector.X, vector.Y), MathF.Max(vector.Z, vector.W));
        }
    }
}
