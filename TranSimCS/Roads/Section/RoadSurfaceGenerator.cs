using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Roads.Section {
    public static partial class RoadSurfaceGenerator {
        public enum RoadSurfaceMode {
            Flat,
            Coons,
            TPS,
            DegeneratePolygon
        }

        public struct Constraint {
            RoadNodeEnd Start;
            Range<float> StartRange;
            RoadNodeEnd End;
            Range<float> EndRange;
            float Weight;

            public Constraint(RoadNodeEnd start, RoadNodeEnd end, float weight = 1, Range<float>? startRange = null, Range<float>? endRange = null) {
                this.Start = start;
                this.End = end;
                this.StartRange = startRange ?? start.Range;
                this.EndRange = endRange ?? end.Range;
                this.Weight = weight;
            }

            public void Validate() {
                ArgumentNullException.ThrowIfNull(Start, nameof(Start));
                ArgumentNullException.ThrowIfNull(End, nameof(End));
            }
        }

        public struct RoadSurfaceInput {

            // Topology
            public RoadNodeEnd[] Nodes;
            public LaneStrip[] Strips;

            // Geometry hints (optional constraints)
            public Constraint[] Constraints;

            // Control parameters
            public Vector3 Normal;
            public float dAngle;
            public float dDistance;
            public float dSegments;

            // External override
            public RoadSurfaceMode? ModeOverride;

            public void Validate() {
                Verify.ThrowIfNullOrContainsNull(Nodes, nameof(Nodes));
                Verify.ThrowIfNullOrContainsNull(Strips, nameof(Strips));
                ArgumentNullException.ThrowIfNull(Constraints, nameof(Constraints));
            }
        }

        public static RoadSurfaceInput FromStrip(RoadStrip strip,
                                                 float dAngle,
                                                 float dDistance,
                                                 float dSegments) {

            return new RoadSurfaceInput {
                Nodes = [strip.StartNode, strip.EndNode],
                Strips = strip.Lanes.ToArray(),
                Constraints = [new Constraint(strip.StartNode, strip.EndNode, 1)],
                Normal = ComputeNormal(strip),
                dAngle = dAngle,
                dDistance = dDistance,
                dSegments = dSegments,

                ModeOverride = RoadSurfaceMode.Coons
            };
        }

        private static Vector3 ComputeNormal(RoadStrip strip) {
            var startFrame = strip.StartNode.CalcReferenceFrame();
            var endFrame = strip.EndNode.CalcReferenceFrame();
            return (startFrame.Y + endFrame.Y).Normalized();
        }

        public static RoadSurfaceInput FromSection(RoadSection section,
                                           float dAngle,
                                           float dDistance,
                                           float dSegments) {

            var strips = section.FindStrips();

            return new RoadSurfaceInput {
                Nodes = section.Nodes.ToArray(),
                Strips = strips,
                Constraints = ExtractSectionConstraints(section),
                Normal = section.Normal,

                dAngle = dAngle,
                dDistance = dDistance,
                dSegments = dSegments,
            };
        }

        private static Constraint[] ExtractSectionConstraints(RoadSection section) {
            return [new Constraint(section.MainSlopeNodes.Value.Start, section.MainSlopeNodes.Value.End)];
        }

        public static RoadSurfaceMode Decide(ref RoadSurfaceInput input) {
            var isFlat = false;
            var isCrossCoupled = false;


            // ---- 1. HARD OVERRIDE ----
            if (input.ModeOverride != null)
                return input.ModeOverride.Value;

            // ---- 2. FLAT MODE ----
            if (isFlat) {
                return RoadSurfaceMode.Flat;
            }

            // ---- 3. TPS REQUIRED ----
            if (isCrossCoupled) {
                return RoadSurfaceMode.TPS;
            }

            // ---- 4. DEFAULT ----
            return RoadSurfaceMode.Coons;
        }

        public static Mesh Generate(RoadSurfaceInput input) {

            var mode = Decide(ref input);

            return mode switch {
                RoadSurfaceMode.Flat => GenerateFlat(input),
                RoadSurfaceMode.Coons => GenerateCoons(input),
                RoadSurfaceMode.TPS => GenerateTPS(input),
                _ => throw new Exception("Unknown mode: " + mode)
            };
        }

        private static Mesh GenerateTPS(RoadSurfaceInput input) {
            throw new NotImplementedException();
        }

        private static Mesh GenerateCoons(RoadSurfaceInput input) {
            throw new NotImplementedException();
        }

        private static Mesh GenerateFlat(RoadSurfaceInput input) {
            throw new NotImplementedException();
        }
    }
}
