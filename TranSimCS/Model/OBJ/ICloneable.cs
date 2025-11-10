using System;

namespace TranSimCS.Model.OBJ {
    public interface ICloneable<T>: ICloneable {
        public new T Clone();

        object ICloneable.Clone() => Clone();
    }
}