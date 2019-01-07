using System;

namespace PathTracer.Library.Graphics
{

    [Serializable]
    public class ShaderCompileException : Exception
    {
        public ShaderCompileException() { }
        public ShaderCompileException(string message) : base(message) { }
        public ShaderCompileException(string message, Exception inner) : base(message, inner) { }
        protected ShaderCompileException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
