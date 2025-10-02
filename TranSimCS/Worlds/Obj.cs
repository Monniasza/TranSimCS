using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using NLog;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;

namespace TranSimCS.Worlds {
    /// <summary>
    /// An object placed in the world. Worlds themselves are objects
    /// </summary>
    public abstract class Obj: INotifyPropertyChanged, IEquatable<Obj?> {
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

        //MESHING
        private Mesh? mesh;
        public Mesh GetMesh() {
            if (mesh == null) {
                mesh = new Mesh();
                GenerateMesh(mesh);
            }
            return mesh;
        }
        public void InvalidateMesh() {
            mesh = null;
            InvalidateMesh0();
        }
        protected virtual void InvalidateMesh0() { }

        //CHILDREN & PARENT
        private Obj? _parent;
        public Obj? Parent {
            get => _parent;
            private set {
                var newParent = value;
                var oldParent = _parent;
                if (oldParent == newParent) return;
                BeforeParentChanged?.Invoke(oldParent!, newParent!);
                oldParent?.BeforeChildRemoved?.Invoke(this);
                newParent?.BeforeChildAdded?.Invoke(this);
                _parent = newParent;
                oldParent?._children?.Remove(this);
                newParent?._children?.Add(this);
                AfterParentChanged?.Invoke(oldParent!, newParent!);
                oldParent?.AfterChildRemoved?.Invoke(this);
                newParent?.AfterChildAdded?.Invoke(this);
            }
        }
        internal ISet<Obj> _children = new HashSet<Obj>();
        public ISet<Obj> Children => new ReadOnlySet<Obj>(_children);
        public event Action<Obj>? BeforeChildAdded;
        public event Action<Obj>? BeforeChildRemoved;
        public event Action<Obj>? AfterChildAdded;
        public event Action<Obj>? AfterChildRemoved;
        public event Action<Obj, Obj>? BeforeParentChanged;
        public event Action<Obj, Obj>? AfterParentChanged;
        public event PropertyChangedEventHandler? PropertyChanged;


        //ABSTRACT METHODS
        protected abstract void GenerateMesh(Mesh mesh);

        public bool Equals(Obj? other) {
            return ReferenceEquals(this, other);
        }
    }

    //Component-interfaces for objects
    public interface IPosition: IDraggableObj {
        public Property<ObjPos> PositionProp { get; }
        public ObjPos PositionData { get => PositionProp.Value; set => PositionProp.Value = value; }

        void IDraggableObj.Drag(Vector3 vector, Vector3 dragFrom) {
            var posdata = PositionData;
            posdata.Position += vector;
            PositionData = posdata;
        }
    }
    public interface IDraggableObj {
        /// <summary>
        /// Moves the object by the specified amount
        /// </summary>
        /// <param name="vector">amount to move</param>
        /// <param name="dragFrom"></param>
        public void Drag(Vector3 vector, Vector3 dragFrom);
        public Plane DragPlane() => InGameMenu.groundPlane;
    }
}
