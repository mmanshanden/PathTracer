using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using PathTracer.Library.Geometry;
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

        private static readonly Vector4 SkyColor = new Vector4(142.0f / 255.0f, 178.0f / 255.0f, 237.0f / 255.0f, 1);

        private int width, height;

        private ShaderProgram quad;
        private ShaderProgram compute;
        private Image screen;

        private Buffer<Material> materials;
        private Buffer<Sphere> spheres;
        private Buffer<Triangle> triangles;

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
                bool emissive = rng.Next(100) > 85;

                int type = (int)MaterialType.Emissive;

                if (!emissive)
                {
                    type = rng.Next(4);

                    while (type == (int)MaterialType.Emissive)
                    {
                        type = rng.Next(4);
                    }
                }

                yield return new Material()
                {
                    Color = emissive ? color * 15 : color,
                    Type = (MaterialType)type,
                    Index = 1.2f
                };
            }
        }

        private static IEnumerable<Sphere> RandomSpheres(int count, Random rng)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)rng.NextDouble() * 5.0f - 2.5f;
                float y = (float)rng.NextDouble() * 5.0f + 1.2f;
                float z = (float)rng.NextDouble() * 5.0f - 2.5f;
                float r = (float)rng.NextDouble() * 0.6f + 0.2f;

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

            Random rng = new Random(7);

            this.materials = new Buffer<Material>(1);
            this.spheres = new Buffer<Sphere>(2);
            this.triangles = new Buffer<Triangle>(3);

            this.materials.Add(new Material()
            {
                Color = new Vector4(0.8f, 0.8f, 0.8f, 0.0f),
                Type = MaterialType.Diffuse
            });

            this.materials.Add(new Material()
            {
                Color = new Vector4(4f, 4f, 4f, 0.0f),
                Type = MaterialType.Emissive
            });

            this.triangles.AddRange(new[]
            {
                new Triangle()
                {
                    Material = 1,
                    V1 = new Vertex() { Position = new Vector4(-5, 10, -5, 0) },
                    V2 = new Vertex() { Position = new Vector4( 5, 10, -5, 0) },
                    V3 = new Vertex() { Position = new Vector4( 5, 10,  5, 0) },
                },
                new Triangle()
                {
                    Material = 1,
                    V1 = new Vertex() { Position = new Vector4( 5, 10,  5, 0) },
                    V2 = new Vertex() { Position = new Vector4(-5, 10,  5, 0) },
                    V3 = new Vertex() { Position = new Vector4(-5, 10, -5, 0) }
                },
            });

            Mesh cube = Mesh.LoadFromFile("Assets/Mesh/torus.obj");

            foreach (Group g in cube.Groups)
            {
                foreach (Face f in g.Faces)
                {
                    foreach (var t in f.Triangulate)
                    {
                        this.triangles.Add(new Triangle()
                        {
                            Material = 0,
                            V1 = new Vertex() { Position = new Vector4(t.Item1.Position, 0) },
                            V2 = new Vertex() { Position = new Vector4(t.Item2.Position, 0) },
                            V3 = new Vertex() { Position = new Vector4(t.Item3.Position, 0) }
                        });
                    }
                }
            }

            this.materials.AddRange(RandomMaterials(29, rng));
            this.spheres.AddRange(RandomSpheres(20, rng));

            this.materials.CopyToDevice();
            this.triangles.CopyToDevice();
            this.spheres.CopyToDevice();

            this.compute.SetUniform("sky_color", SkyColor);
            this.compute.SetUniform("sphere_count", this.spheres.Count);

            this.screen = new Image(1, 1);
            this.camera = new Camera(new Vector3(0, 8, -10), new Vector3(0, 0, 4));
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
                this.camera = new Camera(new Vector3(0, 8, -10), new Vector3(0, 0, 4));
            }

            this.camera.SetUniform("camera", this.compute);
        }

        public void Draw(float dt)
        {
            this.compute.Use();
            this.compute.SetUniform("frame", this.frame++);
            this.compute.SetUniform("samples", this.samples++);
            GL.DispatchCompute(this.width / 8, this.height / 8, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

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
