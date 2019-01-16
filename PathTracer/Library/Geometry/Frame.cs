using System;
using System.Collections.Generic;
using System.Text;

namespace PathTracer.Library.Geometry
{
    struct Frame
    {
        public readonly int I;
        public readonly int J;
        public readonly int K;

        public Frame(int i, int j, int k)
        {
            this.I = i;
            this.J = j;
            this.K = k;
        }        

        public override int GetHashCode()
        {
            return HashCode.Combine(I, J, K);
        }

        public override string ToString()
        {
            return $"({I}, {J}, {K})";
        }
    }
}
