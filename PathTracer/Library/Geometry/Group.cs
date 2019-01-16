using System.Collections.Generic;

namespace PathTracer.Library.Geometry
{
    class Group
    {
        private readonly List<Face> faces;

        public IEnumerable<Face> Faces
        {
            get
            {
                for (int i = 0; i < this.faces.Count; i++)
                {
                    yield return this.faces[i];
                }
            }
        }

        public Group(List<Face> faces)
        {
            this.faces = faces;
        }
    }
}
