using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;

namespace TranSimCS.Roads.Node {
    public static class HalfNodeMethods {
        private static ScratchArray<HalfLane> _insertLanesScratch = new();

        public static IList<HalfLane> GetLaneList(this HalfNode halfNode) => new HalfNodeLanesList(halfNode);

        /// <summary>
        /// Insert a specified lane directly to the right of the anchor, moving other lanes if needed.
        /// </summary>
        /// <param name="anchor">the lane on whose right side a new lane will be inserted and other lanes shifted to make space</param>
        /// <param name="spec">the lane spec for a new lane</param>
        /// <param name="intoMedian">true for insertion to cut into medians, false to shift medians</param>
        public static HalfLane InsertOnRight(this HalfLane anchor, LaneSpec spec, bool intoMedian = false) {
            ArgumentNullException.ThrowIfNull(anchor, nameof(anchor));
            if (spec.Width < 0) throw new ArgumentException("width < 0");

            if (intoMedian)
                EnsureSpaceOnRight(anchor, spec.Width);
            else
                InsertSpaceOnRight(anchor, spec.Width);

            float newPosition = anchor.MiddlePosition + (spec.Width + anchor.Width) / 2;
            HalfLane result = anchor.HalfNode.AddLane(new LaneDefinition(newPosition, spec));
            return result;
        }
        /// <summary>
        /// Insert a space on the right of a lane. Provide a negative value to reduce the space.
        /// </summary>
        /// <param name="anchor">the lane on whose right side other lanes will be shifted to make space</param>
        /// <param name="width">amount to move lanes in meters</param>
        public static void InsertSpaceOnRight(this HalfLane anchor, float width) {
            int startingIndex = anchor.Index;
            int affectedLaneCount = anchor.RoadNode.Lanes.Count - startingIndex - 1;
            var affectedLanes = _insertLanesScratch.GetArray(affectedLaneCount);
            for (int i = 0; i < affectedLaneCount; i++)
                affectedLanes[i] = anchor.HalfNode.GetLaneByIndex(i + startingIndex + 1);
            for (int i = 0; i < affectedLaneCount; i++)
                affectedLanes[i].MiddlePosition += width;
        }
        /// <summary>
        /// Insert a specified lane directly to the left of the anchor, moving other lanes if needed.
        /// </summary>
        /// <param name="anchor">the lane on whose left side a new lane will be inserted and other lanes shifted to make space</param>
        /// <param name="spec">the lane spec for a new lane</param>
        /// <param name="intoMedian">true for insertion to cut into medians, false to shift medians</param>
        public static HalfLane InsertOnLeft(this HalfLane anchor, LaneSpec spec, bool intoMedian = false) {
            ArgumentNullException.ThrowIfNull(anchor, nameof(anchor));
            if (spec.Width < 0) throw new ArgumentException("width < 0");

            if (intoMedian)
                EnsureSpaceOnLeft(anchor, spec.Width);
            else
                InsertSpaceOnLeft(anchor, spec.Width);

            float newPosition = anchor.MiddlePosition - (spec.Width + anchor.Width) / 2;
            HalfLane result = anchor.HalfNode.AddLane(new LaneDefinition(newPosition, spec));
            return result;
        }
        /// <summary>
        /// Insert a space on the left of a lane. Provide a negative value to reduce the space.
        /// </summary>
        /// <param name="anchor">the lane on whose left side other lanes will be shifted to make space</param>
        /// <param name="width">amount to move lanes in meters</param>
        public static void InsertSpaceOnLeft(this HalfLane anchor, float width) {
            int startingIndex = anchor.Index;
            int affectedLaneCount = startingIndex;
            var affectedLanes = _insertLanesScratch.GetArray(affectedLaneCount);
            for (int i = 0; i < affectedLaneCount; i++)
                affectedLanes[i] = anchor.HalfNode.GetLaneByIndex(i);
            for (int i = 0; i < affectedLaneCount; i++)
                affectedLanes[i].MiddlePosition -= width;
        }

        private static readonly ScratchArray<(HalfLane lane, float offset)> _ensureSpaceScratch = new();
        public static void EnsureSpaceOnLeft(this HalfLane anchor, float width) {
            var index = anchor.Index;
            var affectedLaneCount = 0;
            var affectedLanes = _ensureSpaceScratch.GetArray(index);
            for(int i = 0; i < index; i++) {
                var laneIndex = index - i - 1;
                var lane = anchor.HalfNode.GetLaneByIndex(laneIndex);
                var neighbor = anchor.HalfNode.GetLaneByIndex(laneIndex + 1);
                var space = neighbor.Bounds.Min - lane.Bounds.Max;
                width -= space;
                if (width <= 0) break;
                affectedLanes[i] = (lane, width);
                affectedLaneCount++;
            }
            for (int i = 0; i < affectedLaneCount; i++) affectedLanes[i].lane.MiddlePosition -= affectedLanes[i].offset;
        }
        public static void EnsureSpaceOnRight(this HalfLane anchor, float width) {
            var index = anchor.Index;
            var maxLaneCount = anchor.HalfNode.LaneCount - index - 1;
            var affectedLaneCount = 0;
            var affectedLanes = _ensureSpaceScratch.GetArray(maxLaneCount);
            for (int i = 0; i < maxLaneCount; i++) {
                var laneIndex = index + i + 1;
                var lane = anchor.HalfNode.GetLaneByIndex(laneIndex);
                var neighbor = anchor.HalfNode.GetLaneByIndex(laneIndex - 1);
                var space = lane.Bounds.Min - neighbor.Bounds.Max;
                width -= space;
                if (width <= 0) break;
                affectedLanes[i] = (lane, width);
                affectedLaneCount++;
            }
            for (int i = 0; i < affectedLaneCount; i++) affectedLanes[i].lane.MiddlePosition += affectedLanes[i].offset;
        }
    }
    internal class HalfNodeLanesList(HalfNode halfNode) : IList<HalfLane> {
        public HalfLane this[int index] { get => halfNode.GetLaneByIndex(index); set => throw new ReadOnlyException(); }

