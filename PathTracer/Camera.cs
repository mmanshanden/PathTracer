using OpenTK.Input;
using PathTracer.Library.Extensions;
using PathTracer.Library.Graphics;
using System;
using System.Numerics;

namespace PathTracer
{
    class Camera
    {
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
            set => this.focal = value;
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
            p1 = c - 0.5f * this.focal * ar * this.right + 0.5f * this.focal * this.up;
            p2 = c + 0.5f * this.focal * ar * this.right + 0.5f * this.focal * this.up;
            p3 = c - 0.5f * this.focal * ar * this.right - 0.5f * this.focal * this.up;
        }

        public bool HandleInput(KeyboardState state)
        {
            bool changed = false;

            if (state[Key.A]) { changed = true; this.position -= this.right * 0.1f; this.target -= this.right * 0.1f; }
            if (state[Key.D]) { changed = true; this.position += this.right * 0.1f; this.target += this.right * 0.1f; }
            if (state[Key.W]) { changed = true; this.position += this.forward * 0.1f; }
            if (state[Key.S]) { changed = true; this.position -= this.forward * 0.1f; }
            if (state[Key.R]) { changed = true; this.position += this.up * 0.1f; this.target += this.up * 0.1f; }
            if (state[Key.F]) { changed = true; this.position -= this.up * 0.1f; this.target -= this.up * 0.1f; }
            if (state[Key.Up]) { changed = true; this.target += this.up * 0.3f; }
            if (state[Key.Down]) { changed = true; this.target -= this.up * 0.3f; }
            if (state[Key.Left]) { changed = true; this.target -= this.right * 0.3f; }
            if (state[Key.Right]) { changed = true; this.target += this.right * 0.3f; }

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
