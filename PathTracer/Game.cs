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

        private static readonly Vector4 SkyColor = new Vector4(142.0f / 255.0f, 178.0f / 255.0f, 237.0f / 255.0f, 1);

        private ShaderProgram quad;
        private ShaderProgram compute;
        private Image screen;

        private readonly Window window;
        private Camera camera;
        private Scene scene;

        private Uniform<State> state;

        private int width, height;

        public uint Samples => this.state.Data.Samples;

        public Game(Window window)
        {
            this.window = window;
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

        public void Initialize()
        {
            this.quad = new ShaderProgram(
                ShaderArgument.Load(ShaderType.VertexShader, "Shaders/Quad/vertex.glsl"),
                ShaderArgument.Load(ShaderType.FragmentShader, "Shaders/Quad/fragment.glsl"));

            this.compute = new ShaderProgram(
                ShaderArgument.Load(ShaderType.ComputeShader, "Shaders/compute.glsl"));

            Random rng = new Random(4);

            this.state = new Uniform<State>(1);
            this.state.Data.SkyColor = SkyColor;

            this.InitializeScene();
            this.scene.CopyToDevice();

            this.screen = new Image(1, 1);
            this.camera = new Camera(new Vector3(0.5f, 0.5f, 4.0f), Vector3.One * 0.5f);
        }

        private void InitializeScene()
        {
            Material glass = new Material()
            {
                Color = new Vector4(1, 1, 0.92f, 1),
                Type = MaterialType.Dielectric,
                Index = 1.5f
            };

            Func<float, Material> alpha_mat = (float alpha) =>
            {
                return new Material()
                {
                    Color = new Vector4(1.00f, 0.71f, 0.29f, 0.0f),
                    Type = MaterialType.Metal,
                    Alpha = alpha
                };
            };

            Material green = new Material()
            {
                Color = new Vector4(0.2f, 0.8f, 0.2f, 0.0f),
                Type = MaterialType.Metal,
                Alpha = 0.5f
            };

            Material copper1 = new Material()
            {
                Color = new Vector4(0.95f, 0.64f, 0.54f, 0.0f),
                Type = MaterialType.Metal,
                Alpha = 0.1f
            };

            Material copper2 = new Material()
            {
                Color = new Vector4(0.95f, 0.64f, 0.54f, 0.0f),
                Type = MaterialType.Metal,
                Alpha = 0.2f
            };

            this.scene = new Scene();

            //this.scene.AddMesh("Assets/Mesh/floor.obj");
            this.scene.AddMesh("Assets/Mesh/light.obj");

            this.scene.GenerateTiledFoloor(copper1, copper2);

            for (int i = 0; i < 10; i++)
            {
                var mat = Matrix4x4.CreateScale(0.8f, Vector3.One * 0.5f) * Matrix4x4.CreateTranslation(-5 + i, 0, 0);
                this.scene.AddMeshNormalized("Assets/Mesh/cube.obj", mat, alpha_mat(0.1f * i));
            }
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;

            this.screen.Dispose();
            this.screen = new Image(width, height);

            this.state.Data.Samples = 0;
            this.state.Data.Screen.ReciprocalWidth = 1.0f / width;
            this.state.Data.Screen.ReciprocalHeight = 1.0f / height;
            this.state.Data.Screen.AspectRatio = (float)width / height;
        }

        public void Update(float dt, KeyboardState keystate)
        {
            if (keystate.IsKeyDown(Key.W))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveForward(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.S))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveForward(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.A))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveRight(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.D))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveRight(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Q))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveUp(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.E))
            {
                this.state.Data.Samples = 0;
                this.camera.MoveUp(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Left))
            {
                this.state.Data.Samples = 0;
                this.camera.RotateRight(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Right))
            {
                this.state.Data.Samples = 0;
                this.camera.RotateRight(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Up))
            {
                this.state.Data.Samples = 0;
                this.camera.RotateUp(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Down))
            {
                this.state.Data.Samples = 0;
                this.camera.RotateUp(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.R))
            {
                this.state.Data.Samples = 0;
                this.window.ClientSize = new OpenTK.Size(512, 512);
                this.camera = new Camera(new Vector3(0.5f, 0.5f, 4.0f), Vector3.One * 0.5f);
            }

            this.camera.SetUniform(this.state);
        }

        public void Draw(float dt)
        {
            this.state.CopyToDevice();
            this.state.Data.Frame++;
            this.state.Data.Samples++;

            this.compute.Use();
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
            this.scene.Dispose();
        }
    }
}
