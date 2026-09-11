using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Cars {
    public record struct CarEntry(Car car, float positionOnStrip) : IComparable<CarEntry> {
        public int CompareTo(CarEntry other) => positionOnStrip.CompareTo(other.positionOnStrip);
    }
}
