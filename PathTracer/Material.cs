using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct Material
    {
        [FieldOffset(00)] public Vector4 Color;
        [FieldOffset(16)] public MaterialType Type;
        [FieldOffset(20)] public float Index;

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Color, this.Type, this.Index);
        }
    }

    enum MaterialType
    {
        Diffuse = 0,
        Emissive = 1,
        Mirror = 2,
        Dielectric = 3
    }
}
