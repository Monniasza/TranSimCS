using System;

namespace TranSimCS.Spline
{
    public interface ISpline<T> {
        public T this[float t] { get; }
        public ISpline<T> Inverse() => SubRange(1, 0);
        public ISpline<T> SubRange(float from, float to) => new SubRangeSpline<T>(this, from, to);
    }
    public class SplineFromFunction<T>(Func<float, T> fn) : ISpline<T> {
        public T this[float t] => fn(t);
    }

    public class SubRangeSpline<T>(ISpline<T> spline, float from, float to) : ISpline<T> {
        public T this[float t] => spline[from + (to - from) * t];
        public ISpline<T> SubRange(float from2, float to2) {
            var lerpedFrom = from + (to - from) * from2;
            var lerpedTo = from + (to - from) * to2;
            return new SubRangeSpline<T>(spline, lerpedFrom, lerpedTo);
        }
    }
}