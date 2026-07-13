using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads.Node {
    public static class HalfNodeMethods {
        public static IList<HalfLane> GetLaneList(this HalfNode halfNode) => new HalfNodeLanesList(halfNode);
    }
    internal class HalfNodeLanesList(HalfNode halfNode) : IList<HalfLane> {
        public HalfLane this[int index] { get => halfNode.GetLaneByIndex(index); set => throw new ReadOnlyException(); }

        public int Count => halfNode.LaneCount;

        public bool IsReadOnly => true;

        public void Add(HalfLane item) => halfNode.AddLane(item.LaneNode);

        public void Clear() {
            var lanes = new HalfLane[Count];
            for(int i = 0; i < lanes.Length; i++) lanes[i] = this[i];
            for(int i = 0; i < lanes.Length; i++) Remove(lanes[i]);
        }

        public bool Contains(HalfLane item) => item?.HalfNode == halfNode;

        public void CopyTo(HalfLane[] array, int arrayIndex) {
            ArgumentNullException.ThrowIfNull(nameof(array));
            if(arrayIndex < 0 || arrayIndex >= Count) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            for(int i = 0; i < Count && i+arrayIndex < array.Length; i++) array[i+arrayIndex] = this[i];
        }

        public IEnumerator<HalfLane> GetEnumerator() => new GetAndLengthIterator<HalfLane>(Count, x => this[x]);

        public int IndexOf(HalfLane item) => Contains(item) ? item.Index : -1;

        public void Insert(int index, HalfLane item) => Add(item);

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
