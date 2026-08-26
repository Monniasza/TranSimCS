using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public struct Selection {
        public static Selection Invalid => new Selection {
            SceneNode = null,
            SelectedObj = null,
            Tag = null,
            Coordinates = new(float.NaN),
            Distance = float.PositiveInfinity
        };

        public SceneNode? SceneNode;
        public Obj? SelectedObj;
        public object? Tag;
        public Vector3 Coordinates;
        public float Distance;

        public static Selection CalculateSelection(SceneRoot graph, Ray3 ray) {
            Selection result = graph.Find(ray);
            if (result.SceneNode == null) return Selection.Invalid;

            //Check if all parents are enabled
            var node = result.SceneNode;
            while(node != null) {
                if(!node.Active.Value) return Selection.Invalid;
                node = node.Parent;
            }

            return result;
        }


    }
}
