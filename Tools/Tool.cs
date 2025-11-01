                    var laneEnd = laneStrip.GetHalf(half.Value);
                    var quad = RoadRenderer.GenerateLaneQuad(laneEnd, 0.6f, Color.Orange);
                    renderBin.DrawQuad(quad);
                } else {
                    var multiMesh = laneStrip.GetMesh();
                    foreach (var meshEntry in multiMesh.RenderBins) {
                        var mesh = meshEntry.Value;
                        foreach (var vertex in mesh.Vertices) {
                            var coloredVertex = vertex;
                            coloredVertex.Color = Color.Orange;
                            renderBin.AddVertex(coloredVertex);
                        }
                        foreach (var index in mesh.Indices) {
                            renderBin.AddIndex(index);
                        }
                    }
                }
            } else if (roadSelection.SelectedRoadNode != null) {
