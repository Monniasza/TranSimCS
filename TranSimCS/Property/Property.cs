using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Worlds;

namespace TranSimCS.Property {
    public class PropertyChangedEventArgs2<T> : EventArgs {
        public T OldValue { get; init; }
        public T NewValue { get; init; }
        public PropertyChangedEventArgs2(T old, T newv) {
            OldValue = old;
            NewValue = newv;
        }
    }

    public class Property<T> : IProperty<T> {
        public T _val;
        public string name;
        public readonly Obj? Parent;
        public event EventHandler<PropertyChangedEventArgs2<T>> ValueChanged;
        public event EventHandler<PropertyChangedEventArgs2<T>> ValidateChanges;
        public IEqualityComparer<T> comparer;

        public Property(T val, string name, Obj? parent = null, IEqualityComparer<T> equals = null) {
            _val = val;
            this.name = name;
            Parent = parent;
            comparer = equals ?? EqualityComparer<T>.Default;
        }

        public T Value {
            get => _val; set {
                if (comparer.Equals(_val, value)) return;
                var eventArgs = new PropertyChangedEventArgs2<T>(_val, value);
                ValidateChanges?.Invoke(this, eventArgs);
                _val = value;
                var propEvent = new PropertyChangedEventArgs(name);
                ValueChanged?.Invoke(this, eventArgs);
                Parent?.FirePropertyEvent(this, propEvent);
            }
        }
    }

    public class UnidirectionalDerivedProperty<TOrigin, TSelf> : Property<TSelf> {
        public UnidirectionalDerivedProperty(string name, Property<TOrigin> source, Func<TOrigin, TSelf> derive, IEqualityComparer<TSelf> equals = null)
            : base(derive(source.Value), name, source.Parent, equals) {
            source.ValueChanged += (s, e) => this.Value = derive(e.NewValue);
        }
    }

    public class BidirectionalDerivedProperty<TOrigin, TSelf> : Property<TSelf> {
        public BidirectionalDerivedProperty(string name, Property<TOrigin> source, Func<TOrigin, TSelf> derive, Func<TSelf, TOrigin> reverse, IEqualityComparer<TSelf> equals = null)
            : base(derive(source.Value), name, source.Parent, equals) {
            source.ValueChanged += (s, e) => this.Value = derive(e.NewValue);
            this.ValueChanged += (s, e) => source.Value = reverse(e.NewValue);
        }
    }
}