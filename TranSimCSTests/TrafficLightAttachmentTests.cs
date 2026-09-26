using System;
using System.Collections.Generic;
using System.Linq;
using TranSimCS.Roads.Section;
using TranSimCS.Save2;
using TranSimCS.TrafficLights;
using TranSimCS.Worlds;

namespace TranSimCSTests;

/// <summary>
/// Tests the bidirectional attachment behaviour between <see cref="RoadSection"/> and
/// <see cref="TrafficLightGroup"/>. These tests verify the observable behaviour that must remain
/// unchanged when the underlying implementation is refactored from <c>Property&lt;T&gt;</c> to
/// <c>Attachment&lt;T&gt;</c>/<c>AttachmentSet&lt;T&gt;</c>.
/// </summary>
public class TrafficLightAttachmentTests {
    /// <summary>
    /// Creates a <see cref="TSWorld"/> with the static state (materials, spline generators, etc.)
    /// initialized, so that objects can be added to stacks without crashing.
    /// </summary>
    private static TSWorld CreateWorld() {
        TestWorlds.RegisterSplineGenerators();
        TestWorlds.InitializeMaterials();
        TestWorlds.InitializeCarModels();
        JsonProcessor.Init();
        return new TSWorld();
    }

    /// <summary>
    /// Creates a <see cref="RoadSection"/> and registers it in the world's section stack so that
    /// <see cref="Obj.World"/> is set.
    /// </summary>
    private static RoadSection CreateSection(TSWorld world) {
        var section = new RoadSection();
        world.RoadSections.data.Add(section);
        return section;
    }

    /// <summary>
    /// Creates a <see cref="TrafficLightGroup"/>, attaches it to the given section, and registers
    /// it in the world's traffic-light stack. This mirrors the flow in
    /// <c>ModeTrafficLights.OnMousePress</c>.
    /// </summary>
    private static TrafficLightGroup CreateAndAttachTrafficLight(TSWorld world, RoadSection section) {
        var tlg = new TrafficLightGroup();
        section.TrafficLightGroup = tlg;
        world.TrafficLights.data.Add(tlg);
        return tlg;
    }

    // ── Attach ──

    [Fact]
    public void Attach_AddsSectionToControlledSections() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = new TrafficLightGroup();

        section.TrafficLightGroup = tlg;

        Assert.Contains(section, tlg.ControlledSections);
    }

    [Fact]
    public void Attach_SetsSectionTrafficLightGroup() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = new TrafficLightGroup();

        section.TrafficLightGroup = tlg;

        Assert.Same(tlg, section.TrafficLightGroup);
    }

    [Fact]
    public void Attach_ThenRegisterTlg_TlgIsInWorld() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = CreateAndAttachTrafficLight(world, section);

        Assert.Contains(tlg, world.TrafficLights.data);
    }

    // ── Detach ──

    [Fact]
    public void Detach_RemovesSectionFromControlledSections() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = CreateAndAttachTrafficLight(world, section);

        section.TrafficLightGroup = null;

        Assert.DoesNotContain(section, tlg.ControlledSections);
    }

    [Fact]
    public void Detach_ClearsSectionTrafficLightGroup() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = CreateAndAttachTrafficLight(world, section);

        section.TrafficLightGroup = null;

        Assert.Null(section.TrafficLightGroup);
    }

    [Fact]
    public void Detach_LastSection_DemolishesTlg() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = CreateAndAttachTrafficLight(world, section);

        section.TrafficLightGroup = null;

        Assert.DoesNotContain(tlg, world.TrafficLights.data);
    }

    // ── Switch ──

    [Fact]
    public void Switch_RemovesFromOldAddsToNew() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg1 = CreateAndAttachTrafficLight(world, section);
        var tlg2 = new TrafficLightGroup();

        section.TrafficLightGroup = tlg2;

        Assert.DoesNotContain(section, tlg1.ControlledSections);
        Assert.Contains(section, tlg2.ControlledSections);
        Assert.Same(tlg2, section.TrafficLightGroup);
    }

    [Fact]
    public void Switch_LastSectionLeavesOld_DemolishesOldTlg() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg1 = CreateAndAttachTrafficLight(world, section);
        var tlg2 = new TrafficLightGroup();

        section.TrafficLightGroup = tlg2;

        Assert.DoesNotContain(tlg1, world.TrafficLights.data);
    }

    // ── Multiple sections ──

    [Fact]
    public void MultipleSections_BothInControlledSections() {
        var world = CreateWorld();
        var section1 = CreateSection(world);
        var section2 = CreateSection(world);
        var tlg = new TrafficLightGroup();

        section1.TrafficLightGroup = tlg;
        section2.TrafficLightGroup = tlg;
        world.TrafficLights.data.Add(tlg);

        Assert.Contains(section1, tlg.ControlledSections);
        Assert.Contains(section2, tlg.ControlledSections);
        Assert.Equal(2, tlg.ControlledSections.Count);
    }

    [Fact]
    public void MultipleSections_RemovingOneKeepsTlg() {
        var world = CreateWorld();
        var section1 = CreateSection(world);
        var section2 = CreateSection(world);
        var tlg = new TrafficLightGroup();
        section1.TrafficLightGroup = tlg;
        section2.TrafficLightGroup = tlg;
        world.TrafficLights.data.Add(tlg);

        section1.TrafficLightGroup = null;

        Assert.Contains(tlg, world.TrafficLights.data);
        Assert.DoesNotContain(section1, tlg.ControlledSections);
        Assert.Contains(section2, tlg.ControlledSections);
    }

    [Fact]
    public void MultipleSections_RemovingLastDemolishesTlg() {
        var world = CreateWorld();
        var section1 = CreateSection(world);
        var section2 = CreateSection(world);
        var tlg = new TrafficLightGroup();
        section1.TrafficLightGroup = tlg;
        section2.TrafficLightGroup = tlg;
        world.TrafficLights.data.Add(tlg);

        section1.TrafficLightGroup = null;
        section2.TrafficLightGroup = null;

        Assert.DoesNotContain(tlg, world.TrafficLights.data);
    }

    // ── Remove TLG from world ──

    [Fact]
    public void RemoveTrafficLight_DetachesAllSections() {
        var world = CreateWorld();
        var section1 = CreateSection(world);
        var section2 = CreateSection(world);
        var tlg = new TrafficLightGroup();
        section1.TrafficLightGroup = tlg;
        section2.TrafficLightGroup = tlg;
        world.TrafficLights.data.Add(tlg);

        world.TrafficLights.data.Remove(tlg);

        Assert.Null(section1.TrafficLightGroup);
        Assert.Null(section2.TrafficLightGroup);
    }

    // ── DependencyChanged propagation ──

    [Fact]
    public void DependencyChanged_FiresOnAttach() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var fired = false;
        section.DependencyChanged += (target, dep, prop) => fired = true;

        section.TrafficLightGroup = new TrafficLightGroup();

        Assert.True(fired);
    }

    [Fact]
    public void DependencyChanged_FiresOnDetach() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = CreateAndAttachTrafficLight(world, section);
        var fired = false;
        section.DependencyChanged += (target, dep, prop) => fired = true;

        section.TrafficLightGroup = null;

        Assert.True(fired);
    }

    // ── Idempotency ──

    [Fact]
    public void Attach_SameTlgTwice_DoesNotDuplicate() {
        var world = CreateWorld();
        var section = CreateSection(world);
        var tlg = new TrafficLightGroup();

        section.TrafficLightGroup = tlg;
        section.TrafficLightGroup = tlg;

        Assert.Single(tlg.ControlledSections);
    }
}
