using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Model.OBJ {
    public struct ObjData(IList<Vector3> positions, IList<Vector3> normals, IList<Vector2> uv, IList<Submodel> submodels): IEquatable<ObjData>, ICloneable<ObjData> {
        public IList<Vector3> Positions = positions;
        public IList<Vector3> Normals = normals;
        public IList<Vector2> UV = uv;
        public IList<Submodel> Groups = submodels;

        public ObjData Clone() => new(Positions.ToList(), Normals.ToList(), UV.ToList(), Groups.Select(x => x.Clone()).ToList());

        public bool Equals(ObjData other) => 
            Equality.ListEquals(Positions, other.Positions)
            && Equality.ListEquals(Normals, other.Normals)
            && Equality.ListEquals(UV, other.UV)
            && Equality.ListEquals(Groups, other.Groups);

        public override bool Equals(object obj) => obj is ObjData other && Equals(other);
        public static bool operator ==(ObjData left, ObjData right) {
            return left.Equals(right);
        }

        public static bool operator !=(ObjData left, ObjData right) {
            return !(left == right);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Positions, Normals, UV, Groups);
        }
    }
    public struct Submodel : IEquatable<Submodel>, ICloneable<Submodel> {
        public IList<Face> Faces;
        public string Name;
        public MTL Material;

        public Submodel() {
            Faces = new List<Face>();
            Name = null;
            Material = default;
        }
        public Submodel(IList<Face> faces, string name, MTL material) {
            Faces = faces;
            Name = name;
            Material = material;
        }
        public Submodel(string name) {
            Name = name;
            Material = default;
            Faces = new List<Face>();
        }

        public Submodel Clone() {
            return new Submodel(Faces.ToList(), Name, Material);
        }

        public bool Equals(Submodel other) => Equality.ListEquals(Faces, other.Faces) && Name == other.Name && Material == other.Material;
    }
    public struct Face(IList<FaceVertex> vertices): IEquatable<Face>, ICloneable<Face> {
        public IList<FaceVertex> Vertices = vertices;

        public Face Clone() => new(Vertices.ToList());

        public bool Equals(Face other) => Equality.ListEquals<FaceVertex>(Vertices, other.Vertices);
    }
    public struct FaceVertex(int vertexID, int normalID, int uvID) : IEquatable<FaceVertex> {
        public int VertexID = vertexID;
        public int NormalID = normalID;
        public int UVID = uvID;

        public static FaceVertex Parse(string element) {
            int value = int.Parse(element);
            return new FaceVertex(value, value, value);
        }

        public bool Equals(FaceVertex other) => VertexID == other.VertexID && NormalID == other.NormalID && UVID == other.UVID;
        public override bool Equals(object? other) => (other is FaceVertex fv) && Equals(fv);
        public static bool operator ==(FaceVertex left, FaceVertex right) => left.Equals(right);

        public static bool operator !=(FaceVertex left, FaceVertex right) => !(left == right);

        public override int GetHashCode() => HashCode.Combine(VertexID, NormalID, UVID);
    }
}
