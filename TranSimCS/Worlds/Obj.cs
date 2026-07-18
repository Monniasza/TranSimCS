using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using NLog;
using TranSimCS.Model;

namespace TranSimCS.Worlds {
    public interface IGuid {
        public Guid Guid { get; }
    }

    /// <summary>
    /// An object placed in the world. Worlds themselves are objects
    /// </summary>
    public abstract class Obj: INotifyPropertyChanged, IEquatable<Obj?>, IGuid {
        private static Logger log = LogManager.GetCurrentClassLogger();

        //PROPERTIES
        private Guid? guid;
        public Guid Guid { get {
                if (guid == null) guid = Guid.NewGuid();
                return guid.Value;
            } set {
                if (guid != null) return;
                log.Trace($"GUID of node {value} set");
                guid = value;
            } 
        }

        public Obj() {}
        public void FirePropertyEvent(object sender, PropertyChangedEventArgs eventArgs){
            PropertyChanged?.Invoke(sender, eventArgs);
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Equals(Obj? other) {
            return ReferenceEquals(this, other);
        }
        public TSWorld World { get; internal set; }
    }    
}
