using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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

        public static Selection CalculateSelection(SceneNode node, Ray ray) {
            SceneNode hitNode = null;
            float distance = float.PositiveInfinity;
            Vector3 coordinates = new Vector3(float.NaN);
            object? tag = null;

            Selection result = new();

            var isHit = node.Find(ray, out hitNode, out distance, out tag);
            if (isHit) coordinates = ray.Position + distance * ray.Direction;
            if (hitNode is SceneLeaf leaf) result.SelectedObj = leaf.obj; //SelectedObj remains null
            else if (hitNode != null) throw new ArgumentException("non-leaf hit node");
            result.SceneNode = hitNode;
            result.Tag = tag;
            result.Distance = distance;
            result.Coordinates = coordinates;

            return result;
        }
    }
}
