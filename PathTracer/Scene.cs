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
            this.materials = new Buffer<Material>(2);
            this.triangles = new Buffer<Triangle>(3);
            this.nodes = new Buffer<Node>(4);

            this.accelerator = new Accelerator(this.triangles, this.nodes);
            this.lut = new Dictionary<Material, int>();
        }

        public void AddMeshNormalized(string path)
        {
            this.AddMesh(path, true, Matrix4x4.Identity);
        }

        public void AddMeshNormalized(string path, Material material)
        {
            this.AddMesh(path, true, Matrix4x4.Identity, material);
        }

        public void AddMeshNormalized(string path, Matrix4x4 transform)
        {
            this.AddMesh(path, true, transform);
        }

        public void AddMeshNormalized(string path, Matrix4x4 transform, Material material)
        {
            this.AddMesh(path, true, transform, material);
        }

        public void AddMesh(string path)
        {
            this.AddMesh(path, false, Matrix4x4.Identity);
        }

        public void AddMesh(string path, Material material)
        {
            this.AddMesh(path, false, Matrix4x4.Identity, material);
        }

        public void AddMesh(string path, Matrix4x4 transform)
        {
            this.AddMesh(path, false, transform);
        }

        public void AddMesh(string path, Matrix4x4 transform, Material material)
        {
            this.AddMesh(path, false, transform, material);
        }

        private void AddMesh(string path, bool normalized, Matrix4x4 transform)
        {
            Mesh mesh = Mesh.LoadFromFile(path);

            mesh.Normalized = normalized;
            mesh.Transform = transform;

            foreach (Group group in mesh.Groups)
            {
                int material = this.GetMaterialIndex(new Material()
                {
                    Color = new Vector4(group.Material.DiffuseColor, 0),
                    Emissive = new Vector4(group.Material.EmissiveColor, 0),
                    Type = MaterialType.Diffuse
                });

                this.LoadGroup(group, material);
            }
        }

        private void AddMesh(string path, bool normalized, Matrix4x4 transform, Material material)
        {
            Mesh mesh = Mesh.LoadFromFile(path);

            mesh.Normalized = normalized;
            mesh.Transform = transform;

            int m = this.GetMaterialIndex(material);

            foreach (Group group in mesh.Groups)
            {
                this.LoadGroup(group, m);
            }
        }

        private void LoadGroup(Group group, int material)
        {
            foreach (Face face in group.Faces)
            {
                foreach (Library.Geometry.Vertex[] vertices in face.Triangulate)
                {
                    this.accelerator.AddTriangle(new Triangle()
                    {
                        V1 = new Vertex() { Position = new Vector4(vertices[0].Position, 0), Normal = new Vector4(vertices[0].Normal, 0) },
                        V2 = new Vertex() { Position = new Vector4(vertices[1].Position, 0), Normal = new Vector4(vertices[1].Normal, 0) },
                        V3 = new Vertex() { Position = new Vector4(vertices[2].Position, 0), Normal = new Vector4(vertices[2].Normal, 0) },
                        Material = material
                    });
                }
            }
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
