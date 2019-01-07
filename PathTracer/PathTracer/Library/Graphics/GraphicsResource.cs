using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;

namespace PathTracer.Library.Graphics
{
    abstract class GraphicsResource : IDisposable
    {
        public int Handle { get; private set; }
        public bool IsDisposed { get; private set; }

        protected GraphicsResource(int handle)
        {
            this.Handle = handle;
            this.IsDisposed = false;
        }

        ~GraphicsResource()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes of the GraphicsResource, releasing all resources consumed by it.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Throws an exception when the object has been disposed.
        /// </summary>
        public void AssertNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name, "Object was already disposed.");
            }
        }

        /// <summary>
        /// Frees up the consumed GL handle(s).
        /// </summary>
        protected abstract void FreeGraphicsResource();

        private void Dispose(bool manual)
        {
            if (!this.IsDisposed)
            {
                if (manual && GraphicsContext.CurrentContext != null)
                {
                    this.FreeGraphicsResource();
                    this.IsDisposed = true;
                    this.Handle = -1;
                }
                else
                {
                    // object was unsafely disposed.
                    Debug.Print($"{this.GetType().Name} could not be disposed. All GraphicsResource " +
                        $"objects must be disposed manually to prevent leaks.");
                }
            }
        }

        public static void AssertNoError()
        {
            ErrorCode error;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                throw new GraphicsErrorException(error);
            }
        }
    }

}
