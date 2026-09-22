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
    /// <summary>
    /// Owns every <see cref="SplinePath"/> in a world.
    /// <para>
    /// Paths are registered here by GUID, which is what allows a path to be found again after a world is
    /// loaded, and are indexed spatially so that traffic can find the paths near it. The system also runs
    /// the garbage collector that reclaims paths nothing refers to any more.
    /// </para>
    /// </summary>
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

        /// <summary>
        /// Registers a path with this system, so that it can be found by GUID and, when it is active, by
        /// spatial queries.
        /// </summary>
        /// <param name="path">The path to register.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="path"/> has already been collected.
        /// </exception>
        public void AddPath(SplinePath path) {
            if (path.CurrentState == PathState.Deleted)
                throw new ArgumentException("Paths added to PathSystem must not be already GC-collected");
            path.World = Owner;
            if(path.CurrentState is PathState.Active) {
                //Add a path to the spatial index
                _pathsSpatial.Add(path);
            }
            Owner._objects.Add(path.Guid, path);
            _paths.Add(path.Guid, path);
        }

        /// <summary>
        /// Looks up a path by its GUID.
        /// <para>
        /// This is how a path is resolved after loading a world: the saved GUID is looked up here, and
        /// the existing path is reused instead of a new one being created.
        /// </para>
        /// </summary>
        /// <param name="guid">The GUID of the path to find.</param>
        /// <returns>The path with the given GUID, or <see langword="null"/> if there is none.</returns>
        public SplinePath? FindPath(Guid guid) => _paths.GetValueOrDefault(guid);

        /// <summary>
        /// Returns the path with the given GUID, creating and registering it if it does not exist yet.
        /// <para>
        /// This is the entry point used when loading a world. Passing the GUID that was saved with the
        /// path makes the loader reuse the same path, so that references to it stay valid across a save
        /// and load cycle.
        /// </para>
        /// </summary>
        /// <param name="guid">The GUID of the path.</param>
        /// <param name="claimant">The claim to use if the path has to be created.</param>
        /// <param name="attachments">The attachment points to use if the path has to be created.</param>
        /// <returns>The existing or newly created path.</returns>
        public SplinePath GetOrMakePath(Guid guid, IPathClaim? claimant) {
            var existing = FindPath(guid);
            if (existing != null) return existing;
            var path = new SplinePath(claimant, guid);
            AddPath(path);
            return path;
        }

        /// <summary>
        /// Orphans every active path whose attachment points have died.
        /// <para>
        /// This is the safety net for a segment being deleted from under a car without the car being
        /// notified. It is safe to call at any time and is idempotent.
        /// </para>
        /// </summary>
        /// <returns>The number of paths that were orphaned by this call.</returns>
        public int OrphanDeadPaths() {
            int orphaned = 0;
            foreach (var path in _paths.Values.ToArray())
                if (path.OrphanIfAttachmentsDead()) orphaned++;
            return orphaned;
        }


        /// <summary>
        /// Reclaims every path that nothing refers to any more.
        /// <para>
        /// A path is reclaimed when it is unused or orphaned, has no claimant, and has no cars on it.
        /// Paths whose attachment points have died are orphaned first, so that a path is never reclaimed
        /// while a car is still on it.
        /// </para>
        /// </summary>
        internal void RunGC() {
            //First, orphan anything whose attachment points died. This must happen before collection so
            //that a path is never collected while a car is still on it.
            OrphanDeadPaths();

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
                    Owner._objects.Remove(path.Guid);
                }
            }
        }
    }
}
