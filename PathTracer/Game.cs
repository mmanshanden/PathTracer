using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using PathTracer.Library.Graphics;
using System;
using System.Numerics;

namespace PathTracer
{
    class Game : IDisposable
    {
        const float MOVESPEED = 4.5f;
        const float TURNSPEED = 1.0f;

        private int width, height;

        private ShaderProgram quad;
        private ShaderProgram compute;
        private Image screen;

        private Camera camera;

        private uint frame, samples;

        public uint Samples => this.samples;

        public Game()
        {
        }

        public void Initialize()
        {
            this.quad = new ShaderProgram(
                ShaderArgument.Load(ShaderType.VertexShader, "Shaders/Quad/vertex.glsl"),
                ShaderArgument.Load(ShaderType.FragmentShader, "Shaders/Quad/fragment.glsl"));

            this.compute = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/compute.glsl"));

            this.screen = new Image(0, 0);
            this.camera = new Camera(new Vector3(0, 2, -10), Vector3.Zero);
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;

            this.screen.Dispose();
            this.screen = new Image(width, height);
            this.samples = 0;

            this.compute.SetUniform("screen.rcp_width", 1.0f / width);
            this.compute.SetUniform("screen.rcp_height", 1.0f / height);
            this.compute.SetUniform("screen.ar", (float)width / height);
        }

        public void Update(float dt, KeyboardState keystate)
        {
            if (keystate.IsKeyDown(Key.W))
            {
                this.samples = 0;
                this.camera.MoveForward(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.S))
            {
                this.samples = 0;
                this.camera.MoveForward(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.A))
            {
                this.samples = 0;
                this.camera.MoveRight(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.D))
            {
                this.samples = 0;
                this.camera.MoveRight(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Q))
            {
                this.samples = 0;
                this.camera.MoveUp(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.E))
            {
                this.samples = 0;
                this.camera.MoveUp(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Left))
            {
                this.samples = 0;
                this.camera.RotateRight(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Right))
            {
                this.samples = 0;
                this.camera.RotateRight(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Up))
            {
                this.samples = 0;
                this.camera.RotateUp(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Down))
            {
                this.samples = 0;
                this.camera.RotateUp(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.R))
            {
                this.samples = 0;
                this.camera = new Camera(new Vector3(0, 2, -10), Vector3.Zero);
            }

            this.camera.SetUniform("camera", this.compute);
        }

        public void Draw(float dt)
        {
            this.compute.Use();
            this.compute.SetUniform("frame", this.frame++);
            this.compute.SetUniform("samples", this.samples++);
            GL.DispatchCompute(this.width / 8, this.height / 8, 1);

            this.quad.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        public void Dispose()
        {
            this.quad.Dispose();
            this.compute.Dispose();
            this.screen.Dispose();
        }
    }
}
