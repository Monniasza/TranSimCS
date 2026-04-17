using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Roads {
    public static class RoadSurfaceGenerator {
        public enum RoadSurfaceMode {
            Flat,
            Coons,
            TPS
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

        public static RoadSurfaceMode Decide(ref RoadSurfaceInput input) {

            // ---- 1. HARD OVERRIDE ----
            if (input.ModeOverride != null)
                return input.ModeOverride.Value;

            // ---- 2. FLAT MODE ----
            if (input.Flatness < input.dDistance * 0.1f &&
                input.ConstraintDensity < 0.2f) {
                return RoadSurfaceMode.Flat;
            }

            // ---- 3. TPS REQUIRED ----
            if (input.HasCrossLaneCoupling ||
                input.ConstraintDensity > 0.6f) {
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
    }
}
