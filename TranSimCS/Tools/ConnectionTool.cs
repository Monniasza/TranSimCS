using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.AccessControl;
using LanguageExt.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MLEM.Ui;
using MonoGame.Extended;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    /// <summary>
    /// A road strip tool.
    /// </summary>
    public class ConnectionTool: ITool {
        //Tool definition
        public readonly InGameMenu menu;

        //Tool state
        public HalfLane? SourceNode { get; private set; }
        public HalfLane? DestNode { get; private set; }
        public LaneStrip? LaneStrip { get; private set; }

        //Tool properties
        string ITool.Name => "Road Connection Editor";
        string ITool.Description => _description;

        public (object[], string)[] PromptKeys() {
            if (SourceNode == null) return [
                ([MouseButton.Left], "Select a lane to start editing connections")
            ]; else return [
                ([MouseButton.Right], "to cancel"),
                ([MouseButton.Left], "to add/remove/modify a connection")
            ];
            
        }

        //Cached state
        private NextAction nextAction;
        private Color actionColor;
        private string _description;
        private enum NextAction{
            Pick, Hover, Add, Reverse, Edit, Delete
        }
        private static (string description, Color color) GetForAction(NextAction nextAction) => nextAction switch {
            NextAction.Pick => ("Pick a lane end to start editing connections", Color.Transparent),
            NextAction.Hover => ("Editing connections. Hove over lane ends to add, remove and modify connections", Color.White),
            NextAction.Add => ("Editing connections. About to add a connection. [LAlt] for reverse", Color.Lime),
            NextAction.Reverse => ("Editing connections. About to reverse a connection.", Color.Cyan),
            NextAction.Edit => ("Editing connections. About to modify a connection. [LAlt] to reverse instead", Color.Yellow),
            NextAction.Delete => ("Editing connections. About to delete a connection. [LAlt] to reverse instead", Color.Red),
            _ => throw new ArgumentException("Invalid NextAction: " + nextAction)
        };

        public ConnectionTool(InGameMenu menu) {
            this.menu = menu;
        }

        void ITool.Update(GameTime gameTime) {
            if(SourceNode != null) DestNode = menu.MouseOver?.As<LaneEnd>().ToHalfLane();
            if (SourceNode == DestNode) DestNode = null;

            if (SourceNode == null || DestNode == null) {
                LaneStrip = null;
            }else{
                LaneStrip = menu.World.FindLaneStrip(SourceNode.LaneEnd, DestNode.LaneEnd);
            }

            if (SourceNode == null)  nextAction = NextAction.Pick;
            else if (DestNode == null) nextAction = NextAction.Hover;
            else if (LaneStrip == null) nextAction = NextAction.Add;
            else if (menu.Game.KeyboardState.IsKeyDown(Keys.LeftAlt)) nextAction = NextAction.Reverse;
            else if (!LaneStrip.Spec.EqualsExceptWidth(menu.configuration.LaneSpec)) nextAction = NextAction.Edit;
            else nextAction = NextAction.Delete;

            (_description, actionColor) = GetForAction(nextAction);
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Right) {
                SourceNode = DestNode = null;
                LaneStrip = null;
                return;
            }

            var pickedLaneEnd = menu.MouseOver?.As<LaneEnd>();
            if (button == MouseButton.Left) switch (nextAction) {
                case NextAction.Pick:
                    SourceNode = pickedLaneEnd?.ToHalfLane();
                    break;
                case NextAction.Edit:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Edit");
                    LaneStrip.Spec = menu.configuration.LaneSpec;
                    break;
                case NextAction.Delete:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Delete");
                    LaneStrip.Destroy();
                    menu.MouseOver = null;
                    break;
                case NextAction.Add:
                    Debug.Assert(LaneStrip == null, "Got Add with an already existing lane strip");
                    Debug.Assert(DestNode != null, "Invalid destination for AddNode");
                    Debug.Assert(SourceNode != null, "Invalid source for AddNode");
                    var sourceLane = SourceNode.LaneEnd;
                    var destLane = DestNode.LaneEnd;
                    if(menu.Game.KeyboardState.IsKeyDown(Keys.LeftAlt))
                        DataUtil.Swap(ref sourceLane, ref destLane);
                    menu.World.GetOrMakeLaneStrip(sourceLane, destLane, menu.configuration.RoadFinish, menu.configuration.LaneSpec);
                    break;
                case NextAction.Reverse:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Reverse");
                    LaneStrip.ReverseDirection();
                    menu.MouseOver = null;
                    break;
            }
        }

        public void Draw(GameTime gameTime) {
            if (SourceNode == null) return;

            var yoffset = 0.1f;

            var sourceLane = SourceNode;
            var sourceFrame = sourceLane.HalfNode.Cache.ReferenceFrame;
            var centerStartIndex = sourceLane.MiddlePosition;
            var startPos = sourceFrame.O + sourceFrame.X * centerStartIndex + sourceFrame.Y * yoffset;

            var renderBin = menu.renderHelper.GetOrCreateRenderBinForced(Assets.WhiteTransparent);
            var color = actionColor * 0.5f;
            float width = 0.5f;
            
            if(DestNode == null) {
                var endPos = menu.GroundSelection;
                var dist = Vector3.DistanceSquared(startPos, endPos);
                if(dist > 0.0001) renderBin.DrawLine(startPos, endPos, sourceFrame.Y, color, width);
            } else {
                var endLane = DestNode;
                var endFrame = endLane.HalfNode.Cache.ReferenceFrame;
                var centerEndIndex = endLane.MiddlePosition;
                var endPos = endFrame.O + endFrame.X * centerEndIndex + sourceFrame.Y * yoffset;

                var dist = Vector3.DistanceSquared(startPos, endPos);
                if (dist < 0.0001f) return;

                var spline = GeometryUtils.GenerateJoinSpline(startPos, endPos, sourceFrame.Z, endFrame.Z);
                var points = GeometryUtils.GenerateSplinePoints(spline, Settings.RoadAccuracy);
                for(int i = 1; i < points.Length; i++) {
                    var a = points[i];
                    var b = points[i - 1];
                    renderBin.DrawLine(a, b, sourceFrame.Y, color, width);
                }
            }
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showFinishes);
            action.Add(ToolAttribs.showLaneSpecs);
        }
    }
}
