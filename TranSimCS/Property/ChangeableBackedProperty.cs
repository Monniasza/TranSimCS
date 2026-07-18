using System;
using TranSimCS.Property;

namespace TranSimCS.Property {
    public class ChangeableBackedProperty<T> : Property<T> {
        private IProperty<T>? _backingProp;
        public readonly IProperty<IProperty<T>?> Source;

        public ChangeableBackedProperty(string name, IProperty<IProperty<T>?> source)
            : base(default, name, null) {
            Source = source;
            _backingProp = source.Value;
            if (_backingProp != null)
                this.Value = _backingProp.Value;

            // Subscribe to changes in the source (the property of properties)
            Source.ValueChanged += (s, old, val) => {
                var newBacking = val;
                if (_backingProp != null) _backingProp.ValueChanged -= HandleBackingValueChanged;
                _backingProp = newBacking;
                if (_backingProp != null) _backingProp.ValueChanged += HandleBackingValueChanged;

                // Synchronize the current value if both backing properties are available
                if (_backingProp != null) 
                    this.Value = _backingProp.Value;
            };

            ValueChanged += (s, old, val) => _backingProp?.Value = val;

            if (_backingProp != null) _backingProp.ValueChanged += HandleBackingValueChanged;
            
        }

        private void HandleBackingValueChanged(object? sender, T _, T newValue) {
            this.Value = newValue;
        }
    }
}