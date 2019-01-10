using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct Sphere
    {
        [FieldOffset(00)] public Vector4 CenterRadius;
        [FieldOffset(16)] public int MaterialIndex;
    }
}
