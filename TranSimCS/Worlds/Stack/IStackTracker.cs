using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds.Stack {
    public interface IStackTracker<TObj, TStack>
        where TStack: ObjectStack<TObj, TStack>
        where TObj: Obj {
        /// <summary>
        /// Invoked when an element is added
        /// </summary>
        /// <param name="element">element that has been added</param>
        public void ElementAdded(TObj element);
        /// <summary>
        /// Invoked when an element is removed
        /// </summary>
        /// <param name="element">element that has been removed</param>
        public void ElementRemoved(TObj element);
        /// <summary>
        /// Invoked when an element is modified
        /// </summary>
        /// <param name="element">element that has been modified</param>
        public void ElementModified(TObj element, PropertyChangedEventArgs args);

        /// <summary>
        /// Invoked when this stack tracker is added to an object stack
        /// </summary>
        /// <param name="stack">parent stack</param>
        public void OnThisAdded(TStack stack);
        /// <summary>
        /// Invoked when this stack tracker is removed from an object stack
        /// </summary>
        /// <param name="stack">parent stack</param>
        public void OnThisRemoved(TStack stack);
    }
}
