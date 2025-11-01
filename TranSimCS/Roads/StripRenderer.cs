﻿using System;
using Microsoft.Xna.Framework;
using TranSimCS.Model;

namespace TranSimCS.Roads {
    public static class StripRenderer {

        public static void GenerateLaneStripMesh(LaneStrip laneStrip, MultiMesh renderer, float voffset = 0.01f) {
            var tag = laneStrip.Tag;

            var roadBin = renderer.GetOrCreateRenderBinForced(Assets.Road);
            RoadRenderer.GenerateLaneRangeMesh(tag, roadBin, laneStrip.Spec.Color, voffset, laneStrip); // Generate the lane tag mesh

            //Generate arrows
            float aoffset = 0.15f;
            var t = 0.5f;
            var splines = RoadRenderer.GenerateSplines(tag, voffset); // Generate the splines for the left and right lanes
            var avgspline = (splines.Item2 + splines.Item1) / 2;
            var lpoint = splines.Item1[t];
            var rpoint = splines.Item2[t];
            var midpoint = (lpoint + rpoint) / 2;
            var tangent = avgspline.Tangential(t);
            tangent.Normalize();
            var fakebinormal = (rpoint - lpoint);
            var width = Vector3.Cross(tangent, fakebinormal).Length();
            var normalfakebirnormal = Vector3.Normalize(fakebinormal);
            var nrm = Vector3.Cross(tangent, normalfakebirnormal);
            nrm.Normalize();

            var arrowWidth = width / 2;
            var displacement = tangent * width / 2;
            midpoint += nrm * aoffset;

            var arrowBin = renderer.GetOrCreateRenderBinForced(Assets.Arrow);
            arrowBin.DrawLine(midpoint - displacement, midpoint + displacement, nrm, Color.White, arrowWidth);

            //Generate side-lines
        }
    }
}