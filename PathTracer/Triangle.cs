using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    struct Triangle
    {
        [FieldOffset(00)] public int I;
        [FieldOffset(04)] public int J;
        [FieldOffset(08)] public int K;
        [FieldOffset(12)] public int Material;
    }
}
