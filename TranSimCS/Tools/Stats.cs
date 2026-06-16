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
        public int Lanes;
        public int Nodes;
        public int Segments;
        public int Strips;
        public int Sections;
        public int Buildings;
        public int Cars;

        public String Format() => $"""
            Vertices: {Vertices}
            Triangles: {Triangles}
            Materials: {Materials}
            Lanes: {Lanes}
            Nodes: {Nodes}
            Segments: {Segments}
            Sections: {Sections}
            Buildings: {Buildings}
            Cars: {Cars}
        """;
    }
}
