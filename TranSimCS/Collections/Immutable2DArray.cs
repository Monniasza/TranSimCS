using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    public sealed class Immutable2DArray<T>: IEnumerable<T> {
        private readonly T[,] data;
        private Immutable2DArray(T[,] data) {
            this.data = data;
        }

        /// <summary>
        /// Copies data from a 2D array into a new <see cref="Immutable2DArray{T}"/>.
        /// Changes to the original after obtaining the copy will not modify it.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <paramref name="data"/> == null</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Immutable2DArray<T> Copy(T[,] data) {
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            return new((T[,])data.Clone());
        }

        /// <summary>
        /// Wraps a 2D array in a <see cref="Immutable2DArray{T}"/>.
        /// Changes to the original will be reflected in the view.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <paramref name="data"/> == null</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Immutable2DArray<T> Wrap(T[,] data) {
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            return new(data);
        }

        /// <summary>
        /// Returns the number of elements in the X axis
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Width() => data.GetLength(0);

        /// <summary>
        /// Returns the number of element in the Y axis
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Height() => data.GetLength(1);

        /// <summary>
        /// Returns total number of elements. Equal to <see cref="Width"/> * <see cref="Height"/>
        /// </summary>
        public int Length => data.Length;
        /// <summary>
        /// Returns total number of elements. Equal to <see cref="Width"/> * <see cref="Height"/>
        /// </summary>
        public long LongLength => data.LongLength;

        /// <summary>
        /// Enumerates the contents of this <see cref="Immutable2DArray{T}"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator() => (IEnumerator<T>)data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets an element at the [<paramref name="x"/>, <paramref name="y"/>].
        /// </summary>
        /// <param name="x">the horizontal coordinate of this array</param>
        /// <param name="y">the vertical coordinate of this array</param>
        /// <exception cref="IndexOutOfRangeException">when X or Y is out of bounds</exception>
        public T this[int x, int y] => data[x, y];
    }
}
