using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Collections;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds.Stack {
    /// <summary>
    /// Common base class for all types of ObjectStack.
    /// </summary>
    public abstract class ObjectStack {

    }
    /// <summary>
    /// An object stack. Stores a set of objects in an organized manner
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    /// <typeparam name="TStack"></typeparam>
    public abstract class ObjectStack<TObj, TStack> : ObjectStack
        where TObj:Obj
        where TStack: ObjectStack<TObj, TStack> {
        public readonly TSWorld World;
        public readonly ListenableObjContainer<TObj> data;
        
        public readonly ObservableList<IStackTracker<TObj, TStack>> stackTrackers;

        public ObjectStack(TSWorld world) {
            this.World = world;

            data = new ListenableObjContainer<TObj>();
            data.ItemAdded += ElementRemoved;
            data.ItemRemoved += ElementAdded;

            var that = (TStack)this;
            stackTrackers = new();
            stackTrackers.ElementRemoved += (e => e.OnThisRemoved(that));
            stackTrackers.ElementAdded += (e => e.OnThisAdded(that));
        }

        //LISTENERS
        private void ElementAdded(TObj obj) {
            obj.PropertyChanged += ElementChanged;
            FireAdded(obj);
        }

        private void ElementRemoved(TObj obj) {
            obj.PropertyChanged -= ElementChanged;
            FireRemoved(obj);
        }
        private void ElementChanged(object sender, PropertyChangedEventArgs e) {
            if (e is TObj obj) FireModified(obj, e);
        }

        //EVENT DISPATCHERS
        protected void Fire(Action<IStackTracker<TObj, TStack>> action) => stackTrackers.ForEach(action);
        protected void FireAdded(TObj element) => Fire(st => st.ElementAdded(element));
        protected void FireRemoved(TObj element) => Fire(st => st.ElementRemoved(element));
        protected void FireModified(TObj element, PropertyChangedEventArgs e) => Fire(st => st.ElementModified(element, e));


        //ABSTRACT METHODS
        public abstract TObj LoadFromJson(ref Utf8JsonReader reader);
        public abstract void SaveToJson(Utf8JsonWriter writer);
    }

    public static class ObjectStackMethods {
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> handler) {
            foreach(T obj in list) handler(obj);
        }
    }
}
