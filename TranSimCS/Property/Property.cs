using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Worlds;

namespace TranSimCS.Property {
    public delegate void PropertyEventHandler<T>(IProperty<T> property, T oldValue, T newValue);

    public class Property<T> : IProperty<T> {
        private T _val;
        public string name;
        public readonly Obj? Parent;
        public event PropertyEventHandler<T> ValueChanged;
        public event PropertyEventHandler<T> ValidateChanges;
        public IEqualityComparer<T> comparer;

        public Property(T val, string name, Obj? parent = null, IEqualityComparer<T> equals = null) {
            _val = val;
            this.name = name;
            Parent = parent;
            comparer = equals ?? EqualityComparer<T>.Default;
        }

        public T Value {
            get => _val; set {
                var oldValue = _val;
                var newValue = value;

                if (comparer.Equals(_val, value)) return;
                ValidateChanges?.Invoke(this, oldValue, newValue);
                _val = newValue;
                var propEvent = new PropertyChangedEventArgs(name);
                ValueChanged?.Invoke(this, oldValue, newValue);
                Parent?.FirePropertyEvent(this, propEvent);
            }
        }
    }

    public class UnidirectionalDerivedProperty<TOrigin, TSelf> : Property<TSelf> {
        public UnidirectionalDerivedProperty(string name, Property<TOrigin> source, Func<TOrigin, TSelf> derive, IEqualityComparer<TSelf> equals = null)
            : base(derive(source.Value), name, source.Parent, equals) {
            source.ValueChanged += (s, old, val) => this.Value = derive(val);
        }
    }

    public class BidirectionalDerivedProperty<TOrigin, TSelf> : Property<TSelf> {
        public BidirectionalDerivedProperty(string name, Property<TOrigin> source, Func<TOrigin, TSelf> derive, Func<TSelf, TOrigin> reverse, IEqualityComparer<TSelf> equals = null)
            : base(derive(source.Value), name, source.Parent, equals) {
            source.ValueChanged += (s, old, val) => this.Value = derive(val);
            this.ValueChanged += (s, old, val) => source.Value = reverse(val);
        }
    }
}