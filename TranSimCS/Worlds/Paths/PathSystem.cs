using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Spatial;

namespace TranSimCS.Worlds.Paths {
    public class PathSystem {
        public TSWorld Owner { get; private set; }

        internal AABBTree<SplinePath> _pathsSpatial;
        internal Dictionary<Guid, SplinePath> _paths;

        public ReadOnlyDictionary<Guid, SplinePath> Paths;

        public IEnumerable<SplinePath> Query(Func<AABB, bool>? filter = null) => _pathsSpatial.QueryFilter(filter);

        internal PathSystem(TSWorld owner) {
            Owner = owner;
            _paths = new();
            Paths = new(_paths);
            _pathsSpatial = new();
        }

        public void AddPath(SplinePath path) {
            if (path.CurrentState == PathState.Deleted)
                throw new ArgumentException("Paths added to PathSystem must not be already GC-collected");
            path.World = Owner;
            if(path.CurrentState is PathState.Active) {
                //Add a path to the spatial index
                _pathsSpatial.Add(path);
            }
            _paths.Add(path.Guid, path);
        }


        //Garbage collector
        internal void RunGC() {
            foreach (var path in _paths.Values.ToArray()) {
                //Analyze the node if it is eligible
                Debug.Assert(path.CurrentState != PathState.Deleted, "Detected a deleted path in the index");
                var isUnused = (path.CurrentState is PathState.Unused or PathState.Orphaned);
                var isUnassigned = path.Claimant == null;
                var isUnoccupied = path.Cars.Count == 0;
                if(isUnused && isUnassigned && isUnoccupied) {
                    path.Collect();
                    _pathsSpatial.Remove(path);
                    _paths.Remove(path.Guid);
                }
            }
        }
    }
}
