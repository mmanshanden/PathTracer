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

        // private static readonly Vector4 SkyColor = Vector4.Zero;
        private static readonly Vector4 SkyColor = new Vector4(142.0f / 255.0f, 178.0f / 255.0f, 237.0f / 255.0f, 1);

        private readonly Window window;
        
        private ShaderProgram quad;
        private Wavefront wavefront;
        private Image screen;

        private Uniform<State.RenderState> renderState;
        private Uniform<State.FrameState> frameState;

        private Camera camera;
        private Scene scene;

        public int Samples => this.frameState.Data.Samples;

        public Game(Window window)
        {
            this.window = window;
        }

        public void Initialize()
        {
            this.quad = new ShaderProgram(
                ShaderArgument.Load(ShaderType.VertexShader, "Shaders/Quad/vertex.glsl"),
                ShaderArgument.Load(ShaderType.FragmentShader, "Shaders/Quad/fragment.glsl"));

            this.screen = new Image(1, 1);
            this.camera = new Camera(new Vector3(0.5f, 0.5f, 4.0f), Vector3.One * 0.5f);
            this.scene = new Scene();

            this.renderState = new Uniform<State.RenderState>(0);
            this.frameState = new Uniform<State.FrameState>(1);

            this.InitializeScene();
            this.scene.CopyToDevice();

            this.wavefront = new Wavefront();
        }

        private void InitializeScene()
        {
            this.scene.AddMesh("Assets/Mesh/light.obj");
            this.scene.AddMesh("Assets/Mesh/floor.obj");

            Material test = new Material()
            {
                Color = new Vector4(0.2f, 0.7f, 0.2f, 0),
                Type = MaterialType.Dielectric,
                Index = 1.2f
            };

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var mat = Matrix4x4.CreateScale(0.8f) * Matrix4x4.CreateTranslation(x, 0, y);
                    this.scene.AddMeshNormalized("Assets/Mesh/bunny.obj", mat, test);
                }
            }
        }

        public void Resize(int width, int height)
        {
            this.frameState.Data.Samples = 0;

            this.screen.Dispose();
            this.screen = new Image(width, height);

            this.wavefront.Allocate(width * height);            

            this.renderState.Data.Screen.Width = width;
            this.renderState.Data.Screen.Height = height;
            this.renderState.Data.SkyColor = SkyColor;
            this.renderState.CopyToDevice();
        }

        public void Update(float dt, KeyboardState keystate)
        {
            if (keystate.IsKeyDown(Key.W))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveForward(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.S))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveForward(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.A))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveRight(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.D))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveRight(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Q))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveUp(MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.E))
            {
                this.frameState.Data.Samples = 0;
                this.camera.MoveUp(-MOVESPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Left))
            {
                this.frameState.Data.Samples = 0;
                this.camera.RotateRight(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Right))
            {
                this.frameState.Data.Samples = 0;
                this.camera.RotateRight(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Up))
            {
                this.frameState.Data.Samples = 0;
                this.camera.RotateUp(TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.Down))
            {
                this.frameState.Data.Samples = 0;
                this.camera.RotateUp(-TURNSPEED * dt);
            }

            if (keystate.IsKeyDown(Key.R))
            {
                this.frameState.Data.Samples = 0;
                this.window.ClientSize = new OpenTK.Size(512, 512);
                this.camera = new Camera(new Vector3(0.5f, 0.5f, 4.0f), Vector3.One * 0.5f);
            }

            this.camera.SetUniform(this.frameState);
        }

        public void Draw(float dt)
        {
            this.frameState.CopyToDevice();
            this.frameState.Data.Frames++;
            this.frameState.Data.Samples++;

            this.wavefront.Invoke();

            this.quad.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        public void Dispose()
        {
            this.quad.Dispose();
            this.screen.Dispose();
        }
    }
}
