using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public static class RoadStripMethods {
        public static bool IsSingleEnded(this RoadStrip strip) => strip.StartNode == strip.EndNode;
        public static IndexSpline GenerateDegenerateIndexStrips(this HalfNode node) {
            var tangent = node.Cache.ReferenceFrame.Z;
            var bounds = node.Bounds;
            var scale = 2.0f / 3;
            var scaledTangent = (bounds.Max - bounds.Min) * tangent * scale;
            IndexPoint left = new(bounds.Min, scaledTangent);
            IndexPoint right = new(bounds.Max, scaledTangent);
            return new(left, right);
        }
        public static RoadSection? BelongsToRoadSection(this RoadStrip strip) {
            if (strip.StartNode.ConnectedSection.Value == null) return null;
            if (strip.EndNode.ConnectedSection.Value != strip.StartNode.ConnectedSection.Value) return null;
            return strip.EndNode.ConnectedSection.Value;
        }

        /// <summary>
        /// Sets tangent lengths of a road strip from a vector. Supply NaN or +-Infinity to any of the attributes to not set its repsective weight.
        /// If <paramref name="preserveUninterestedLengths"/> is set to true, the method will preserve unassigned lengths. Otherwise it will preserve unassigned weights.
        /// </summary>
        /// <param name="lengths">new (start, end) length, or not finite to keep the start length unchanged</param>
        /// <param name="preserveUninterestedLengths">if set to true, lengths that are assigned non-finite values will be kept the exact same magnitude. Otherwise, weights will be kept instead</param>

        public static void SetTangentLengths(this RoadStrip strip, Vector2 lengths, bool preserveUninterestedLengths = true) {
            float? assignedStartLength = float.IsFinite(lengths.X) ? lengths.X : null;
            float? assignedEndLength = float.IsFinite(lengths.Y) ? lengths.Y : null;
            SetTangentLengths(strip, assignedStartLength, assignedEndLength, preserveUninterestedLengths);
        }
        /// <summary>
        /// Sets tangent lengths of a road strip. Supply null to any of the attributes to not set its repsective weight.
        /// If <paramref name="preserveUninterestedLengths"/> is set to true, the method will preserve unassigned lengths. Otherwise it will preserve unassigned weights.
        /// </summary>
        /// <param name="startLength">new start length, or null to keep the start length unchanged</param>
        /// <param name="endLength">new end length, or null to keep the end length unchanged</param>
        /// <param name="preserveUninterestedLengths">if set to true, lengths that are assigned null will be kept the exact same magnitude. Otherwise, weights will be kept instead</param>
        public static void SetTangentLengths(this RoadStrip strip, float? startLength, float? endLength, bool preserveUninterestedLengths = true) {
            if(startLength == null &&  endLength == null) return;
            if (preserveUninterestedLengths) {
                startLength ??= strip.IndexStrip.Start.Tangent.Length();
                endLength ??= strip.IndexStrip.End.Tangent.Length();
            }

            if(startLength != null) {
                //Set the start length
                float currentStartLength = strip.IndexStrip.Start.Tangent.Length();
                var oldStartWeight = strip.SplineWeightStart;
                var newStartWeight = strip.SplineWeightStart *= startLength.Value / currentStartLength;
                Debug.WriteLine(
                $"Start: weight={oldStartWeight} -> {newStartWeight}, " +
                $"tangent={currentStartLength}, " +
                $"target={startLength}");
            }
            if(endLength != null) {
                float currentEndLength = strip.IndexStrip.End.Tangent.Length();
                var oldEndWeight = strip.SplineWeightEnd;
                var newEndWeight = strip.SplineWeightEnd *= endLength.Value / currentEndLength;
                Debug.WriteLine(
                    $"End: weight={oldEndWeight} -> {newEndWeight}, " +
                    $"tangent={currentEndLength}, " +
                    $"target={endLength}");
            }
        }
    }
}
