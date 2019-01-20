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
            return new Vertex(
                frame.I < 0 ? default(Vector3) : this.mesh.GetPosition(frame.I),
                frame.J < 0 ? default(Vector2) : this.mesh.GetTexcoord(frame.J),
                frame.K < 0 ? default(Vector3) : this.mesh.GetNormal(frame.K));
        }
    }
}
