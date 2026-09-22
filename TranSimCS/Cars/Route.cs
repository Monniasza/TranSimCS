using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.UnitsOfMeasure;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Cars {
    public record struct RouteElement(SplinePath road, bool isReverse){ }
}
