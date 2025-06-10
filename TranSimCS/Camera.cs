using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS {
    public class Camera {
        public Vector3 Position { get; set; } // Camera position in the world
        public float Distance { get; set; } // Camera zoom level
        public float Azimuth { get; set; } // Camera rotation in radians
        public float Elevation { get; set; } // Camera elevation in radians
        public Camera() { }

        public Camera(Vector3 position, float distance, float azimuth, float elevation) {
            Position = position;
            Distance = distance;
            Azimuth = azimuth;
            Elevation = elevation;
        }
        public Matrix GetViewMatrix() {
            // Calculate the camera's target position based on its azimuth and elevation
            Vector3 targetPosition = new Vector3(
                Distance * (float)Math.Sin(Azimuth) * (float)Math.Cos(Elevation),
                -Distance * (float)Math.Sin(Elevation),
                Distance * (float)Math.Cos(Azimuth) * (float)Math.Cos(Elevation)
            );
            // Create the view matrix using the camera's position and target position
            return Matrix.CreateLookAt(Position - targetPosition, Position, Vector3.Up);
        }
    }
}
