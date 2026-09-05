using System;
using System.Collections;
using System.Collections.Generic;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Roads {
    public struct HalfNodePair(HalfNode? start, HalfNode? end): IReadOnlyList<HalfNode> {
        public HalfNode? Start = start;
        public HalfNode? End = end;

        //Conversion to collections
        public (HalfNode?, HalfNode?) ToTuple => (Start, End);
        public HalfNode?[] ToArray => [Start, End];
        public HalfNode? GetElement(int index) {
            if (index == 0) return Start;
            if (index == 1) return End;
            throw new IndexOutOfRangeException();
        }
        public HalfNode? GetElement(SegmentHalf index) {
            if (index == SegmentHalf.Start) return Start;
            if (index == SegmentHalf.End) return End;
            throw new IndexOutOfRangeException();
        }

        //Implementation of I(ReadOnly)List
        public int Count => 2;

        public IEnumerator<HalfNode?> GetEnumerator() {
            IEnumerable<HalfNode?> e = ToArray;
            return e.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerable<HalfNode> this[int key] => [GetElement(key)];

        HalfNode IReadOnlyList<HalfNode>.this[int index] => GetElement(index);
    }
}
