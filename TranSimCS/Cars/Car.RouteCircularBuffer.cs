using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if(elementsAfterEnd > 0) Array.Copy(_routeBuffer, 0, newBuffer, elementsBeforeEnd, elementsAfterEnd); //If needec, copy elements after the end

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
            _routeBufferHead = 0;
            GrowCapacity(route.Route.LaneStrips.Length);
            for (int i = 0; i < route.Route.LaneStrips.Length; i++) _routeBuffer[i] = route.Route.LaneStrips[i].ToRouteInput();
            _routeBufferCount = route.Route.LaneStrips.Length;
            RoutePositionFromStart = route.Position;
        }
    }
}
