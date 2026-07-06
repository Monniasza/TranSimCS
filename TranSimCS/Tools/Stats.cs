using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Tools {
    public struct Stats {
        public int Vertices;
        public int Triangles;
        public int Materials;
        public int MeshModels;
        public int MeshInstances;
        public int MeshDraws;
        public int Lanes;
        public int Nodes;
        public int Segments;
        public int Strips;
        public int Sections;
        public int Buildings;
        public int Cars;
        public int Tags;

        public string Format() => $"""
            Vertices: {Vertices}
            Triangles: {Triangles}
            Materials: {Materials}
            Mesh instances: {MeshInstances}
            Mesh models: {MeshModels}
            Render calls: {MeshDraws}
            Lanes: {Lanes}
            Nodes: {Nodes}
            Segments: {Segments}
            Strips: {Strips}
            Sections: {Sections}
            Buildings: {Buildings}
            Cars: {Cars}
            Tags: {Tags}
        """;
    }
}
