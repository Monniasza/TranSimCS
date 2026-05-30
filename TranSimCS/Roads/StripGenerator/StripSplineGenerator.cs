using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2.TypeRegistry;
using TranSimCS.Spline;

namespace TranSimCS.Roads.StripGenerator {
    public abstract class StripSplineGenerator(string typeId): ITypeRegistered<StripSplineGenerator> {

        public static readonly TypeRegistry<StripSplineGenerator> typeRegistry = new();

        public (string TypeId, TypeRegistry<StripSplineGenerator> TypeRegistry) TypeInfo() => (typeId, typeRegistry);
        public abstract IndexStrip GenerateSplines(RoadStrip road);
    }
}
