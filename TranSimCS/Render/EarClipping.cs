using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.SilkNet;

namespace TranSimCS.Render {
    public class DLNode<T> {
        public static DLNode<T> CreateLinear(params T[] list) => CreateLinear((IEnumerable<T>)list);
        public static DLNode<T> CreateLinear(IEnumerable<T> list) {
            DLNode<T>? currentNode = null;
            DLNode<T>? startNode = null;
            foreach (var element in list) {
                var node = new DLNode<T>(element);
                startNode ??= node;
                node.Prev = currentNode;
                currentNode = node;
            }
            if (startNode == null) throw new ArgumentException("The input list is empty");
            return startNode;
        }
        public static DLNode<T> CreateCircular(IEnumerable<T> list) {
            var nodes = list.Select(x => new DLNode<T>(x)).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                var currNode = nodes[i];
                var nextNode = nodes[(i + 1) % nodes.Length];
                currNode.Next = nextNode;
                //nextNode.Prev = currNode;
            }
            return nodes[0];
        }


        private DLNode<T> _prev;
        private DLNode<T> _next;
        public T val;

        public DLNode(T value) {
            val = value;
        }

        public DLNode<T> Prev {
            get => _prev; set {
                var oldPrev = _prev;
                var newPrev = value;
                oldPrev?._next = null;
                newPrev?._next = this;
                _prev = value;
            }
        }
        public DLNode<T> Next {
            get => _next; set {
                var oldNext = _next;
                var newNext = value;
                oldNext?._prev = null;
                newNext?._prev = this;
                _next = value;
            }
        }

        public void ClipOut() {
            var next = Next;
            var prev = Prev;
            Prev = null;
            Next = null;
            prev.Next = next;
            next.Prev = prev;
        }

        /// <summary>
        /// Iterates over values from this node and next values.
        /// WARNING: If the list is circular, this iterator will not finish.
        /// If you want to be sure, use <see cref="IterateValuesNextNoCycle()"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> IterateValuesNext() {
            var node = this;
            while (node != null) {
                yield return node.val;
                node = node.Next;
            }
        }
        public IEnumerable<T> IterateValuesNextNoCycle() {
            var node = this;
            while (node != null) {
                yield return node.val;
                node = node.Next;
                if (node == this) yield break;
            }
        }
    }
}