using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace PathTracer.Library.Graphics
{
    class Atomic : BindableResource
    {
        private int binding;

        public Atomic(int binding)
            : base(GL.GenBuffer(), BufferTarget.AtomicCounterBuffer)
        {
            this.binding = binding;

            this.Bind();

            GL.BindBufferBase(
                BufferRangeTarget.AtomicCounterBuffer,
                this.binding,
                this.Handle);

            GL.BufferData(
                BufferTarget.AtomicCounterBuffer,
                4,
                IntPtr.Zero,
                BufferUsageHint.StaticRead);

            Console.WriteLine($"Initializing Atomic (binding={this.binding})");
        }

        public void Reset()
        {
            this.Bind();
            uint zero = 0;

            GL.BufferSubData(
                BufferTarget.AtomicCounterBuffer,
                IntPtr.Zero,
                4,
                ref zero);
        }

        public uint Read()
        {
            this.Bind();

            uint val = 0;
            GL.GetBufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, 4, ref val);

            return val;
        }

        protected override void BindGraphicsResource()
        {
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, this.Handle);
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteBuffer(this.Handle);
        }
    }
}
