using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Car;

namespace TranSimCS.Tools {
    public class CarLauncherTool(InGameMenu menu) : ITool {
        public string Name => "Add cars into the world";

        public string Description => "";

        public readonly CarLauncherTab settings = menu.ToolsPanel.GetPanel<CarLauncherTab>(ToolAttribs.showCarOptions);

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "to add a new car"),
            ([MouseButton.Right], "to delete the car")
        ];

        void ITool.OnClick(MouseButton button) {
            var selection = menu.MouseOver;
            switch (button) {
                case MouseButton.Left:
                    //Add a new car
                    var roadElement = selection?.AsRoadElement();
                    var node = roadElement?.GetLane();
                    var strip = roadElement?.GetLaneStrip();
                    if(node != null) {
                        //Pick a random road strip to place a car on
                        var strips = node.Connections.ToArray();
                        strip = strips.GetRandomElement();
                    }

                    var newCarPosition = PositionEulerAngles.Zero;
                    if (strip != null) {
                        var startingLane = strip.StartLane;
                        newCarPosition = startingLane.GetRoadNode().PositionProp.Value;
                        if (startingLane.end == NodeEnd.Backward) newCarPosition.Azimuth ^= (1 << 31);
                    } else {
                        if(selection != null && selection.Value.Distance < float.MaxValue) {
                            newCarPosition.Position = selection.Value.Coordinates;
                        } else {
                            newCarPosition.Position = menu.GroundSelection;
                        }
                        newCarPosition.Azimuth = GeometryUtils.RadiansToField(menu.renderManager.Camera.Azimuth);
                    }
                    Car car = new Car();
                    car.Randomize();
                    car.PositionProp.Value = newCarPosition; //selected position is NaN
                    car.Speed = settings.CarVelocity;
                    menu.World.Cars.data.Add(car);
                    break;
            }
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showCarOptions);
        }
    }
}
