using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Property {
    public interface IProperty<T> {
        T Value { get; set; }
        public event EventHandler<PropertyChangedEventArgs2<T>> ValueChanged;
        public event EventHandler<PropertyChangedEventArgs2<T>> ValidateChanges;
    }
}
