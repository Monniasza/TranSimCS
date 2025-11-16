using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;

namespace TranSimCS.Model.OBJ {
    public static class MeshComplexMethods {
        public static void AddAll(this MeshComplex dest, MeshComplex src) => dest.Elements.AddRange(src.Elements);
    }
}
