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

            this.min = Vector3.Zero;
            this.max = Vector3.Zero;
        }

        public Vector3 GetPosition(int i)
        {
            return this.positions[i];
        }

        public Vector2 GetTexcoord(int j)
        {
            return this.texcoords[j];
        }

        public Vector3 GetNormal(int k)
        {
            return this.normals[k];
        }

        public static Mesh LoadFromFile(string path)
        {
            Mesh mesh = new Mesh();
            List<Face> faces = new List<Face>();

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

                        case "g":
                        case "usemtl":
                            if (faces.Count > 0)
                            {
                                mesh.groups.Add(new Group(faces));
                                faces = new List<Face>();
                            }
                            break;
                    }

                    line = reader.ReadLine();
                }

                // add remaining faces
                if (faces.Count > 0)
                {
                    mesh.groups.Add(new Group(faces));
                }
            }

            return mesh;
        }

        private static string[] ReadParts(string s)
        {
            return s.Split(' ').Where(str => str != string.Empty).ToArray();
        }

        private static void ReadObjLine(string s, out string key, out string arg)
        {
            key = string.Empty;
            arg = string.Empty;

            if (s == string.Empty)
            {
                return;
            }

            string[] parts = s.Split(' ', 2);

            if (parts.Length < 2)
            {
                return;
            }

            key = parts[0];
            arg = parts[1];
        }

        private static Vector2 ReadVector2(string s)
        {
            string[] parts = ReadParts(s);

            if (parts.Length != 2)
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

            if (parts.Length != 3)
            {
                throw new InvalidDataException("Cannot parse .obj file");
            }

            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture)
            );
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
