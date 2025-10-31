using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds.Stack {
    public sealed class TrackerSpatial<TObj, TStack>: IStackTracker<TObj, TStack>
        where TObj: Obj, IObjMesh<TObj>
        where TStack: ObjectStack<TObj, TStack>{
        public readonly SceneTree sceneTree;
        public TrackerSpatial(TSWorld world) {
            sceneTree = new SceneTree();
        }

        public void ElementAdded(TObj element) {
            sceneTree.Add(element.Mesh.Leaf);
        }

        public void ElementModified(TObj element, PropertyChangedEventArgs args) {
            //unused, invalidation done by the scene tree and leaf nodes
        }

        public void ElementRemoved(TObj element) {
            sceneTree.Remove(element.Mesh.Leaf);
        }

        public void OnThisAdded(TStack stack) {
            Debug.Print("Added to the stack");
            stack.World.RootGraph.Add(sceneTree);
        }

        public void OnThisRemoved(TStack stack) {
            stack.World.RootGraph.Remove(sceneTree);
        }
    }
}
