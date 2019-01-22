using PathTracer.Library.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace PathTracer.Library.Geometry
{
    class Face
    {
        private readonly Mesh mesh;
        private readonly Frame[] frames;

        public IEnumerable<Vertex> Vertices
        {
            get
            {
                for (int i = 0; i < this.frames.Length; i++)
                {
                    yield return this.BuildVertex(this.frames[i]);
                }
            }
        }

        public IEnumerable<Vertex[]> Triangulate
        {
            get
            {
                Vertex a = this.BuildVertex(this.frames[0]);
                Vertex b = this.BuildVertex(this.frames[1]);

                for (int i = 2; i < this.frames.Length; i++)
                {
                    Vertex c = this.BuildVertex(this.frames[i]);
                    Vector3 sum = a.Normal + b.Normal + c.Normal;

                    if (sum.LengthSquared() < 0.5f)
                    {
                        Vector3 edge1 = a.Position - c.Position;
                        Vector3 edge2 = b.Position - c.Position;

                        Vector3 normal = Vector3.Cross(edge1, edge2).Normalized();

                        a.Normal = b.Normal = c.Normal = normal;
                    }

                    yield return new[] { a, b, c };
                    b = c;
                }
            }
        }

        public Face(Mesh mesh, Frame[] frames)
        {
            this.mesh = mesh;
            this.frames = frames;
        }

        private Vertex BuildVertex(Frame frame)
        {
            return new Vertex()
            {
                Position = frame.I < 0 ? default(Vector3) : this.mesh.GetPosition(frame.I),
                Texcoord = frame.J < 0 ? default(Vector2) : this.mesh.GetTexcoord(frame.J),
                Normal = frame.K < 0 ? default(Vector3) : this.mesh.GetNormal(frame.K)
            };
        }
    }
}
