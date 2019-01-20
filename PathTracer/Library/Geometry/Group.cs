using System.Collections.Generic;

namespace PathTracer.Library.Geometry
{
    class Group
    {
        private readonly List<Face> faces;
        
        public string Name { get; private set; }
        public Material Material { get; private set; }

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

        public Group(List<Face> faces, string name, Material material)
        {
            this.faces = faces;
            this.Name = name;
            this.Material = material;
        }
    }
}
