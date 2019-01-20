using System.Numerics;

namespace PathTracer.Library.Geometry
{
    class Material
    {
        public Vector3 AmbientColor; // ka
        public Vector3 DiffuseColor; // kd
        public Vector3 SpecularColor; // ks
        public Vector3 EmissiveColor; // ke
        public float SpecularExponent;
        public float Transparency;

        public Material()
        {
            this.AmbientColor = new Vector3(0);
            this.DiffuseColor = new Vector3(1);
            this.SpecularColor = new Vector3(1);
            this.EmissiveColor = new Vector3(0);
            this.SpecularExponent = 0;
            this.Transparency = 0;
        }
    }

    enum IlluminationModel
    {
        Diffuse = 0,
        DiffuseAmbient = 1,
        Specular = 2,
        Reflective = 3,
    }
}
