using System;

namespace PathTracer.Library.Graphics
{

    [Serializable]
    public class ShaderLinkException : Exception
    {
        public ShaderLinkException() { }
        public ShaderLinkException(string message) : base(message) { }
        public ShaderLinkException(string message, Exception inner) : base(message, inner) { }
        protected ShaderLinkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
