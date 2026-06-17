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
        public SceneNode SceneNode;
        public Obj SelectedObj;
        public object? Tag;
        public Vector3 Coordinates;
        public float Distance;
    }
}
