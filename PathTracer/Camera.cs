using OpenTK.Input;
using PathTracer.Library.Extensions;
using PathTracer.Library.Graphics;
using System;
using System.Numerics;

namespace PathTracer
{
    class Camera
    {
        private const float MoveSpeed = 5.0f;
        private const float RotateSpeed = 1.5f;

        private readonly Window window;
        private readonly Scene scene;

        private Vector3 position;
        private Vector3 target;
        private Vector3 forward;
        private Vector3 right;
        private Vector3 up;
        private Vector3 p1, p2, p3;
        private float focal;

        public float Focal
        {
            get => this.focal;
        }

        public Camera(Window window, Scene scene)
        {
            this.window = window;
            this.scene = scene;

            this.focal = 1.0f;
            this.Set(Vector3.One, Vector3.Zero);
        }

        public void Set(Vector3 position, Vector3 target)
        {
            this.position = position;
            this.target = target;
            this.Update();
        }

        private void Update()
        {
            this.forward = Vector3.Normalize(this.target - this.position);
            this.right = Vector3.Cross(Vector3.UnitY, this.forward).Normalized();
            this.up = Vector3.Cross(this.forward, this.right).Normalized();
            this.focal = this.GetFocalDistance();

            float ar = this.window.Width / (float)this.window.Height;

            Vector3 c = this.position + this.focal * this.forward;
            this.p1 = c - 0.5f * this.focal * ar * this.right + 0.5f * this.focal * this.up;
            this.p2 = c + 0.5f * this.focal * ar * this.right + 0.5f * this.focal * this.up;
            this.p3 = c - 0.5f * this.focal * ar * this.right - 0.5f * this.focal * this.up;

            this.target = this.position + this.forward;
        }

        public bool HandleInput(KeyboardState state, float dt)
        {
            bool changed = false;

            float move = MoveSpeed * dt;
            float rotate = RotateSpeed * dt;

            if (state[Key.A]) { changed = true; this.position -= this.right * move; this.target -= this.right * move; }
            if (state[Key.D]) { changed = true; this.position += this.right * move; this.target += this.right * move; }
            if (state[Key.W]) { changed = true; this.position += this.forward * move; this.target += this.forward * move; }
            if (state[Key.S]) { changed = true; this.position -= this.forward * move; this.target -= this.forward * move; }
            if (state[Key.Q]) { changed = true; this.position += this.up * move; this.target += this.up * move; }
            if (state[Key.E]) { changed = true; this.position -= this.up * move; this.target -= this.up * move; }
            if (state[Key.Up]) { changed = true; this.target += this.up * rotate; }
            if (state[Key.Down]) { changed = true; this.target -= this.up * rotate; }
            if (state[Key.Left]) { changed = true; this.target -= this.right * rotate; }
            if (state[Key.Right]) { changed = true; this.target += this.right * rotate; }

            if (changed)
            {
                this.Update();
            }

            return changed;
        }

        private float GetFocalDistance()
        {
            var r = new Ray(this.position, this.forward);
            var i = this.scene.Intersect(ref r);

            return MathF.Min(i.Distance, 20);
        }

        public void SetUniform(Uniform<State.FrameState> state)
        {
            state.Data.Camera.P1 = this.p1;
            state.Data.Camera.P2 = this.p2;
            state.Data.Camera.P3 = this.p3;
            state.Data.Camera.Position = this.position;
            state.Data.Camera.Right = this.right;
            state.Data.Camera.Up = this.up;
        }
    }
}
