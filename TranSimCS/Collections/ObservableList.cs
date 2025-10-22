using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    public class ObservableList<T> : IList<T> {
        public event Action<T>? ElementAdded;
        public event Action<T>? ElementRemoved;

        private List<T> _elements = new List<T>();

        public ObservableList() { }

        public ObservableList(IEnumerable<T> data) {
            _elements.AddRange(data);
        }


        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item) {
            _elements.Add(item);
            ElementAdded?.Invoke(item);
        }

        public void Clear() {
            foreach (var item in _elements) ElementRemoved?.Invoke(item);
            _elements.Clear();
        }

        public bool Contains(T item) => _elements.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();

        public int IndexOf(T item) => _elements.IndexOf(item);

        public void Insert(int index, T item) {
            _elements.Insert(index, item);
            ElementAdded?.Invoke(item);
        }

        public bool Remove(T item) {
            var result = _elements.Remove(item);
            if(result) ElementRemoved?.Invoke(item);
            return result;
        }

        public void RemoveAt(int index) {
            var elementToGo = _elements[index];
            ElementRemoved?.Invoke(elementToGo);
            _elements.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
