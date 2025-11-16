using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model.OBJ {
    

    public struct MTLLibrary {
        private readonly Dictionary<string, MTL> data = [];
        public readonly ReadOnlyDictionary<string, MTL> Data;
        public MTLLibrary(IEnumerable<MTL> mtls, DuplicatePolicy policy = DuplicatePolicy.Replace) {
            foreach (MTL mtl in mtls) {
                if(data.TryGetValue(mtl.Name, out MTL dupe)){
                    data[mtl.Name] = policy.Replace(dupe, mtl);
                } else {
                    data[mtl.Name] = mtl;
                }
            }

            Data = new(data);
        }
    }

    /// <summary>
    /// One material definition from an .mtl file
    /// </summary>
    public struct MTL: IEquatable<MTL> {
        /// <summary>
        /// Name of this material
        /// </summary>
        public string Name;
        /// <summary>
        /// Specular shininess
        /// </summary>
        public float Ns; //Ns
        /// <summary>
        /// Ambient color
        /// </summary>
        public Vector3 Ka; //Ka
        /// <summary>
        /// Diffuse color
        /// </summary>
        public Vector3 Kd; //Kd
        /// <summary>
        /// Specular color
        /// </summary>
        public Vector3 Ks = new(1, 1, 1); //Ks
        /// <summary>
        /// Emissive color
        /// </summary>
        public Vector3 Ke; //Ke
        /// <summary>
        /// Optical density
        /// </summary>
        public float Ni = float.PositiveInfinity; //Ni
        /// <summary>
        /// Opacity
        /// </summary>
        public float d = 1; //d
        /// <summary>
        /// Type of illumination
        /// </summary>
        public int illum = 11; //illum

        public MTL() {}

        public bool Equals(MTL other) =>
            this.Name == other.Name &&
            this.Ns == other.Ns &&
            this.Ka == other.Ka &&
            this.Kd == other.Kd &&
            this.Ks == other.Ks &&
            this.Ke == other.Kd &&
            this.Ni == other.Ni &&
            this.d == other.d &&
            this.illum == other.illum;

        public override bool Equals(object obj) {
            return obj is MTL && Equals((MTL)obj);
        }
        public static bool operator ==(MTL left, MTL right) {
            return left.Equals(right);
        }

        public static bool operator !=(MTL left, MTL right) {
            return !(left == right);
        }

        public override int GetHashCode() {
            var h1 = HashCode.Combine(Name, Ns, Ka, Kd, Ks);
            var h2 = HashCode.Combine(Ke, Ni, d, illum);
            return HashCode.Combine(h1, h2);
        }
    }
}
