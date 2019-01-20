using PathTracer.Library.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace PathTracer.Library.Geometry
{
    class Mesh
    {
        private readonly List<Vector3> positions;
        private readonly List<Vector3> normals;
        private readonly List<Vector2> texcoords;

        private readonly List<Group> groups;

        public bool Normalized { get; set; }
        public Matrix4x4 Transform { get; set; }

        private Vector3 min;
        private Vector3 max;

        public IEnumerable<Group> Groups
        {
            get
            {
                for (int i = 0; i < this.groups.Count; i++)
                {
                    yield return this.groups[i];
                }
            }
        }

        private Mesh()
        {
            this.positions = new List<Vector3>();
            this.normals = new List<Vector3>();
            this.texcoords = new List<Vector2>();

            this.groups = new List<Group>();

            this.min = new Vector3(float.PositiveInfinity);
            this.max = new Vector3(float.NegativeInfinity);

            this.Normalized = false;
            this.Transform = Matrix4x4.Identity;
        }

        public Vector3 GetPosition(int i)
        {
            Vector3 p = this.positions[i];

            if (this.Normalized)
            {
                p = (p - this.min) / (this.max - this.min).MaxValue();
            }

            return Vector3.Transform(p, this.Transform);
        }

        public Vector2 GetTexcoord(int j)
        {
            return this.texcoords[j];
        }

        public Vector3 GetNormal(int k)
        {
            return Vector3.TransformNormal(this.normals[k], this.Transform).Normalized();
        }

        public static Mesh LoadFromFile(string path)
        {
            Mesh mesh = new Mesh();
            var faces = new List<Face>();
            var materials = new Dictionary<string, Material>();

            Material material = new Material();
            string name = string.Empty;

            using (StreamReader reader = new StreamReader(path))
            {
                string line = reader.ReadLine();

                while (line != null)
                {
                    ReadObjLine(line, out string key, out string arg);

                    switch (key)
                    {
                        case "v":
                            Vector3 position = ReadVector3(arg);
                            mesh.positions.Add(position);
                            mesh.min = Vector3.Min(mesh.min, position);
                            mesh.max = Vector3.Max(mesh.max, position);
                            break;

                        case "vt":
                            Vector2 texcoord = ReadVector2(arg);
                            mesh.texcoords.Add(texcoord);
                            break;

                        case "vn":
                            Vector3 normal = ReadVector3(arg);
                            mesh.normals.Add(normal);
                            break;

                        case "f":
                            string[] parts = ReadParts(arg);
                            Frame[] frames = new Frame[parts.Length];

                            for (int idx = 0; idx < frames.Length; idx++)
                            {
                                Frame f = ReadFrame(parts[idx]);

                                int i = f.I < 0 ? f.I + mesh.positions.Count : f.I - 1;
                                int j = f.J < 0 ? f.J + mesh.texcoords.Count : f.J - 1;
                                int k = f.K < 0 ? f.K + mesh.normals.Count : f.K - 1;

                                frames[idx] = new Frame(i, j, k);
                            }

                            faces.Add(new Face(mesh, frames));
                            break;
                        case "mtllib":
                            string mtlpath = Path.Join(Path.GetDirectoryName(path), arg);

                            foreach (var mtl in ReadMtlFile(mtlpath))
                            {
                                if (materials.ContainsKey(mtl.Key))
                                {
                                    materials[mtl.Key] = mtl.Value;
                                }
                                else
                                {
                                    materials.Add(mtl.Key, mtl.Value);
                                }
                            }

                            break;
                        case "g":
                            if (faces.Count > 0)
                            {
                                mesh.groups.Add(new Group(faces, name, material));
                                faces = new List<Face>();
                            }

                            name = arg;
                            break;
                        case "usemtl":
                            if (faces.Count > 0)
                            {
                                mesh.groups.Add(new Group(faces, name, material));
                                faces = new List<Face>();
                            }

                            if (materials.ContainsKey(arg))
                            {
                                material = materials[arg];
                            }
                            else
                            {
                                material = new Material();
                            }
                            break;
                    }

                    line = reader.ReadLine();
                }

                // add remaining faces
                if (faces.Count > 0)
                {
                    mesh.groups.Add(new Group(faces, name, material));
                }
            }

            return mesh;
        }

        private static Dictionary<string, Material> ReadMtlFile(string path)
        {
            var materials = new Dictionary<string, Material>();

            using (StreamReader reader = new StreamReader(path))
            {
                string line = reader.ReadLine();
                string name = string.Empty;
                Material material = new Material();

                while (line != null)
                {
                    ReadObjLine(line, out string key, out string arg);

                    switch (key)
                    {
                        case "newmtl":
                            materials.Add(name, material);

                            name = arg;
                            material = new Material();
                            break;
                        case "Ka":
                            material.EmissiveColor = ReadVector3(arg);
                            break;
                        case "Kd":
                            material.DiffuseColor = ReadVector3(arg);
                            break;
                        case "Ke":
                            material.EmissiveColor = ReadVector3(arg);
                            break;
                        case "Ks":
                            material.SpecularColor = ReadVector3(arg);
                            break;
                        case "Ns":
                            material.SpecularExponent = ReadFloat(arg);
                            break;
                        case "Tr":
                            material.Transparency = ReadFloat(arg);
                            break;
                        case "d":
                            material.Transparency = 1.0f - ReadFloat(arg);
                            break;
                    }

                    line = reader.ReadLine();
                }

                materials.Add(name, material);
            }

            return materials;
        }

        private static string[] ReadParts(string s)
        {
            return s.Trim().Replace('\t', ' ').Split(' ').Where(str => str != "").ToArray();
        }

        private static void ReadObjLine(string s, out string key, out string arg)
        {
            key = string.Empty;
            arg = string.Empty;

            if (s == string.Empty)
            {
                return;
            }

            string[] parts = s.Replace('\t', ' ').Trim().Split(' ', 2);

            if (parts.Length < 2)
            {
                return;
            }

            key = parts[0].Trim();
            arg = parts[1].Trim();
        }

        private static Vector2 ReadVector2(string s)
        {
            string[] parts = ReadParts(s);

            if (parts.Length < 2)
            {
                throw new InvalidDataException("Cannot parse .obj file");
            }

            return new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture)
            );
        }

        private static Vector3 ReadVector3(string s)
        {
            string[] parts = ReadParts(s);

            if (parts.Length < 3)
            {
                throw new InvalidDataException("Cannot parse .obj file");
            }

            return new Vector3(
                ReadFloat(parts[0]),
                ReadFloat(parts[1]),
                ReadFloat(parts[2])
            );
        }

        private static float ReadFloat(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        private static Frame ReadFrame(string s)
        {
            string[] parts = s.Split('/');

            int i = 0;
            int j = 0;
            int k = 0;

            switch (parts.Length)
            {
                case 1:
                    i = int.Parse(parts[0]);
                    break;
                case 2:
                    i = int.Parse(parts[0]);
                    j = int.Parse(parts[1]);
                    break;
                case 3:
                    i = int.Parse(parts[0]);

                    if (parts[1] != string.Empty)
                    {
                        j = int.Parse(parts[1]);
                    }

                    k = int.Parse(parts[2]);
                    break;
            }

            return new Frame(i, j, k);
        }
    }
}
