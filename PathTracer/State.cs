using PathTracer.Library.Extensions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    class State
    {
        [StructLayout(LayoutKind.Explicit, Size = 96)]
        public struct CameraState
        {
            [FieldOffset(00)] public Vector3 P1;
            [FieldOffset(16)] public Vector3 P2;
            [FieldOffset(32)] public Vector3 P3;
            [FieldOffset(48)] public Vector3 Position;
            [FieldOffset(64)] public Vector3 Right;
            [FieldOffset(80)] public Vector3 Up;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct ScreenState
        {
            [FieldOffset(00)] public int Width;
            [FieldOffset(04)] public int Height;
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public struct RenderState
        {
            [FieldOffset(00)] public ScreenState Screen;
            [FieldOffset(16)] public Vector4 SkyColor;
        }

        [StructLayout(LayoutKind.Explicit, Size = 112)]
        public struct FrameState
        {
            [FieldOffset(00)] public int Frames;
            [FieldOffset(04)] public int Samples;
            [FieldOffset(08)] public float FocalDistance;
            [FieldOffset(16)] public CameraState Camera;
        }
    }
}
