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

    public delegate void DependencyHandler(Obj targetObject, Obj dependencyObject, string? propertyName);

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

        public Obj() {
            PropertyChanged += HandlePropertyChanged;
        }
        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e) => FireDependencyEvent(this, this, e.PropertyName);

        public void FirePropertyEvent(object sender, PropertyChangedEventArgs eventArgs){
            PropertyChanged?.Invoke(sender, eventArgs);
        }
        public void FireDependencyEvent(Obj targetObject, Obj dependencyObject, string? propertyName) {
            DependencyChanged?.Invoke(targetObject, dependencyObject, propertyName);
        }
        /// <summary>
        /// Invoked when any property of the object changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Invoked when any dependency changes (including itself)
        /// </summary>
        public event DependencyHandler? DependencyChanged;

        public bool Equals(Obj? other) {
            return ReferenceEquals(this, other);
        }
        public TSWorld World { get; internal set; }
    }    
}
