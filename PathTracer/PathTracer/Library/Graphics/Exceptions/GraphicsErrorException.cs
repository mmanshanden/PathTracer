using OpenTK.Graphics.OpenGL4;
using System;

namespace PathTracer.Library.Graphics
{

    [Serializable]
    public class GraphicsErrorException : Exception
    {
        ErrorCode ErrorCode { get; }

        public GraphicsErrorException() { }
        public GraphicsErrorException(string message) : base(message) { }
        public GraphicsErrorException(ErrorCode error) : base()
        {
            this.ErrorCode = error;
        }
        public GraphicsErrorException(string message, Exception inner) : base(message, inner) { }
        protected GraphicsErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
