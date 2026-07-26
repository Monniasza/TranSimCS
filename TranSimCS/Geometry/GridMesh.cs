using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;

namespace TranSimCS.Geometry {
    public struct GridCrossSectionalRecord<T> {
        public int MinIndex;
        public int MaxIndex;
        public T Value;

        public GridCrossSectionalRecord(int minIndex, int maxIndex, T value) {
            MinIndex = minIndex;
            MaxIndex = maxIndex;
            Value = value;
        }
    }
    public sealed class GridMesh<TVertex, TRecord> {
        public Immutable2DArray<TVertex> Vertices { get; private set; }
        public ImmutableArray<GridCrossSectionalRecord<TRecord>> CrossSections { get; private set; }

        public GridMesh(Immutable2DArray<TVertex> vertices, ImmutableArray<GridCrossSectionalRecord<TRecord>> crossSections) {
            Vertices = vertices;
            CrossSections = crossSections;
        }
    }
}
