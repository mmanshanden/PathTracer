using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct Node
    {
        [FieldOffset(00)] public Box Bounds;
        [FieldOffset(24)] public int LeftFirst;
        [FieldOffset(28)] public int Count;

        public override string ToString()
        {
            if (Count == 0)
            {
                return $"Node";
            }
            else
            {
                return $"Leaf";
            }
        }
    }
}
