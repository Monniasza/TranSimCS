using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Model {

    /// <summary>
    /// Generalization for all mesh elements Use <see cref="MeshElement{TMaterial, TVertex}"/> specialized implementations
    /// </summary>
    public abstract class MeshElement {
        protected MeshElement(string name, MeshTri[] triangles, bool isVisible = true) {
            Name = name;
            Triangles = triangles;
            IsVisible = isVisible;
        }

        public string Name;
        public abstract Type MaterialType();
        public abstract Type VertexType();

        public MeshTri[] Triangles { get; set; }
        public bool IsVisible { get; set; }
        public abstract MeshBvh GetSpatial();
        public abstract IList Vertices0();

        public abstract bool Render(RenderManager gd);

        public static string GoodName(Obj obj, params string[] elements) {
            return $"{obj.Guid}.{string.Join('/', elements)}";
        }

        private static long count = 1 << 32;
        public static string NewName() => (count++).ToString();
    }

    /// <summary>
    /// Specialization of <see cref="MeshElement"/> for particular materials/vertices.
    /// </summary>
    /// <typeparam name="TMaterial"></typeparam>
    /// <typeparam name="TVertex"></typeparam>
    public class MeshElement<TMaterial, TVertex>: MeshElement{
        public TMaterial Material { get; set; }
        public TVertex[] Vertices { get; set; }
        public override IList Vertices0() => Vertices;
        public IVertexProcessor<TMaterial, TVertex>? VertexProcessor { get; set; }
        public IVertexProcessor<TMaterial, TVertex>? GetVertexProcessor() => VertexProcessor ?? VertexProcessor<TMaterial, TVertex>.Default;
        public IVertexProcessor<TMaterial, TVertex> GetVertexProcessorStrict() => VertexProcessor ?? VertexProcessor<TMaterial, TVertex>.GetDefault();

        public MeshElement(string name, TMaterial material, TVertex[] vertices, MeshTri[] triangles, bool isVisible = true, IVertexProcessor<TMaterial, TVertex> vp = null)
            :base(name, triangles, isVisible) {
            
            Material = material;
            Vertices = vertices;
            VertexProcessor = vp;
        }

        public override Type MaterialType() => typeof(TMaterial);
        public override Type VertexType() => typeof(TVertex);

        public override bool Render(RenderManager gd) {
            if(!IsVisible) return false;
            var vp = GetVertexProcessor();
            if(vp == null) return false;
            vp.Render(gd, this);
            return true;
        }

        private MeshBvh<TMaterial, TVertex>? meshBvh;
        public override MeshBvh GetSpatial() => GetSpatial0();
        public MeshBvh<TMaterial, TVertex> GetSpatial0() {
            meshBvh ??= MeshBvh<TMaterial, TVertex>.Build(this);
            return meshBvh;
        }
    }
}
