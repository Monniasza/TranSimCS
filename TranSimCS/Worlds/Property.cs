using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds {
    public class PropertyChangedEventArgs2<T> : EventArgs {
        public T OldValue { get; init; }
        public T NewValue { get; init; }
        public PropertyChangedEventArgs2(T old, T newv) {
            OldValue = old;
            NewValue = newv;
        }
    }

    public class Property<T> {
        public T _val;
        public string name;
        public readonly Obj? Parent;
        public event EventHandler<PropertyChangedEventArgs2<T>> ValueChanged;

        public Property(T val, string name, Obj parent = null) {
            _val = val;
            this.name = name;
            Parent = parent;
        }

        public T Value { get => _val; set {
            if (EqualityComparer<T>.Default.Equals(_val, value)) return;
            var eventArgs = new PropertyChangedEventArgs2<T>(_val, value);
            _val = value;
            var propEvent = new PropertyChangedEventArgs(name);
            ValueChanged?.Invoke(this, eventArgs);
            Parent?.FirePropertyEvent(this, propEvent);
        }}
    }
}
