using OpenTK.Graphics.OpenGL4;

namespace PathTracer.Library.Graphics
{
    class Image : Texture
    {
        public Image(int width, int height, int unit)
            : base(width, height, unit)
        {
            GL.BindImageTexture(
                this.Unit,
                this.Handle,
                0,
                false,
                0,
                TextureAccess.ReadWrite,
                SizedInternalFormat.Rgba32f
            );
        }

        public Image(int width, int height) 
            : this(width, height, 0)
        { }
    }
}
