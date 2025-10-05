using System;
using System.Collections;
using System.Collections.Generic;

namespace TranSimCS.Roads {
    public struct RoadNodeEndPair(RoadNodeEnd start, RoadNodeEnd end): IReadOnlyList<RoadNodeEnd> {
        public RoadNodeEnd Start = start;
        public RoadNodeEnd End = end;

        //Conversion to collections
        public (RoadNodeEnd, RoadNodeEnd) ToTuple => (Start, End);
        public RoadNodeEnd[] ToArray => [Start, End];
        public RoadNodeEnd GetElement(int index) {
            if (index == 0) return Start;
            if (index == 1) return End;
            throw new IndexOutOfRangeException();
        }
        public RoadNodeEnd GetElement(SegmentHalf index) {
            if (index == SegmentHalf.Start) return Start;
            if (index == SegmentHalf.End) return End;
            throw new IndexOutOfRangeException();
        }

        //Implementation of I(ReadOnly)List
        public int Count => 2;

        public IEnumerator<RoadNodeEnd> GetEnumerator() {
            IEnumerable<RoadNodeEnd> e = ToArray;
            return e.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerable<RoadNodeEnd> this[int key] => [GetElement(key)];

        RoadNodeEnd IReadOnlyList<RoadNodeEnd>.this[int index] => GetElement(index);
    }
}
