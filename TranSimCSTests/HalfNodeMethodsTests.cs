using System.Data;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCSTests;

public class HalfNodeMethodsTests {
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

    private static float[] Positions(HalfNode halfNode) =>
        Enumerable.Range(0, halfNode.LaneCount)
            .Select(i => halfNode.GetLaneByIndex(i).MiddlePosition)
            .ToArray();

    [Fact]
    public void InsertSpaceOnRight_ShiftsOnlyLanesToRight() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2),
            (6, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertSpaceOnRight(2);

        Assert.Equal(
            new[] { -3f, 0f, 5f, 8f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnLeft_ShiftsOnlyLanesToLeft() {
        var node = CreateNode(
            (-6, 2),
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(2);

        anchor.InsertSpaceOnLeft(2);

        Assert.Equal(
            new[] { -8f, -5f, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnRight_WithZeroWidth_DoesNothing() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);
        var before = Positions(node.FrontHalf);

        anchor.InsertSpaceOnRight(0);

        Assert.Equal(before, Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnLeft_WithZeroWidth_DoesNothing() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);
        var before = Positions(node.FrontHalf);

        anchor.InsertSpaceOnLeft(0);

        Assert.Equal(before, Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnRight_WithNegativeWidth_MovesLanesLeft() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertSpaceOnRight(-2);

        Assert.Equal(
            new[] { -3f, 0f, 1f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnLeft_WithNegativeWidth_MovesLanesRight() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertSpaceOnLeft(-2);

        Assert.Equal(
            new[] { -1f, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnRight_RightmostLaneDoesNothing() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(2);
        var before = Positions(node.FrontHalf);

        anchor.InsertSpaceOnRight(5);

        Assert.Equal(before, Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertSpaceOnLeft_LeftmostLaneDoesNothing() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(0);
        var before = Positions(node.FrontHalf);

        anchor.InsertSpaceOnLeft(5);

        Assert.Equal(before, Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnRight_InsertsImmediatelyAfterAnchor() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        var inserted = anchor.InsertOnRight(Spec(2));

        Assert.Same(anchor.HalfNode, inserted.HalfNode);
        Assert.Equal(2, inserted.Width);
        Assert.Equal(2, inserted.MiddlePosition);

        Assert.Equal(
            new[] { -3f, 0f, 2f, 5f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnLeft_InsertsImmediatelyBeforeAnchor() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        var inserted = anchor.InsertOnLeft(Spec(2));

        Assert.Same(anchor.HalfNode, inserted.HalfNode);
        Assert.Equal(2, inserted.Width);
        Assert.Equal(-2, inserted.MiddlePosition);

        Assert.Equal(
            new[] { -5f, -2f, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnRight_ShiftsAllLanesToRight() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2),
            (6, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertOnRight(Spec(4));

        Assert.Equal(
            new[] { -3f, 0f, 3f, 7f, 10f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnLeft_ShiftsAllLanesToLeft() {
        var node = CreateNode(
            (-6, 2),
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(2);

        anchor.InsertOnLeft(Spec(4));

        Assert.Equal(
            new[] { -10f, -7f, -3, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnRight_RightmostAnchorDoesNotShiftExistingLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(2);

        var inserted = anchor.InsertOnRight(Spec(2));

        Assert.Equal(4, node.FrontHalf.LaneCount);
        Assert.Equal(5, inserted.MiddlePosition);
        Assert.Equal(
            new[] { -3f, 0f, 3f, 5f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnLeft_LeftmostAnchorDoesNotShiftExistingLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(0);

        var inserted = anchor.InsertOnLeft(Spec(2));

        Assert.Equal(4, node.FrontHalf.LaneCount);
        Assert.Equal(-5, inserted.MiddlePosition);
        Assert.Equal(
            new[] { -5f, -3f, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnRight_ZeroWidthDoesNotMoveOtherLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertOnRight(Spec(0));

        Assert.Equal(
            new[] { -3f, 0f, 1f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnLeft_ZeroWidthDoesNotMoveOtherLanes() {
        var node = CreateNode(
            (-3, 2),
            (0, 2),
            (3, 2));

        var anchor = node.FrontHalf.GetLaneByIndex(1);

        anchor.InsertOnLeft(Spec(0));

        Assert.Equal(
            new[] { -3f, -1f, 0f, 3f },
            Positions(node.FrontHalf));
    }

    [Fact]
    public void InsertOnRight_NegativeWidthThrows() {
        var node = CreateNode((0, 2));
        var anchor = node.FrontHalf.GetLaneByIndex(0);

        Assert.Throws<ArgumentException>(
            () => anchor.InsertOnRight(Spec(-1)));
    }

    [Fact]
    public void InsertOnLeft_NegativeWidthThrows() {
        var node = CreateNode((0, 2));
        var anchor = node.FrontHalf.GetLaneByIndex(0);

        Assert.Throws<ArgumentException>(
            () => anchor.InsertOnLeft(Spec(-1)));
    }

    [Fact]
    public void InsertOnRight_NullAnchorThrows() {
        Assert.Throws<ArgumentNullException>(
            () => HalfNodeMethods.InsertOnRight(null!, Spec(2)));
    }

    [Fact]
    public void InsertOnLeft_NullAnchorThrows() {
        Assert.Throws<ArgumentNullException>(
            () => HalfNodeMethods.InsertOnLeft(null!, Spec(2)));
    }
}
