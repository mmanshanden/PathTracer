using OpenTK.Graphics.OpenGL4;
using PathTracer.Library.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace PathTracer
{
    class Game
    {
        private int width, height;

        private ShaderProgram quad;
        private ShaderProgram compute;
        private Image screen;

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
        }

        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;

            this.screen.Dispose();
            this.screen = new Image(width, height);
        }

        public void Update(float dt)
        {
            
        }

        public void Draw(float dt)
        {
            this.compute.Use();
            GL.DispatchCompute(this.width / 8, this.height / 8, 1);

            this.quad.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }
    }
}
