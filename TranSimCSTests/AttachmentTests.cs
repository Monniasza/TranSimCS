using System;
using System.Collections.Generic;
using System.Linq;
using TranSimCS.Worlds;

namespace TranSimCSTests;

/// <summary>
/// Unit tests for <see cref="Attachment{TElement}"/> and <see cref="AttachmentSet{TElement}"/>,
/// verifying bidirectional link maintenance, property-change notification, dependency forwarding,
/// and GUID-based serialization with deferred resolution.
/// </summary>
public class AttachmentTests {
    /// <summary>
    /// A minimal <see cref="Obj"/> subclass for testing attachments without the complexity of
    /// real game objects.
    /// </summary>
    private class TestObj : Obj {
        public TestObj(Guid? guid = null) : base(guid) { }
    }

    // ══════════════════════════════════════════
    // Attachment<TElement> tests
    // ══════════════════════════════════════════

    [Fact]
    public void Attachment_Value_SetAndGet_ReturnsTarget() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner);

        attachment.Value = target;

        Assert.Same(target, attachment.Value);
    }

    [Fact]
    public void Attachment_Value_SetNull_ClearsTarget() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) { Value = target };

        attachment.Value = null;

        Assert.Null(attachment.Value);
    }

    [Fact]
    public void Attachment_Value_SetSameReference_DoesNotFireEvents() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) { Value = target };
        var fired = false;
        attachment.ValueChanged += (old, neu) => fired = true;

        attachment.Value = target;

        Assert.False(fired);
    }

    [Fact]
    public void Attachment_OnTargetAttached_FiresWhenSettingValue() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? attached = null;
        var attachment = new Attachment<TestObj>(owner) {
            OnTargetAttached = t => attached = t
        };

        attachment.Value = target;

        Assert.Same(target, attached);
    }

    [Fact]
    public void Attachment_OnTargetDetached_FiresWhenClearingValue() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? detached = null;
        var attachment = new Attachment<TestObj>(owner) {
            OnTargetAttached = _ => { },
            OnTargetDetached = t => detached = t,
            Value = target
        };

        attachment.Value = null;

        Assert.Same(target, detached);
    }

    [Fact]
    public void Attachment_OnTargetDetached_FiresBeforeOnTargetAttached_WhenSwitching() {
        var owner = new TestObj();
        var target1 = new TestObj();
        var target2 = new TestObj();
        var order = new List<string>();
        var attachment = new Attachment<TestObj>(owner) {
            OnTargetDetached = t => order.Add($"detach:{t.Guid}"),
            OnTargetAttached = t => order.Add($"attach:{t.Guid}"),
            Value = target1
        };
        order.Clear();

        attachment.Value = target2;

        Assert.Equal(
            [$"detach:{target1.Guid}", $"attach:{target2.Guid}"],
            order);
    }

    [Fact]
    public void Attachment_ValueChanged_FiresWithOldAndNew() {
        var owner = new TestObj();
        var target1 = new TestObj();
        var target2 = new TestObj();
        var attachment = new Attachment<TestObj>(owner) { Value = target1 };
        TestObj? oldReceived = null;
        TestObj? newReceived = null;
        attachment.ValueChanged += (old, neu) => { oldReceived = old; newReceived = neu; };

        attachment.Value = target2;

        Assert.Same(target1, oldReceived);
        Assert.Same(target2, newReceived);
    }

    [Fact]
    public void Attachment_Name_FiresPropertyChangedOnOwner() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) { Name = "testProp" };
        var fired = false;
        owner.PropertyChanged += (s, e) => fired = e.PropertyName == "testProp";

        attachment.Value = target;

        Assert.True(fired);
    }

    [Fact]
    public void Attachment_NameNull_DoesNotFirePropertyChanged() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner);
        var fired = false;
        owner.PropertyChanged += (s, e) => fired = true;

        attachment.Value = target;

        Assert.False(fired);
    }

    [Fact]
    public void Attachment_ForwardDependencies_True_ForwardsTargetDependencyChanged() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) {
            ForwardDependencies = true,
            Value = target
        };
        var ownerFired = false;
        owner.DependencyChanged += (t, d, p) => ownerFired = true;

        target.FireDependencyEvent(target, target, "inner");

        Assert.True(ownerFired);
    }

    [Fact]
    public void Attachment_ForwardDependencies_False_DoesNotForward() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) {
            ForwardDependencies = false,
            Value = target
        };
        var ownerFired = false;
        owner.DependencyChanged += (t, d, p) => ownerFired = true;

        target.FireDependencyEvent(target, target, "inner");

        Assert.False(ownerFired);
    }

    [Fact]
    public void Attachment_ForwardDependencies_UnsubscribesOnDetach() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) {
            ForwardDependencies = true,
            Value = target
        };

        attachment.Value = null;
        var ownerFired = false;
        owner.DependencyChanged += (t, d, p) => ownerFired = true;

        target.FireDependencyEvent(target, target, "inner");

        Assert.False(ownerFired);
    }

    [Fact]
    public void Attachment_GetGuid_ReturnsTargetGuid() {
        var guid = Guid.NewGuid();
        var owner = new TestObj();
        var target = new TestObj(guid);
        var attachment = new Attachment<TestObj>(owner) { Value = target };

        Assert.Equal(guid, attachment.GetGuid());
    }

    [Fact]
    public void Attachment_GetGuid_ReturnsNullWhenEmpty() {
        var owner = new TestObj();
        var attachment = new Attachment<TestObj>(owner);

        Assert.Null(attachment.GetGuid());
    }

    [Fact]
    public void Attachment_SetGuid_ClearsTarget() {
        var owner = new TestObj();
        var target = new TestObj();
        var attachment = new Attachment<TestObj>(owner) { Value = target };

        attachment.SetGuid(Guid.NewGuid());

        Assert.Null(attachment.Value);
    }

    [Fact]
    public void Attachment_Resolve_FindsTargetByGuid() {
        var guid = Guid.NewGuid();
        var owner = new TestObj();
        var target = new TestObj(guid);
        var world = new TSWorld();
        world._objects[guid] = target;
        target.World = world;
        owner.World = world;
        var attachment = new Attachment<TestObj>(owner);
        attachment.SetGuid(guid);

        attachment.Resolve(world);

        Assert.Same(target, attachment.Value);
    }

    [Fact]
    public void Attachment_Resolve_NoMatch_KeepsPending() {
        var owner = new TestObj();
        var world = new TSWorld();
        owner.World = world;
        var attachment = new Attachment<TestObj>(owner);
        var guid = Guid.NewGuid();
        attachment.SetGuid(guid);

        attachment.Resolve(world);

        Assert.Null(attachment.Value);
        Assert.Equal(guid, attachment.GetGuid());
    }

    // ══════════════════════════════════════════
    // AttachmentSet<TElement> tests
    // ══════════════════════════════════════════

    [Fact]
    public void AttachmentSet_Add_AddsToSet() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);

        set.Add(target);

        Assert.Contains(target, set);
    }

    [Fact]
    public void AttachmentSet_Add_Duplicate_DoesNotAddAgain() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);

        set.Add(target);
        set.Add(target);

        Assert.Single(set);
    }

    [Fact]
    public void AttachmentSet_Remove_RemovesFromSet() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(target);

        var result = set.Remove(target);

        Assert.True(result);
        Assert.DoesNotContain(target, set);
    }

    [Fact]
    public void AttachmentSet_Remove_NotInSet_ReturnsFalse() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);

        var result = set.Remove(target);

        Assert.False(result);
    }

    [Fact]
    public void AttachmentSet_Count_ReturnsCorrectCount() {
        var owner = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);

        set.Add(new TestObj());
        set.Add(new TestObj());

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void AttachmentSet_Contains_ReturnsTrueForMember() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(target);

        Assert.True(set.Contains(target));
    }

    [Fact]
    public void AttachmentSet_OnTargetAttached_FiresOnAdd() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? attached = null;
        var set = new AttachmentSet<TestObj>(owner) {
            OnTargetAttached = t => attached = t
        };

        set.Add(target);

        Assert.Same(target, attached);
    }

    [Fact]
    public void AttachmentSet_OnTargetDetached_FiresOnRemove() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? detached = null;
        var set = new AttachmentSet<TestObj>(owner) {
            OnTargetAttached = _ => { },
            OnTargetDetached = t => detached = t
        };
        set.Add(target);

        set.Remove(target);

        Assert.Same(target, detached);
    }

    [Fact]
    public void AttachmentSet_ElementAdded_FiresOnAdd() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? added = null;
        var set = new AttachmentSet<TestObj>(owner);
        set.ElementAdded += t => added = t;

        set.Add(target);

        Assert.Same(target, added);
    }

    [Fact]
    public void AttachmentSet_ElementRemoved_FiresOnRemove() {
        var owner = new TestObj();
        var target = new TestObj();
        TestObj? removed = null;
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(target);
        set.ElementRemoved += t => removed = t;

        set.Remove(target);

        Assert.Same(target, removed);
    }

    [Fact]
    public void AttachmentSet_Clear_RemovesAll() {
        var owner = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(new TestObj());
        set.Add(new TestObj());

        set.Clear();

        Assert.Empty(set);
    }

    [Fact]
    public void AttachmentSet_Clear_FiresOnTargetDetachedForEach() {
        var owner = new TestObj();
        var t1 = new TestObj();
        var t2 = new TestObj();
        var detached = new List<TestObj>();
        var set = new AttachmentSet<TestObj>(owner) {
            OnTargetAttached = _ => { },
            OnTargetDetached = t => detached.Add(t)
        };
        set.Add(t1);
        set.Add(t2);

        set.Clear();

        Assert.Equal(2, detached.Count);
        Assert.Contains(t1, detached);
        Assert.Contains(t2, detached);
    }

    [Fact]
    public void AttachmentSet_GetGuids_ReturnsAllGuids() {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var owner = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(new TestObj(guid1));
        set.Add(new TestObj(guid2));

        var guids = set.GetGuids().ToArray();

        Assert.Contains(guid1, guids);
        Assert.Contains(guid2, guids);
    }

    [Fact]
    public void AttachmentSet_AddGuid_AddsPendingGuid() {
        var owner = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        var guid = Guid.NewGuid();

        set.AddGuid(guid);

        Assert.Contains(guid, set.GetGuids());
        Assert.Empty(set);
    }

    [Fact]
    public void AttachmentSet_Resolve_AddsAllPending() {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var owner = new TestObj();
        var target1 = new TestObj(guid1);
        var target2 = new TestObj(guid2);
        var world = new TSWorld();
        world._objects[guid1] = target1;
        world._objects[guid2] = target2;
        target1.World = world;
        target2.World = world;
        owner.World = world;
        var set = new AttachmentSet<TestObj>(owner);
        set.AddGuid(guid1);
        set.AddGuid(guid2);

        set.Resolve(world);

        Assert.Equal(2, set.Count);
        Assert.Contains(target1, set);
        Assert.Contains(target2, set);
    }

    [Fact]
    public void AttachmentSet_GetEnumerator_IteratesAll() {
        var owner = new TestObj();
        var t1 = new TestObj();
        var t2 = new TestObj();
        var set = new AttachmentSet<TestObj>(owner);
        set.Add(t1);
        set.Add(t2);

        var items = set.ToArray();

        Assert.Equal(2, items.Length);
        Assert.Contains(t1, items);
        Assert.Contains(t2, items);
    }

    [Fact]
    public void AttachmentSet_Name_FiresPropertyChangedOnOwner() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner) { Name = "testSet" };
        var fired = false;
        owner.PropertyChanged += (s, e) => fired = e.PropertyName == "testSet";

        set.Add(target);

        Assert.True(fired);
    }

    [Fact]
    public void AttachmentSet_ForwardDependencies_True_ForwardsTargetDependencyChanged() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner) {
            ForwardDependencies = true
        };
        set.Add(target);
        var ownerFired = false;
        owner.DependencyChanged += (t, d, p) => ownerFired = true;

        target.FireDependencyEvent(target, target, "inner");

        Assert.True(ownerFired);
    }

    [Fact]
    public void AttachmentSet_ForwardDependencies_UnsubscribesOnRemove() {
        var owner = new TestObj();
        var target = new TestObj();
        var set = new AttachmentSet<TestObj>(owner) {
            ForwardDependencies = true
        };
        set.Add(target);
        set.Remove(target);
        var ownerFired = false;
        owner.DependencyChanged += (t, d, p) => ownerFired = true;

        target.FireDependencyEvent(target, target, "inner");

        Assert.False(ownerFired);
    }
}
