        public const float maxT = 0.7f;
        public override void Draw(GameTime time) {
            //Clear the screen to a solid color and clear the render helper
            renderHelper.Clear();

            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(roadTexture);

            // Draw the asphalt texture for the road
            foreach (var roadSegment in World.RoadSegments) {
                RoadRenderer.RenderRoadSegment(roadSegment, renderBin, 0.001f); // Render each road segment with a slight vertical offset
            }