using PathTracer.Library.Extensions;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracer
{
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    struct State
    {
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct ScreenState
        {
            [FieldOffset(00)] public float ReciprocalWidth;
            [FieldOffset(04)] public float ReciprocalHeight;
            [FieldOffset(08)] public float AspectRatio;
        }

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        public struct CameraState
        {
            [FieldOffset(00)] public Vector3 Position;
            [FieldOffset(16)] public Vector3 Forward;
            [FieldOffset(32)] public Vector3 Right;
            [FieldOffset(48)] public Vector3 Up;
            [FieldOffset(64)] public float FocalDistance;

            public void Set(Vector3 position, Vector3 lookat)
            {
                this.Position = position;
                this.FocalDistance = 1.0f;

                this.Forward = Vector3.Normalize(lookat - position);
                this.Right = Vector3.Cross(Vector3.UnitY, this.Forward).Normalized();
                this.Up = Vector3.Cross(this.Forward, this.Right).Normalized();
            }

            public void MoveForward(float distance)
            {
                this.Position += this.Forward * distance;
            }

            public void MoveRight(float distance)
            {
                this.Position += this.Right * distance;
            }

            public void MoveUp(float distance)
            {
                this.Position += this.Up * distance;
            }

            public void RotateUp(float distance)
            {
                this.Set(this.Position, this.Position + this.Forward + this.Up * distance);
            }

            public void RotateRight(float distance)
            {
                this.Set(this.Position, this.Position + this.Forward + this.Right * distance);
            }
        }

        [FieldOffset(000)] public uint Frame;
        [FieldOffset(004)] public uint Samples;
        [FieldOffset(016)] public ScreenState Screen;
        [FieldOffset(032)] public CameraState Camera;
        [FieldOffset(112)] public Vector4 SkyColor;
    }
}
