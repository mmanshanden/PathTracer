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

        private readonly ShaderProgram generate;
        private readonly ShaderProgram extend;
        private readonly ShaderProgram shade;

        private readonly Atomic atomic;

        private uint pixels;

        public Wavefront()
        {
            this.rayDirection       = new Buffer<Vector4>(0);
            this.rayOrigin          = new Buffer<Vector4>(1);
            this.sampleThroughput   = new Buffer<Vector4>(2);
            this.intersection       = new Buffer<Vector4>(3);

            this.generate = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/generate.glsl"));
            
            this.extend = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/extend.glsl"));

            this.shade = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/Wavefront/shade.glsl"));

            this.atomic = new Atomic(0);
        }

        public void Invoke()
        {
            uint n = this.pixels;
            this.InvokeShader(this.generate, n);

            int i;

            for (i = 0; i < 20 && n > 50; i++)
            {
                this.atomic.Reset();
                
                this.InvokeShader(this.extend, n);
                this.InvokeShader(this.shade, n);

                n = this.atomic.Read();
            }

            Console.WriteLine(i);
        }

        private void InvokeShader(ShaderProgram program, uint n)
        {
            int r = (int)MathF.Ceiling(n / 32.0f);

            program.Use();
            GL.DispatchCompute(r, 1, 1);
        }

        public void Allocate(int n)
        {
            this.rayOrigin.Allocate(n);
            this.rayDirection.Allocate(n);
            this.sampleThroughput.Allocate(n);
            this.intersection.Allocate(n);

            this.pixels = (uint)n;
        }
    }
}
