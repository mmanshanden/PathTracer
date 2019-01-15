using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    struct Vertex
    {
        [FieldOffset(00)] public Vector4 Position;
    }
}
