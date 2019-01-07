using OpenTK.Graphics.OpenGL4;
using System;

namespace PathTracer.Library.Graphics
{
    class Texture : BindableResource
    {
        public int Unit { get; set; }
        public int Width { get; }
        public int Height { get; }

        public Texture(int width, int height, int unit)
            : base(GL.GenTexture(), TextureTarget.Texture2D)
        {
            this.Unit = unit;
            this.Width = width;
            this.Height = height;

            this.Bind();

            GL.TexStorage2D(TextureTarget2d.Texture2D, 1, SizedInternalFormat.Rgba32f, width, height);
        }

        public Texture(int width, int height)
            : this(width, height, 0)
        { }

        public void SetProperty(TextureParameterName parameter, All value)
        {
            this.Bind();
            GL.TexParameter(TextureTarget.Texture2D, parameter, (int)value);
        }

        public void SetData(uint[] data)
        {
            this.Bind();
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                this.Width,
                this.Height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                data);
        }

        protected override void FreeGraphicsResource()
        {
            GL.DeleteTexture(this.Handle);
        }

        protected override void BindGraphicsResource()
        {
            GL.ActiveTexture(TextureUnit.Texture0 + this.Unit);
            GL.BindTexture(TextureTarget.Texture2D, this.Handle);
        }
    }
}
