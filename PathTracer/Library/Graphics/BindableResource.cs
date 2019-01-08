using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace PathTracer.Library.Graphics
{
    abstract class BindableResource : GraphicsResource
    {
        private readonly int target;

        private BindableResource(int handle, int target)
            : base(handle)
        {
            this.target = target;
        }

        public BindableResource(int handle, BufferTarget target)
            : this(handle, (int)target)
        { }

        public BindableResource(int handle, TextureTarget target)
            : this(handle, (int)target)
        { }

        protected abstract void BindGraphicsResource();

        public void Bind()
        {
            this.AssertNotDisposed();
            this.BindGraphicsResource();
        }


    }
}
