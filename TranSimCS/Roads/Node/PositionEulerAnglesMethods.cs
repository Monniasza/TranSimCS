using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    public static class PositionEulerAnglesMethods {
        public static PositionEulerAngles Around(this PositionEulerAngles pea) => new(pea.Position, pea.Azimuth ^ int.MinValue, pea.Inclination, pea.Tilt);
    }
}