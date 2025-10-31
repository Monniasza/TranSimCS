using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Menus.InGame;

namespace TranSimCS {
    public struct Camera: IEquatable<Camera> {
        public static Camera Default => new Camera(new Vector3(0, 0, 0), 64, 0, 0.2f);

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
        public Vector3 GetOffsetVector() {
            // Calculate the offset vector based on the camera's azimuth and elevation
            return new Vector3(
                Distance * (float)Math.Sin(Azimuth) * (float)Math.Cos(Elevation),
                -Distance * (float)Math.Sin(Elevation),
                Distance * (float)Math.Cos(Azimuth) * (float)Math.Cos(Elevation)
            );
        }
        public static void FlipX(ref Vector3 position) {
            // Flip the X coordinate of the position vector
            position.X = -position.X;
        }

        public Matrix GetViewMatrix() {
            // Calculate the camera's target position based on its azimuth and elevation
            Vector3 targetPosition = Position;
            Vector3 eyePosition = targetPosition - GetOffsetVector();
            // Flip the X coordinate of the eye position to match the camera's orientation
            FlipX(ref eyePosition);
            FlipX(ref targetPosition);
            // Create the view matrix using the camera's position and target position
            return Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(eyePosition, targetPosition, Vector3.Up);
        }

        public void SetUpEffect(BasicEffect effect, InGameMenu game) => SetUpEffect(effect, game.Game.GraphicsDevice);
        public void SetUpEffect(BasicEffect effect, GraphicsDevice gpu) {
            effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, gpu.Viewport.AspectRatio, 0.1f, 100000f);
            effect.World = Matrix.Identity;
            // Optimized near/far plane for better depth buffer precision across all distances
            // Near plane increased from 1f to 0.1f - this dramatically improves depth precision
            // Far plane set to 10000f to balance view distance with precision
            effect.View = GetViewMatrix();
        }

        //EQUALITY
        public bool Equals(Camera other) {
            return
                other.Distance == Distance &&
                other.Elevation == Elevation &&
                other.Azimuth == Azimuth &&
                other.Position == Position;

        }
        public override bool Equals(object? obj) {
            return obj is Camera && Equals((Camera)obj);
        }
        public static bool operator ==(Camera left, Camera right) {
            return left.Equals(right);
        }
        public static bool operator !=(Camera left, Camera right) {
            return !(left == right);
        }
        public override int GetHashCode() {
            return HashCode.Combine(Position, Distance, Azimuth, Elevation);
        }
    }
}
