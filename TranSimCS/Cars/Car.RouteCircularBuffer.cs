using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Cars {
    //Contains the route circular buffer and related algorithms
    public partial class Car {
        //Route buffer internals
        internal RouteInput[] _routeBuffer = new RouteInput[16];
        internal int _routeBufferHead = 0;
        internal int _routeBufferCount = 0;
        internal int MapIndex(int index) => (index + _routeBufferHead) % _routeBuffer.Length;
        private void GrowCapacity(int minCapacity) {
            if (minCapacity <= _routeBuffer.Length) return;
            int newCapacity = _routeBuffer.Length;
            while (newCapacity < minCapacity) newCapacity *= 2;

            int elementsBeforeEnd = int.Min(_routeBufferCount, _routeBuffer.Length - _routeBufferHead);
            int elementsAfterEnd = _routeBufferCount - elementsBeforeEnd;

            var newBuffer = new RouteInput[newCapacity];
            Array.Copy(_routeBuffer, _routeBufferHead, newBuffer, 0, elementsBeforeEnd); //Copy elements before the end
            if(elementsAfterEnd > 0) Array.Copy(_routeBuffer, 0, newBuffer, elementsBeforeEnd, elementsAfterEnd); //If needed, copy elements after the end

            _routeBufferHead = 0;
            _routeBuffer = newBuffer;
        }

        //Basic route algorithms
        public int RouteElementCount => _routeBufferCount;
        public RouteInput GetRouteElement(int index) => _routeBuffer[MapIndex(index)];
        public void PopRouteElements(int count) {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _routeBufferCount, nameof(count));
            _routeBufferHead = MapIndex(count);
            _routeBufferCount -= count;

            if (_routeBufferCount == 0)
                _routeBufferHead = 0;
        }
        public void TrimRouteElements(int count) {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _routeBufferCount, nameof(count));
            _routeBufferCount -= count;
            if (_routeBufferCount == 0)
                _routeBufferHead = 0;
        }
        public void PushRouteElement(RouteInput routeElement) {
            GrowCapacity(_routeBufferCount + 1);
            var index = MapIndex(_routeBufferCount);
            _routeBuffer[index] = routeElement;
            _routeBufferCount++;
        }

        public RoutePosition GetRoute() {
            RouteInput[] routeInputs = new RouteInput[_routeBufferCount];
            int elementsBeforeEnd = int.Min(_routeBufferCount, _routeBuffer.Length - _routeBufferHead);
            int elementsAfterEnd = _routeBufferCount - elementsBeforeEnd;
            Array.Copy(_routeBuffer, _routeBufferHead, routeInputs, 0, elementsBeforeEnd); //Copy elements before the end
            if (elementsAfterEnd > 0) Array.Copy(_routeBuffer, 0, routeInputs, elementsBeforeEnd, elementsAfterEnd); //If needec, copy elements after the end
            return new(new Route(routeInputs), RoutePositionFromStart);
        }

        public void SetRoute(RoutePosition route) {
            ReleasePath();
            _routeBufferHead = 0;
            GrowCapacity(route.Route.LaneStrips.Length);
            for (int i = 0; i < route.Route.LaneStrips.Length; i++) _routeBuffer[i] = route.Route.LaneStrips[i].ToRouteInput();
            _routeBufferCount = route.Route.LaneStrips.Length;
            RoutePositionFromStart = route.Position;
            AcquirePath();
        }

        //Path occupancy
        private SplinePath? _occupiedPath;

        /// <summary>
        /// The path this car is currently occupying, or <see langword="null"/> if it is not on a path.
        /// <para>
        /// A car registers itself on the path of the strip it is currently driving on. This is what
        /// keeps an orphaned path alive while traffic is still draining off it: the path system will not
        /// collect a path that still has cars on it.
        /// </para>
        /// </summary>
        public SplinePath? OccupiedPath => _occupiedPath;

        /// <summary>
        /// Registers this car on the path of the strip it currently occupies.
        /// <para>
        /// Does nothing when the car has no route, or when the strip it is on has no path. The path is
        /// created on demand by the strip, so a car driving onto a strip is what brings that strip's
        /// path into existence.
        /// </para>
        /// </summary>
        internal void AcquirePath() {
            if (RouteElementCount == 0) return;
            var strip = GetRouteElement(0).road;
            if (strip == null) return;
            var path = strip.Path;
            if (path == null) return;
            if (ReferenceEquals(_occupiedPath, path)) return;

            ReleasePath();
            _occupiedPath = path;
            path._cars.Add(this);
        }

        /// <summary>
        /// Releases this car's registration on the path it currently occupies.
        /// <para>
        /// Called when the car moves onto a different strip, and when the car is demolished. Releasing
        /// the last car on an orphaned path makes that path eligible for collection.
        /// </para>
        /// </summary>
        internal void ReleasePath() {
            if (_occupiedPath == null) return;
            _occupiedPath._cars.Remove(this);
            _occupiedPath = null;
        }
    }
}
