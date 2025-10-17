using System;
using System.Collections.Generic;

namespace TranSimCS.Geometry {
    public class Node<T> {
        public T Value;
        private Node<T>? prev;
        public Node<T>? Prev {
            get => prev; set => SetRelationship(value, this);
        }

        private Node<T>? next;
        public Node<T>? Next {
            get => next; set => SetRelationship(this, value);
        }

        public static void SetRelationship(Node<T>? prev, Node<T>? next) {
            prev?.next = next;
            next?.prev = prev;
        }

        public Node(T value) {
            Value = value;
        }

        public static Node<T>[] Circular(T[] values) {
            var nodes = new Node<T>[values.Length];
            for (int i = 0; i < values.Length; i++) nodes[i] = new Node<T>(values[i]);
            ArrangeCircular(nodes);
            return nodes;
        }

        public static void ArrangeCircular(Node<T>[] nodes) {
            for (int i = 0; i < nodes.Length; i++) {
                var prev = nodes[i];
                var next = nodes[(i + 1) % nodes.Length];
                prev?.Next = next;
                next?.Prev = prev;
            }
        }
    }

    public static class Nodes {
        public static Node<T>? Find<T>(this Node<T> startNode, Func<T, bool> test) {
            var currentNode = startNode;
            while (true) {
                var nextNode = currentNode.Next;
                if (test(currentNode.Value)) return currentNode;
                if (nextNode == null) return null;
                if (nextNode == startNode) return null;
                currentNode = nextNode;
            }
        }

        public static Node<T>? FindMin<T>(this Node<T> startNode, IComparer<T> comparer) {
            Node<T> minNode = startNode;
            var currentNode = startNode.Next;
            while (currentNode != null) {
                var minValue = minNode.Value;
                var candidate = currentNode.Value;
                var comparison = comparer.Compare(candidate, minValue);
                if (comparison > 0) minValue = candidate;
                currentNode = currentNode.Next;
            }
            return minNode;
        }

        public static void ClipOut<T>(this Node<T> node) {
            var n = node.Next;
            var p = node.Prev;
            Node<T>.SetRelationship(node, null);
            Node<T>.SetRelationship(null, node);
            Node<T>.SetRelationship(p, n);
        }
    }
}
