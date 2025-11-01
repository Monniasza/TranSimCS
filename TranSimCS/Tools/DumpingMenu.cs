using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using MonoGame.Extended;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class DumpingMenu: Panel {
        public NumberField diameter;
        public NumberField nodecount;
        public NumberField roadcount;
        public InGameMenu game;

        public DumpingMenu(InGameMenu game): base(MLEM.Ui.Anchor.CenterLeft, new(200, 0.5f)) {
            this.game = game;
            Paragraph diameterLabel = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Spread diameter", true);
            AddChild(diameterLabel);
            diameter = new NumberField(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), null, 400);
            AddChild(diameter);

            Paragraph countLabel = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Number of nodes", true);
            AddChild(countLabel);
            nodecount = new NumberField(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), null, 40);
            AddChild(nodecount);

            Paragraph roadsLabel = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Number of road segments", true);
            AddChild(roadsLabel);
            roadcount = new NumberField(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), null, 400);
            AddChild(roadcount);

            Button button = new Button(MLEM.Ui.Anchor.AutoLeft, new(1, 20), "GO! GO! GO!", "Fill the world with specified number of road nodes and segments within the specified diameter");
            button.OnPressed += GO;
            AddChild(button);
        }

        public void GO(Element e) {
            var world = game.World;
            float radius = diameter.Value / 2;
            int nodeCount = (int)nodecount.Value;
            int segmentCount = (int)roadcount.Value;

            Random random = new Random();
            RoadNode[] nodes = new RoadNode[nodeCount];
            for (int i = 0; i < nodes.Length; i++) {
                var spec = RandomLaneSpec(random);
                var pos = RandomPosition(random, radius);
                RoadNode node = new RoadNode(world, "", pos);
                Generator.GenerateLanes(1, node, spec);
                nodes[i] = node;
                world.Nodes.data.Add(node);
            }
            RoadStrip[] segments = new RoadStrip[segmentCount];
            NodeEnd[] endsA = random.GetItems([NodeEnd.Forward, NodeEnd.Backward], segmentCount);
            NodeEnd[] endsB = random.GetItems([NodeEnd.Forward, NodeEnd.Backward], segmentCount);
            for (int i = 0; i < segmentCount; ++i) {
                var nodeIdx = random.Next(nodeCount);
                var node = nodes[nodeIdx];
                var sideA = endsA[i];
                var sideB = endsB[i];
                var nodeEndA = node.GetEnd(sideA);
                var nodeEndB = node.GetEnd(sideB);
                var laneA = nodeEndA.GetLaneEnd(0);
                var laneB = nodeEndB.GetLaneEnd(0);
                var spec = RandomLaneSpec(random);
                var lanestrip = world.GetOrMakeLaneStrip(laneA, laneB);
                lanestrip.Spec = spec;
            }
        }

        public LaneSpec RandomLaneSpec(Random random) {
            LaneSpec spec = new LaneSpec();
            spec.Color = random.NextColor();
            spec.VehicleTypes = (VehicleTypes)random.NextIntFullRange();
            spec.Flags = (LaneFlags)random.NextIntFullRange();
            spec.SpeedLimit = random.NextSingle() * 256;
            spec.Width = random.NextSingle() * 4;
            return spec;
        }

        public ObjPos RandomPosition(Random random, float radius) {
            var azimuth = random.NextIntFullRange();
            var pitch = random.NextAngle();
            var roll = random.NextAngle();
            var pos = new Vector3(RandomCentered(random, radius), random.NextSingle() * 10, RandomCentered(random, radius));
            return new ObjPos(pos, azimuth, pitch, roll);

        }
        public float RandomCentered(Random random, float radius) => (random.NextSingle() * radius * 2) - radius;
    }
}
