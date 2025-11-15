using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model.OBJ;

namespace TranSimCS.Model {
    /// <summary>
    /// A builder for a <see cref="MeshElement{TMaterial, TVertex}"/>
    /// </summary>
    /// <typeparam name="TMaterial"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public class MeshBuilder<TMaterial, TVertex>: ICloneable<MeshBuilder<TMaterial, TVertex>>  {
        public TMaterial Material;
        public readonly List<TVertex> Verts = [];
        public readonly List<MeshTri> Tris = [];
        public string Name = "";
        public bool IsVisible = true;
        public IVertexProcessor<TMaterial, TVertex>? VertexProcessor;

        public int AddVertex(TVertex vertex) {
            Verts.Add(vertex);
            return Verts.Count - 1;
        }
        public int AddVertices(TVertex[] verts) {
            Verts.AddRange(verts);
            return Verts.Count - verts.Length;
        }

        /// <summary>
        /// Add vertices. Meant to be used with object initializers
        /// </summary>
        public IEnumerable<TVertex> AddVerts { set => Verts.AddRange(value); }
        /// <summary>
        /// Adds triangles. Meant to be used with object initializers
        /// </summary>
        public IEnumerable<MeshTri> AddTris {  set => Tris.AddRange(value); }

        //Constructors
        /// <summary>
        /// Creates an empty MeshBuilder
        /// </summary>
        public MeshBuilder() { }
        /// <summary>
        /// Creates a copy of given MeshBuilder
        /// </summary>
        /// <param name="mb">source MeshBuilder</param>
        public MeshBuilder(MeshBuilder<TMaterial, TVertex> mb): this(mb.Material, mb.Verts, mb.Tris, mb.Name, mb.IsVisible, mb.VertexProcessor) { }
        /// <summary>
        /// Creates a new MeshBuilder with data from a mesh. Handy if a mesh is to be changed.
        /// </summary>
        /// <param name="mesh"></param>
        public MeshBuilder(MeshElement<TMaterial, TVertex> mesh) : this(mesh.Material, mesh.Vertices, mesh.Triangles, mesh.Name, mesh.IsVisible, mesh.VertexProcessor) { }


        /// <summary>
        /// Creates and populates a new MeshBuilder. All parameters are optional
        /// </summary>
        /// <param name="material"></param>
        /// <param name="verts"></param>
        /// <param name="tris"></param>
        /// <param name="name"></param>
        /// <param name="isVisible"></param>
        /// <param name="vertexProcessor"></param>
        public MeshBuilder(TMaterial material = default, IEnumerable<TVertex>? verts = null, IEnumerable<MeshTri>? tris = null, string name = "", bool isVisible = true, IVertexProcessor<TMaterial, TVertex>? vertexProcessor = null) {
            Material = material;
            if(verts != null) Verts.AddRange(verts);
            if(tris != null) Tris.AddRange(tris);
            Name = name;
            IsVisible = isVisible;
            VertexProcessor = vertexProcessor;
        }

        public MeshBuilder<TMaterial, TVertex> Clone() {
            return new(this);
        }
        public MeshElement<TMaterial, TVertex> Create() {
            return new MeshElement<TMaterial, TVertex>(Name, Material, Verts.ToArray(), Tris.ToArray(), IsVisible, VertexProcessor);
        }
    }

    public static class MeshBuilder {
        public static MeshBuilder<SimpleMaterial, VertexPositionColorTexture> NewBuilder(Texture2D texture) {
            var mb = new MeshBuilder<SimpleMaterial, VertexPositionColorTexture>();
            mb.Name = MeshElement.NewName();
            mb.Material = new SimpleMaterial(texture);
            return mb;
        }
    }
}
