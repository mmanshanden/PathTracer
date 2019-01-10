using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using PathTracer.Library.Graphics;
using System;
using System.Collections.Generic;
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
        private Buffer<Material> materials;
        private Buffer<Sphere> spheres;

        private Camera camera;

        private uint frame, samples;

        public uint Samples => this.samples;

        public Game()
        {
        }


        private static IEnumerable<Material> RandomMaterials(int count, Random rng)
        {
            for (int i = 0; i < count; i++)
            {
                float r = (float)rng.NextDouble();
                float g = (float)rng.NextDouble();
                float b = (float)rng.NextDouble();

                Vector4 color = new Vector4(r, g, b, 1);
                int emissive = rng.Next(100) > 70 ? 1 : 0;

                yield return new Material()
                {
                    Color = emissive == 1 ? color * 5 : color,
                    Emissive = emissive
                };
            }
        }

        private static IEnumerable<Sphere> RandomSpheres(int count, Random rng)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * 10.0f - 10.0f;
                float y = (float)rng.NextDouble() * 10.0f + 1.2f;
                float z = (float)rng.NextDouble() * 20.0f - 10.0f;
                float r = (float)rng.NextDouble() * 1.0f + 0.2f;

                yield return new Sphere()
                {
                    CenterRadius = new Vector4(x, y, z, r),
                    MaterialIndex = rng.Next(30)
                };
            }
        }

        public void Initialize()
        {
            this.quad = new ShaderProgram(
                ShaderArgument.Load(ShaderType.VertexShader, "Shaders/Quad/vertex.glsl"),
                ShaderArgument.Load(ShaderType.FragmentShader, "Shaders/Quad/fragment.glsl"));

            this.compute = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/compute.glsl"));

            Random rng = new Random();

            this.materials = new Buffer<Material>(1);
            this.materials.Add(new Material()
            {
                Color = new Vector4(1, 1, 1, 1),
                Emissive = 0
            });

            this.spheres = new Buffer<Sphere>(2);
            this.spheres.Add(new Sphere()
            {
                CenterRadius = new Vector4(0, -1000, 0, 999.9f),
                MaterialIndex = 0
            });

            this.materials.AddRange(RandomMaterials(31, rng));
            this.spheres.AddRange(RandomSpheres(60, rng));
            
            this.materials.CopyToDevice();
            this.spheres.CopyToDevice();

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
