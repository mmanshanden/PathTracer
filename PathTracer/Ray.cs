using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PathTracer
{
    class Ray
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly Vector3 Inverse;

        public Ray(Vector3 origin, Vector3 direction)
        {
            this.Origin = origin;
            this.Direction = direction;
            this.Inverse = new Vector3(1.0f / direction.X, 1.0f / direction.Y, 1.0f / direction.Z);
        }

        public Vector3 Traverse(float t)
        {
            return this.Origin + this.Direction * t;
        }
    }
}
