using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 112)]
    struct Triangle
    {
        [FieldOffset(00)] public Vertex V1;
        [FieldOffset(32)] public Vertex V2;
        [FieldOffset(64)] public Vertex V3;
        [FieldOffset(96)] public int Material;
    }
}
