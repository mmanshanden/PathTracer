using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace PathTracer.Library.Graphics
{
    class ShaderProgram : GraphicsResource
    {
        private static int current = 0;

        public ShaderProgram(params ShaderArgument[] arguments)
            : base(GL.CreateProgram())
        {
            int[] shaders = new int[arguments.Length];

            // compile and attach shaders
            for (int i = 0; i < arguments.Length; i++)
            {
                shaders[i] = GL.CreateShader(arguments[i].Type);

                GL.ShaderSource(shaders[i], arguments[i].Code);
                GL.CompileShader(shaders[i]);

                GL.GetShader(shaders[i], ShaderParameter.CompileStatus, out int shaderstatus);
                if (shaderstatus == 0)
                {
                    string info = GL.GetShaderInfoLog(shaders[i]);
#if DEBUG
                    Console.WriteLine(info);
                    return;
#else
                    throw new ShaderCompileException(info);
#endif
                }

                GL.AttachShader(this.Handle, shaders[i]);
            }

            GL.LinkProgram(this.Handle);

            GL.GetProgram(this.Handle, GetProgramParameterName.LinkStatus, out int linkstatus);
            if (linkstatus == 0)
            {
                string info = GL.GetProgramInfoLog(this.Handle);
#if DEBUG
                Console.WriteLine(info);
                return;
#else
                    throw new ShaderCompileException(info);
#endif
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                GL.DeleteShader(shaders[i]);
            }
        }

        public void Use()
        {
            if (this.Handle != current)
            {
                GL.UseProgram(this.Handle);
                current = this.Handle;
            }
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteProgram(this.Handle);
        }
    }
}
