using System.Data;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCSTests;

public class HalfNodeLanesListTests {
    private static LaneSpec Spec(float width) {
        var spec = new LaneSpec();
        spec.Width = width;
        return spec;
    }

    private static RoadNode CreateNode(params (float center, float width)[] lanes) {
        var node = new RoadNode("test", default);

        foreach (var (center, width) in lanes)
            node.FrontHalf.AddLane(new LaneDefinition(center, Spec(width)));

        return node;
    }

    [Fact]
    public void Count_ReturnsLaneCount() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Indexer_ReturnsLanesInHalfNodeOrder() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();

        Assert.Equal(-3, list[0].MiddlePosition);
        Assert.Equal(0, list[1].MiddlePosition);
        Assert.Equal(3, list[2].MiddlePosition);
    }

    [Fact]
    public void IsReadOnly_ReturnsFalse() {
        var node = CreateNode((0, 2));

        var list = node.FrontHalf.GetLaneList();

        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void IndexerSetter_Throws() {
        var node = CreateNode((0, 2));
        var list = node.FrontHalf.GetLaneList();

        Assert.Throws<ReadOnlyException>(
            () => list[0] = node.FrontHalf.GetLaneByIndex(0));
    }

    [Fact]
    public void Contains_ReturnsTrueForLaneBelongingToHalf() {
        var node = CreateNode((0, 2));

        var lane = node.FrontHalf.GetLaneByIndex(0);
        var list = node.FrontHalf.GetLaneList();

        Assert.True(list.Contains(lane));
    }

    [Fact]
    public void Contains_ReturnsFalseForLaneFromAnotherHalf() {
        var node1 = CreateNode((0, 2));
        var node2 = CreateNode((0, 2));

        var lane = node2.FrontHalf.GetLaneByIndex(0);
        var list = node1.FrontHalf.GetLaneList();

        Assert.False(list.Contains(lane));
    }

    [Fact]
    public void Contains_NullReturnsFalse() {
        var node = CreateNode((0, 2));
        var list = node.FrontHalf.GetLaneList();

        Assert.False(list.Contains(null!));
    }

    [Fact]
    public void IndexOf_ReturnsLaneIndex() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var lane = node.FrontHalf.GetLaneByIndex(1);

        Assert.Equal(1, node.FrontHalf.GetLaneList().IndexOf(lane));
    }

    [Fact]
    public void IndexOf_ReturnsMinusOneForForeignLane() {
        var node1 = CreateNode((0, 2));
        var node2 = CreateNode((0, 2));

        var lane = node2.FrontHalf.GetLaneByIndex(0);

        Assert.Equal(-1, node1.FrontHalf.GetLaneList().IndexOf(lane));
    }

    [Fact]
    public void Enumeration_ReturnsAllLanesInOrder() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();

        Assert.Equal(
            new[] { -3f, 0f, 3f },
            list.Select(x => x.MiddlePosition));
    }

    [Fact]
    public void CopyTo_CopiesLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();
        var array = new HalfLane[3];

        list.CopyTo(array, 0);

        Assert.Equal(
            new[] { -3f, 0f, 3f },
            array.Select(x => x.MiddlePosition));
    }

    [Fact]
    public void CopyTo_RespectsDestinationIndex() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();
        var array = new HalfLane[5];

        list.CopyTo(array, 2);

        Assert.Null(array[0]);
        Assert.Null(array[1]);
        Assert.Equal(-3, array[2].MiddlePosition);
        Assert.Equal(0, array[3].MiddlePosition);
        Assert.Equal(3, array[4].MiddlePosition);
    }

    [Fact]
    public void Add_AddsLane() {
        var node = CreateNode((0, 2));
        var other = CreateNode((5, 2));

        var lane = other.FrontHalf.GetLaneByIndex(0);
        var list = node.FrontHalf.GetLaneList();

        list.Add(lane);

        Assert.Equal(2, list.Count);
        Assert.Contains(lane.Guid, node.LaneXRef.Keys);
    }

    [Fact]
    public void Remove_RemovesLane() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();
        var lane = list[1];

        Assert.True(list.Remove(lane));

        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(lane.Guid, node.LaneXRef.Keys);
    }

    [Fact]
    public void Remove_ForeignLaneReturnsFalse() {
        var node1 = CreateNode((0, 2));
        var node2 = CreateNode((0, 2));

        var lane = node2.FrontHalf.GetLaneByIndex(0);

        Assert.False(node1.FrontHalf.GetLaneList().Remove(lane));
    }

    [Fact]
    public void RemoveAt_RemovesLaneAtIndex() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();

        list.RemoveAt(1);

        Assert.Equal(
            new[] { -3f, 3f },
            list.Select(x => x.MiddlePosition));
    }

    [Fact]
    public void Clear_RemovesAllLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var list = node.FrontHalf.GetLaneList();

        list.Clear();

        Assert.Empty(list);
        Assert.Empty(node.Lanes);
    }

    [Fact]
    public void Insert_AtBeginning() {
        var node = CreateNode(
            (-3, 2),
            (3, 2));

        var other = CreateNode((0, 2));
        var item = other.FrontHalf.GetLaneByIndex(0);

        var list = node.FrontHalf.GetLaneList();

        list.Insert(0, item);

        Assert.Equal(3, list.Count);
        Assert.Equal(-5, list[0].MiddlePosition);
        Assert.Equal(-3, list[1].MiddlePosition);
        Assert.Equal(3, list[2].MiddlePosition);
        Assert.Equal(item.Guid, list[0].Guid);
    }

    [Fact]
    public void Insert_AtEnd() {
        var node = CreateNode(
            (-3, 2),
            (3, 2));

        var other = CreateNode((0, 2));
        var item = other.FrontHalf.GetLaneByIndex(0);

        var list = node.FrontHalf.GetLaneList();

        list.Insert(list.Count, item);

        Assert.Equal(3, list.Count);
        Assert.Equal(-3, list[0].MiddlePosition);
        Assert.Equal(3, list[1].MiddlePosition);
        Assert.Equal(5, list[2].MiddlePosition);
        Assert.Equal(item.Guid, list[2].Guid);
    }

    [Fact]
    public void Insert_InWideGap_UsesMiddleOfGap() {
        var node = CreateNode(
            (-5, 2),
            (5, 2));

        var other = CreateNode((0, 2));
        var item = other.FrontHalf.GetLaneByIndex(0);

        var list = node.FrontHalf.GetLaneList();

        list.Insert(1, item);

        Assert.Equal(0, list[1].MiddlePosition);
        Assert.Equal(item.Guid, list[1].Guid);
    }

    [Fact]
    public void Insert_InNarrowGap_PushesBothSidesApart() {
        var node = CreateNode(
            (-1, 2),
            (1, 2));

        var other = CreateNode((0, 4));
        var item = other.FrontHalf.GetLaneByIndex(0);

        var list = node.FrontHalf.GetLaneList();

        list.Insert(1, item);

        Assert.Equal(-3, list[0].MiddlePosition);
        Assert.Equal(0, list[1].MiddlePosition);
        Assert.Equal(3, list[2].MiddlePosition);
    }

    [Fact]
    public void Insert_PreservesLaneIdentity() {
        var node = CreateNode((-3, 2), (3, 2));
        var other = CreateNode((0, 2));

        var item = other.FrontHalf.GetLaneByIndex(0);
        var guid = item.Guid;

        node.FrontHalf.GetLaneList().Insert(1, item);

        Assert.Equal(guid, node.FrontHalf.GetLaneByIndex(1).Guid);
    }

    [Fact]
    public void Insert_NegativeIndexThrows() {
        var node = CreateNode((0, 2));
        var other = CreateNode((0, 2));
        var item = other.FrontHalf.GetLaneByIndex(0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => node.FrontHalf.GetLaneList().Insert(-1, item));
    }

    [Fact]
    public void Insert_IndexAboveCountThrows() {
        var node = CreateNode((0, 2));
        var other = CreateNode((0, 2));
        var item = other.FrontHalf.GetLaneByIndex(0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => node.FrontHalf.GetLaneList().Insert(2, item));
    }

    [Fact]
    public void Insert_NullItemThrows() {
        var node = CreateNode((0, 2));

        Assert.Throws<ArgumentNullException>(
            () => node.FrontHalf.GetLaneList().Insert(0, null!));
    }

    [Fact]
    public void Insert_DuplicateGuidThrows() {
        var node = CreateNode(
            (-3, 2),
            (3, 2));

        var item = node.FrontHalf.GetLaneByIndex(0);

        Assert.Throws<InvalidOperationException>(
            () => node.FrontHalf.GetLaneList().Insert(1, item));
    }
}
