using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds.Property {
    public delegate void Resolver<T>(T startA, T startB, out T result);

    public static class Resolvers {
        public static void PreferA<T>(T startA, T startB, out T result) {
            result = startA;
        }
        public static void PreferB<T>(T startA, T startB, out T result) {
            result = startB;
        }
    }

    /// <summary>
    /// Links two properties together
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkProps<T> {
        

        public LinkProps(Resolver<T> rA, Resolver<T> rB) {
            var eq = Equality.ReferenceEqualComparer<Property<T>>();
            resolverA = rA;
            resolverB = rB;
            PropA = new(null, "a", null, eq);
            PropB = new(null, "b", null, eq);
            PropA.ValueChanged += PropA_ValueChanged; 
            PropB.ValueChanged += PropB_ValueChanged;
        }

        private void PropB_ValueChanged(object? sender, PropertyChangedEventArgs2<Property<T>> e) {
            var oldProp = e.OldValue;
            var newProp = e.NewValue;
            oldProp?.ValueChanged -= HandleB;
            newProp?.ValueChanged += HandleB;
            if (A != null && B != null) {
                resolverB(A.Value, B.Value, out var newValue);
                A.Value = newValue;
                B.Value = newValue;
            }
        }

        private void PropA_ValueChanged(object? sender, PropertyChangedEventArgs2<Property<T>> e) {
            var oldProp = e.OldValue;
            var newProp = e.NewValue;
            oldProp?.ValueChanged -= HandleA;
            newProp?.ValueChanged += HandleA;
            if(A != null && B != null) {
                resolverA(A.Value, B.Value, out var newValue);
                A.Value = newValue;
                B.Value = newValue;
            }
        }

        private void HandleA(object? sender, PropertyChangedEventArgs2<T> e) {
            B?.Value = e.NewValue;
        }
        private void HandleB(object? sender, PropertyChangedEventArgs2<T> e) {
            A?.Value = e.NewValue;
        }

        public readonly Resolver<T> resolverA;
        public readonly Property<Property<T>> PropA;
        public Property<T> A{
            get => PropA.Value;
            set => PropA.Value = value;
        }

        public readonly Resolver<T> resolverB;
        public readonly Property<Property<T>> PropB;
        public Property<T> B {
            get => PropB.Value;
            set => PropB.Value = value;
        }
    }
}
