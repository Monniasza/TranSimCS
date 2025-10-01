using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Worlds;

namespace TranSimCS.Collections {
    public class ListenableObjContainer<T> : ISet<T> where T : Obj {
        private Dictionary<Guid, T> data = new();

        public event Action<T>? ItemAdded;
        public event Action<T>? ItemRemoved;

        public T Find(Guid id) => data[id];
        public bool TryFind(Guid id, out T result) => data.TryGetValue(id, out result);
        public int Count => data.Count;
        public bool IsReadOnly => false;

        public bool Add(T item) {
            var result = data.TryAdd(item.Guid, item);
            if (result) ItemAdded?.Invoke(item);
            return result;
        }

        public void Clear() {
            var items = data.Values.ToArray();
            foreach (var item in items) Remove(item);
        }

        public bool Contains(T item) => data.ContainsKey(item.Guid);

        public void CopyTo(T[] array, int arrayIndex) => data.Values.CopyTo(array, arrayIndex);

        public void ExceptWith(IEnumerable<T> other) {
            foreach(var item in other) Remove(item);
        }

        public bool ContainsKey(Guid id) => data.ContainsKey(id);

        public IEnumerator<T> GetEnumerator() => data.Values.GetEnumerator();

        public void IntersectWith(IEnumerable<T> other) {
            var elements = data.Values.ToArray();
            foreach(var element in elements)
                if (!other.Contains(element)) Remove(element); 
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            return IsSubsetOf(other) && Count < other.Count();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other) {
            return IsSupersetOf(other) && Count > other.Count();
        }

        public bool IsSubsetOf(IEnumerable<T> other) {
            foreach(var element in this) 
                if (!other.Contains(element)) return false;
            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other) {
            foreach(var element in other)
                if(!Contains(element)) return false;
            return true;
        }

        public bool Overlaps(IEnumerable<T> other) {
            foreach (var item in other)
                if(Contains(item)) return true;
            return false;
        }

        public bool Remove(T item) {
            var result = data.Remove(item.Guid);
            if (result) ItemRemoved?.Invoke(item);
            return result;
        }

        public bool SetEquals(IEnumerable<T> other) {
            return IsSupersetOf(other) && IsSubsetOf(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            var union = new HashSet<T>(Equality.By<Guid, Obj>((obj) => obj.Guid));
            union.UnionWith(other);
            union.ExceptWith(this);
            ExceptWith(union);
        }

        public void UnionWith(IEnumerable<T> other) {
            foreach (var item in other) Add(item);
        }

        void ICollection<T>.Add(T item) => Add(item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ListenableObjContainerMap<T> : IDictionary<Guid, T> where T : Obj {
        private readonly ListenableObjContainer<T> container;
        internal ListenableObjContainerMap(ListenableObjContainer<T> container) {
            this.container = container;
        }

        public T this[Guid key] { get => container.Find(key); set => Add(key, value); }

        public ICollection<Guid> Keys => throw new NotImplementedException();

        public ListenableObjContainer<T> Container => container;
        public ICollection<T> Values => container;

        public int Count => container.Count;

        public bool IsReadOnly => container.IsReadOnly;

        public void Add(Guid key, T value) => TryAdd(key, value);
        public bool TryAdd(Guid key, T value) {
            ((Obj)value).Guid = key;
            return container.Add(value);
        }

        public void Add(KeyValuePair<Guid, T> item) => Add(item.Key, item.Value);

        public void Clear() {
            container.Clear();
        }

        public bool Contains(KeyValuePair<Guid, T> item) {
            if (container.TryFind(item.Key, out T value))
                return Object.Equals(value, item.Value);
            return false;
        }

        public bool ContainsKey(Guid key) {
            return container.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<Guid, T>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<Guid, T>> GetEnumerator() {
            throw new NotImplementedException();
        }

        public bool Remove(Guid key) {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Guid, T> item) {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Guid key, [MaybeNullWhen(false)] out T value) {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
