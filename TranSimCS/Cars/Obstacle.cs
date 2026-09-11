using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Cars {
    public record struct Obstacle(float RoutePosition, float Velocity) {
        public Obstacle Combine(Obstacle other){
            if (other.RoutePosition < RoutePosition) return other;
            return this;
        }
    }
}
