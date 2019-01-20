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
        private readonly Buffer<Node> nodes;

        private readonly Accelerator accelerator;
        private readonly Dictionary<Material, int> lut;

        public Scene()
        {
            this.materials = new Buffer<Material>(1);
            this.triangles = new Buffer<Triangle>(3);
            this.nodes = new Buffer<Node>(4);

            this.accelerator = new Accelerator(this.triangles, this.nodes);
            this.lut = new Dictionary<Material, int>();
        }

        public void AddMesh(string path)
        {
            Mesh mesh = Mesh.LoadFromFile(path);

            foreach (Group group in mesh.Groups)
            {
                int material = this.GetMaterialIndex(new Material()
                {
                    Color = new Vector4(group.Material.DiffuseColor, 0),
                    Type = MaterialType.Diffuse
                });

                foreach (Face face in group.Faces)
                {
                    foreach (Library.Geometry.Vertex[] vertices in face.Triangulate)
                    {
                        this.accelerator.AddTriangle(new Triangle()
                        {
                            V1 = new Vertex() { Position = new Vector4(vertices[0].Position, 0) },
                            V2 = new Vertex() { Position = new Vector4(vertices[1].Position, 0) },
                            V3 = new Vertex() { Position = new Vector4(vertices[2].Position, 0) },
                            Material = material
                        });
                    }
                }
            }
        }

        public void AddMesh(string path, Material material)
        {
            Mesh mesh = Mesh.LoadFromFile(path);

            foreach (Group group in mesh.Groups)
            {
                foreach (Face face in group.Faces)
                {
                    foreach (Library.Geometry.Vertex[] vertices in face.Triangulate)
                    {
                        this.accelerator.AddTriangle(new Triangle()
                        {
                            V1 = new Vertex() { Position = new Vector4(vertices[0].Position, 0) },
                            V2 = new Vertex() { Position = new Vector4(vertices[1].Position, 0) },
                            V3 = new Vertex() { Position = new Vector4(vertices[2].Position, 0) },
                            Material = this.GetMaterialIndex(material)
                        });
                    }
                }
            }
        }

        public void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Material material)
        {
            Vertex v1 = new Vertex() { Position = new Vector4(p1, 0) };
            Vertex v2 = new Vertex() { Position = new Vector4(p2, 0) };
            Vertex v3 = new Vertex() { Position = new Vector4(p3, 0) };
            Vertex v4 = new Vertex() { Position = new Vector4(p4, 0) };

            this.accelerator.AddTriangle(new Triangle()
            {
                V1 = v1,
                V2 = v2,
                V3 = v3,
                Material = this.GetMaterialIndex(material)
            });

            this.accelerator.AddTriangle(new Triangle()
            {
                V1 = v3,
                V2 = v4,
                V3 = v1,
                Material = this.GetMaterialIndex(material)
            });
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
            this.accelerator.Build();

            this.materials.CopyToDevice();
            this.triangles.CopyToDevice();
            this.nodes.CopyToDevice();
        }

        public void Dispose()
        {
            this.materials.Dispose();
            this.triangles.Dispose();
            this.nodes.Dispose();
        }

        private int GetMaterialIndex(Material material)
        {
            if (this.lut.ContainsKey(material))
            {
                return this.lut[material];
            }

            int count = this.lut.Count;
            this.lut.Add(material, count);
            this.materials.Add(material);

            return count;
        }
    }
}
