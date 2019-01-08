using OpenTK.Graphics.OpenGL4;
using System.IO;

namespace PathTracer.Library.Graphics
{
    class ShaderArgument
    {
        public ShaderType Type { get; protected set; }
        public string Code { get; protected set; }

        public ShaderArgument(ShaderType type, string code)
        {
            this.Type = type;
            this.Code = code;
        }

        public static ShaderArgument Load(ShaderType type, string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                return new ShaderArgument(type, reader.ReadToEnd());
            }
        }
    }
}
