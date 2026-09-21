using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    /// <summary>
    /// Manages a reusable array, reallocates if needed.
    /// </summary>
    /// <typeparam name="T">type of elements in the array</typeparam>
    public sealed class ScratchArray<T> {
        private T[] array;
        public ScratchArray(int startingCapacity = 16) {
            array = new T[startingCapacity];
        }
        /// <summary>
        /// Gets the existing array if it will fit <paramref name="length"/> elements, or constructs a large enough array otherwise.
        /// The array is not guaranteed to be empty
        /// </summary>
        /// <param name="length">requested minimum capacity of the array</param>
        public T[] GetArray(int length) {
            if(array.Length < length) {
                int newCapacity = Math.Max(array.Length, 1);
                while (newCapacity < length)
                    newCapacity *= 2;
                array = new T[newCapacity];
            }
            return array;
        }
    }
}
