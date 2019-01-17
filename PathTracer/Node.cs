using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    struct Node
    {
        [FieldOffset(00)] public Box Bounds;
        [FieldOffset(32)] public int LeftFirst;
        [FieldOffset(36)] public int Count;

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
