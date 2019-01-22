using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace PathTracer.Library.Graphics
{
    class Uniform<T> : BindableResource
        where T : struct
    {
        private int binding;
        private int stride;

        private T data;

        public ref T Data => ref this.data;

        public Uniform(int binding)
            : base(GL.GenBuffer(), BufferTarget.UniformBuffer)
        {
            this.binding = binding;
            this.stride = Marshal.SizeOf<T>();

            this.data = default(T);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, this.binding, this.Handle);

            GL.BufferData(
                BufferTarget.UniformBuffer, 
                this.stride, 
                IntPtr.Zero, 
                BufferUsageHint.StaticRead);
            
            Console.WriteLine($"Initializing Uniform<{this.data.GetType().Name}>\t\t(binding={this.binding}, stride={this.stride})");
        }

        public void CopyToDevice()
        {
            GL.BufferSubData(
                BufferTarget.UniformBuffer, 
                IntPtr.Zero, 
                this.stride, 
                ref data);
        }

        protected override void BindGraphicsResource()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, this.Handle);
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteBuffer(this.Handle);
        }
    }
}