        public int Count => halfNode.LaneCount;

        public bool IsReadOnly => false;

        public void Add(HalfLane item) => halfNode.AddLane(item.LaneNode);

        public void Clear() {
            var lanes = new HalfLane[Count];
            for(int i = 0; i < lanes.Length; i++) lanes[i] = this[i];
            for(int i = 0; i < lanes.Length; i++) Remove(lanes[i]);
        }

        public bool Contains(HalfLane item) => item?.HalfNode == halfNode;

        public void CopyTo(HalfLane[] array, int arrayIndex) {
            ArgumentNullException.ThrowIfNull(array, nameof(array));
            if(arrayIndex < 0 || arrayIndex >= array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            for(int i = 0; i < Count && i+arrayIndex < array.Length; i++) array[i+arrayIndex] = this[i];
        }

        public IEnumerator<HalfLane> GetEnumerator() => new GetAndLengthIterator<HalfLane>(Count, x => this[x]);

        public int IndexOf(HalfLane item) => Contains(item) ? item.Index : -1;

        /// <summary>
        /// Inserts a lane at <paramref name="index"/> in this half's own left-to-right order, where 0 is
        /// the leftmost position and <see cref="Count"/> is the rightmost.
        /// <para>
        /// The lane is placed by giving it a centre position in the gap between its new neighbours, so
        /// that <see cref="HalfNode.GetLaneByIndex"/> reports it at the requested index. Lanes are ordered
        /// by centre position, so the new lane must land strictly between the lanes it is inserted
        /// between; when there is no room for that, the neighbours are pushed apart first.
        /// </para>
        /// </summary>
        public void Insert(int index, HalfLane item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in [0, {Count}].");

            var laneNode = item.LaneNode;
            var width = laneNode.LaneSpec.Width;

            //The centre position the new lane needs, in this half's own coordinate space.
            var centerPos = ComputeInsertionCenter(index, width);

            //Build a LaneNode rather than a LaneDefinition so that the lane keeps its identity: the
            //LaneDefinition overload generates a fresh Guid. AddLane(LaneNode) mirrors the position for
            //the Backward end itself, so the position is handed over in this half's own space.
            halfNode.AddLane(new LaneNode(laneNode.LaneSpec, centerPos, item.Guid));
        }

        /// <summary>
        /// Works out the centre position for a lane of the given width inserted at <paramref name="index"/>
        /// in this half's own left-to-right order, pushing existing lanes apart when the gap is too small.
        /// </summary>
        private float ComputeInsertionCenter(int index, float width) {
            var count = Count;
            var half = width / 2;

            //The gap the new lane has to fit into, in this half's own coordinate space.
            var lower = index > 0 ? this[index - 1].Bounds.Max : float.NegativeInfinity;
            var upper = index < count ? this[index].Bounds.Min : float.PositiveInfinity;

            //No neighbours at all: centre the lane on the origin.
            if (float.IsNegativeInfinity(lower) && float.IsPositiveInfinity(upper)) return 0;

            //Only a right neighbour: sit immediately to its left.
            if (float.IsNegativeInfinity(lower)) return upper - half;

            //Only a left neighbour: sit immediately to its right.
            if (float.IsPositiveInfinity(upper)) return lower + half;

            //Both neighbours: use the middle of the gap when it is wide enough.
            if (upper - lower >= width) return (lower + upper) / 2;

            //The gap is too narrow, so push the lanes on each side apart by half the shortfall.
            var shortfall = width - (upper - lower);
            var push = shortfall / 2;
            for (int i = 0; i < index; i++) this[i].MiddlePosition -= push;
            for (int i = index; i < count; i++) this[i].MiddlePosition += push;
            return (lower - push + upper + push) / 2;
        }

        public bool Remove(HalfLane item) {
            if(!Contains(item)) return false;
            halfNode.Delete(item); return true;
        }

        public void RemoveAt(int index) => Remove(this[index]);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public class GetAndLengthIterator<T>(int length, Func<int, T> getter) : IEnumerator<T> {
    private int _index = -1;

    public T Current => getter(_index);

    object IEnumerator.Current => Current;

    public void Dispose() { }

    public bool MoveNext() {
        if(_index == length - 1) return false;
        _index++;
        return true;
    }

    public void Reset() => _index = -1;
}
