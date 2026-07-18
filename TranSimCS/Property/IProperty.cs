using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Property {
    public interface IProperty<T> {
        T Value { get; set; }
        public event PropertyEventHandler<T> ValueChanged;
        public event PropertyEventHandler<T> ValidateChanges;
    }
}
