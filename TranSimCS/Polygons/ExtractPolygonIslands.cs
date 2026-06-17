using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using LanguageExt.ClassInstances.Pred;
using LanguageExt.Common;

namespace TranSimCS.Polygons {
    public struct PolygonIsland {
        public PathsD Paths;
        public int OuterLoopIndex;
        public PathD OuterLoop => Paths[OuterLoopIndex];
    }

    public class ExtractPolygonIslands {
        public static PolygonIsland[] ExtractIslands(PathsD input, FillRule fillRule, int precision = 6) {
            var tree = new PolyTreeD();
            var clipper = new ClipperD();

            clipper.AddSubject(input);
            clipper.Execute(
                ClipType.Union,
                fillRule,
                tree);

            var result = new List<PolygonIsland>();

            for(int i = 0; i < tree.Count; i++) {
                var child = tree[i];
                ExtractIsland(child, result);
            }

            return result.ToArray();
        }

        private static void ExtractIsland(PolyPathD outerNode, List<PolygonIsland> islands) {
            if (!outerNode.IsHole) {
                var paths = new PathsD();
                CollectIslandLoops(outerNode, paths, includeNestedOuters: false);
                islands.Add(new PolygonIsland {
                    Paths = paths,
                    OuterLoopIndex = 0
                });
            }

            for (int i = 0; i < outerNode.Count; i++) {
                var child = outerNode[i];
                ExtractIsland(child, islands);
            }
        }

        private static void CollectIslandLoops(PolyPathD node, PathsD paths, bool includeNestedOuters) {
            paths.Add(node.Polygon);
            for (int i = 0; i < node.Count; i++) {
                var child = node[i];
                if (!child.IsHole) {
                    if (includeNestedOuters) CollectIslandLoops(child, paths, true);
                    continue;
                }
                CollectIslandLoops(child, paths, false);
            }
        }
    }
}
