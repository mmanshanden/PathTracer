using PathTracer.Library.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PathTracer
{
    class Accelerator
    {
        const float Epsilon = 0.00001f;
        const int LeafNodeSize = 3;
        const int BucketCount = 8;

        private class Primitive
        {
            public int Index;
            public Box Bounds;
            public Vector3 Centroid;
        }

        private readonly List<Triangle> references;
        private readonly List<Primitive> primitives;

        private readonly Buffer<Triangle> triangles;
        private readonly Buffer<Node> nodes;

        public int Count => this.primitives.Count;

        public Accelerator(Buffer<Triangle> triangles, Buffer<Node> nodes)
        {
            this.references = new List<Triangle>();
            this.primitives = new List<Primitive>();

            this.triangles = triangles;
            this.nodes = nodes;
        }

        public void AddTriangle(Triangle triangle)
        {
            this.references.Add(triangle);

            Box box = Box.FromTriangle(triangle);
            this.primitives.Add(new Primitive()
            {
                Index = this.references.Count - 1,
                Bounds = box,
                Centroid = box.Centroid()
            });
        }

        public void Build()
        {
            Console.WriteLine($"Building bvh containing {this.primitives.Count} primitives ...");

            this.nodes.Clear();
            this.triangles.Clear();

            this.nodes.Add(new Node()
            {
                Bounds = this.Bounds(0, this.primitives.Count),
                LeftFirst = 0,
                Count = this.primitives.Count
            });

            this.Subdivide(0);

            for (int i = 0; i < this.primitives.Count; i++)
            {
                int j = this.primitives[i].Index;
                this.triangles.Add(this.references[j]);
            }
        }

        private void Subdivide(int index)
        {
            if (this.nodes[index].Count < LeafNodeSize)
            {
                return;
            }

            int p = this.Parition(this.nodes[index]);

            if (p < 0)
            {
                return;
            }

            this.nodes.Add(new Node()
            {
                Bounds = this.Bounds(this.nodes[index].LeftFirst, p),
                LeftFirst = this.nodes[index].LeftFirst,
                Count = p
            });

            this.nodes.Add(new Node()
            {
                Bounds = this.Bounds(this.nodes[index].LeftFirst + p, this.nodes[index].Count - p),
                LeftFirst = this.nodes[index].LeftFirst + p,
                Count = this.nodes[index].Count - p
            });

            this.nodes[index] = new Node()
            {
                Bounds = this.nodes[index].Bounds,
                LeftFirst = this.nodes.Count - 2,
                Count = 0
            };

            this.Subdivide(this.nodes[index].LeftFirst);
            this.Subdivide(this.nodes[index].LeftFirst + 1);
        }

        public void Clear()
        {
            this.references.Clear();
            this.primitives.Clear();
        }

        private int Parition(Node node)
        {
            Func<Vector3, float> axis;

            switch (node.Bounds.Domain())
            {
                case 0: axis = (v => v.X); break;
                case 1: axis = (v => v.Y); break;
                default: axis = (v => v.Z); break;
            }

            Box bounds = this.CentroidBounds(node.LeftFirst, node.Count);

            float k0 = axis(bounds.Min);
            float l = axis(bounds.Max) - k0;

            if (l < Epsilon)
            {
                return -1;
            }

            float k1 = (BucketCount - Epsilon) / l;

            var bins = new List<Primitive>[BucketCount];
            var domains = new Box[BucketCount];

            for (int i = 0; i < BucketCount; i++)
            {
                bins[i] = new List<Primitive>();
                domains[i] = Box.Negative;
            }

            // bin primitives
            for (int i = node.LeftFirst; i < node.LeftFirst + node.Count; i++)
            {
                float c = axis(this.primitives[i].Centroid);
                float b = (c - k0) * k1;
                int j = (int)b;

                bins[j].Add(this.primitives[i]);
                domains[j] = Box.Union(domains[j], this.primitives[i].Bounds);
            }
            
            int partition = -1;
            float cost = node.Bounds.Area() * node.Count;

            int ln = 0, rn = node.Count;

            // select best bin split
            for (int i = 1; i < BucketCount; i++)
            {
                ln += bins[i - 1].Count;
                rn -= bins[i - 1].Count;

                Box lb = Box.Negative;
                Box rb = Box.Negative;

                for (int j = 0; j < i; j++)
                {
                    lb = Box.Union(domains[j], lb);
                }

                for (int j = i; j < BucketCount; j++)
                {
                    rb = Box.Union(domains[j], rb);
                }

                float c = ln * lb.Area() + rn * rb.Area();

                if (c < cost)
                {
                    partition = ln;
                    cost = c;
                }
            }

            if (partition < 0)
            {
                return -1;
            }

            int p = node.LeftFirst;

            for (int i = 0; i < BucketCount; i++)
            {
                for (int j = 0; j < bins[i].Count; j++)
                {
                    this.primitives[p++] = bins[i][j];
                }
            }

            return partition;
        }

        private Box Bounds(int first, int count)
        {
            Box bounds = Box.Negative;

            for (int i = first; i < first + count; i++)
            {
                bounds = Box.Union(this.primitives[i].Bounds, bounds);
            }

            return bounds;
        }

        private Box CentroidBounds(int first, int count)
        {
            Vector3 min = new Vector3(float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity);

            for (int i = first; i < first + count; i++)
            {
                min = Vector3.Min(this.primitives[i].Centroid, min);
                max = Vector3.Max(this.primitives[i].Centroid, max);
            }

            return new Box()
            {
                Max = max,
                Min = min
            };
        }

        private void Sort(int first, int count, Func<Primitive, float> selector)
        {
            Comparer<Primitive> cmp = Comparer<Primitive>.Create((a, b) =>
            {
                float p = selector(a);
                float q = selector(b);

                return p.CompareTo(q);
            });

            this.primitives.Sort(first, count, cmp);
        }
    }
}
