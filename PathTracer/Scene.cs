﻿using PathTracer.Library.Geometry;
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
            this.materials = new Buffer<Material>(10);
            this.triangles = new Buffer<Triangle>(11);
            this.nodes = new Buffer<Node>(12);

            this.accelerator = new Accelerator(this.triangles, this.nodes);
            this.lut = new Dictionary<Material, int>();
        }
        
        public void Clear()
        {
            this.materials.Clear();
            this.accelerator.Clear();
            this.lut.Clear();
        }

        public Intersection Intersect(ref Ray ray)
        {
            Intersection i = new Intersection()
            {
                Distance = float.PositiveInfinity
            };

            this.accelerator.Intersect(ref ray, ref i);

            return i;
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

        public void GenerateTiledFoloor(Material a, Material b)
        {
            int i = this.GetMaterialIndex(a);
            int j = this.GetMaterialIndex(b);

            for (int x = -100; x < 99; x++)
            {
                for (int y = -100; y < 99; y++)
                {
                    int mat = ((x & 1) ^ (y & 1)) == 0 ? i : j;

                    Vertex v4 = new Vertex() { Position = new Vector3(x, 0, y),          Normal = new Vector3(0, 1, 0) };
                    Vertex v3 = new Vertex() { Position = new Vector3(x + 1, 0, y),      Normal = new Vector3(0, 1, 0) };
                    Vertex v2 = new Vertex() { Position = new Vector3(x + 1, 0, y + 1),  Normal = new Vector3(0, 1, 0) };
                    Vertex v1 = new Vertex() { Position = new Vector3(x, 0, y + 1),      Normal = new Vector3(0, 1, 0) };

                    this.accelerator.AddTriangle(new Triangle()
                    {
                        V1 = v1,
                        V2 = v2,
                        V3 = v3,
                        Material = mat
                    });

                    this.accelerator.AddTriangle(new Triangle()
                    {
                        V1 = v3,
                        V2 = v4,
                        V3 = v1,
                        Material = mat
                    });
                }
            }
        }

        private void AddMesh(string path, bool normalized, Matrix4x4 transform)
        {
            Console.WriteLine($"Loading mesh at path {path} ...");
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
            Console.WriteLine($"Loading mesh at path {path} ...");
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
                        V1 = new Vertex() { Position = vertices[0].Position, Normal = vertices[0].Normal },
                        V2 = new Vertex() { Position = vertices[1].Position, Normal = vertices[1].Normal },
                        V3 = new Vertex() { Position = vertices[2].Position, Normal = vertices[2].Normal },
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
