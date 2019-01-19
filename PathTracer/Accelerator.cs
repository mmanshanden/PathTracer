using PathTracer.Library.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace PathTracer
{
    class Accelerator
    {
        const int LeafNodeSize = 3;

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

        private int Parition(Node node)
        {
            switch (node.Bounds.Domain())
            {
                case 0: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.X); break;
                case 1: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.Y); break;
                case 2: this.Sort(node.LeftFirst, node.Count, pr => pr.Centroid.Z); break;
            }

            float cost = node.Bounds.Area() * node.Count;

            int split = -1;

            for (int l = 0; l < node.Count; l++)
            {
                int r = node.Count - l;

                Box lb = this.Bounds(node.LeftFirst, l);
                Box rb = this.Bounds(node.LeftFirst + l, r);

                float c = lb.Area() * l + rb.Area() * r;

                if (c <= cost)
                {
                    cost = c;
                    split = l;
                }
            }

            return split;
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
