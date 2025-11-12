using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Model {
    public class MeshComplex {
        public readonly Dictionary<string, MeshElement> Elements;

        public MeshComplex() {
            Elements = [];
        }
    }
}
