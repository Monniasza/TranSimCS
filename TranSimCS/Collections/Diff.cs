using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    public sealed class Diff<T> {
        public readonly ISet<T> additions;
        public readonly ISet<T> removals;

        public Diff() : this(new HashSet<T>(), new HashSet<T>()) { }
        public Diff(ISet<T> additions, ISet<T> removals) {
            this.additions = additions;
            this.removals = removals;
        }

        public void Add(T item) {
            additions.Add(item);
            removals.Remove(item);
        }
        public void Remove(T item) {
            removals.Add(item);
            additions.Remove(item);
        }
        public void Ignore(T item) {
            removals.Remove(item);
            additions.Remove(item);
        }
        public void Clear() {
            removals.Clear();
            additions.Clear();
        }
    }
}
