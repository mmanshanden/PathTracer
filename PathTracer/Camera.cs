using PathTracer.Library.Extensions;
using PathTracer.Library.Graphics;
using System.Numerics;

namespace PathTracer
{
    class Camera
    {
        private Vector3 position;
        private Vector3 forward;
        private Vector3 right;
        private Vector3 up;
        private float focal;

        public float Focal
        {
            get => this.focal;
            set => this.focal = value;
        }

        public Camera(Vector3 position, Vector3 lookat)
        {
            this.focal = 1.0f;
            this.Set(position, lookat);
        }

        public void Set(Vector3 position, Vector3 lookat)
        {
            this.position = position;

            this.forward = Vector3.Normalize(lookat - position);
            this.right = Vector3.Cross(Vector3.UnitY, this.forward).Normalized();
            this.up = Vector3.Cross(this.forward, this.right).Normalized();
        }

        public void MoveForward(float distance)
        {
            this.position += this.forward * distance;
        }

        public void MoveRight(float distance)
        {
            this.position += this.right * distance;
        }

        public void MoveUp(float distance)
        {
            this.position += this.up * distance;
        }

        public void RotateUp(float distance)
        {
            this.Set(this.position, this.position + this.forward + this.up * distance);
        }

        public void RotateRight(float distance)
        {
            this.Set(this.position, this.position + this.forward + this.right * distance);
        }

        public void SetUniform(string variable, ShaderProgram progam)
        {
            progam.SetUniform($"{variable}.position", this.position);
            progam.SetUniform($"{variable}.forward", this.forward);
            progam.SetUniform($"{variable}.right", this.right);
            progam.SetUniform($"{variable}.up", this.up);
            progam.SetUniform($"{variable}.focal_distance", this.focal);
        }
    }
}
