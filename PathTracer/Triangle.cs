using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct Triangle
    {
        [FieldOffset(00)] public Vertex V1;
        [FieldOffset(16)] public Vertex V2;
        [FieldOffset(32)] public Vertex V3;
        [FieldOffset(48)] public int Material;
    }
}
