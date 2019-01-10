using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct Material
    {
        [FieldOffset(00)] public Vector4 Color;
        [FieldOffset(16)] public int Emissive;
    }
}
