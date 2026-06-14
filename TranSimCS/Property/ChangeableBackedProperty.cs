using System;
using TranSimCS.Property;

namespace TranSimCS.Property {
    public class ChangeableBackedProperty<T> : Property<T> {
        private IProperty<T> _backingProp;
        public IProperty<IProperty<T>> Source { get; set; }

        public ChangeableBackedProperty(string name, IProperty<IProperty<T>> source)
            : base(source.Value.Value, name, null) {
            Source = source;
            _backingProp = source.Value;

            // Subscribe to changes in the source (the property of properties)
            Source.ValueChanged += (s, e) => {
                var newBacking = e.NewValue;
                if (_backingProp != null) _backingProp.ValueChanged -= HandleBackingValueChanged;
                _backingProp = newBacking;
                if (_backingProp != null) _backingProp.ValueChanged += HandleBackingValueChanged;
                this.Value = _backingProp.Value; // Update local value
            };

            if (_backingProp != null) {
                _backingProp.ValueChanged += HandleBackingValueChanged;
            }
        }

        private void HandleBackingValueChanged(object? sender, PropertyChangedEventArgs2<T> e) {
            this.Value = e.NewValue;
        }
    }
}