using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct Vertex
    {
        [FieldOffset(00)] public Vector4 Position;
        [FieldOffset(16)] public Vector4 Normal;
    }
}
