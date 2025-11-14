using System;

namespace TranSimCS.Model.OBJ {
    public interface ICloneable<T>: ICloneable {
        /// <summary>
        /// Clones this object. The copy is independent of the original
        /// </summary>
        /// <returns>a copy of this object</returns>
        public new T Clone();

        object ICloneable.Clone() => Clone();
    }
}