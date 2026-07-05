using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collections.Pooled;

namespace TranSimCS.Collections {
    public sealed class CollectionPool<T> : IDisposable{
        private readonly Func<int, T> _constructor;
        private readonly Action<T> _destructor;
        private readonly Func<T, int> _getCapacity;
        private readonly int _minSize;
        private readonly int _minPerBucket;

        private readonly Dictionary<int, Stack<T>> _buckets = new();
        private readonly HashSet<T> _owned = new();

        private bool _disposed;

        public CollectionPool(
            Func<int, T> constructor,
            Action<T> destructor,
            Func<T, int> getCapacity,
            int minSize,
            int minPerBucket = 0
        ){
            ArgumentNullException.ThrowIfNull(constructor);
            ArgumentNullException.ThrowIfNull(destructor);
            ArgumentNullException.ThrowIfNull(getCapacity);

            if (minSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(minSize));

            if (minPerBucket < 0)
                throw new ArgumentOutOfRangeException(nameof(minPerBucket));

            _constructor = constructor;
            _destructor = destructor;
            _minSize = minSize;
            _minPerBucket = minPerBucket;
            _getCapacity = getCapacity;
        }

        public T Rent(int count) {
            ThrowIfDisposed();

            int bucket = GetBucket(count);

            if (_buckets.TryGetValue(bucket, out var stack) &&
                stack.Count > 0) {
                return stack.Pop();
            }

            int capacity = BucketCapacity(bucket);

            var obj = _constructor(capacity);
            _owned.Add(obj);

            MaintainBucket(bucket);

            return obj;
        }

        public void Return(T element) {
            ThrowIfDisposed();

            if (!_owned.Contains(element))
                throw new InvalidOperationException("Object was not created by this pool.");

            int bucket = GetBucket(_getCapacity(element));

            if (!_buckets.TryGetValue(bucket, out var stack)) {
                stack = new Stack<T>();
                _buckets.Add(bucket, stack);
            }

            stack.Push(element);
        }

        public struct DisposableRental<T>: IDisposable {
            public readonly T Value;
            private CollectionPool<T> _pool;
            internal DisposableRental(T value, CollectionPool<T> pool) {
                Value = value;
                _pool = pool;
            }

            void IDisposable.Dispose() => _pool.Dispose();
        }
        public DisposableRental<T> RentAsDisposable(int length) {
            var rental = Rent(length);
            return new DisposableRental<T>(rental, this);
        }

        public bool IsCollectionPooled(T element) {
            return _owned.Contains(element);
        }

        private void MaintainBucket(int bucket) {
            if (_minPerBucket == 0)
                return;

            if (!_buckets.TryGetValue(bucket, out var stack)) {
                stack = new Stack<T>();
                _buckets.Add(bucket, stack);
            }

            while (stack.Count < _minPerBucket) {
                int capacity = BucketCapacity(bucket);
                var obj = _constructor(capacity);

                _owned.Add(obj);
                stack.Push(obj);
            }
        }

        private int GetBucket(int count) {
            int capacity = _minSize;
            int bucket = 0;

            while (capacity < count) {
                capacity <<= 1;
                bucket++;
            }

            return bucket;
        }

        private int BucketCapacity(int bucket) {
            return _minSize << bucket;
        }

        private void ThrowIfDisposed() {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Dispose() {
            if (_disposed)
                return;

            foreach (var obj in _owned)
                _destructor(obj);

            _owned.Clear();
            _buckets.Clear();

            _disposed = true;
        }
    }
}
