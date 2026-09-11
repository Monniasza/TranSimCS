using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Cars {
    public record struct Obstacle(float relativeDistance, float Velocity) {
        public Obstacle Combine(Obstacle other){
            if (other.relativeDistance < relativeDistance) return other;
            return this;
        }
    }
}
