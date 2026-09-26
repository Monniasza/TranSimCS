using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TranSimCS.Worlds {
    /// <summary>
    /// A mutable, single-valued binding from an owner <see cref="Obj"/> to a target <see cref="Obj"/>.
    /// <para>
    /// Manages bidirectional link maintenance via <see cref="OnTargetAttached"/>/<see cref="OnTargetDetached"/>
    /// callbacks, property-change notification on the owner, and GUID-based serialization with deferred
    /// resolution for save/load.
    /// </para>
    /// </summary>
    /// <typeparam name="TElement">The target element type, must inherit from <see cref="Obj"/>.</typeparam>
    public class Attachment<TElement> where TElement : Obj {
        private readonly Obj _owner;
        private TElement? _target;
        private Guid? _pendingGuid;

        /// <summary>
        /// Called when a new target is attached, after the old target (if any) has been detached.
        /// </summary>
        public Action<TElement>? OnTargetAttached { get; init; }

        /// <summary>
        /// Called when the current target is detached, before the new target (if any) is attached.
        /// </summary>
        public Action<TElement>? OnTargetDetached { get; init; }

        /// <summary>
        /// Raised with (oldValue, newValue) after the value has changed and all callbacks have fired.
        /// </summary>
        public event Action<TElement?, TElement?>? ValueChanged;

        /// <summary>
        /// The property name used for <see cref="Obj.PropertyChanged"/> and
        /// <see cref="Obj.DependencyChanged"/> notifications on the owner. When null, no property-change
        /// event is fired (only <see cref="ValueChanged"/>).
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// When true, <see cref="Obj.DependencyChanged"/> events raised by the target are forwarded to
        /// the owner so that the owner's dependents (e.g. mesh generators) react to target changes.
        /// Defaults to false, matching the behaviour of <see cref="Property{T}"/>.
        /// </summary>
        public bool ForwardDependencies { get; init; }

        /// <summary>
        /// Creates a new attachment owned by <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">The object that owns this attachment.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is null.</exception>
        public Attachment(Obj owner) {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Gets or sets the target element. Setting to a different value detaches from the old target
        /// (firing <see cref="OnTargetDetached"/>), then attaches to the new target (firing
        /// <see cref="OnTargetAttached"/>), then fires property-change and <see cref="ValueChanged"/>.
        /// </summary>
        public TElement? Value {
            get => _target;
            set {
                if (ReferenceEquals(_target, value)) return;
                var old = _target;

                if (old != null) {
                    if (ForwardDependencies) old.DependencyChanged -= ForwardDependencyChanged;
                    OnTargetDetached?.Invoke(old);
                }

                _target = value;
                _pendingGuid = null;

                if (value != null) {
                    if (ForwardDependencies) value.DependencyChanged += ForwardDependencyChanged;
                    OnTargetAttached?.Invoke(value);
                }

                if (Name != null)
                    _owner.FirePropertyEvent(_owner, new(Name));
                ValueChanged?.Invoke(old, value);
            }
        }

        private void ForwardDependencyChanged(Obj target, Obj dependency, string? prop) {
            _owner.FireDependencyEvent(_owner, dependency, prop);
        }

        // ── GUID-based serialization ──

        /// <summary>
        /// Returns the GUID of the current target, or the pending GUID if the target has not yet been
        /// resolved. Returns null when the attachment is empty.
        /// </summary>
        public Guid? GetGuid() => _target?.Guid ?? _pendingGuid;

        /// <summary>
        /// Stores a GUID for deferred resolution. The current target (if any) is cleared.
        /// Call <see cref="Resolve()"/> or <see cref="Resolve(TSWorld)"/> later to look up the live object.
        /// </summary>
        public void SetGuid(Guid guid) {
            _pendingGuid = guid;
            _target = null;
        }

        /// <summary>
        /// Resolves a pending GUID against the owner's world.
        /// </summary>
        public void Resolve() => Resolve(_owner.World);

        /// <summary>
        /// Resolves a pending GUID against the given world's object index.
        /// </summary>
        public void Resolve(TSWorld world) {
            if (world == null) return;
            if (_target == null && _pendingGuid is Guid guid) {
                if (world._objects.TryGetValue(guid, out var obj) && obj is TElement el)
                    Value = el;
            }
        }
    }

    /// <summary>
    /// A mutable, multi-valued binding from an owner <see cref="Obj"/> to a collection of target
    /// <see cref="Obj"/>s. Provides the same bidirectional link maintenance, property-change notification,
    /// and GUID-based serialization as <see cref="Attachment{TElement}"/>, but for one-to-many relations.
    /// </summary>
    /// <typeparam name="TElement">The target element type, must inherit from <see cref="Obj"/>.</typeparam>
    public class AttachmentSet<TElement> : IReadOnlyCollection<TElement> where TElement : Obj {
        private readonly Obj _owner;
        private readonly HashSet<TElement> _targets = new();
        private readonly List<Guid> _pendingGuids = new();

        /// <summary>
        /// Called when a target is added to the set.
        /// </summary>
        public Action<TElement>? OnTargetAttached { get; init; }

        /// <summary>
        /// Called when a target is removed from the set.
        /// </summary>
        public Action<TElement>? OnTargetDetached { get; init; }

        /// <summary>
        /// Raised after a target is added.
        /// </summary>
        public event Action<TElement>? ElementAdded;

        /// <summary>
        /// Raised after a target is removed.
        /// </summary>
        public event Action<TElement>? ElementRemoved;

        /// <summary>
        /// The property name used for <see cref="Obj.PropertyChanged"/> notifications on the owner.
        /// When null, no property-change event is fired.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// When true, dependency changes on any target are forwarded to the owner.
        /// </summary>
        public bool ForwardDependencies { get; init; }

        /// <summary>
        /// Creates a new attachment set owned by <paramref name="owner"/>.
        /// </summary>
        public AttachmentSet(Obj owner) {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Adds a target to the set. If the target was already present, this is a no-op.
        /// </summary>
        public void Add(TElement target) {
            if (_targets.Add(target)) {
                if (ForwardDependencies) target.DependencyChanged += ForwardDependencyChanged;
                OnTargetAttached?.Invoke(target);
                ElementAdded?.Invoke(target);
                if (Name != null)
                    _owner.FirePropertyEvent(_owner, new(Name));
            }
        }

        /// <summary>
        /// Removes a target from the set. Returns true if the target was present and removed.
        /// </summary>
        public bool Remove(TElement target) {
            if (!_targets.Remove(target)) return false;
            if (ForwardDependencies) target.DependencyChanged -= ForwardDependencyChanged;
            OnTargetDetached?.Invoke(target);
            ElementRemoved?.Invoke(target);
            if (Name != null)
                _owner.FirePropertyEvent(_owner, new(Name));
            return true;
        }

        /// <summary>
        /// Returns true if the set contains the given target.
        /// </summary>
        public bool Contains(TElement target) => _targets.Contains(target);

        /// <inheritdoc/>
        public int Count => _targets.Count;

        /// <inheritdoc/>
        public IEnumerator<TElement> GetEnumerator() => _targets.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Removes all targets from the set.
        /// </summary>
        public void Clear() {
            foreach (var target in _targets) {
                if (ForwardDependencies) target.DependencyChanged -= ForwardDependencyChanged;
                OnTargetDetached?.Invoke(target);
                ElementRemoved?.Invoke(target);
            }
            _targets.Clear();
            if (Name != null)
                _owner.FirePropertyEvent(_owner, new(Name));
        }

        // ── GUID-based serialization ──

        /// <summary>
        /// Returns the GUIDs of all current targets plus any pending GUIDs not yet resolved.
        /// </summary>
        public IEnumerable<Guid> GetGuids() => _targets.Select(t => t.Guid).Concat(_pendingGuids);

        /// <summary>
        /// Stores a GUID for deferred resolution. Call <see cref="Resolve()"/> later to look up the live object.
        /// </summary>
        public void AddGuid(Guid guid) => _pendingGuids.Add(guid);

        /// <summary>
        /// Resolves all pending GUIDs against the owner's world.
        /// </summary>
        public void Resolve() => Resolve(_owner.World);

        /// <summary>
        /// Resolves all pending GUIDs against the given world's object index.
        /// </summary>
        public void Resolve(TSWorld world) {
            if (world == null) return;
            foreach (var guid in _pendingGuids.ToArray()) {
                if (world._objects.TryGetValue(guid, out var obj) && obj is TElement el)
                    Add(el);
            }
            _pendingGuids.Clear();
        }

        private void ForwardDependencyChanged(Obj target, Obj dependency, string? prop) {
            _owner.FireDependencyEvent(_owner, dependency, prop);
        }
    }
}
