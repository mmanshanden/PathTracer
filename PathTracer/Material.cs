using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    struct Material
    {
        [FieldOffset(00)] public Vector4 Color;
        [FieldOffset(16)] public Vector4 Emissive;
        [FieldOffset(32)] public MaterialType Type;
        [FieldOffset(36)] public float Index;
        [FieldOffset(40)] public float Alpha;

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
        Dielectric = 3,
        Metal = 4
    }
}
