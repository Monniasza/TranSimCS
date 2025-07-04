using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds {
    public class PropertyChangedEventArgs<T> : EventArgs {
        public T OldValue { get; init; }
        public T NewValue { get; init; }
        public PropertyChangedEventArgs(T old, T newv) {
            OldValue = old;
            NewValue = newv;
        }
    }

    public class Property<T> {
        public T _val;
        public string name;
        public event EventHandler<PropertyChangedEventArgs<T>> ValueChanged;

        public Property(T val, string name) {
            _val = val;
            this.name = name;
        }

        public T Value { get => _val; set {
            var eventArgs = new PropertyChangedEventArgs<T>(_val, value);
            ValueChanged?.Invoke(this, eventArgs);
        }}
    }
}
