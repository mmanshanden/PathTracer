using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace PathTracer.Library.Geometry
{
    class Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector2 Texcoord;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector2 texcoord, Vector3 normal)
        {
            this.Position = position;
            this.Texcoord = texcoord;
            this.Normal = normal;
        }
    }
}
