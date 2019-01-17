using PathTracer.Library.Geometry;
using PathTracer.Library.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PathTracer
{
    class Scene : IDisposable
    {
        private readonly Buffer<Material> materials;
        private readonly Buffer<Triangle> triangles;

        private readonly Dictionary<Material, int> indices;

        public Scene()
        {
            this.materials = new Buffer<Material>(1);
            this.triangles = new Buffer<Triangle>(3);

            this.indices = new Dictionary<Material, int>();
        }

        public void LoadMesh(string path)
        {
            Mesh mesh = Mesh.LoadFromFile(path);

            foreach (Group group in mesh.Groups)
            {
                foreach (Face face in group.Faces)
                {
                    foreach (Library.Geometry.Vertex[] vertices in face.Triangulate)
                    {
                        this.triangles.Add(new Triangle()
                        {
                            V1 = new Vertex() { Position = new Vector4(vertices[0].Position, 0) },
                            V2 = new Vertex() { Position = new Vector4(vertices[1].Position, 0) },
                            V3 = new Vertex() { Position = new Vector4(vertices[2].Position, 0) },
                            Material = 0
                        });
                    }
                }
            }
        }

        public void AddMaterial(IEnumerable<Material> materials)
        {
            foreach (Material material in materials)
            {
                this.AddMaterial(material);
            }
        }

        public void AddMaterial(Material material)
        {
            this.GetMaterialIndex(material);
        }

        public void CopyToDevice()
        {
            this.materials.CopyToDevice();
            this.triangles.CopyToDevice();
        }

        public void Dispose()
        {
            this.materials.Dispose();
            this.triangles.Dispose();
        }

        private int GetMaterialIndex(Material material)
        {
            if (this.indices.ContainsKey(material))
            {
                return this.indices[material];
            }

            int count = this.indices.Count;
            this.indices.Add(material, count);
            this.materials.Add(material);

            return count;
        }
    }
}
