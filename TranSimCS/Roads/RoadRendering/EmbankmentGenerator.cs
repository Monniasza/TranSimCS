using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Model;
using TranSimCS.Roads.Node;

namespace TranSimCS.Roads.RoadRendering {
    public struct RoadNodeEndRange {
        public RoadNodeEnd End;
        public Range<float> Range;
    }

    public struct EndCapNode {
        public RoadNodeEndRange End;
        public EndCapNodeSide Left;
        public EndCapNodeSide Right;
    }
    public struct EndCapNodeSide {
        public Vector3 Top, Middle, Bottom;
    }

    public static class EmbankmentGenerator {
        public static void GenerateEmbankments(RoadFinish finish, RoadNodeEndRange range, MultiMesh mesh) {
            //Construct the endcaps
            var endcaps = new List<EndCapNode>();


        }
    }
}
