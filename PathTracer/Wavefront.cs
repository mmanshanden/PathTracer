using OpenTK.Graphics.OpenGL4;
using PathTracer.Library.Graphics;
using System;
using System.Numerics;

namespace PathTracer
{
    class Wavefront
    {
        private readonly Buffer<Vector4> rayDirection;
        private readonly Buffer<Vector4> rayOrigin;
        private readonly Buffer<Vector4> sampleThroughput;
        private readonly Buffer<Vector4> intersection;
        private readonly Buffer<uint>    atomics;

        private readonly ShaderProgram generate;
        private readonly ShaderProgram extend;
        private readonly ShaderProgram shade;

        private uint pixels;

        public Wavefront()
        {
            this.rayDirection       = new Buffer<Vector4>(0);
            this.rayOrigin          = new Buffer<Vector4>(1);
            this.sampleThroughput   = new Buffer<Vector4>(2);
            this.intersection       = new Buffer<Vector4>(3);
            this.atomics            = new Buffer<uint>(4);

            this.generate = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/generate.glsl"));
            
            this.extend = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/extend.glsl"));

            this.shade = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/shade.glsl"));
        }

        public void Invoke()
        {
            uint n = this.pixels;
            this.InvokeShader(this.generate, n);
            
            for (int i = 0; i < 20 && n > 50; i++)
            {
                this.atomics[0] = 0;
                this.atomics.CopyToDevice();
                
                this.InvokeShader(this.extend, n);
                this.InvokeShader(this.shade, n);

                this.atomics.CopyFromDevice();
                n = this.atomics[0];
            }
        }

        private void InvokeShader(ShaderProgram program, uint n)
        {
            int r = (int)MathF.Ceiling(n / 64.0f);

            program.Use();
            GL.DispatchCompute(r, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        }

        public void Allocate(int n)
        {
            this.rayOrigin.Allocate(n);
            this.rayDirection.Allocate(n);
            this.sampleThroughput.Allocate(n);
            this.intersection.Allocate(n);

            this.atomics.Clear();
            this.atomics.Add(0);
            this.atomics.Add(0);

            this.atomics.CopyToDevice();

            this.pixels = (uint)n;
        }
    }
}
