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
        private readonly Buffer<Node> nodebuffer;
        private readonly Buffer<int> indexbuffer;

        private readonly Dictionary<Material, int> lut;

        private readonly List<Primitive> primitives;
        private readonly List<int> indices;

        public Scene()
        {
            this.materials = new Buffer<Material>(1);
            this.triangles = new Buffer<Triangle>(3);
            this.nodebuffer = new Buffer<Node>(4);
            this.indexbuffer = new Buffer<int>(5);

            this.lut = new Dictionary<Material, int>();

            this.primitives = new List<Primitive>();
            this.indices = new List<int>();
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
                        this.AddTriangle(new Triangle()
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

            this.nodebuffer.Clear();
            this.indexbuffer.Clear();

            this.nodebuffer.AddRange(this.BuildAcceleration());
            this.indexbuffer.AddRange(this.indices);

            this.nodebuffer.CopyToDevice();
            this.indexbuffer.CopyToDevice();
        }

        public void Dispose()
        {
            this.materials.Dispose();
            this.triangles.Dispose();
            this.nodebuffer.Dispose();
            this.indexbuffer.Dispose();
        }

        private void AddTriangle(Triangle triangle)
        {
            this.triangles.Add(triangle);

            Box aabb = Box.FromTriangle(triangle);

            this.primitives.Add(new Primitive()
            {
                Index = this.primitives.Count,
                Bounds = aabb,
                Centroid = aabb.Centroid()
            });

            this.indices.Add(this.primitives.Count - 1);
        }

        private Node[] BuildAcceleration()
        {
            Node[] nodes = new Node[this.triangles.Count * 2 - 1];

            indices.Clear();
            for (int i = 0; i < this.triangles.Count; i++)
            {
                indices.Add(i);
            }

            nodes[0] = new Node()
            {
                Count = this.triangles.Count,
                LeftFirst = 0
            };

            Stack<int> stack = new Stack<int>();
            stack.Push(0);

            int pos = 1;
            
            while (stack.Count > 0)
            {
                ref Node node = ref nodes[stack.Pop()];

                (node.Bounds, _) = this.GetBounds(node.LeftFirst, node.Count);

                if (node.Count < 3)
                {
                    continue;
                }

                var d = node.Bounds.Domain();

                switch (d)
                {
                    case 0: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.X); break;
                    case 1: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.Y); break;
                    case 2: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.Z); break;
                }

                float cost = node.Bounds.Area() * node.Count;
                int p = node.Count / 2;

                for (int l = 1; l < node.Count; l++)
                {
                    int r = node.Count - l;

                    (Box lb, _) = this.GetBounds(node.LeftFirst, l);
                    (Box rb, _) = this.GetBounds(node.LeftFirst + l, r);

                    float c = lb.Area() * l + rb.Area() * r;

                    if (c <= cost)
                    {
                        p = l;
                        cost = c;
                    }
                }

                nodes[pos++] = new Node()
                {
                    LeftFirst = node.LeftFirst,
                    Count = p
                };

                nodes[pos++] = new Node()
                {
                    LeftFirst = node.LeftFirst + p,
                    Count = node.Count - p
                };

                node.LeftFirst = pos - 2;
                node.Count = 0;

                stack.Push(node.LeftFirst);
                stack.Push(node.LeftFirst + 1);
            }

            return nodes;
        }

        private (Box, Box) GetBounds(int from, int count)
        {
            Box bounds = Box.Negative;

            Vector4 min = new Vector4(float.PositiveInfinity);
            Vector4 max = new Vector4(float.NegativeInfinity);

            for (int i = from; i < from + count; i++)
            {
                int j = this.indices[i];

                bounds = Box.Union(this.primitives[j].Bounds, bounds);
                min = Vector4.Min(this.primitives[j].Centroid, min);
                max = Vector4.Max(this.primitives[j].Centroid, max);
            }

            return (bounds, new Box() { Min = min, Max = max });
        }

        private void Sort(int first, int count, Func<Primitive, float> selector)
        {
            Comparer<int> cmp = Comparer<int>.Create((a, b) =>
            {
                float p = selector(this.primitives[a]);
                float q = selector(this.primitives[b]);

                return p.CompareTo(q);
            });

            this.indices.Sort(first, count, cmp);
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
